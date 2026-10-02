namespace GuardVsMannequin.UI
{
    /// <summary>
    /// 룸 화면 하단 상태 줄에 잠시 안내 문구를 띄운다. (예: 방 코드 복사됨, 시작할 수 없는 이유)
    /// </summary>
    public interface IRoomNotice
    {
        /// <summary>안내 문구를 잠시 표시한다.</summary>
        void ShowNotice(string message);
    }
}
