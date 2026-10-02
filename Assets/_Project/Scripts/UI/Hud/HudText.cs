using GuardVsMannequin.Core;
using GuardVsMannequin.Localization;
using UnityEngine;

namespace GuardVsMannequin.UI
{
    /// <summary>
    /// HUD 영역들이 함께 쓰는 문구 도우미.
    /// </summary>
    public static class HudText
    {
        /// <summary>
        /// 역할의 표시 이름을 반환한다.
        /// </summary>
        public static string GetRoleName(PlayerRole role)
        {
            return Loc.Get(role switch
            {
                PlayerRole.Guard => "role.guard",
                PlayerRole.Mannequin => "role.mannequin",
                _ => "role.none"
            });
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
