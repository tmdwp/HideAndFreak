using UnityEngine;
using UnityEngine.UI;

namespace GuardVsMannequin.UI
{
    /// <summary>
    /// 하위 uGUI Text 중 Unity 기본 폰트(LegacyRuntime)를 쓰는 것에만 한글을 지원하는 OS 폰트를 적용한다.
    /// 기본 폰트는 한글 글리프가 없어 네모로 보이기 때문이다.
    /// 직접 지정한 폰트(UI 에셋의 폰트 등)는 바꾸지 않는다. 모든 Text에 폰트를 지정했다면 이 컴포넌트는 지워도 된다.
    /// </summary>
    public class OsFontApplier : MonoBehaviour
    {
        private const string BuiltInFontName = "LegacyRuntime";
        private static readonly string[] PreferredFonts = { "Malgun Gothic", "맑은 고딕", "Apple SD Gothic Neo", "Noto Sans CJK KR", "Arial" };
        private static Font cachedFont;

        [Tooltip("비워 두면 OS 한글 폰트를 쓴다. 폰트 에셋을 넣으면 기본 폰트를 쓰는 Text를 이 폰트로 바꾼다.")]
        [SerializeField] private Font replacementFont;

        /// <summary>
        /// 하위 Text(비활성 포함) 중 기본 폰트를 쓰는 것만 바꾼다.
        /// </summary>
        private void Awake()
        {
            Font font = replacementFont != null ? replacementFont : GetOsFont();
            foreach (Text text in GetComponentsInChildren<Text>(true))
            {
                if (text.font == null || text.font.name == BuiltInFontName)
                    text.font = font;
            }
        }

        /// <summary>
        /// OS 폰트로 동적 폰트를 한 번만 만든다. (코드로 만드는 UI도 사용)
        /// </summary>
        public static Font GetOsFont()
        {
            if (cachedFont == null)
                cachedFont = Font.CreateDynamicFontFromOSFont(PreferredFonts, 16);
            return cachedFont;
        }
    }
}
