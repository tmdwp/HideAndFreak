using System.Collections.Generic;
using GuardVsMannequin.Cameras;
using GuardVsMannequin.Core;
using GuardVsMannequin.Objectives;
using GuardVsMannequin.Player;

namespace GuardVsMannequin.UI
{
    /// <summary>
    /// HUD 영역들이 함께 읽는 게임 상태와 로컬 플레이어 정보.
    /// 씬 오브젝트 참조는 <see cref="GameHud"/>가 넘겨주고, 로컬 플레이어는 이 객체가 이벤트로 추적한다.
    /// </summary>
    public sealed class HudContext
    {
        private static readonly ITool[] NoTools = new ITool[0];

        public GameStateMachine StateMachine { get; }
        public PlayerRegistry PlayerRegistry { get; }
        public ObjectiveTracker ObjectiveTracker { get; }
        public MatchResult MatchResult { get; }

        /// <summary>관전 카메라. (없을 수 있음)</summary>
        public SpectatorController Spectator { get; }

        /// <summary>살아 있는 로컬 아바타. 사망·미참가면 null.</summary>
        public PlayerIdentity LocalPlayer { get; private set; }
        public PlayerHealth LocalHealth { get; private set; }
        public IReadOnlyList<ITool> LocalTools { get; private set; } = NoTools;

        /// <summary>이번 판의 로컬 역할. 사망 후 안내·결과에 쓰려고 아바타가 사라져도 유지한다.</summary>
        public PlayerRole LastLocalRole { get; private set; } = PlayerRole.None;

        public HudContext(GameStateMachine stateMachine, PlayerRegistry playerRegistry, ObjectiveTracker objectiveTracker,
            MatchResult matchResult, SpectatorController spectator)
        {
            StateMachine = stateMachine;
            PlayerRegistry = playerRegistry;
            ObjectiveTracker = objectiveTracker;
            MatchResult = matchResult;
            Spectator = spectator;
        }

        /// <summary>
        /// 로컬 플레이어 이벤트를 구독한다.
        /// </summary>
        public void Attach()
        {
            LocalPlayerEvents.Spawned += OnLocalPlayerSpawned;
            LocalPlayerEvents.Despawned += OnLocalPlayerDespawned;
        }

        /// <summary>
        /// 이벤트 구독을 해제한다.
        /// </summary>
        public void Detach()
        {
            LocalPlayerEvents.Spawned -= OnLocalPlayerSpawned;
            LocalPlayerEvents.Despawned -= OnLocalPlayerDespawned;
        }

        /// <summary>
        /// 판이 끝나 대기 상태가 되면 지난 판의 역할을 지운다.
        /// </summary>
        public void ResetRole()
        {
            LastLocalRole = PlayerRole.None;
        }

        /// <summary>
        /// 로컬 아바타의 체력과 도구를 캐시한다.
        /// </summary>
        private void OnLocalPlayerSpawned(PlayerIdentity identity)
        {
            LocalPlayer = identity;
            LocalHealth = identity.GetComponent<PlayerHealth>();
            LocalTools = identity.GetComponents<ITool>();
            LastLocalRole = identity.Role;
        }

        /// <summary>
        /// 로컬 아바타 참조를 비운다. 역할은 사망 후 안내를 위해 유지한다.
        /// </summary>
        private void OnLocalPlayerDespawned(PlayerIdentity identity)
        {
            if (LocalPlayer != identity)
                return;

            LocalPlayer = null;
            LocalHealth = null;
            LocalTools = NoTools;
        }
    }
}
