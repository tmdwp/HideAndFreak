using System.Collections.Generic;
using System.Linq;
using GuardVsMannequin.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace GuardVsMannequin.Editor
{
    /// <summary>
    /// 사냥 시간 마네킹 외형 Ghoul(Generic 리그)의 원본 모델과 애니메이터를 준비한다.
    /// - 임포트: Generic + 자체 Avatar, 클립 이름을 짧게(Ghoul_Armature|Idle → Idle), 공격 외 클립은 반복
    /// - 머티리얼: 몸은 Ghoul_Scribble 텍스처를 쓰는 URP Lit 머티리얼(M_Ghoul, 이미 있으면 그대로 둠),
    ///   이름에 "Eye"가 들어간 머티리얼(눈·이빨)은 Ghoul_Eye가 있으면 그것으로 연결
    /// - 공격 클립: Hunch_Reach 자세에서 오른팔로 할퀴는 Ghoul_AttackHunched (<see cref="GhoulSwipeClipBuilder"/>가 만듦)
    /// - 애니메이터: 기본·이동 = Hunch_Reach, Attack 트리거 → Ghoul_AttackHunched 후 복귀
    /// 외형 프리팹·<see cref="CharacterSkin"/> 저장은 <see cref="CharacterAssetBuilder"/>가 한다.
    /// </summary>
    public static class GhoulSkinBuilder
    {
        public const string SourcePath = CharacterAssetBuilder.SkinFolder + "/Ghoul.fbx";
        private const string TexturePath = CharacterAssetBuilder.SkinFolder + "/Ghoul_Scribble.png";
        private const string MaterialPath = CharacterAssetBuilder.SkinFolder + "/M_Ghoul.mat";
        private const string EyeMaterialPath = CharacterAssetBuilder.SkinFolder + "/Ghoul_Eye.mat";
        private const string EyeMaterialKeyword = "eye";
        private const string ControllerPath = CharacterAssetBuilder.SkinFolder + "/AC_Ghoul.controller";

        private const string LocomotionClip = "Hunch_Reach";
        private const string AttackClip = "Attack_Swipe";

        // 공격 진입은 즉시, 복귀는 클립이 거의 끝날 때 부드럽게
        private const float AttackEnterDuration = 0.05f;
        private const float AttackExitTime = 0.9f;
        private const float AttackExitDuration = 0.15f;

        // Attack_Swipe에서 손이 앞으로 내려와 맞는 지점(클립 길이 비율)
        private const float AttackHitFraction = 0.45f;

        // Hunch_Reach 자세로 만든 할퀴기 클립 (GhoulSwipeClipBuilder)
        private const string SwipeClipPath = CharacterAssetBuilder.SkinFolder + "/Ghoul_AttackHunched.anim";

        /// <summary>
        /// 공격 동작 시작부터 맞는 순간까지의 기본 시간(초). 클립이 없으면 0(즉시 판정).
        /// </summary>
        public static float DefaultAttackHitDelay()
        {
            AnimationClip attack = FindClip(AttackClip);
            return attack != null ? AttackEnterDuration + attack.length * AttackHitFraction : 0f;
        }

        /// <summary>
        /// 임포트 설정·머티리얼을 맞추고 애니메이터 컨트롤러를 만들어 반환한다. 원본이 없으면 null.
        /// </summary>
        public static AnimatorController Prepare()
        {
            if (AssetImporter.GetAtPath(SourcePath) is not ModelImporter importer)
            {
                Debug.LogError($"[GhoulSkinBuilder] 원본 모델을 찾을 수 없습니다: {SourcePath}");
                return null;
            }

            ConfigureImport(importer);
            RemapMaterials(importer, LoadOrCreateMaterial(), AssetDatabase.LoadAssetAtPath<Material>(EyeMaterialPath));
            return CreateController();
        }

        /// <summary>
        /// Generic 리그와 자체 Avatar로 임포트하고, 클립 이름·반복 여부를 정한다. 바뀐 것이 있을 때만 다시 임포트한다.
        /// </summary>
        private static void ConfigureImport(ModelImporter importer)
        {
            bool changed = false;
            if (importer.animationType != ModelImporterAnimationType.Generic)
            {
                importer.animationType = ModelImporterAnimationType.Generic;
                changed = true;
            }
            if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
            {
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                changed = true;
            }
            if (changed)
                importer.SaveAndReimport();

            ModelImporterClipAnimation[] clips = importer.clipAnimations;
            if (clips.Length == 0)
                clips = importer.defaultClipAnimations;

            bool clipsChanged = false;
            foreach (ModelImporterClipAnimation clip in clips)
            {
                string shortName = ShortName(clip.takeName);
                bool loop = shortName != AttackClip;
                if (clip.name == shortName && clip.loopTime == loop)
                    continue;
                clip.name = shortName;
                clip.loopTime = loop;
                clipsChanged = true;
            }
            if (!clipsChanged)
                return;

            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        /// <summary>
        /// "Ghoul_Armature|Hunch_Reach"처럼 아마추어 이름이 붙은 테이크 이름에서 클립 이름만 남긴다.
        /// </summary>
        private static string ShortName(string takeName)
        {
            int separator = takeName.LastIndexOf('|');
            return separator >= 0 ? takeName.Substring(separator + 1) : takeName;
        }

        /// <summary>
        /// M_Ghoul 머티리얼을 불러오고, 없으면 Ghoul_Scribble 텍스처로 만든다. (만든 뒤 사용자가 고친 값은 유지)
        /// </summary>
        private static Material LoadOrCreateMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material != null)
                return material;

            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath));
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", 0.2f);
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }

        /// <summary>
        /// 모델의 원본 머티리얼(아직 내장된 것 + 이미 연결된 것)을 이름에 따라 몸/눈 머티리얼로 연결한다.
        /// 눈 머티리얼이 없으면 전부 몸 머티리얼로 연결한다. 바뀐 연결이 있을 때만 다시 임포트한다.
        /// </summary>
        private static void RemapMaterials(ModelImporter importer, Material body, Material eye)
        {
            Dictionary<AssetImporter.SourceAssetIdentifier, Object> current = importer.GetExternalObjectMap();
            var sources = current.Keys.Where(key => key.type == typeof(Material)).Select(key => key.name)
                .Concat(AssetDatabase.LoadAllAssetsAtPath(SourcePath).OfType<Material>().Select(material => material.name))
                .Distinct();

            bool changed = false;
            foreach (string sourceName in sources)
            {
                Material target = eye != null && sourceName.ToLowerInvariant().Contains(EyeMaterialKeyword) ? eye : body;
                var identifier = new AssetImporter.SourceAssetIdentifier(typeof(Material), sourceName);
                if (current.TryGetValue(identifier, out Object mapped) && mapped == target)
                    continue;
                importer.AddRemap(identifier, target);
                changed = true;
            }
            if (changed)
                importer.SaveAndReimport();
        }

        /// <summary>
        /// Hunch_Reach(기본·이동)와 굽힌 자세 공격(Attack 트리거) 두 상태의 컨트롤러를 만든다. 같은 경로에 있으면 지우고 새로 만든다.
        /// </summary>
        private static AnimatorController CreateController()
        {
            AnimationClip locomotion = FindClip(LocomotionClip);
            AnimationClip attack = FindClip(AttackClip);
            if (locomotion == null || attack == null)
            {
                Debug.LogError($"[GhoulSkinBuilder] 클립을 찾을 수 없습니다: {LocomotionClip}={locomotion != null}, {AttackClip}={attack != null}");
                return null;
            }

            // Hunch_Reach 자세로 팔만 휘두르는 클립을 만든다. 맞는 순간·길이는 원본 공격과 같게 해 서버 판정 시점(attackHitDelay)과 맞춘다.
            // 만들지 못하면 원본 공격 클립을 그대로 쓴다.
            AnimationClip hunchedAttack = GhoulSwipeClipBuilder.Create(SourcePath, SwipeClipPath, locomotion, attack.length, AttackHitFraction, AttackExitTime) ?? attack;

            AssetDatabase.DeleteAsset(ControllerPath);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter(CharacterAnimatorParameters.Attack, AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimatorState locomotionState = stateMachine.AddState("Locomotion");
            locomotionState.motion = locomotion;
            AnimatorState attackState = stateMachine.AddState("Attack");
            attackState.motion = hunchedAttack;
            stateMachine.defaultState = locomotionState;

            AnimatorStateTransition toAttack = stateMachine.AddAnyStateTransition(attackState);
            toAttack.AddCondition(AnimatorConditionMode.If, 0f, CharacterAnimatorParameters.Attack);
            toAttack.hasExitTime = false;
            toAttack.duration = AttackEnterDuration;
            toAttack.canTransitionToSelf = false;

            // 굽힌 자세 클립은 Hunch_Reach 첫 자세로 끝나므로 그 지점(처음)으로 돌아간다.
            AnimatorStateTransition toLocomotion = attackState.AddTransition(locomotionState);
            toLocomotion.hasExitTime = true;
            toLocomotion.exitTime = AttackExitTime;
            toLocomotion.duration = AttackExitDuration;

            EditorUtility.SetDirty(controller);
            return controller;
        }

        /// <summary>
        /// 원본 모델에서 이름이 일치하는 클립을 찾는다.
        /// </summary>
        private static AnimationClip FindClip(string clipName)
        {
            return AssetDatabase.LoadAllAssetsAtPath(SourcePath).OfType<AnimationClip>()
                .FirstOrDefault(clip => clip.name == clipName);
        }
    }
}
