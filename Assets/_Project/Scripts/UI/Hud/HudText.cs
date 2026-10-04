using GuardVsMannequin.Core;
using GuardVsMannequin.Localization;
using UnityEngine;

namespace GuardVsMannequin.UI
{
    /// <summary>
    /// HUD 영역들이 함께 쓰는 문구 도우미. 역할별 문구는 <see cref="RoleDefinitions"/>에서 꺼낸다.
    /// </summary>
    public static class HudText
    {
        /// <summary>역할 안내 문구 종류: 목표. (포획/생존, 전원 장치 파괴/사냥)</summary>
        public const string ObjectivePart = "objective";

        /// <summary>역할 안내 문구 종류: 좌클릭 동작.</summary>
        public const string PrimaryPart = "primary";

        /// <summary>역할 안내 문구 종류: 우클릭 동작.</summary>
        public const string SecondaryPart = "secondary";

        /// <summary>
        /// 역할의 표시 이름을 반환한다.
        /// </summary>
        public static string GetRoleName(PlayerRole role)
        {
            return Loc.Get(RoleDefinitions.Get(role).NameKey);
        }

        /// <summary>
        /// 역할별·단계별 안내 문구를 반환한다. (key: 역할 안내 접두어 + "." + 종류 + ".playing"/".hunting")
        /// 안내가 없는 역할이면 빈 문자열.
        /// </summary>
        /// <param name="part"><see cref="ObjectivePart"/>, <see cref="PrimaryPart"/>, <see cref="SecondaryPart"/> 중 하나.</param>
        public static string GetRoleText(PlayerRole role, string part, bool isHunting)
        {
            string prefix = RoleDefinitions.Get(role).GuideKeyPrefix;
            if (string.IsNullOrEmpty(prefix))
                return string.Empty;
            return Loc.Get($"{prefix}.{part}{(isHunting ? ".hunting" : ".playing")}");
        }

        /// <summary>
        /// 단계 이름을 반환한다. 대기 상태는 빈 문자열.
        /// </summary>
        public static string GetPhaseName(GameState state)
        {
            return state switch
            {
                GameState.Preparation => Loc.Get("hud.phase.preparation"),
                GameState.Playing => Loc.Get("hud.phase.playing"),
                GameState.Hunting => Loc.Get("hud.phase.hunting"),
                GameState.Ended => Loc.Get("hud.phase.ended"),
                _ => string.Empty
            };
        }

        /// <summary>
        /// 초를 분:초 형식으로 바꾼다.
        /// </summary>
        public static string FormatTime(float seconds)
        {
            int total = Mathf.CeilToInt(Mathf.Max(0f, seconds));
            return $"{total / 60}:{total % 60:00}";
        }
    }
}
