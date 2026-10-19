using UnityEngine;

namespace GuardVsMannequin.UI
{
    /// <summary>
    /// 게임 UI 기본 폰트. Resources/Fonts의 프로젝트 폰트(NeoDunggeunmo Pro)를 쓰고, 없으면 한글을 지원하는 OS 폰트를 쓴다.
    /// 프로젝트 폰트에 없는 글자는 폰트 임포트 설정의 Font Names(맑은 고딕 등)로 대신 그린다.
    /// </summary>
    public static class UiFont
    {
        /// <summary>프로젝트 폰트의 Resources 경로. (확장자 제외)</summary>
        public const string ResourcePath = "Fonts/NeoDunggeunmoPro-Regular";

        private static readonly string[] OsFallbackFonts = { "Malgun Gothic", "맑은 고딕", "Apple SD Gothic Neo", "Noto Sans CJK KR", "Arial" };
        private static Font cachedFont;

        /// <summary>
        /// UI에 쓸 폰트. 한 번만 불러온다. (코드로 만드는 UI도 사용)
        /// </summary>
        public static Font Default
        {
            get
            {
                if (cachedFont == null)
                    cachedFont = LoadProjectFont() ?? Font.CreateDynamicFontFromOSFont(OsFallbackFonts, 16);
                return cachedFont;
            }
        }

        /// <summary>
        /// 프로젝트 폰트 에셋을 불러온다. 없으면 null. (에디터에서 UI를 만들 때도 사용)
        /// </summary>
        public static Font LoadProjectFont()
        {
            Font font = Resources.Load<Font>(ResourcePath);
            return font != null ? font : null;
        }
    }
}
