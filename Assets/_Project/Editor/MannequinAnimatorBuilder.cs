using System.Linq;
using GuardVsMannequin.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace GuardVsMannequin.Editor
{
    /// <summary>
    /// 마네킹(더미 외형) 전용 애니메이터 컨트롤러를 만든다.
    /// - Base Layer: 휴머노이드 이동 블렌드 트리 + 전신 감정 표현(Emotes 하위 상태 머신, 1~5번)
    /// - UpperBody Layer(상체 마스크): 6번 감정 표현과 전원 장치 상호작용(Interact)이 같은 클립(Working On Device)을 쓴다.
    /// 클립은 Assets/_Project/Animation의 Mixamo FBX를 휴머노이드로 임포트해 쓴다. 다시 실행하면 컨트롤러를 새로 만든다.
    /// </summary>
    public static class MannequinAnimatorBuilder
    {
        private const string AnimationFolder = "Assets/_Project/Animation";
        private const string ControllerPath = CharacterAssetBuilder.SkinFolder + "/AC_MannequinHumanoid.controller";
        private const string UpperBodyMaskPath = CharacterAssetBuilder.SkinFolder + "/AM_UpperBody.mask";

        private const string UpperBodyLayerName = "UpperBody";
        private const float EmoteBlendTime = 0.15f;
        private const float EmoteExitTime = 0.92f;
        private const float InteractBlendTime = 0.25f;

        /// <summary>이 속도(m/s)를 넘게 움직이면 전신 감정 표현을 끊고 이동 애니메이션으로 돌아간다.</summary>
        private const float EmoteCancelSpeed = 0.3f;

        /// <summary>감정 표현 하나의 애니메이션 정보.</summary>
        private readonly struct EmoteClip
        {
            public readonly int Id;
            public readonly string FileName;
            public readonly bool UpperBodyOnly;
            public readonly bool Loop;

            public EmoteClip(int id, string fileName, bool upperBodyOnly = false, bool loop = false)
            {
                Id = id;
                FileName = fileName;
                UpperBodyOnly = upperBodyOnly;
                Loop = loop;
            }

            public string AssetPath => $"{AnimationFolder}/{FileName}.fbx";
            public string ClipName => FileName.Replace(" ", string.Empty);
        }

        /// <summary>
        /// 감정 표현 번호(1~6번 키)와 클립. 6번은 상체만 쓰고 전원 장치 상호작용에도 반복 재생한다.
        /// </summary>
        private static readonly EmoteClip[] Emotes =
        {
            new(1, "Salute"),
            new(2, "Thoughtful Head Shake"),
            new(3, "Talking On Phone"),
            new(4, "Situps"),
            new(5, "Backflip"),
            new(6, "Working On Device", upperBodyOnly: true, loop: true)
        };

        /// <summary>상호작용에 쓰는 감정 표현 번호.</summary>
        private const int InteractEmoteId = 6;

        /// <summary>
        /// 컨트롤러를 만들고 더미 외형에 지정한다.
        /// </summary>
        [MenuItem("GuardVsMannequin/마네킹 감정 표현 애니메이터 생성")]
        public static void BuildAndAssign()
        {
            AnimatorController controller = CreateController();
            if (controller == null)
                return;

            CharacterSkin dummy = AssetDatabase.LoadAssetAtPath<CharacterSkin>(CharacterAssetBuilder.DummySkinPath);
            if (dummy != null)
            {
                dummy.animatorController = controller;
                EditorUtility.SetDirty(dummy);
            }
            else
            {
                Debug.LogWarning($"[MannequinAnimatorBuilder] 더미 외형이 없어 지정하지 못했습니다: {CharacterAssetBuilder.DummySkinPath}");
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[MannequinAnimatorBuilder] 마네킹 애니메이터 생성 완료: {ControllerPath}");
        }

        /// <summary>
        /// 클립 임포트 설정을 맞추고 마네킹 컨트롤러를 만든다.
        /// </summary>
        public static AnimatorController CreateController()
        {
            AnimatorController controller = CharacterAssetBuilder.CreateHumanoidLocomotionController(ControllerPath);
            AnimatorState locomotion = controller.layers[0].stateMachine.defaultState;

            foreach (EmoteClip emote in Emotes)
                controller.AddParameter(CharacterAnimatorParameters.EmoteTrigger(emote.Id), AnimatorControllerParameterType.Trigger);
            controller.AddParameter(CharacterAnimatorParameters.Interact, AnimatorControllerParameterType.Bool);

            AddFullBodyEmotes(controller, locomotion);
            AddUpperBodyLayer(controller);

            EditorUtility.SetDirty(controller);
            return controller;
        }

        #region 레이어 구성

        /// <summary>
        /// Base Layer에 전신 감정 표현을 추가한다.
        /// 어느 상태에서든 트리거로 들어가고, 클립이 끝나거나 움직이면 이동 애니메이션으로 돌아간다.
        /// </summary>
        private static void AddFullBodyEmotes(AnimatorController controller, AnimatorState locomotion)
        {
            AnimatorStateMachine root = controller.layers[0].stateMachine;
            AnimatorStateMachine emoteMachine = root.AddStateMachine("Emotes");

            foreach (EmoteClip emote in Emotes.Where(e => !e.UpperBodyOnly))
            {
                AnimationClip clip = ImportClip(emote);
                if (clip == null)
                    continue;

                AnimatorState state = emoteMachine.AddState(StateName(emote.Id));
                state.motion = clip;

                AnimatorStateTransition enter = root.AddAnyStateTransition(state);
                ConfigureTrigger(enter, emote.Id);

                AnimatorStateTransition finish = state.AddTransition(locomotion);
                finish.hasExitTime = true;
                finish.exitTime = EmoteExitTime;
                finish.duration = EmoteBlendTime;

                AnimatorStateTransition cancel = state.AddTransition(locomotion);
                cancel.hasExitTime = false;
                cancel.duration = EmoteBlendTime;
                cancel.AddCondition(AnimatorConditionMode.Greater, EmoteCancelSpeed, CharacterAnimatorParameters.Speed);
            }
        }

        /// <summary>
        /// 상체 마스크 레이어를 추가한다. 비어 있는 기본 상태에서는 Base Layer 동작을 그대로 보여 준다.
        /// - Interact(Bool)가 켜져 있는 동안 상호작용 클립을 반복한다.
        /// - 상체 전용 감정 표현 트리거가 오면 한 번 재생하고 돌아간다.
        /// </summary>
        private static void AddUpperBodyLayer(AnimatorController controller)
        {
            controller.AddLayer(UpperBodyLayerName);
            AnimatorControllerLayer[] layers = controller.layers;
            int layerIndex = layers.Length - 1;
            layers[layerIndex].avatarMask = CreateUpperBodyMask();
            layers[layerIndex].defaultWeight = 1f;
            layers[layerIndex].blendingMode = AnimatorLayerBlendingMode.Override;
            controller.layers = layers;

            AnimatorStateMachine machine = controller.layers[layerIndex].stateMachine;
            AnimatorState empty = machine.AddState("Empty");
            machine.defaultState = empty;

            EmoteClip interactEmote = Emotes.First(e => e.Id == InteractEmoteId);
            AnimationClip interactClip = ImportClip(interactEmote);
            if (interactClip != null)
            {
                AnimatorState interact = machine.AddState("Interact");
                interact.motion = interactClip;

                AnimatorStateTransition begin = empty.AddTransition(interact);
                begin.hasExitTime = false;
                begin.duration = InteractBlendTime;
                begin.AddCondition(AnimatorConditionMode.If, 0f, CharacterAnimatorParameters.Interact);

                AnimatorStateTransition end = interact.AddTransition(empty);
                end.hasExitTime = false;
                end.duration = InteractBlendTime;
                end.AddCondition(AnimatorConditionMode.IfNot, 0f, CharacterAnimatorParameters.Interact);
            }

            foreach (EmoteClip emote in Emotes.Where(e => e.UpperBodyOnly))
            {
                AnimationClip clip = ImportClip(emote);
                if (clip == null)
                    continue;

                AnimatorState state = machine.AddState(StateName(emote.Id));
                state.motion = clip;

                AnimatorStateTransition enter = machine.AddAnyStateTransition(state);
                ConfigureTrigger(enter, emote.Id);

                AnimatorStateTransition finish = state.AddTransition(empty);
                finish.hasExitTime = true;
                finish.exitTime = EmoteExitTime;
                finish.duration = InteractBlendTime;
            }
        }

        /// <summary>
        /// 감정 표현 트리거로 들어가는 전이를 설정한다. 같은 감정 표현을 연타해도 처음부터 다시 시작하지 않는다.
        /// </summary>
        private static void ConfigureTrigger(AnimatorStateTransition transition, int emoteId)
        {
            transition.hasExitTime = false;
            transition.duration = EmoteBlendTime;
            transition.canTransitionToSelf = false;
            transition.AddCondition(AnimatorConditionMode.If, 0f, CharacterAnimatorParameters.EmoteTrigger(emoteId));
        }

        /// <summary>
        /// 감정 표현 상태 이름. (트리거 이름과 같게 해 Animator 창에서 찾기 쉽게 한다)
        /// </summary>
        private static string StateName(int emoteId) => CharacterAnimatorParameters.EmoteTrigger(emoteId);

        /// <summary>
        /// 상체(몸통·머리·팔·손가락·손 IK)만 켠 휴머노이드 마스크를 만든다. 다리와 루트는 Base Layer를 따른다.
        /// </summary>
        private static AvatarMask CreateUpperBodyMask()
        {
            AssetDatabase.DeleteAsset(UpperBodyMaskPath);
            var mask = new AvatarMask();
            for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
                mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, false);

            AvatarMaskBodyPart[] upperBody =
            {
                AvatarMaskBodyPart.Body, AvatarMaskBodyPart.Head,
                AvatarMaskBodyPart.LeftArm, AvatarMaskBodyPart.RightArm,
                AvatarMaskBodyPart.LeftFingers, AvatarMaskBodyPart.RightFingers,
                AvatarMaskBodyPart.LeftHandIK, AvatarMaskBodyPart.RightHandIK
            };
            foreach (AvatarMaskBodyPart part in upperBody)
                mask.SetHumanoidBodyPartActive(part, true);

            AssetDatabase.CreateAsset(mask, UpperBodyMaskPath);
            return mask;
        }

        #endregion

        #region 클립 임포트

        /// <summary>
        /// Mixamo FBX를 휴머노이드로 임포트하고 클립 설정을 맞춘 뒤 클립을 반환한다. 없으면 null.
        /// </summary>
        private static AnimationClip ImportClip(EmoteClip emote)
        {
            if (AssetImporter.GetAtPath(emote.AssetPath) is not ModelImporter importer)
            {
                Debug.LogWarning($"[MannequinAnimatorBuilder] 애니메이션 파일을 찾을 수 없습니다: {emote.AssetPath}");
                return null;
            }

            EnsureHumanoid(importer);
            ConfigureClips(importer, emote);

            AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(emote.AssetPath).OfType<AnimationClip>()
                .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
            if (clip == null)
                Debug.LogWarning($"[MannequinAnimatorBuilder] 클립이 없습니다: {emote.AssetPath}");
            return clip;
        }

        /// <summary>
        /// 리타깃할 수 있도록 휴머노이드(이 모델로 Avatar 생성)로 바꾼다. 클립 기본값이 바뀌므로 바로 다시 임포트한다.
        /// </summary>
        private static void EnsureHumanoid(ModelImporter importer)
        {
            if (importer.animationType == ModelImporterAnimationType.Human
                && importer.avatarSetup == ModelImporterAvatarSetup.CreateFromThisModel)
                return;

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            importer.SaveAndReimport();
        }

        /// <summary>
        /// 클립을 제자리 재생으로 맞춘다.
        /// - 회전·높이는 포즈에 굽고(원본 기준) 수평 이동은 루트 모션으로 분리한다. 루트 모션을 쓰지 않으므로 캐릭터가 캡슐 안에서 재생된다.
        ///   (백플립·윗몸 일으키기도 몸이 충돌체 밖으로 밀려나지 않음)
        /// - 반복 여부는 감정 표현 정보에 따른다.
        /// </summary>
        private static void ConfigureClips(ModelImporter importer, EmoteClip emote)
        {
            ModelImporterClipAnimation[] clips = importer.clipAnimations;
            if (clips.Length == 0)
                clips = importer.defaultClipAnimations;
            if (clips.Length == 0)
                return;

            foreach (ModelImporterClipAnimation clip in clips)
            {
                clip.name = emote.ClipName;
                clip.loopTime = emote.Loop;
                clip.lockRootRotation = true;
                clip.keepOriginalOrientation = true;
                clip.lockRootHeightY = true;
                clip.keepOriginalPositionY = true;
                clip.lockRootPositionXZ = false;
                clip.keepOriginalPositionXZ = true;
            }

            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        #endregion
    }
}
