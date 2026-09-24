namespace GuardVsMannequin.Core.States
{
    /// <summary>
    /// 하나의 <see cref="GameState"/>에서의 서버 로직을 담당한다.
    /// 새 규칙은 핸들러를 추가하거나 교체해서 확장한다(OCP).
    /// </summary>
    public interface IGameStateHandler
    {
        /// <summary>이 핸들러가 담당하는 상태.</summary>
        GameState State { get; }

        /// <summary>
        /// 상태에 진입할 때 호출된다.
        /// </summary>
        void Enter(IGameStateContext context);

        /// <summary>
        /// 매 프레임 호출된다. 다음 상태로 넘어가야 하면 그 상태를, 아니면 null을 반환한다.
        /// </summary>
        GameState? Tick(IGameStateContext context, float deltaTime);

        /// <summary>
        /// 상태에서 나갈 때 호출된다.
        /// </summary>
        void Exit(IGameStateContext context);
    }
}
