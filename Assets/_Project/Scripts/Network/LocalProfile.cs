using Steamworks;

namespace GuardVsMannequin.Network
{
    /// <summary>
    /// 이 PC 플레이어의 표시 이름. Steam이 켜져 있으면 Steam 닉네임을 사용한다.
    /// </summary>
    public static class LocalProfile
    {
        /// <summary>
        /// 표시 이름을 반환한다. Steam을 쓸 수 없으면 빈 문자열(서버가 "Player N"으로 대체)이다.
        /// </summary>
        public static string DisplayName => SteamBootstrap.IsInitialized ? SteamFriends.GetPersonaName() : string.Empty;
    }
}
