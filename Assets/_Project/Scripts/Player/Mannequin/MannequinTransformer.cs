using System;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using GuardVsMannequin.Core;
using GuardVsMannequin.Localization;
using GuardVsMannequin.Map;
using GuardVsMannequin.Player.Emotes;
using GuardVsMannequin.Props;
using UnityEngine;

namespace GuardVsMannequin.Player.Mannequin
{
    /// <summary>
    /// 마네킹의 변신 상태를 관리한다.
    /// 우클릭으로 조준한 사물로 변신하거나 변신을 해제하며, 상태는 PropId 하나로 동기화된다.
    /// 맡은 일은 입력 → 서버 검증 → 동기화뿐이고, 나머지는 나눠 맡긴다:
    /// 조준한 사물 찾기 <see cref="PropTargetFinder"/>, 사물 목록 결정 <see cref="IPropDatabaseSource"/>,
    /// 쿨타임 <see cref="ToolCooldown"/>, 외형 적용 <see cref="PropDisguiseView"/>.
    /// </summary>
    [RequireComponent(typeof(CameraAimProvider))]
    public class MannequinTransformer : NetworkBehaviour, ITool, IEmoteBlocker
    {
        [Tooltip("맵이 사물 목록을 지정하지 않았을 때 쓸 기본 목록.")]
        [SerializeField] private PropDatabase propDatabase;

        private readonly SyncVar<int> currentPropId = new(PropDatabase.NoProp);
        private readonly ToolCooldown cooldown = new();

        private IPlayerInput playerInput;
        private IPropDatabaseSource databaseSource;
        private PropTargetFinder targetFinder;

        /// <summary>변신 상태가 바뀌었을 때. 인자는 새 PropId(해제 시 NoProp). 각 피어에서 한 번씩 호출된다.</summary>
        public event Action<int> DisguiseChanged;

        /// <summary>외형 조회에 사용할 사물 목록. 맵이 목록을 지정하면 그것을, 아니면 프리팹의 기본 목록을 쓴다.</summary>
        public PropDatabase Database => databaseSource.Database;

        /// <summary>현재 변신한 PropId. 변신하지 않았으면 NoProp.</summary>
        public int CurrentPropId => currentPropId.Value;

        /// <summary>변신 중인지 여부.</summary>
        public bool IsDisguised => currentPropId.Value != PropDatabase.NoProp;

        /// <summary>변신 중에는 감정 표현을 할 수 없다.</summary>
        public bool IsEmoteBlocked => IsDisguised;

        public string DisplayName => Loc.Get("tool.disguise");
        public float CooldownRemaining => cooldown.LocalRemaining;
        public bool IsReady => CooldownRemaining <= 0f && CanTransformIn(GameServices.CurrentStateOrWaiting);

        /// <summary>
        /// 입력·사물 찾기 구현을 만들고 동기화 콜백을 연결한다.
        /// </summary>
        private void Awake()
        {
            playerInput = GetComponent<IPlayerInput>();
            databaseSource = new MapPropDatabaseSource(propDatabase);
            targetFinder = new PropTargetFinder(transform, GetComponent<IAimProvider>(), databaseSource, GameTuning.TransformRange);
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
                cooldown.StartLocal(GameTuning.UntransformCooldown);
                UntransformServerRpc();
                return;
            }

            if (!IsReady)
                return;

            if (TryGetAimedProp(out TransformableProp prop))
                TransformServerRpc(prop.PropId);
        }

        /// <summary>
        /// 지금 조준하고 있는 변신 가능 사물을 찾는다. (변신 사거리 안, 사물 목록에 있는 것만)
        /// 외곽선 표시(<see cref="PropHighlighter"/>)도 같은 판정을 쓴다.
        /// </summary>
        public bool TryGetAimedProp(out TransformableProp prop)
        {
            return targetFinder.TryFind(out prop);
        }

        /// <summary>
        /// 서버에서 변신 요청을 검증하고 적용한다.
        /// </summary>
        [ServerRpc]
        private void TransformServerRpc(int propId)
        {
            PropDatabase database = Database;
            if (IsDisguised || database == null || !database.IsValid(propId))
                return;
            if (!CanTransformIn(GameServices.CurrentStateOrWaiting))
                return;
            if (!cooldown.IsServerReady)
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
            cooldown.StartServer(GameTuning.UntransformCooldown);
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
        /// 변신이 가능한 상태인지 확인한다. (탐색/사냥 시간만, 판 시작 준비 중에는 불가)
        /// </summary>
        private static bool CanTransformIn(GameState state)
        {
            return state == GameState.Playing || state == GameState.Hunting;
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
