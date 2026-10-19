namespace GuardVsMannequin.Editor
{
    /// <summary>
    /// 에디터 상단 메뉴 경로. 게임 개발 도구와 모드 SDK 도구를 한 메뉴(GameSettingTools) 아래에 모은다.
    /// (모드 SDK에도 들어가는 어셈블리라 게임 에디터 어셈블리도 이 값을 함께 쓴다)
    /// </summary>
    public static class EditorMenus
    {
        /// <summary>최상위 메뉴.</summary>
        public const string Root = "GameSettingTools/";

        /// <summary>모드 제작 도구 메뉴. (모드 SDK에서 쓰는 메뉴)</summary>
        public const string Mod = Root + "모드 제작/";
    }
}
