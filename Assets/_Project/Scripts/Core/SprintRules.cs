namespace GuardVsMannequin.Core
{
    /// <summary>
    /// 달리기·스태미나 계산. 소유자의 스태미나(<c>StaminaModel</c>)와 서버 이동 검증(<c>MovementValidator</c>)이
    /// 같은 수치(<see cref="GameTuning"/>)로 계산하도록 한곳에 둔다.
    /// </summary>
    public static class SprintRules
    {
        // 0으로 나누지 않도록 하는 최소 시간(초)
        private const float MinSeconds = 0.01f;

        /// <summary>달리기 중 이동 속도 배율. (1 미만이면 1로 본다)</summary>
        public static float SpeedMultiplier => System.Math.Max(1f, GameTuning.SprintSpeedMultiplier);

        /// <summary>
        /// 초당 스태미나 소모량(0~1 비율).
        /// </summary>
        public static float DrainPerSecond => 1f / System.Math.Max(MinSeconds, GameTuning.StaminaSprintSeconds);

        /// <summary>
        /// 초당 스태미나 회복량(0~1 비율).
        /// </summary>
        public static float RecoverPerSecond => 1f / System.Math.Max(MinSeconds, GameTuning.StaminaRecoverSeconds);

        /// <summary>회복 시작 대기 시간(초).</summary>
        public static float RecoverDelay => GameTuning.StaminaRecoverDelay;

        /// <summary>스태미나를 다 쓴 뒤 다시 달릴 수 있게 되는 회복 비율(0~1).</summary>
        public static float RestartRatio => GameTuning.StaminaRestartRatio;

        /// <summary>
        /// 스태미나가 가득 찼을 때 달리기로 더 갈 수 있는 최대 거리(m). (서버 검증의 달리기 예산 상한)
        /// </summary>
        /// <param name="baseSpeed">달리지 않을 때의 이동 속도(m/s).</param>
        public static float GetMaxExtraDistance(float baseSpeed)
        {
            return baseSpeed * (SpeedMultiplier - 1f) * GameTuning.StaminaSprintSeconds;
        }

        /// <summary>
        /// 달리기 예산이 초당 회복되는 거리(m/s). 스태미나 회복 속도와 같은 비율로 찬다.
        /// </summary>
        /// <param name="baseSpeed">달리지 않을 때의 이동 속도(m/s).</param>
        public static float GetExtraDistanceRecoverRate(float baseSpeed)
        {
            return GetMaxExtraDistance(baseSpeed) * RecoverPerSecond;
        }
    }
}
