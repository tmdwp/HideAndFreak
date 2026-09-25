using GuardVsMannequin.Core;
using GuardVsMannequin.Core.States;
using GuardVsMannequin.Objectives;
using GuardVsMannequin.Player;
using UnityEngine;

namespace GuardVsMannequin.Rules
{
    /// <summary>
    /// 가드 vs 마네킹의 승패 조건과 사냥 시간 진입 조건. (서버에서만 호출됨)
    /// - 가드 승리: 마네킹 전멸 / 제한 시간 내 목표 미달성 / 사냥 시간 종료까지 가드 생존
    /// - 마네킹 승리: 사냥 시간 중 가드 전멸
    /// - 사냥 시간 진입: 목표 달성, 또는 목표 0회일 때 탐색 시작 20초 후
    /// </summary>
    public class MatchRules : MonoBehaviour, IMatchRules
    {
        [SerializeField] private PlayerRegistry playerRegistry;
        [SerializeField] private ObjectiveTracker objectiveTracker;
        [SerializeField] private MatchResult matchResult;

        /// <summary>
        /// 탐색 단계의 전환 조건을 평가한다.
        /// </summary>
        public GameState? EvaluatePlaying(IGameStateContext context)
        {
            if (playerRegistry.AliveMannequinCount == 0)
                return End(MatchWinner.Guard, WinReason.AllMannequinsEliminated);

            // 가드가 모두 나간 경우(접속 종료)에는 마네킹 승리로 처리한다.
            if (playerRegistry.AliveGuardCount == 0)
                return End(MatchWinner.Mannequin, WinReason.AllGuardsEliminated);

            if (ShouldStartHunting(context))
                return GameState.Hunting;

            if (context.RemainingTime <= 0f)
                return End(MatchWinner.Guard, WinReason.ObjectiveFailed);

            return null;
        }

        /// <summary>
        /// 사냥 시간의 전환 조건을 평가한다.
        /// </summary>
        public GameState? EvaluateHunting(IGameStateContext context)
        {
            if (playerRegistry.AliveGuardCount == 0)
                return End(MatchWinner.Mannequin, WinReason.AllGuardsEliminated);

            if (playerRegistry.AliveMannequinCount == 0)
                return End(MatchWinner.Guard, WinReason.AllMannequinsEliminated);

            if (context.RemainingTime <= 0f)
                return End(MatchWinner.Guard, WinReason.GuardsSurvived);

            return null;
        }

        /// <summary>
        /// 목표를 달성했거나, 목표가 0회이고 탐색 시작 후 일정 시간이 지났는지 확인한다.
        /// </summary>
        private bool ShouldStartHunting(IGameStateContext context)
        {
            if (!HuntTiming.IsTimedHunt(context.Settings))
                return objectiveTracker.IsComplete;

            return HuntTiming.GetProtectionRemaining(context.Settings, context.RemainingTime) <= 0f;
        }

        /// <summary>
        /// 결과를 기록하고 종료 상태를 반환한다.
        /// </summary>
        private GameState End(MatchWinner winner, WinReason reason)
        {
            matchResult.SetResult(winner, reason);
            return GameState.Ended;
        }
    }
}
