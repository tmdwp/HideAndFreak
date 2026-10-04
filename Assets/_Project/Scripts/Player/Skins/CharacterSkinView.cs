using System.Collections.Generic;
using GuardVsMannequin.Core;
using GuardVsMannequin.Map;
using UnityEngine;

namespace GuardVsMannequin.Player
{
    /// <summary>
    /// 캐릭터 외형(<see cref="CharacterSkin"/>)을 붙이고 애니메이터를 준비한다.
    /// - 외형: 맵(<see cref="MapInfo.GetSkin"/>)이 역할별 외형을 지정하면 그것을, 아니면 기본 외형을 쓴다.
    ///   외형이 없으면 기본 도형(fallbackRenderers)을 그대로 보여준다.
    /// - 애니메이션: 이동 값은 <see cref="LocomotionAnimatorDriver"/>가 넣고, 파라미터 확인은 <see cref="AnimatorParameterSet"/>이 한다.
    ///   애니메이터에 있는 파라미터만 설정한다. (이름 규칙: <see cref="CharacterAnimatorParameters"/>)
    /// 몸체 렌더러는 <see cref="BodyRenderers"/>로 제공한다. (변신 숨김, 1인칭 그림자 처리에 사용)
    /// </summary>
    public class CharacterSkinView : MonoBehaviour
    {
        [Tooltip("맵에서 외형을 고를 때 사용할 역할.")]
        [SerializeField] private PlayerRole role = PlayerRole.Mannequin;

        [Tooltip("맵이 외형을 지정하지 않았을 때의 외형.")]
        [SerializeField] private CharacterSkin defaultSkin;

        [Tooltip("외형 모델을 붙일 부모. (감정 표현이 이 트랜스폼을 움직인다)")]
        [SerializeField] private Transform visualRoot;

        [Tooltip("외형이 없을 때 쓰는 기본 도형 렌더러. 외형을 붙이면 숨긴다.")]
        [SerializeField] private Renderer[] fallbackRenderers;

        private readonly List<Renderer> skinRenderers = new();
        private Renderer[] bodyRenderers;
        private AnimatorParameterSet parameters;
        private LocomotionAnimatorDriver locomotion;
        private HandAimIK aimIK;
        private CharacterSkin activeSkin;
        private bool isApplied;

        /// <summary>현재 몸체 렌더러. (외형이 있으면 외형 렌더러, 없으면 기본 도형)</summary>
        public Renderer[] BodyRenderers
        {
            get
            {
                EnsureApplied();
                return bodyRenderers;
            }
        }

        /// <summary>적용된 외형. 외형 없이 기본 도형을 쓰면 null.</summary>
        public CharacterSkin ActiveSkin
        {
            get
            {
                EnsureApplied();
                return activeSkin;
            }
        }

        /// <summary>외형 모델의 애니메이터. 없으면 null.</summary>
        public Animator Animator
        {
            get
            {
                EnsureApplied();
                return parameters?.Animator;
            }
        }

        /// <summary>
        /// 외형을 적용한다.
        /// </summary>
        private void Awake()
        {
            EnsureApplied();
        }

        /// <summary>
        /// 이동 속도를 애니메이터에 전달한다.
        /// </summary>
        private void Update()
        {
            locomotion?.Tick(Time.deltaTime);
        }

        /// <summary>
        /// 애니메이터에 해당 이름의 파라미터가 있는지 확인한다.
        /// </summary>
        public bool HasParameter(string parameterName)
        {
            EnsureApplied();
            return parameters != null && parameters.Has(parameterName);
        }

        /// <summary>
        /// 트리거가 있으면 발동하고 true, 없으면 아무것도 하지 않고 false. (애니메이션이 없는 외형은 건너뜀)
        /// </summary>
        public bool TryPlayTrigger(string triggerName)
        {
            EnsureApplied();
            return parameters != null && parameters.TrySetTrigger(triggerName);
        }

        /// <summary>
        /// 정수 파라미터가 있으면 설정하고 true.
        /// </summary>
        public bool TrySetInteger(string parameterName, int value)
        {
            EnsureApplied();
            return parameters != null && parameters.TrySetInteger(parameterName, value);
        }

        /// <summary>
        /// 논리 파라미터가 있으면 설정하고 true.
        /// </summary>
        public bool TrySetBool(string parameterName, bool value)
        {
            EnsureApplied();
            return parameters != null && parameters.TrySetBool(parameterName, value);
        }

        /// <summary>
        /// 대상에 외형 컴포넌트가 있으면 그 몸체 렌더러를, 없으면 지정한 렌더러를 반환한다.
        /// (외형 시스템 도입 전 프리팹과 호환하기 위함)
        /// </summary>
        public static Renderer[] ResolveBodyRenderers(Component owner, Renderer[] fallback)
        {
            CharacterSkinView skinView = owner.GetComponent<CharacterSkinView>();
            return skinView != null ? skinView.BodyRenderers : fallback ?? new Renderer[0];
        }

        /// <summary>
        /// 조준 IK가 따라갈 방향(카메라 피벗)을 지정한다. 외형이 IK를 쓰지 않으면 무시된다.
        /// </summary>
        public void SetAimSource(Transform aimSource)
        {
            EnsureApplied();
            if (aimIK != null)
                aimIK.AimSource = aimSource;
        }

        /// <summary>
        /// 조준 IK의 오른손 위치(조준 기준 좌표)를 지정한다. 외형이 IK를 쓰지 않으면 무시된다.
        /// </summary>
        public void SetAimHandOffset(Vector3 handOffset)
        {
            if (aimIK != null)
                aimIK.HandOffset = handOffset;
        }

        /// <summary>
        /// 외형을 한 번만 적용한다. 다른 컴포넌트가 Awake 순서와 관계없이 렌더러를 조회할 수 있게 한다.
        /// </summary>
        private void EnsureApplied()
        {
            if (isApplied)
                return;
            isApplied = true;

            CharacterSkin skin = ChooseSkin();
            if (skin == null || skin.modelPrefab == null)
            {
                bodyRenderers = fallbackRenderers ?? new Renderer[0];
                return;
            }

            activeSkin = skin;
            Transform parent = visualRoot != null ? visualRoot : transform;
            GameObject model = Instantiate(skin.modelPrefab, parent, false);
            model.name = skin.modelPrefab.name;
            model.transform.localRotation = Quaternion.Euler(0f, skin.yawOffset, 0f);

            model.GetComponentsInChildren(true, skinRenderers);
            bodyRenderers = skinRenderers.ToArray();
            foreach (Renderer fallback in fallbackRenderers ?? new Renderer[0])
            {
                if (fallback != null)
                    fallback.enabled = false;
            }

            SetupAnimator(model, skin);
        }

        /// <summary>
        /// 맵이 지정한 역할별 외형을 우선하고, 없으면 기본 외형을 쓴다.
        /// </summary>
        private CharacterSkin ChooseSkin()
        {
            CharacterSkin mapSkin = MapInfo.Current != null ? MapInfo.Current.GetSkin(role) : null;
            return mapSkin != null ? mapSkin : defaultSkin;
        }

        /// <summary>
        /// 애니메이터 컨트롤러를 지정하고, 루트 모션을 끄고, 필요하면 조준 IK를 붙인 뒤 이동 애니메이션 구동을 준비한다.
        /// </summary>
        private void SetupAnimator(GameObject model, CharacterSkin skin)
        {
            Animator animator = model.GetComponentInChildren<Animator>();
            if (animator == null)
                return;

            if (skin.animatorController != null)
                animator.runtimeAnimatorController = skin.animatorController;
            animator.applyRootMotion = false;

            // 에셋 팩 클립의 이벤트는 팩 스크립트(외형 생성 시 제거됨)를 부르므로 기본으로 끈다. ("has no receiver" 오류 방지)
            animator.fireEvents = skin.fireAnimationEvents;

            parameters = new AnimatorParameterSet(animator);
            locomotion = new LocomotionAnimatorDriver(parameters, transform);

            if (skin.useAimIK && animator.isHuman)
                aimIK = animator.gameObject.AddComponent<HandAimIK>();

            // 일반 모델(슬라임 등)의 이동 클립은 뼈가 앞으로 나갔다 돌아오므로 제자리로 고정한다. (휴머노이드는 루트 모션을 끄면 이미 제자리)
            if (skin.keepRootBoneInPlace && !animator.isHuman)
                animator.gameObject.AddComponent<RootBoneInPlaceLock>();
        }
    }
}
