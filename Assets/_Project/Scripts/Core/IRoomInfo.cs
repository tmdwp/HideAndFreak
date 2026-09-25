namespace GuardVsMannequin.Core
{
    /// <summary>
    /// 게임 씬이 룸에서 필요로 하는 정보. (룸 구현에 직접 의존하지 않도록 분리, DIP)
    /// </summary>
    public interface IRoomInfo
    {
        /// <summary>호스트가 룸에서 정한 게임 설정.</summary>
        GameSettings Settings { get; }

        /// <summary>
        /// 클라이언트의 표시 이름을 반환한다. 모르면 "Player N".
        /// </summary>
        string GetDisplayName(int clientId);
    }

    /// <summary>
    /// 게임이 끝났을 때 룸으로 돌아가는 흐름. (서버 전용)
    /// </summary>
    public interface ISceneFlow
    {
        /// <summary>
        /// 모든 플레이어를 룸 씬으로 돌려보낸다.
        /// </summary>
        void ReturnToRoom();
    }
}
