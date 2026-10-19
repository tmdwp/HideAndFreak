using GuardVsMannequin.Core;
using GuardVsMannequin.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace GuardVsMannequin.UI
{
    /// <summary>
    /// 크레딧 화면. 크레딧 페이지와 이용한 에셋 페이지를 버튼으로 오간다. (빌드 폴더의 GameData/Credits가 우선)
    /// - 크레딧: credits.&lt;언어 코드&gt;.txt. 현재 언어 파일이 없으면 첫 번째 언어(ko) 파일을 보여준다.
    /// - 이용한 에셋: assets.txt (에셋 이름은 번역하지 않으므로 언어 공통, 제목만 현재 언어)
    /// </summary>
    public class CreditsView : MonoBehaviour
    {
        private const string CreditsPathPrefix = "Credits/credits.";
        private const string AssetsPath = "Credits/assets";

        [SerializeField] private Text bodyText;
        [SerializeField] private Button closeButton;
        [Tooltip("크레딧 ↔ 이용한 에셋 페이지 전환 버튼. 비우면 크레딧 페이지만 보인다.")]
        [SerializeField] private Button pageButton;
        [SerializeField] private Text pageButtonLabel;

        private bool showingAssets;

        /// <summary>
        /// 닫기·페이지 전환 버튼을 연결한다.
        /// </summary>
        private void Awake()
        {
            closeButton.onClick.AddListener(Close);
            if (pageButton != null)
                pageButton.onClick.AddListener(TogglePage);
        }

        /// <summary>
        /// 열려 있는 동안 언어가 바뀌면 내용을 다시 읽는다.
        /// </summary>
        private void OnEnable()
        {
            Loc.LanguageChanged += Refresh;
        }

        /// <summary>
        /// 언어 변경 구독을 해제한다.
        /// </summary>
        private void OnDisable()
        {
            Loc.LanguageChanged -= Refresh;
        }

        /// <summary>
        /// 화면을 크레딧 페이지로 열고 내용을 채운다.
        /// </summary>
        public void Open()
        {
            gameObject.SetActive(true);
            ShowPage(false);
        }

        /// <summary>
        /// 화면을 닫는다.
        /// </summary>
        public void Close()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 크레딧 ↔ 이용한 에셋 페이지를 바꾼다.
        /// </summary>
        private void TogglePage()
        {
            ShowPage(!showingAssets);
        }

        /// <summary>
        /// 페이지를 바꾸고 맨 위부터 보여준다.
        /// </summary>
        private void ShowPage(bool assets)
        {
            showingAssets = assets;
            Refresh();

            ScrollRect scroll = bodyText.GetComponentInParent<ScrollRect>();
            if (scroll != null)
                scroll.verticalNormalizedPosition = 1f;
        }

        /// <summary>
        /// 현재 페이지 내용과 전환 버튼 문구를 현재 언어로 채운다.
        /// </summary>
        private void Refresh()
        {
            bodyText.text = showingAssets ? LoadAssetsPage() : LoadCreditsPage();
            if (pageButtonLabel != null)
                pageButtonLabel.text = Loc.Get(showingAssets ? "credits.showCredits" : "credits.showAssets");
        }

        /// <summary>
        /// 현재 언어(없으면 첫 번째 언어)의 크레딧 내용.
        /// </summary>
        private static string LoadCreditsPage()
        {
            if (TryLoadText(CreditsPathPrefix + Loc.CurrentLanguage, out string text)
                || (Loc.Languages.Count > 0 && TryLoadText(CreditsPathPrefix + Loc.Languages[0], out text)))
                return text;
            return Loc.Get("credits.missing");
        }

        /// <summary>
        /// 제목(현재 언어) + 이용한 에셋 목록.
        /// </summary>
        private static string LoadAssetsPage()
        {
            string list = TryLoadText(AssetsPath, out string text) ? text : Loc.Get("credits.assetsMissing");
            return Loc.Get("credits.assetsTitle") + "\n\n" + list;
        }

        /// <summary>
        /// 크레딧 텍스트 파일을 읽는다. 빌드 폴더의 외부 파일이 우선이며, 없거나 깨졌으면 내장 원본을 쓴다.
        /// </summary>
        private static bool TryLoadText(string resourcePath, out string text)
        {
            return DataFiles.TryLoad<string>(resourcePath, ".txt", TryParseText, out text);
        }

        /// <summary>
        /// 크레딧은 텍스트 그대로 쓴다. (빈 파일은 DataFiles가 실패로 처리)
        /// </summary>
        private static bool TryParseText(string source, out string text)
        {
            text = source;
            return true;
        }
    }
}
