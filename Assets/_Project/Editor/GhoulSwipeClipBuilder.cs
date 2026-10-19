using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GuardVsMannequin.Editor
{
    /// <summary>
    /// Ghoul의 Hunch_Reach 첫 자세를 바탕으로 오른팔 할퀴기 공격 클립을 코드로 만든다. (원본 Attack_Swipe는 쓰지 않음)
    /// 다리·골반은 굽힌 자세 그대로 두고, 오른팔을 치켜들었다가 → 앞으로 내려 할퀴고 → 왼쪽 아래로 휘두른 뒤 원래 자세로 돌아온다.
    /// 몸통은 휘두르는 방향으로 조금 비틀고 숙인다. 시작과 끝은 Hunch_Reach 첫 자세와 같아 앞뒤 동작과 매끄럽게 이어진다.
    /// 방향은 모델 뼈 위치로 구한 캐릭터 기준(오른쪽 = 왼어깨→오른어깨, 위, 앞)이라 모델이 어느 축을 보든 같다.
    /// </summary>
    public static class GhoulSwipeClipBuilder
    {
        private const float SampleRate = 30f;

        // 뼈 이름 (Ghoul.fbx)
        private const string SpineBone = "Spine";
        private const string HeadBone = "Head";
        private const string LeftShoulderBone = "LeftShoulder";
        private const string RightShoulderBone = "RightShoulder";
        private const string UpperArmBone = "RightUpperArm";
        private const string LowerArmBone = "RightLowerArm";
        private const string HandBone = "RightHand";

        // 몸통을 비튼 만큼 머리는 반대로 돌려 시선을 앞에 둔다.
        private const float HeadCounterTwist = 0.6f;
        // 휘두르기가 끝나고 굽힌 자세로 돌아가기 시작하는 지점(클립 비율). 돌아오기는 호출자가 준 exitFraction에서 끝난다.
        private const float RecoverStart = 0.62f;

        /// <summary>
        /// 휘두르기 핵심 자세. 방향은 캐릭터 기준 (x = 오른쪽, y = 위, z = 앞) 성분.
        /// </summary>
        private readonly struct SwipeKey
        {
            public readonly float time;
            public readonly Vector3 upperArm;
            public readonly Vector3 lowerArm;
            public readonly float twist;
            public readonly float lean;

            /// <param name="time">클립 비율.</param>
            /// <param name="upperArm">위팔(어깨→팔꿈치)이 향할 방향.</param>
            /// <param name="lowerArm">아래팔(팔꿈치→손)이 향할 방향.</param>
            /// <param name="twist">몸통 비틀기(도, +면 오른어깨가 뒤로).</param>
            /// <param name="lean">몸통 숙이기(도, +면 앞으로).</param>
            public SwipeKey(float time, Vector3 upperArm, Vector3 lowerArm, float twist, float lean)
            {
                this.time = time;
                this.upperArm = upperArm;
                this.lowerArm = lowerArm;
                this.twist = twist;
                this.lean = lean;
            }
        }

        /// <summary>
        /// 찾은 뼈와 캐릭터 기준 방향.
        /// </summary>
        private sealed class Rig
        {
            public Transform spine, head, upperArm, lowerArm, hand;
            public Vector3 right, up, forward;

            /// <summary>캐릭터 기준 성분(x 오른쪽, y 위, z 앞)을 모델 공간 방향으로 바꾼다.</summary>
            public Vector3 ToModel(Vector3 local)
            {
                return (right * local.x + up * local.y + forward * local.z).normalized;
            }
        }

        /// <summary>
        /// 핵심 자세 목록을 만든다. 맞는 순간은 hitFraction(서버 판정 시점과 같은 비율)에 둔다.
        /// </summary>
        private static SwipeKey[] CreateKeys(float hitFraction)
        {
            return new[]
            {
                // 치켜들기: 위팔을 바깥 위로, 손톱은 머리 위
                new SwipeKey(hitFraction - 0.17f, new Vector3(0.6f, 0.8f, -0.2f), new Vector3(0.2f, 0.9f, 0.35f), 20f, -5f),
                // 맞는 순간: 팔을 앞으로 뻗어 아래로 할퀴기
                new SwipeKey(hitFraction, new Vector3(0.1f, -0.25f, 1f), new Vector3(-0.3f, -0.5f, 0.8f), -15f, 10f),
                // 휘두른 뒤: 몸 앞을 가로질러 왼쪽 아래
                new SwipeKey(hitFraction + 0.13f, new Vector3(-0.6f, -0.7f, 0.4f), new Vector3(-0.8f, -0.6f, 0.15f), -25f, 8f),
            };
        }

        /// <summary>
        /// 할퀴기 클립을 만들어 저장한다. (이미 있으면 내용만 바꿔 GUID 유지)
        /// </summary>
        /// <param name="modelPath">Ghoul 원본 모델 경로.</param>
        /// <param name="savePath">저장할 .anim 경로.</param>
        /// <param name="hunch">바탕 자세 클립(Hunch_Reach, 첫 프레임 사용).</param>
        /// <param name="length">클립 길이(초).</param>
        /// <param name="hitFraction">손이 맞는 지점(클립 비율).</param>
        /// <param name="exitFraction">굽힌 자세로 완전히 돌아오는 지점(클립 비율, 애니메이터 복귀 시점).</param>
        /// <returns>만든 클립. 모델이나 뼈를 찾지 못하면 null.</returns>
        public static AnimationClip Create(string modelPath, string savePath, AnimationClip hunch, float length, float hitFraction, float exitFraction)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null)
                return null;

            GameObject instance = Object.Instantiate(model);
            instance.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                Transform root = instance.transform;
                Transform[] bones = root.GetComponentsInChildren<Transform>(true).Where(bone => bone != root).ToArray();

                hunch.SampleAnimation(instance, 0f);
                Pose[] basePose = bones.Select(bone => new Pose(bone.localPosition, bone.localRotation)).ToArray();
                if (!TryFindRig(bones, out Rig rig))
                    return null;

                SwipeKey[] keys = CreateKeys(hitFraction);
                int frameCount = Mathf.CeilToInt(length * SampleRate) + 1;
                var curves = new BoneCurves(bones, root, frameCount);

                for (int frame = 0; frame < frameCount; frame++)
                {
                    float time = Mathf.Min(frame / SampleRate, length);
                    float fraction = time / length;
                    for (int b = 0; b < bones.Length; b++)
                    {
                        bones[b].localPosition = basePose[b].position;
                        bones[b].localRotation = basePose[b].rotation;
                    }

                    ApplySwipe(rig, Evaluate(keys, fraction), Weight(keys[0].time, fraction, exitFraction));
                    curves.Record(frame, time);
                }

                AnimationClip clip = curves.ToClip(Path.GetFileNameWithoutExtension(savePath), SampleRate);
                return Save(clip, savePath);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        /// <summary>
        /// 필요한 뼈를 찾고, 굽힌 자세의 어깨 위치로 캐릭터 기준 방향을 정한다.
        /// </summary>
        private static bool TryFindRig(Transform[] bones, out Rig rig)
        {
            Transform Find(string name) => bones.FirstOrDefault(bone => string.Equals(bone.name, name, StringComparison.OrdinalIgnoreCase));

            rig = new Rig
            {
                spine = Find(SpineBone),
                head = Find(HeadBone),
                upperArm = Find(UpperArmBone),
                lowerArm = Find(LowerArmBone),
                hand = Find(HandBone)
            };
            Transform leftShoulder = Find(LeftShoulderBone);
            Transform rightShoulder = Find(RightShoulderBone);
            if (rig.spine == null || rig.upperArm == null || rig.lowerArm == null || rig.hand == null || leftShoulder == null || rightShoulder == null)
            {
                Debug.LogError($"[GhoulSwipeClipBuilder] 뼈를 찾을 수 없습니다: {SpineBone}, {LeftShoulderBone}, {RightShoulderBone}, {UpperArmBone}, {LowerArmBone}, {HandBone}");
                return false;
            }

            rig.up = Vector3.up;
            rig.right = Vector3.ProjectOnPlane(rightShoulder.position - leftShoulder.position, Vector3.up).normalized;
            rig.forward = Vector3.Cross(rig.right, rig.up);
            return rig.right.sqrMagnitude > 0f;
        }

        /// <summary>
        /// 클립 비율에 해당하는 휘두르기 자세. 핵심 자세 사이는 부드럽게 잇고, 처음 자세 전·마지막 자세 뒤는 그 자세를 유지한다.
        /// </summary>
        private static SwipeKey Evaluate(SwipeKey[] keys, float fraction)
        {
            if (fraction <= keys[0].time)
                return keys[0];
            for (int i = 1; i < keys.Length; i++)
            {
                if (fraction > keys[i].time)
                    continue;
                SwipeKey from = keys[i - 1];
                SwipeKey to = keys[i];
                float t = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(from.time, to.time, fraction));
                return new SwipeKey(fraction,
                    Vector3.Slerp(from.upperArm.normalized, to.upperArm.normalized, t),
                    Vector3.Slerp(from.lowerArm.normalized, to.lowerArm.normalized, t),
                    Mathf.Lerp(from.twist, to.twist, t),
                    Mathf.Lerp(from.lean, to.lean, t));
            }
            return keys[keys.Length - 1];
        }

        /// <summary>
        /// 휘두르기 적용 비율(0~1). 시작 → 치켜들기까지 0→1, RecoverStart → exitFraction까지 1→0.
        /// </summary>
        private static float Weight(float raiseFraction, float fraction, float exitFraction)
        {
            float blendIn = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, raiseFraction, fraction));
            float blendOut = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(RecoverStart, exitFraction, fraction));
            return blendIn * (1f - blendOut);
        }

        /// <summary>
        /// 굽힌 자세 위에 몸통 비틀기·숙이기와 오른팔 방향을 weight만큼 적용한다. (부모 뼈부터)
        /// </summary>
        private static void ApplySwipe(Rig rig, SwipeKey key, float weight)
        {
            float twist = key.twist * weight;
            rig.spine.rotation = Quaternion.AngleAxis(key.lean * weight, rig.right) * Quaternion.AngleAxis(twist, rig.up) * rig.spine.rotation;
            if (rig.head != null)
                rig.head.rotation = Quaternion.AngleAxis(-twist * HeadCounterTwist, rig.up) * rig.head.rotation;

            Aim(rig.upperArm, rig.lowerArm, rig.ToModel(key.upperArm), weight);
            Aim(rig.lowerArm, rig.hand, rig.ToModel(key.lowerArm), weight);
        }

        /// <summary>
        /// 뼈(→ 자식 뼈 방향)를 목표 방향 쪽으로 weight만큼 돌린다.
        /// </summary>
        private static void Aim(Transform bone, Transform child, Vector3 direction, float weight)
        {
            Vector3 current = (child.position - bone.position).normalized;
            Vector3 target = Vector3.Slerp(current, direction, weight);
            bone.rotation = Quaternion.FromToRotation(current, target) * bone.rotation;
        }

        /// <summary>
        /// 클립을 저장한다. 이미 있으면 내용만 덮어써 컨트롤러 등에서의 참조(GUID)를 유지한다.
        /// </summary>
        private static AnimationClip Save(AnimationClip clip, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(clip, path);
                return clip;
            }

            EditorUtility.CopySerialized(clip, existing);
            Object.DestroyImmediate(clip);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        /// <summary>
        /// 모든 뼈의 지역 회전·위치를 프레임마다 기록해 클립 곡선으로 만든다.
        /// </summary>
        private sealed class BoneCurves
        {
            private static readonly string[] Properties =
            {
                "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w",
                "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z"
            };

            private readonly Transform[] bones;
            private readonly string[] paths;
            private readonly Keyframe[][][] keys;
            private readonly Quaternion[] previousRotations;

            /// <summary>
            /// 뼈 목록과 프레임 수로 기록 공간을 만든다.
            /// </summary>
            public BoneCurves(Transform[] bones, Transform root, int frameCount)
            {
                this.bones = bones;
                paths = bones.Select(bone => AnimationUtility.CalculateTransformPath(bone, root)).ToArray();
                keys = bones.Select(_ => Properties.Select(__ => new Keyframe[frameCount]).ToArray()).ToArray();
                previousRotations = new Quaternion[bones.Length];
            }

            /// <summary>
            /// 지금 뼈 자세를 한 프레임으로 기록한다.
            /// </summary>
            public void Record(int frame, float time)
            {
                for (int b = 0; b < bones.Length; b++)
                {
                    Quaternion rotation = bones[b].localRotation;
                    // 같은 회전의 부호가 프레임마다 뒤집히면 보간 중 한 바퀴 돌므로 이전 프레임과 같은 쪽으로 맞춘다.
                    if (frame > 0 && Quaternion.Dot(previousRotations[b], rotation) < 0f)
                        rotation = new Quaternion(-rotation.x, -rotation.y, -rotation.z, -rotation.w);
                    previousRotations[b] = rotation;

                    Vector3 position = bones[b].localPosition;
                    float[] values = { rotation.x, rotation.y, rotation.z, rotation.w, position.x, position.y, position.z };
                    for (int c = 0; c < Properties.Length; c++)
                        keys[b][c][frame] = new Keyframe(time, values[c]);
                }
            }

            /// <summary>
            /// 기록한 프레임으로 클립을 만든다.
            /// </summary>
            public AnimationClip ToClip(string name, float frameRate)
            {
                var clip = new AnimationClip { name = name, frameRate = frameRate };
                for (int b = 0; b < bones.Length; b++)
                {
                    for (int c = 0; c < Properties.Length; c++)
                    {
                        var curve = new AnimationCurve(keys[b][c]);
                        for (int k = 0; k < curve.length; k++)
                            curve.SmoothTangents(k, 0f);
                        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(paths[b], typeof(Transform), Properties[c]), curve);
                    }
                }
                clip.EnsureQuaternionContinuity();
                return clip;
            }
        }
    }
}
