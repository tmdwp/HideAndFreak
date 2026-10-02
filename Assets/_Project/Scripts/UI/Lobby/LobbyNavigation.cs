namespace GuardVsMannequin.UI
{
    /// <summary>
    /// 로비 화면(패널) 종류.
    /// </summary>
    public enum LobbyScreen
    {
        Main,
        Create,
        SteamCreate,
        Join,
        SteamJoin,
        DirectJoin
    }

    /// <summary>
    /// 로비 화면 이동. 각 메뉴 코드(메인/방 만들기/참가)는 이 인터페이스로만 화면을 바꾼다.
    /// </summary>
    public interface ILobbyNavigator
    {
        /// <summary>화면 하나만 보이게 한다.</summary>
        void Show(LobbyScreen screen);

        /// <summary>이전 화면으로 돌아간다.</summary>
        void Back();

        /// <summary>설정 화면을 연다.</summary>
        void OpenSettings();

        /// <summary>크레딧 화면을 연다.</summary>
        void OpenCredits();
    }
}
