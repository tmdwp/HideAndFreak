using UnityEngine;

namespace GuardVsMannequin.Core
{
    /// <summary>
    /// 누가 언제 누구에게 피해를 줄 수 있는지 결정한다.
    /// 피해 규칙을 한곳에 모아 도구/체력 코드가 규칙을 중복 구현하지 않게 한다(SRP).
    /// </summary>
    public static class DamageRules
    {
        /// <summary>
        /// 공격자 역할, 대상 역할, 현재 상태로 피해 가능 여부를 판정한다.
        /// </summary>
        public static bool CanDamage(PlayerRole attackerRole, PlayerRole targetRole, GameState state)
        {
            // 가드는 탐색/사냥 시간 모두 마네킹을 공격할 수 있다.
            if (attackerRole == PlayerRole.Guard && targetRole == PlayerRole.Mannequin)
                return state == GameState.Playing || state == GameState.Hunting;

            // 마네킹은 사냥 시간에만 가드를 공격할 수 있다. (Phase 3)
            if (attackerRole == PlayerRole.Mannequin && targetRole == PlayerRole.Guard)
                return state == GameState.Hunting;

            return false;
        }

        /// <summary>
        /// 가드 도구(총, 수류탄)를 사용할 수 있는지 확인한다.
        /// 탐색 시작 후 출발 보호 시간(<see cref="GameTuning.GuardToolLockTime"/>) 동안은 쓸 수 없다.
        /// </summary>
        public static bool CanUseGuardTools(IGameStateProvider provider)
        {
            if (provider == null)
                return false;
            GameState state = provider.CurrentState;
            return (state == GameState.Playing || state == GameState.Hunting) && GetGuardToolLockRemaining(provider) <= 0f;
        }

        /// <summary>
        /// 가드 도구 출발 보호의 남은 시간(초). 탐색 단계가 아니면 0.
        /// </summary>
        public static float GetGuardToolLockRemaining(IGameStateProvider provider)
        {
            if (provider == null || provider.CurrentState != GameState.Playing)
                return 0f;

            float elapsed = provider.Settings.playTime - provider.RemainingTime;
            return Mathf.Max(0f, GameTuning.GuardToolLockTime - elapsed);
        }
    }
}
