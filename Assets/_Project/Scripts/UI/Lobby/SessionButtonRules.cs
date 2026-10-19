using GuardVsMannequin.Network;

namespace GuardVsMannequin.UI
{
    /// <summary>
    /// 로비 버튼을 누를 수 있는 조건. (방 만들기·참가 메뉴가 함께 쓴다)
    /// </summary>
    public static class SessionButtonRules
    {
        /// <summary>Steam 화면으로 들어가는 버튼: Steam을 쓸 수 있을 때.</summary>
        public static bool CanOpenSteamMenu(this ISessionService session)
        {
            return session.IsSteamAvailable;
        }

        /// <summary>Steam 방 만들기/입장: 진행 중인 작업이 없고 Steam을 쓸 수 있을 때.</summary>
        public static bool CanConnectSteam(this ISessionService session)
        {
            return !session.IsBusy && session.IsSteamAvailable;
        }

        /// <summary>직접 방 만들기/IP 참가: 진행 중인 작업이 없을 때.</summary>
        public static bool CanConnectDirect(this ISessionService session)
        {
            return !session.IsBusy;
        }
    }
}
