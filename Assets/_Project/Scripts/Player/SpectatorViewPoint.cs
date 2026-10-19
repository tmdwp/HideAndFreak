using GuardVsMannequin.Core;
using GuardVsMannequin.Player.Mannequin;
using UnityEngine;
using UnityEngine.Rendering;

namespace GuardVsMannequin.Player
{
    /// <summary>
    /// 관전자가 이 플레이어를 볼 때의 카메라 위치와 방향을 계산한다. (대상 플레이어의 시야)
    /// - 1인칭(가드, 사냥 시간의 마네킹): 눈 위치에서 동기화된 시선 방향
    /// - 3인칭(평소의 마네킹): 동기화된 시선 방향으로 뒤에서 따라가는 궤도 카메라
    /// </summary>
    [RequireComponent(typeof(PlayerViewSync), typeof(PlayerIdentity))]
    public class SpectatorViewPoint : MonoBehaviour
    {
        [Tooltip("눈 위치. (가드: 카메라 피벗, 마네킹: 추적 대상)")]
        [SerializeField] private Transform eyePoint;

        [Tooltip("평소에 3인칭으로 보는 역할인지 여부. (마네킹)")]
        [SerializeField] private bool isThirdPerson;

        [SerializeField] private float followDistance = 3.5f;
        [SerializeField] private float cameraCollisionRadius = 0.2f;

        [Tooltip("1인칭에서 눈 앞으로 당기는 거리. (얼굴 모형이 화면을 가리지 않도록)")]
        [SerializeField] private float firstPersonForwardOffset = 0.35f;

        private PlayerViewSync viewSync;
        private PlayerIdentity identity;
        private IViewEffect huntViewEffect;
        private CharacterSkinView skinView;
        private PropDisguiseView disguiseView;
        private Renderer[] hiddenBodyRenderers;
        private bool isSpectated;
        private bool isEffectApplied;

        /// <summary>대상 플레이어의 표시 이름과 역할.</summary>
        public PlayerIdentity Identity => identity;

        /// <summary>평소 3인칭 역할(마네킹)이 사냥 시간이 되어 1인칭으로 보이는지 여부.</summary>
        private bool IsHuntFirstPerson => isThirdPerson && GameServices.CurrentStateOrWaiting == GameState.Hunting;

        /// <summary>
        /// 같은 오브젝트의 구성 요소를 캐시한다. 사냥 시간 시야 효과·외형·변신 외형은 없을 수 있다.
        /// </summary>
        private void Awake()
        {
            viewSync = GetComponent<PlayerViewSync>();
            identity = GetComponent<PlayerIdentity>();
            huntViewEffect = GetComponent<IViewEffect>();
            skinView = GetComponent<CharacterSkinView>();
            disguiseView = GetComponent<PropDisguiseView>();
        }

        /// <summary>
        /// 관전되는 동안 사냥 시간이면 대상의 시야 효과를 관전자 화면에도 적용한다.
        /// (마네킹의 개인 조명과 안개는 본인 화면에만 켜지므로, 관전자는 그대로 두면 아무것도 보이지 않음)
        /// </summary>
        private void Update()
        {
            UpdateBodyHidden();

            bool shouldApply = isSpectated && huntViewEffect != null
                               && GameServices.CurrentStateOrWaiting == GameState.Hunting;
            if (shouldApply == isEffectApplied)
                return;

            isEffectApplied = shouldApply;
            if (shouldApply)
                huntViewEffect.Activate();
            else
                huntViewEffect.Deactivate();
        }

        /// <summary>
        /// 사냥 시간 1인칭으로 관전되는 동안 대상의 몸과 변신 사물을 그림자만 남긴다. (머리·사물이 관전 화면을 가리지 않도록)
        /// 사냥 외형으로 바뀌면 렌더러 목록도 바뀌므로 목록이 달라질 때마다 다시 적용한다.
        /// 관전 대상은 항상 원격 아바타이므로(관전자는 아바타가 없음) 되돌릴 때는 그림자 켬으로 돌린다.
        /// </summary>
        private void UpdateBodyHidden()
        {
            bool shouldHide = isSpectated && IsHuntFirstPerson;
            Renderer[] current = shouldHide && skinView != null ? skinView.BodyRenderers : null;
            if (current == hiddenBodyRenderers)
                return;

            SetShadowMode(hiddenBodyRenderers, ShadowCastingMode.On);
            SetShadowMode(current, ShadowCastingMode.ShadowsOnly);
            hiddenBodyRenderers = current;

            if (disguiseView != null)
                disguiseView.SetDisguiseShadowsOnly(shouldHide);
        }

        /// <summary>
        /// 살아 있는 렌더러의 그림자 모드를 바꾼다. (외형 교체로 제거된 렌더러는 건너뜀)
        /// </summary>
        private static void SetShadowMode(Renderer[] renderers, ShadowCastingMode mode)
        {
            if (renderers == null)
                return;
            foreach (Renderer target in renderers)
            {
                if (target != null)
                    target.shadowCastingMode = mode;
            }
        }

        /// <summary>
        /// 관전 대상이 되었는지 알린다. (SpectatorController가 호출)
        /// </summary>
        public void SetSpectated(bool spectated)
        {
            isSpectated = spectated;
        }

        /// <summary>
        /// 관전 카메라의 위치와 방향을 계산한다.
        /// </summary>
        public void GetPose(out Vector3 position, out Quaternion rotation)
        {
            rotation = Quaternion.Euler(viewSync.ViewPitch, viewSync.ViewYaw, 0f);
            Vector3 eye = eyePoint.position;

            // 마네킹은 사냥 시간에 1인칭으로 바뀐다.
            bool thirdPerson = isThirdPerson && !IsHuntFirstPerson;
            if (!thirdPerson)
            {
                position = eye + rotation * Vector3.forward * firstPersonForwardOffset;
                return;
            }

            Vector3 back = rotation * Vector3.back;
            float distance = followDistance;
            if (AimRaycaster.TryRaycast(new Ray(eye, back), followDistance, transform, out RaycastHit hit))
                distance = Mathf.Max(0.2f, hit.distance - cameraCollisionRadius);

            position = eye + back * distance;
        }
    }
}
