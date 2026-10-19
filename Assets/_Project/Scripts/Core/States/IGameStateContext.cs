namespace GuardVsMannequin.Core.States
{
    /// <summary>
    /// 상태 핸들러가 상태 머신에 접근할 때 사용하는 최소한의 기능.
    /// 핸들러가 구체적인 NetworkBehaviour에 의존하지 않도록 분리한다(DIP, ISP).
    /// </summary>
    public interface IGameStateContext
    {
        /// <summary>이번 판에 확정된 게임 설정.</summary>
        GameSettings Settings { get; }

        /// <summary>현재 상태 타이머의 남은 시간(초).</summary>
        float RemainingTime { get; }

        /// <summary>
        /// 상태 타이머를 지정한 시간으로 시작한다.
        /// </summary>
        void StartTimer(float seconds);

        /// <summary>
        /// 상태 타이머를 멈춘다.
        /// </summary>
        void StopTimer();
    }
}
