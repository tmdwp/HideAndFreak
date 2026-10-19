namespace GuardVsMannequin.Core
{
    /// <summary>
    /// 바꾸면 구조가 달라지는 고정 수치. (인원 제한, 감정 표현 개수, 맵 크기와 맞물린 사거리 등)
    /// 밸런스 조정용 수치는 <see cref="GameTuning"/>(game_tuning.csv), 호스트 설정값은 <see cref="GameSettings"/>를 사용한다.
    /// </summary>
    public static class GameConstants
    {
        public const int MinPlayers = 2;
        public const int MaxPlayers = 10;

        public const int Damage = 1;

        /// <summary>가드 히트스캔 사거리. 모든 맵의 천장 높이보다 커야 한다.</summary>
        public const float GunRange = 100f;

        /// <summary>감정 표현 개수(1~6번 키).</summary>
        public const int EmoteCount = 6;
    }
}
