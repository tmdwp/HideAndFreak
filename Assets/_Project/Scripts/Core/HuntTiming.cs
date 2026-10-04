namespace GuardVsMannequin.Core
{
    /// <summary>
    /// 목표 수행 횟수가 0회일 때의 사냥 시간 진입 시점 계산.
    /// 탐색 단계 시작 후 <see cref="GameTuning.ZeroObjectiveHuntDelay"/>초 동안 가드 보호 시간을 준 뒤 사냥 시간이 시작된다.
    /// 승패 규칙과 HUD가 같은 계산을 쓰도록 한곳에 둔다.
    /// </summary>
    public static class HuntTiming
    {
        /// <summary>
        /// 목표 없이(0회) 시간만으로 사냥 시간에 진입하는 판인지 확인한다.
        /// </summary>
        public static bool IsTimedHunt(GameSettings settings)
        {
            return settings.objectiveCount <= 0;
        }

        /// <summary>
        /// 탐색 단계에서 사냥 시간 시작까지 남은 가드 보호 시간(초)을 반환한다. 0 이하이면 사냥 시간 진입 시점이다.
        /// </summary>
        /// <param name="settings">이번 판 설정.</param>
        /// <param name="remainingPlayTime">탐색 단계 타이머의 남은 시간.</param>
        public static float GetProtectionRemaining(GameSettings settings, float remainingPlayTime)
        {
            float elapsed = settings.playTime - remainingPlayTime;
            return GameTuning.ZeroObjectiveHuntDelay - elapsed;
        }
    }
}
