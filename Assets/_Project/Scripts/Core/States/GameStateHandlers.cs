namespace GuardVsMannequin.Core.States
{
    /// <summary>
    /// 접속 대기 상태. 호스트가 시작을 요청할 때까지 아무것도 하지 않는다.
    /// </summary>
    public sealed class WaitingStateHandler : IGameStateHandler
    {
        public GameState State => GameState.Waiting;

        /// <summary>
        /// 대기 상태에서는 타이머를 사용하지 않는다.
        /// </summary>
        public void Enter(IGameStateContext context)
        {
            context.StopTimer();
        }

        /// <summary>
        /// 외부 요청으로만 상태가 바뀌므로 항상 null을 반환한다.
        /// </summary>
        public GameState? Tick(IGameStateContext context, float deltaTime)
        {
            return null;
        }

        /// <summary>
        /// 종료 시 처리할 것이 없다.
        /// </summary>
        public void Exit(IGameStateContext context) { }
    }

    /// <summary>
    /// 가드 대기 시간. 끝나면 탐색 단계로 넘어간다.
    /// </summary>
    public sealed class PreparationStateHandler : TimedStateHandler
    {
        public override GameState State => GameState.Preparation;
        protected override GameState NextState => GameState.Playing;

        /// <summary>
        /// 가드 대기 시간을 반환한다.
        /// </summary>
        protected override float GetDuration(GameSettings settings) => settings.preparationTime;
    }

    /// <summary>
    /// 탐색 단계. 제한 시간 타이머를 시작하고, 전환 여부는 <see cref="IMatchRules"/>에 위임한다.
    /// </summary>
    public sealed class PlayingStateHandler : IGameStateHandler
    {
        private readonly IMatchRules rules;

        public GameState State => GameState.Playing;

        /// <summary>
        /// 승패 규칙을 주입받는다.
        /// </summary>
        public PlayingStateHandler(IMatchRules rules)
        {
            this.rules = rules;
        }

        /// <summary>제한 시간 타이머를 시작한다.</summary>
        public void Enter(IGameStateContext context) => context.StartTimer(context.Settings.playTime);

        /// <summary>규칙에 따라 사냥 시간 또는 종료로 전환한다.</summary>
        public GameState? Tick(IGameStateContext context, float deltaTime) => rules.EvaluatePlaying(context);

        /// <summary>타이머를 멈춘다.</summary>
        public void Exit(IGameStateContext context) => context.StopTimer();
    }

    /// <summary>
    /// 사냥 시간. 사냥 시간 타이머를 시작하고, 전환 여부는 <see cref="IMatchRules"/>에 위임한다.
    /// </summary>
    public sealed class HuntingStateHandler : IGameStateHandler
    {
        private readonly IMatchRules rules;

        public GameState State => GameState.Hunting;

        /// <summary>
        /// 승패 규칙을 주입받는다.
        /// </summary>
        public HuntingStateHandler(IMatchRules rules)
        {
            this.rules = rules;
        }

        /// <summary>사냥 시간 타이머를 시작한다.</summary>
        public void Enter(IGameStateContext context) => context.StartTimer(context.Settings.huntTime);

        /// <summary>규칙에 따라 종료로 전환한다.</summary>
        public GameState? Tick(IGameStateContext context, float deltaTime) => rules.EvaluateHunting(context);

        /// <summary>타이머를 멈춘다.</summary>
        public void Exit(IGameStateContext context) => context.StopTimer();
    }

    /// <summary>
    /// 결과 표시 단계. 일정 시간 후 대기 상태로 돌아간다.
    /// (Phase 4에서 룸 씬 이동으로 대체된다.)
    /// </summary>
    public sealed class EndedStateHandler : TimedStateHandler
    {
        public override GameState State => GameState.Ended;
        protected override GameState NextState => GameState.Waiting;

        /// <summary>
        /// 결과 표시 시간을 반환한다.
        /// </summary>
        protected override float GetDuration(GameSettings settings) => GameConstants.ResultDisplayDuration;
    }
}
