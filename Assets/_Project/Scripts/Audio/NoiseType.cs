namespace GuardVsMannequin.Audio
{
    /// <summary>
    /// 게임플레이 정보를 전달하는 소음 종류.
    /// </summary>
    public enum NoiseType : byte
    {
        /// <summary>가드 사격.</summary>
        GunShot = 0,

        /// <summary>가드 탐지기 작동.</summary>
        DetectorPing = 1,

        /// <summary>탐지기 범위 안의 마네킹이 내는 소음.</summary>
        MannequinDetected = 2,

        /// <summary>변신 중인 마네킹의 주기적 소음.</summary>
        DisguiseCreak = 3,

        /// <summary>전원 장치 파괴.</summary>
        GeneratorDestroyed = 4
    }
}
