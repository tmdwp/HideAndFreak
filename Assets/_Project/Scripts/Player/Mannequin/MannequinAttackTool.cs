using FishNet.Object;
using GuardVsMannequin.Audio;
using GuardVsMannequin.Core;
using GuardVsMannequin.Localization;
using UnityEngine;

namespace GuardVsMannequin.Player.Mannequin
{
    /// <summary>
    /// 사냥 시간 마네킹의 근접 공격. 좌클릭 시 서버가 공격 범위(호스트 설정) 안의 가드를 판정한다.
    /// 탐색 단계의 좌클릭(전원 장치 파괴)과는 게임 상태로 구분된다.
    /// </summary>
    [RequireComponent(typeof(CameraAimProvider), typeof(NoiseEmitter))]
    public class MannequinAttackTool : NetworkBehaviour, ITool
    {
        private const float CooldownTolerance = 0.15f;
        private const float MaxOriginOffset = 3f;

        private readonly RaycastHit[] hitBuffer = new RaycastHit[16];

        private IAimProvider aimProvider;
        private IPlayerInput playerInput;
        private NoiseEmitter noiseEmitter;
        private float localNextAttackTime;
        private float serverNextAttackTime;

        public string DisplayName => Loc.Get("tool.attack");
        public float CooldownRemaining => Mathf.Max(0f, localNextAttackTime - Time.time);
        public bool IsReady => CooldownRemaining <= 0f && GameServices.CurrentStateOrWaiting == GameState.Hunting;

        /// <summary>호스트 설정의 공격 범위.</summary>
        private static float AttackRange => GameServices.State?.Settings.mannequinAttackRange ?? 2f;

        /// <summary>
        /// 구성 요소를 캐시한다.
        /// </summary>
        private void Awake()
        {
            aimProvider = GetComponent<IAimProvider>();
            playerInput = GetComponent<IPlayerInput>();
            noiseEmitter = GetComponent<NoiseEmitter>();
        }

        /// <summary>
        /// 소유자만 좌클릭 입력을 구독한다.
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
        /// 사냥 시간이고 쿨타임이 끝났으면 조준 방향으로 공격을 요청한다.
        /// </summary>
        private void OnPrimaryPressed()
        {
            if (!IsReady)
                return;

            Ray aim = aimProvider.GetAimRay();
            localNextAttackTime = Time.time + GameConstants.MannequinAttackCooldown;
            AttackServerRpc(aim.origin, aim.direction);
        }

        /// <summary>
        /// 서버에서 공격을 검증하고, 범위 안의 첫 대상에게 피해를 준다.
        /// </summary>
        [ServerRpc]
        private void AttackServerRpc(Vector3 origin, Vector3 direction)
        {
            if (GameServices.CurrentStateOrWaiting != GameState.Hunting)
                return;
            if (Time.time < serverNextAttackTime - CooldownTolerance)
                return;
            serverNextAttackTime = Time.time + GameConstants.MannequinAttackCooldown;

            if ((origin - transform.position).sqrMagnitude > MaxOriginOffset * MaxOriginOffset)
                origin = transform.position + Vector3.up * 1.5f;

            IDamageable target = FindTarget(new Ray(origin, direction.normalized));
            target?.TakeDamage(GameConstants.Damage, NetworkObject);

            AttackObserversRpc();
        }

        /// <summary>
        /// 구체 판정으로 범위 안에서 자기 자신을 제외한 가장 가까운 피해 대상을 찾는다.
        /// 벽 등 다른 물체가 먼저 막으면 대상이 없는 것으로 본다.
        /// </summary>
        private IDamageable FindTarget(Ray ray)
        {
            int count = Physics.SphereCastNonAlloc(ray, GameConstants.MannequinAttackRadius, hitBuffer, AttackRange,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

            RaycastHit? closest = null;
            for (int i = 0; i < count; i++)
            {
                if (hitBuffer[i].transform.IsChildOf(transform))
                    continue;
                if (closest == null || hitBuffer[i].distance < closest.Value.distance)
                    closest = hitBuffer[i];
            }

            return closest?.collider.GetComponentInParent<IDamageable>();
        }

        /// <summary>
        /// 모든 클라이언트에서 공격 소리를 재생한다.
        /// </summary>
        [ObserversRpc]
        private void AttackObserversRpc()
        {
            if (noiseEmitter != null)
                noiseEmitter.PlayLocal(NoiseType.MannequinAttack);
        }
    }
}
