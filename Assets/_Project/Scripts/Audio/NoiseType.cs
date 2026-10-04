namespace GuardVsMannequin.Audio
{
    /// <summary>
    /// 게임플레이 정보를 전달하는 소음 종류. 번호는 에셋(NoiseLibrary)에 저장되므로 바꾸거나 다시 쓰지 않는다.
    /// </summary>
    public enum NoiseType : byte
    {
        /// <summary>가드 사격.</summary>
        GunShot = 0,

        /// <summary>(사용 안 함) 가드 탐지기 작동. Phase 7에서 수류탄으로 교체. 번호 1은 다시 쓰지 않는다.</summary>
        [System.Obsolete("Phase 7: 탐지기 제거")]
        DetectorPing = 1,

        /// <summary>(사용 안 함) 탐지기 범위 안의 마네킹이 내는 소음. 번호 2는 다시 쓰지 않는다.</summary>
        [System.Obsolete("Phase 7: 탐지기 제거")]
        MannequinDetected = 2,

        /// <summary>변신 중인 마네킹의 주기적 소음.</summary>
        DisguiseCreak = 3,

        /// <summary>전원 장치 파괴.</summary>
        GeneratorDestroyed = 4,

        /// <summary>사냥 시간 진입 시 맵 전체 전원 차단. (2D로 전원에게 재생)</summary>
        PowerDown = 5,

        /// <summary>사냥 시간 마네킹의 공격.</summary>
        MannequinAttack = 6,

        /// <summary>손전등 스위치.</summary>
        FlashlightClick = 7,

        /// <summary>가드 수류탄 폭발.</summary>
        GrenadeExplosion = 8
    }
}
