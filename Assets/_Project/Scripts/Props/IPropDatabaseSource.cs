namespace GuardVsMannequin.Props
{
    /// <summary>
    /// 지금 쓸 변신 사물 목록을 알려준다. (맵 전용 목록, 기본 목록, 나중에는 모드 맵의 목록)
    /// </summary>
    public interface IPropDatabaseSource
    {
        /// <summary>현재 사물 목록. 없으면 null.</summary>
        PropDatabase Database { get; }
    }
}
