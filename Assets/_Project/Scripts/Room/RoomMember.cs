namespace GuardVsMannequin.Room
{
    /// <summary>
    /// 룸 참가자 한 명의 정보. 네트워크로 동기화되므로 값 타입 필드만 사용한다.
    /// </summary>
    public struct RoomMember
    {
        public string displayName;
        public bool isReady;
        public bool isHost;
    }
}
