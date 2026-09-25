using System.Collections.Generic;

namespace GuardVsMannequin.Player
{
    /// <summary>
    /// 이 클라이언트에서 보이는(스폰되어 살아 있는) 플레이어 아바타 목록.
    /// 사망하면 아바타가 제거되므로 목록에는 생존자만 남는다. 관전 대상 선택에 사용한다.
    /// </summary>
    public static class ClientPlayerList
    {
        private static readonly List<PlayerIdentity> players = new();

        /// <summary>생존 중인 아바타 목록. (스폰 순서)</summary>
        public static IReadOnlyList<PlayerIdentity> Players => players;

        /// <summary>
        /// 클라이언트에 스폰된 아바타를 추가한다.
        /// </summary>
        public static void Add(PlayerIdentity identity)
        {
            if (!players.Contains(identity))
                players.Add(identity);
        }

        /// <summary>
        /// 클라이언트에서 제거된 아바타를 뺀다.
        /// </summary>
        public static void Remove(PlayerIdentity identity)
        {
            players.Remove(identity);
        }
    }
}
