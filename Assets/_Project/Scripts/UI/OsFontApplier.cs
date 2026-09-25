using UnityEngine;
using UnityEngine.UI;

namespace GuardVsMannequin.UI
{
    /// <summary>
    /// 하위의 모든 uGUI Text에 한글을 지원하는 OS 폰트를 적용한다.
    /// 기본 폰트(LegacyRuntime)는 한글 글리프가 없어 네모로 보이기 때문이다.
    /// (Phase 6에서 TextMeshPro + 한글 폰트 에셋으로 교체 예정)
    /// </summary>
    public class OsFontApplier : MonoBehaviour
    {
        private static readonly string[] PreferredFonts = { "Malgun Gothic", "맑은 고딕", "Apple SD Gothic Neo", "Noto Sans CJK KR", "Arial" };
        private static Font cachedFont;

        /// <summary>
        /// 하위 Text(비활성 포함)에 폰트를 적용한다.
        /// </summary>
        private void Awake()
        {
            Font font = GetFont();
            foreach (Text text in GetComponentsInChildren<Text>(true))
                text.font = font;
        }

        /// <summary>
        /// OS 폰트로 동적 폰트를 한 번만 만든다.
        /// </summary>
        private static Font GetFont()
        {
            if (cachedFont == null)
                cachedFont = Font.CreateDynamicFontFromOSFont(PreferredFonts, 16);
            return cachedFont;
        }
    }
}
