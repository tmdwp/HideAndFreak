namespace GuardVsMannequin.Core
{
    /// <summary>
    /// 판 진행을 조작하는 명령. (호스트만 실행할 수 있음)
    /// </summary>
    public interface IMatchControl
    {
        /// <summary>이 피어가 판을 조작할 수 있는지(서버인지) 여부.</summary>
        bool CanControlMatch { get; }

        /// <summary>판을 끝내고 모두를 룸으로 돌려보낸다. 조작할 수 없으면 아무것도 하지 않는다.</summary>
        void ReturnToRoom();
    }
}
