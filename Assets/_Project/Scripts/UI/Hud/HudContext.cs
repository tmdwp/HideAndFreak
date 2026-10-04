using GuardVsMannequin.Cameras;
using GuardVsMannequin.Core;
using GuardVsMannequin.Objectives;
using GuardVsMannequin.Player;

namespace GuardVsMannequin.UI
{
    /// <summary>
    /// HUD 영역들이 함께 읽는 게임 정보. <see cref="GameHud"/>가 씬 연결로 만들어 영역들에 넘긴다.
    /// 게임 상태는 읽기 전용(<see cref="IGameStateProvider"/>)으로, 판 조작은 <see cref="IMatchControl"/>로만 노출한다.
    /// </summary>
    public sealed class HudContext
    {
        /// <summary>현재 단계, 설정, 남은 시간.</summary>
        public IGameStateProvider State { get; }

        /// <summary>룸으로 돌아가기 등 판 조작. (호스트만 가능)</summary>
        public IMatchControl MatchControl { get; }

        public PlayerRegistry PlayerRegistry { get; }
        public ObjectiveTracker ObjectiveTracker { get; }
        public MatchResult MatchResult { get; }

        /// <summary>관전 카메라. (없을 수 있음)</summary>
        public SpectatorController Spectator { get; }

        /// <summary>로컬 플레이어 아바타와 이번 판 역할.</summary>
        public LocalPlayerTracker LocalPlayer { get; }

        public HudContext(IGameStateProvider state, IMatchControl matchControl, PlayerRegistry playerRegistry,
            ObjectiveTracker objectiveTracker, MatchResult matchResult, SpectatorController spectator, LocalPlayerTracker localPlayer)
        {
            State = state;
            MatchControl = matchControl;
            PlayerRegistry = playerRegistry;
            ObjectiveTracker = objectiveTracker;
            MatchResult = matchResult;
            Spectator = spectator;
            LocalPlayer = localPlayer;
        }
    }
}
