using GuardVsMannequin.Core;
using GuardVsMannequin.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace GuardVsMannequin.UI
{
    /// <summary>
    /// 크레딧 화면. 내용은 credits.&lt;언어 코드&gt;.txt 텍스트 파일에서 읽는다. (빌드 폴더의 GameData/Credits가 우선)
    /// 현재 언어 파일이 없으면 첫 번째 언어(ko) 파일을 보여준다.
    /// </summary>
    public class CreditsView : MonoBehaviour
    {
        private const string CreditsPathPrefix = "Credits/credits.";

        [SerializeField] private Text bodyText;
        [SerializeField] private Button closeButton;

        /// <summary>
        /// 닫기 버튼을 연결한다.
        /// </summary>
        private void Awake()
        {
            closeButton.onClick.AddListener(Close);
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
        /// 화면을 열고 내용을 채운다.
        /// </summary>
        public void Open()
        {
            gameObject.SetActive(true);
            Refresh();
        }

        /// <summary>
        /// 화면을 닫는다.
        /// </summary>
        public void Close()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 현재 언어의 크레딧 파일을 표시한다.
        /// </summary>
        private void Refresh()
        {
            if (TryLoadCredits(Loc.CurrentLanguage, out string text)
                || (Loc.Languages.Count > 0 && TryLoadCredits(Loc.Languages[0], out text)))
            {
                bodyText.text = text;
                return;
            }
            bodyText.text = Loc.Get("credits.missing");
        }

        /// <summary>
        /// 언어별 크레딧 파일을 읽는다. 빌드 폴더의 외부 파일이 우선이며, 없거나 깨졌으면 내장 원본을 쓴다.
        /// </summary>
        private static bool TryLoadCredits(string languageCode, out string text)
        {
            return DataFiles.TryLoad<string>(CreditsPathPrefix + languageCode, ".txt", TryParseText, out text);
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
