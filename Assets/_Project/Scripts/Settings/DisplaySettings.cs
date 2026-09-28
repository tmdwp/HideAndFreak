using System.Collections.Generic;
using UnityEngine;

namespace GuardVsMannequin.Settings
{
    /// <summary>
    /// 화면 크기(해상도)와 화면 모드를 고르고 적용한다.
    /// 해상도 목록은 모니터가 지원하는 것 중 가로×세로가 같은 항목을 하나로 합친 것이다. (재생 빈도 무시)
    /// 에디터에서는 게임 뷰 크기를 바꿀 수 없으므로 적용하지 않는다.
    /// </summary>
    public static class DisplaySettings
    {
        /// <summary>
        /// 고를 수 있는 화면 모드와 표시 문구 key. (순서 = GameOptions.screenMode 번호)
        /// </summary>
        public static readonly (FullScreenMode mode, string labelKey)[] Modes =
        {
            (FullScreenMode.FullScreenWindow, "options.screenMode.borderless"),
            (FullScreenMode.Windowed, "options.screenMode.windowed"),
            (FullScreenMode.MaximizedWindow, "options.screenMode.maximized")
        };

        /// <summary>
        /// 고를 수 있는 해상도 목록. (작은 것부터)
        /// </summary>
        public static List<Vector2Int> GetResolutions()
        {
            var result = new List<Vector2Int>();
            foreach (Resolution resolution in Screen.resolutions)
            {
                var size = new Vector2Int(resolution.width, resolution.height);
                if (!result.Contains(size))
                    result.Add(size);
            }

            // 목록을 얻지 못하는 환경(일부 창 모드 등)에서는 현재 크기라도 고를 수 있게 한다.
            var current = new Vector2Int(Screen.width, Screen.height);
            if (!result.Contains(current))
                result.Add(current);

            result.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            return result;
        }

        /// <summary>
        /// 설정의 화면 모드 번호를 목록 범위로 맞춘다.
        /// </summary>
        public static int ClampMode(int modeIndex)
        {
            return Mathf.Clamp(modeIndex, 0, Modes.Length - 1);
        }

        /// <summary>
        /// 설정의 해상도와 화면 모드를 적용한다. 해상도를 고르지 않았으면(0) 현재 크기를 유지한다.
        /// </summary>
        public static void Apply(GameOptions options)
        {
            if (Application.isEditor)
                return;

            int width = options.screenWidth > 0 ? options.screenWidth : Screen.width;
            int height = options.screenHeight > 0 ? options.screenHeight : Screen.height;
            FullScreenMode mode = Modes[ClampMode(options.screenMode)].mode;
            Screen.SetResolution(width, height, mode);
        }
    }
}
