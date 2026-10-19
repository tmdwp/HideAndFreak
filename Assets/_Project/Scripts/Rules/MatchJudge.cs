using GuardVsMannequin.Core;

namespace GuardVsMannequin.Rules
{
    /// <summary>
    /// 승패 판정 결과: 다음 상태(없으면 계속), 판이 끝났다면 승리 진영과 사유.
    /// </summary>
    public readonly struct MatchVerdict
    {
        /// <summary>판정 없음. (현재 단계 계속)</summary>
        public static readonly MatchVerdict Continue = new(null, MatchWinner.None, WinReason.None);

        /// <summary>바꿀 다음 상태. null이면 그대로.</summary>
        public readonly GameState? NextState;

        /// <summary>판이 끝났을 때의 승리 진영.</summary>
        public readonly MatchWinner Winner;

        /// <summary>판이 끝났을 때의 사유.</summary>
        public readonly WinReason Reason;

        /// <summary>이 판정으로 판이 끝나는지 여부.</summary>
        public bool EndsMatch => NextState == GameState.Ended;

        private MatchVerdict(GameState? nextState, MatchWinner winner, WinReason reason)
        {
            NextState = nextState;
            Winner = winner;
            Reason = reason;
        }

        /// <summary>판을 끝내는 판정.</summary>
        public static MatchVerdict End(MatchWinner winner, WinReason reason) => new(GameState.Ended, winner, reason);

        /// <summary>다른 단계로 넘어가는 판정.</summary>
        public static MatchVerdict MoveTo(GameState state) => new(state, MatchWinner.None, WinReason.None);
    }

    /// <summary>
    /// 가드 vs 마네킹의 승패 조건과 사냥 시간 진입 조건을 판정한다. 값만 받아 결과를 반환하며 아무것도 바꾸지 않는다.
    /// - 가드 승리: 마네킹 전멸 / 제한 시간 내 목표 미달성 / 사냥 시간 종료까지 가드 생존
    /// - 마네킹 승리: 가드 전멸 (탐색 단계에서는 가드가 모두 나간 경우)
    /// - 사냥 시간 진입: 목표 달성, 또는 목표 0회일 때 탐색 시작 후 보호 시간이 지남
    /// </summary>
    public static class MatchJudge
    {
        /// <summary>
        /// 탐색 단계를 판정한다.
        /// </summary>
        /// <param name="aliveGuards">살아 있는 가드 수.</param>
        /// <param name="aliveMannequins">살아 있는 마네킹 수.</param>
        /// <param name="huntReady">사냥 시간에 들어갈 조건(목표 달성 또는 보호 시간 종료)을 채웠는지.</param>
        /// <param name="remainingTime">탐색 단계 남은 시간(초).</param>
        public static MatchVerdict JudgePlaying(int aliveGuards, int aliveMannequins, bool huntReady, float remainingTime)
        {
            if (aliveMannequins == 0)
                return MatchVerdict.End(MatchWinner.Guard, WinReason.AllMannequinsEliminated);

            // 가드가 모두 나간 경우(접속 종료)에는 마네킹 승리로 처리한다.
            if (aliveGuards == 0)
                return MatchVerdict.End(MatchWinner.Mannequin, WinReason.AllGuardsEliminated);

            if (huntReady)
                return MatchVerdict.MoveTo(GameState.Hunting);

            if (remainingTime <= 0f)
                return MatchVerdict.End(MatchWinner.Guard, WinReason.ObjectiveFailed);

            return MatchVerdict.Continue;
        }

        /// <summary>
        /// 사냥 시간을 판정한다.
        /// </summary>
        /// <param name="aliveGuards">살아 있는 가드 수.</param>
        /// <param name="aliveMannequins">살아 있는 마네킹 수.</param>
        /// <param name="remainingTime">사냥 시간 남은 시간(초).</param>
        public static MatchVerdict JudgeHunting(int aliveGuards, int aliveMannequins, float remainingTime)
        {
            if (aliveGuards == 0)
                return MatchVerdict.End(MatchWinner.Mannequin, WinReason.AllGuardsEliminated);

            if (aliveMannequins == 0)
                return MatchVerdict.End(MatchWinner.Guard, WinReason.AllMannequinsEliminated);

            if (remainingTime <= 0f)
                return MatchVerdict.End(MatchWinner.Guard, WinReason.GuardsSurvived);

            return MatchVerdict.Continue;
        }

        /// <summary>
        /// 사냥 시간 진입 조건: 목표가 있으면 달성 여부, 목표가 0회면 보호 시간 종료 여부.
        /// </summary>
        public static bool IsHuntReady(GameSettings settings, float remainingTime, bool objectiveComplete)
        {
            if (!HuntTiming.IsTimedHunt(settings))
                return objectiveComplete;

            return HuntTiming.GetProtectionRemaining(settings, remainingTime) <= 0f;
        }
    }
}
