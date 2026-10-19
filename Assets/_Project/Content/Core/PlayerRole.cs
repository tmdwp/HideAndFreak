namespace GuardVsMannequin.Core
{
    /// <summary>
    /// 플레이어의 역할.
    /// </summary>
    public enum PlayerRole : byte
    {
        /// <summary>역할 미배정.</summary>
        None = 0,

        /// <summary>시설을 지키는 가드.</summary>
        Guard = 1,

        /// <summary>시설을 탈출하려는 마네킹.</summary>
        Mannequin = 2,

        /// <summary>사망 후 관전자.</summary>
        Spectator = 3
    }
}
