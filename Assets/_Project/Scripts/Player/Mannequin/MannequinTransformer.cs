using System;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using GuardVsMannequin.Core;
using GuardVsMannequin.Localization;
using GuardVsMannequin.Player.Emotes;
using GuardVsMannequin.Props;
using UnityEngine;

namespace GuardVsMannequin.Player.Mannequin
{
    /// <summary>
    /// 마네킹의 변신 상태를 관리한다.
    /// 우클릭으로 조준한 사물로 변신하거나 변신을 해제하며, 상태는 PropId 하나로 동기화된다.
    /// 외형 적용은 <see cref="PropDisguiseView"/>가 담당한다(SRP).
    /// </summary>
    [RequireComponent(typeof(CameraAimProvider))]
    public class MannequinTransformer : NetworkBehaviour, ITool, IEmoteBlocker
    {
        private const float CooldownTolerance = 0.15f;

        [SerializeField] private PropDatabase propDatabase;

        private readonly SyncVar<int> currentPropId = new(PropDatabase.NoProp);

        private IAimProvider aimProvider;
        private IPlayerInput playerInput;
        private float localCooldownEnd;
        private float serverCooldownEnd;

        /// <summary>변신 상태가 바뀌었을 때. 인자는 새 PropId(해제 시 NoProp). 각 피어에서 한 번씩 호출된다.</summary>
        public event Action<int> DisguiseChanged;

        /// <summary>외형 조회에 사용할 데이터베이스.</summary>
        public PropDatabase Database => propDatabase;

        /// <summary>현재 변신한 PropId. 변신하지 않았으면 NoProp.</summary>
        public int CurrentPropId => currentPropId.Value;

        /// <summary>변신 중인지 여부.</summary>
        public bool IsDisguised => currentPropId.Value != PropDatabase.NoProp;

        /// <summary>변신 중에는 감정 표현을 할 수 없다.</summary>
        public bool IsEmoteBlocked => IsDisguised;

        public string DisplayName => Loc.Get("tool.disguise");
        public float CooldownRemaining => Mathf.Max(0f, localCooldownEnd - Time.time);
        public bool IsReady => CooldownRemaining <= 0f && CanTransformIn(GameServices.CurrentStateOrWaiting);

        /// <summary>
        /// 입력/조준 구현을 캐시하고 동기화 콜백을 연결한다.
        /// </summary>
        private void Awake()
        {
            aimProvider = GetComponent<IAimProvider>();
            playerInput = GetComponent<IPlayerInput>();
            currentPropId.OnChange += OnPropIdChanged;
        }

        /// <summary>
        /// 동기화 콜백을 해제한다.
        /// </summary>
        private void OnDestroy()
        {
            currentPropId.OnChange -= OnPropIdChanged;
        }

        /// <summary>
        /// 소유자만 우클릭 입력을 구독한다.
        /// </summary>
        public override void OnStartClient()
        {
            base.OnStartClient();
            if (IsOwner && playerInput != null)
                playerInput.SecondaryPressed += OnSecondaryPressed;
        }

        /// <summary>
        /// 입력 구독을 해제한다.
        /// </summary>
        public override void OnStopClient()
        {
            base.OnStopClient();
            if (IsOwner && playerInput != null)
                playerInput.SecondaryPressed -= OnSecondaryPressed;
        }

        /// <summary>
        /// 변신 중이면 해제를, 아니면 조준한 사물로 변신을 요청한다.
        /// </summary>
        private void OnSecondaryPressed()
        {
            if (IsDisguised)
            {
                localCooldownEnd = Time.time + GetUntransformCooldown(GameServices.CurrentStateOrWaiting);
                UntransformServerRpc();
                return;
            }

            if (!IsReady)
                return;

            if (TryFindTargetProp(out int propId))
                TransformServerRpc(propId);
        }

        /// <summary>
        /// 화면 중앙 광선으로 변신 가능한 사물을 찾는다. 몸에서 너무 먼 사물은 제외한다.
        /// </summary>
        private bool TryFindTargetProp(out int propId)
        {
            propId = PropDatabase.NoProp;
            Ray aim = aimProvider.GetAimRay();

            // 3인칭 카메라가 몸 뒤에 있으므로 카메라-몸 거리만큼 사거리를 늘린다.
            float cameraDistance = Vector3.Distance(aim.origin, transform.position);
            if (!AimRaycaster.TryRaycast(aim, GameConstants.TransformRange + cameraDistance, transform, out RaycastHit hit))
                return false;
            if (Vector3.Distance(hit.point, transform.position) > GameConstants.TransformRange)
                return false;

            TransformableProp prop = hit.collider.GetComponentInParent<TransformableProp>();
            if (prop == null || !propDatabase.IsValid(prop.PropId))
                return false;

            propId = prop.PropId;
            return true;
        }

        /// <summary>
        /// 서버에서 변신 요청을 검증하고 적용한다.
        /// </summary>
        [ServerRpc]
        private void TransformServerRpc(int propId)
        {
            if (IsDisguised || !propDatabase.IsValid(propId))
                return;
            if (!CanTransformIn(GameServices.CurrentStateOrWaiting))
                return;
            if (Time.time < serverCooldownEnd - CooldownTolerance)
                return;

            currentPropId.Value = propId;
        }

        /// <summary>
        /// 서버에서 변신을 해제하고 해제 쿨타임을 시작한다.
        /// </summary>
        [ServerRpc]
        private void UntransformServerRpc()
        {
            if (!IsDisguised)
                return;

            currentPropId.Value = PropDatabase.NoProp;
            serverCooldownEnd = Time.time + GetUntransformCooldown(GameServices.CurrentStateOrWaiting);
        }

        /// <summary>
        /// 쿨타임 없이 강제로 변신을 해제한다. 사냥 시간 진입 시 사용하며, 이후 다시 변신할 수 있다.
        /// </summary>
        [Server]
        public void ForceUntransform()
        {
            currentPropId.Value = PropDatabase.NoProp;
        }

        /// <summary>
        /// 변신이 가능한 상태인지 확인한다. (준비/탐색/사냥 시간 가능)
        /// </summary>
        private static bool CanTransformIn(GameState state)
        {
            return state == GameState.Preparation || state == GameState.Playing || state == GameState.Hunting;
        }

        /// <summary>
        /// 변신 해제 쿨타임을 반환한다. 가드 대기 시간에는 쿨타임이 없다.
        /// </summary>
        private static float GetUntransformCooldown(GameState state)
        {
            return state == GameState.Preparation ? 0f : GameConstants.UntransformCooldown;
        }

        /// <summary>
        /// PropId 변경을 피어마다 한 번만 알린다. (호스트는 클라이언트 콜백에서만 처리)
        /// </summary>
        private void OnPropIdChanged(int prev, int next, bool asServer)
        {
            if (asServer && IsClientInitialized)
                return;

            DisguiseChanged?.Invoke(next);
        }
    }
}
