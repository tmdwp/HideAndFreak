namespace GuardVsMannequin.Core.States
{
    /// <summary>
    /// 탐색/사냥 단계에서 승패 판정과 사냥 시간 진입을 결정하는 규칙.
    /// 상태 핸들러는 이 인터페이스에만 의존하고, 실제 규칙은 교체할 수 있다(DIP, OCP).
    /// </summary>
    public interface IMatchRules
    {
        /// <summary>
        /// 탐색 단계 진행 중 호출된다. 전환할 상태가 있으면 반환하고 없으면 null.
        /// </summary>
        GameState? EvaluatePlaying(IGameStateContext context);

        /// <summary>
        /// 사냥 시간 진행 중 호출된다. 전환할 상태가 있으면 반환하고 없으면 null.
        /// </summary>
        GameState? EvaluateHunting(IGameStateContext context);
    }

    /// <summary>
    /// 시간만으로 단계를 넘기는 기본 규칙. 규칙 컴포넌트가 연결되지 않았을 때 사용한다.
    /// </summary>
    public sealed class TimeOnlyMatchRules : IMatchRules
    {
        /// <summary>제한 시간이 끝나면 종료한다.</summary>
        public GameState? EvaluatePlaying(IGameStateContext context)
        {
            return context.RemainingTime <= 0f ? GameState.Ended : null;
        }

        /// <summary>사냥 시간이 끝나면 종료한다.</summary>
        public GameState? EvaluateHunting(IGameStateContext context)
        {
            return context.RemainingTime <= 0f ? GameState.Ended : null;
        }
    }
}
