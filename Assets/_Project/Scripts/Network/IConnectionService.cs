namespace GuardVsMannequin.Network
{
    /// <summary>
    /// 방 생성/참가/종료를 추상화한다.
    /// 개발 중에는 직접 접속(Tugboat), Phase 5에서는 Steam 로비 구현으로 교체한다(DIP).
    /// </summary>
    public interface IConnectionService
    {
        /// <summary>서버(호스트)가 실행 중인지 여부.</summary>
        bool IsServerRunning { get; }

        /// <summary>클라이언트가 접속 중인지 여부.</summary>
        bool IsClientRunning { get; }

        /// <summary>
        /// 서버와 로컬 클라이언트를 함께 시작한다.
        /// </summary>
        void StartHost();

        /// <summary>
        /// 지정한 주소의 호스트에 참가한다.
        /// </summary>
        void StartClient(string address);

        /// <summary>
        /// 접속을 종료한다. 호스트라면 서버도 함께 종료한다.
        /// </summary>
        void Stop();
    }
}
