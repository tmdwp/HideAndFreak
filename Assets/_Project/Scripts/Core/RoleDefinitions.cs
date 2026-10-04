using System;
using System.Collections.Generic;

namespace GuardVsMannequin.Core
{
    /// <summary>
    /// 역할 하나의 능력치. 이번 판 설정(<see cref="GameSettings"/>)에서 역할에 맞는 값만 골라 담는다.
    /// </summary>
    public readonly struct RoleStats
    {
        /// <summary>최대 체력.</summary>
        public readonly int MaxHealth;

        /// <summary>기본 이동 속도(m/s).</summary>
        public readonly float MoveSpeed;

        /// <summary>공격 한 번의 피해량. (가드: 총, 마네킹: 사냥 시간 공격)</summary>
        public readonly int AttackDamage;

        public RoleStats(int maxHealth, float moveSpeed, int attackDamage)
        {
            MaxHealth = maxHealth;
            MoveSpeed = moveSpeed;
            AttackDamage = attackDamage;
        }
    }

    /// <summary>
    /// HUD 목표 영역에서 목표 문구 아래에 보여줄 남은 수.
    /// </summary>
    public enum ObjectiveCounter : byte
    {
        /// <summary>표시하지 않음.</summary>
        None = 0,

        /// <summary>남은 전원 장치 수. (목표 횟수 - 파괴한 수)</summary>
        RemainingGenerators = 1,

        /// <summary>살아 있는 가드 수.</summary>
        RemainingGuards = 2,

        /// <summary>살아 있는 마네킹 수.</summary>
        RemainingMannequins = 3
    }

    /// <summary>
    /// 역할 하나의 정의: 표시 이름·안내 문구 key, 소속 진영, 단계별 이동 속도 배율, HUD 목표 표시, 설정에서 능력치를 고르는 방법.
    /// </summary>
    public sealed class RoleDefinition
    {
        private readonly Func<GameSettings, RoleStats> selectStats;
        private readonly ObjectiveCounter playingCounter;
        private readonly ObjectiveCounter huntingCounter;

        /// <summary>역할.</summary>
        public PlayerRole Role { get; }

        /// <summary>역할 이름 문구 key. (strings.csv)</summary>
        public string NameKey { get; }

        /// <summary>
        /// HUD 목표·조작 안내 문구 key 앞부분. 뒤에 ".objective"/".primary"/".secondary"와 ".playing"/".hunting"을 붙인다.
        /// 비어 있으면 안내 없음.
        /// </summary>
        public string GuideKeyPrefix { get; }

        /// <summary>이 역할이 이기면 기록되는 승리 진영.</summary>
        public MatchWinner Side { get; }

        /// <summary>사냥 시간의 이동 속도 배율.</summary>
        public float HuntSpeedMultiplier { get; }

        public RoleDefinition(PlayerRole role, string nameKey, string guideKeyPrefix, MatchWinner side, float huntSpeedMultiplier,
            ObjectiveCounter playingCounter, ObjectiveCounter huntingCounter, Func<GameSettings, RoleStats> selectStats)
        {
            Role = role;
            NameKey = nameKey;
            GuideKeyPrefix = guideKeyPrefix;
            Side = side;
            HuntSpeedMultiplier = huntSpeedMultiplier;
            this.playingCounter = playingCounter;
            this.huntingCounter = huntingCounter;
            this.selectStats = selectStats;
        }

        /// <summary>
        /// 이번 판 설정에서 이 역할의 능력치를 고른다.
        /// </summary>
        public RoleStats GetStats(GameSettings settings)
        {
            return selectStats != null ? selectStats(settings) : default;
        }

        /// <summary>
        /// 현재 단계의 이동 속도. (사냥 시간에는 배율 적용)
        /// </summary>
        public float GetMoveSpeed(GameSettings settings, GameState state)
        {
            float multiplier = state == GameState.Hunting ? HuntSpeedMultiplier : 1f;
            return GetStats(settings).MoveSpeed * multiplier;
        }

        /// <summary>
        /// 현재 단계에서 HUD 목표 아래에 보여줄 남은 수의 종류.
        /// </summary>
        public ObjectiveCounter GetObjectiveCounter(bool isHunting)
        {
            return isHunting ? huntingCounter : playingCounter;
        }

        /// <summary>
        /// 이 역할 플레이어가 이긴 판인지 여부.
        /// </summary>
        public bool IsWinner(MatchWinner winner)
        {
            return Side != MatchWinner.None && Side == winner;
        }
    }

    /// <summary>
    /// 역할별 정의를 한곳에 모은다. 역할에 따라 달라지는 값은 여기서만 꺼내고, 다른 코드에서 역할로 분기하지 않는다.
    /// 역할을 추가하면 이 표에 항목 하나를 더하면 된다.
    /// </summary>
    public static class RoleDefinitions
    {
        private static readonly RoleDefinition None =
            new(PlayerRole.None, "role.none", string.Empty, MatchWinner.None, 1f, ObjectiveCounter.None, ObjectiveCounter.None, null);

        private static readonly Dictionary<PlayerRole, RoleDefinition> definitions = new()
        {
            // 가드: 탐색 시간 "포획", 사냥 시간 "생존" (남은 마네킹은 상단에 있으므로 따로 표시하지 않음)
            [PlayerRole.Guard] = new RoleDefinition(PlayerRole.Guard, "role.guard", "hud.guide.guard", MatchWinner.Guard, 1f,
                ObjectiveCounter.None, ObjectiveCounter.None,
                settings => new RoleStats(settings.guardMaxHealth, settings.guardMoveSpeed, settings.gunDamage)),
            // 마네킹: 탐색 시간 "전원 장치 파괴"(남은 전원 장치), 사냥 시간 "사냥"(남은 가드)
            [PlayerRole.Mannequin] = new RoleDefinition(PlayerRole.Mannequin, "role.mannequin", "hud.guide.mannequin", MatchWinner.Mannequin,
                GameConstants.MannequinHuntSpeedMultiplier,
                ObjectiveCounter.RemainingGenerators, ObjectiveCounter.RemainingGuards,
                settings => new RoleStats(settings.mannequinMaxHealth, settings.mannequinMoveSpeed, settings.mannequinAttackDamage)),
        };

        /// <summary>
        /// 역할의 정의. 정의가 없는 역할(없음/관전)은 이름만 있는 빈 정의를 반환한다.
        /// </summary>
        public static RoleDefinition Get(PlayerRole role)
        {
            return definitions.TryGetValue(role, out RoleDefinition definition) ? definition : None;
        }

        /// <summary>
        /// 이번 판 설정에서 역할의 능력치를 고른다.
        /// </summary>
        public static RoleStats GetStats(this GameSettings settings, PlayerRole role)
        {
            return Get(role).GetStats(settings);
        }
    }
}
