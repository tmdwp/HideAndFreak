namespace GuardVsMannequin.Core.States
{
    /// <summary>
    /// 정해진 시간이 지나면 다음 상태로 넘어가는 상태의 공통 구현.
    /// </summary>
    public abstract class TimedStateHandler : IGameStateHandler
    {
        public abstract GameState State { get; }

        /// <summary>시간이 다 되었을 때 넘어갈 상태.</summary>
        protected abstract GameState NextState { get; }

        /// <summary>
        /// 이 상태의 지속 시간을 설정에서 계산한다.
        /// </summary>
        protected abstract float GetDuration(GameSettings settings);

        /// <summary>
        /// 상태 진입 시 지속 시간만큼 타이머를 시작한다.
        /// </summary>
        public virtual void Enter(IGameStateContext context)
        {
            context.StartTimer(GetDuration(context.Settings));
        }

        /// <summary>
        /// 타이머가 끝나면 다음 상태를 반환한다.
        /// </summary>
        public virtual GameState? Tick(IGameStateContext context, float deltaTime)
        {
            return context.RemainingTime <= 0f ? NextState : null;
        }

        /// <summary>
        /// 상태 종료 시 타이머를 멈춘다.
        /// </summary>
        public virtual void Exit(IGameStateContext context)
        {
            context.StopTimer();
        }
    }
}
