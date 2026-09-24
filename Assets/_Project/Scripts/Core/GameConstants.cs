namespace GuardVsMannequin.Core
{
    /// <summary>
    /// 호스트가 변경할 수 없는 고정 게임 수치.
    /// 호스트 설정값은 <see cref="GameSettings"/>를 사용한다.
    /// </summary>
    public static class GameConstants
    {
        public const int MinPlayers = 2;
        public const int MaxPlayers = 10;

        public const float GuardMoveSpeed = 2.0f;
        public const float MannequinMoveSpeed = 1.5f;
        public const float MannequinHuntMoveSpeed = 2.2f;

        public const int Damage = 1;

        public const float UntransformCooldown = 5.0f;
        public const float FakeMannequinPurgeTime = 20.0f;
        public const float ZeroObjectiveHuntDelay = 20.0f;
        public const float CoopDestroySpeedBase = 1.4f;

        /// <summary>가드 히트스캔 사거리. 모든 맵의 천장 높이보다 커야 한다.</summary>
        public const float GunRange = 100f;

        /// <summary>마네킹이 변신 대상을 지정할 수 있는 최대 거리(몸 기준).</summary>
        public const float TransformRange = 8f;

        /// <summary>전원 장치와 상호작용할 수 있는 최대 거리.</summary>
        public const float InteractRange = 2.2f;

        /// <summary>감정 표현 개수(1~6번 키).</summary>
        public const int EmoteCount = 6;

        /// <summary>감정 표현 한 번의 재생 시간(초). 실제/가짜 마네킹이 동일해야 구분되지 않는다.</summary>
        public const float EmoteDuration = 1.5f;

        /// <summary>가드 탐지기 반경.</summary>
        public const float DetectorRadius = 10f;

        /// <summary>변신 중 주기적 소음 간격(초) 범위.</summary>
        public const float DisguiseNoiseIntervalMin = 8f;
        public const float DisguiseNoiseIntervalMax = 14f;

        /// <summary>게임 시작 시 생성할 가짜 마네킹 수 범위.</summary>
        public const int FakeMannequinMinCount = 6;
        public const int FakeMannequinMaxCount = 12;

        /// <summary>결과 화면을 보여준 뒤 대기 상태로 돌아가기까지의 시간(Phase 4에서 룸 씬 이동으로 대체).</summary>
        public const float ResultDisplayDuration = 5.0f;
    }
}
