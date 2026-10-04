using FishNet.Object;
using GuardVsMannequin.Audio;
using GuardVsMannequin.Core;
using GuardVsMannequin.Localization;
using UnityEngine;

namespace GuardVsMannequin.Player.Guard
{
    /// <summary>
    /// 가드의 총. 좌클릭 시 서버가 히트스캔으로 판정한다.
    /// 쿨타임은 서버가 최종 판정하고, 소유자는 HUD 표시용으로 같은 값을 예측한다.
    /// </summary>
    [RequireComponent(typeof(CameraAimProvider))]
    public class GunTool : NetworkBehaviour, ITool
    {
        /// <summary>비정상 원점을 대체할 눈 높이(m).</summary>
        private const float EyeHeight = 1.6f;

        [SerializeField] private ShotTracerView tracerView;

        private readonly ToolCooldown cooldown = new();
        private IAimProvider aimProvider;
        private IPlayerInput playerInput;
        private NoiseEmitter noiseEmitter;
        private CharacterSkinView skinView;

        public string DisplayName => Loc.Get("tool.gun");
        public float CooldownRemaining => cooldown.LocalRemaining;
        public bool IsReady => CooldownRemaining <= 0f && DamageRules.CanUseGuardTools(GameServices.CurrentStateOrWaiting);

        /// <summary>호스트 설정의 총 쿨타임.</summary>
        private static float Cooldown => GameServices.CurrentSettingsOrDefault.gunCooldown;

        /// <summary>한 발의 피해량. (가드 능력치, 최소 1)</summary>
        private static int Damage => Mathf.Max(1, GameServices.CurrentSettingsOrDefault.GetStats(PlayerRole.Guard).AttackDamage);

        /// <summary>
        /// 조준, 입력, 소음 구현을 캐시한다.
        /// </summary>
        private void Awake()
        {
            aimProvider = GetComponent<IAimProvider>();
            noiseEmitter = GetComponent<NoiseEmitter>();
            playerInput = GetComponent<IPlayerInput>();
            skinView = GetComponent<CharacterSkinView>();
        }

        /// <summary>
        /// 소유자만 사격 입력을 구독한다.
        /// </summary>
        public override void OnStartClient()
        {
            base.OnStartClient();
            if (IsOwner && playerInput != null)
                playerInput.PrimaryPressed += OnPrimaryPressed;
        }

        /// <summary>
        /// 입력 구독을 해제한다.
        /// </summary>
        public override void OnStopClient()
        {
            base.OnStopClient();
            if (IsOwner && playerInput != null)
                playerInput.PrimaryPressed -= OnPrimaryPressed;
        }

        /// <summary>
        /// 쿨타임이 끝났으면 조준 광선을 서버로 보낸다.
        /// </summary>
        private void OnPrimaryPressed()
        {
            if (!IsReady)
                return;

            Ray aim = aimProvider.GetAimRay();
            cooldown.StartLocal(Cooldown);
            FireServerRpc(aim.origin, aim.direction);
        }

        /// <summary>
        /// 서버에서 사격을 검증하고 히트스캔 판정을 한다.
        /// </summary>
        [ServerRpc]
        private void FireServerRpc(Vector3 origin, Vector3 direction)
        {
            if (!DamageRules.CanUseGuardTools(GameServices.CurrentStateOrWaiting))
                return;
            if (!cooldown.TryStartServer(Cooldown))
                return;

            origin = AimRaycaster.SanitizeOrigin(origin, transform, EyeHeight);

            Ray ray = new Ray(origin, direction.normalized);
            Vector3 endPoint = ray.GetPoint(GameConstants.GunRange);

            bool didHit = AimRaycaster.TryRaycast(ray, GameConstants.GunRange, transform, out RaycastHit hit);
            if (didHit)
            {
                endPoint = hit.point;
                IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
                target?.TakeDamage(Damage, NetworkObject);
            }

            ShotFiredObserversRpc(origin, endPoint, didHit);
        }

        /// <summary>
        /// 모든 클라이언트에 사격 궤적, 탄착 지점, 총소리를 재생한다.
        /// </summary>
        [ObserversRpc]
        private void ShotFiredObserversRpc(Vector3 origin, Vector3 endPoint, bool didHit)
        {
            if (tracerView != null)
                tracerView.Show(origin, endPoint, didHit);

            // 사격 RPC가 이미 모든 클라이언트에 전달되므로 소음은 추가 전송 없이 로컬 재생한다.
            if (noiseEmitter != null)
                noiseEmitter.PlayLocal(NoiseType.GunShot);

            // 외형 애니메이터에 Shoot 트리거가 있을 때만 사격 동작을 재생한다.
            if (skinView != null)
                skinView.TryPlayTrigger(CharacterAnimatorParameters.Shoot);
        }
    }
}
