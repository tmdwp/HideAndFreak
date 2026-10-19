using GuardVsMannequin.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace GuardVsMannequin.UI
{
    /// <summary>
    /// 언어 설정(◀ 언어 이름 ▶). 목록은 strings.csv의 언어 열 순서이며, 열을 추가하면 자동으로 나타난다.
    /// "모두 기본값으로"로는 되돌리지 않는다.
    /// </summary>
    public class LanguageSettingsSection : MonoBehaviour, ISettingsSection
    {
        [SerializeField] private Button previousLanguageButton;
        [SerializeField] private Button nextLanguageButton;
        [SerializeField] private Text languageValueText;

        /// <summary>
        /// 버튼 이벤트를 연결한다.
        /// </summary>
        public void Initialize()
        {
            previousLanguageButton.onClick.AddListener(() => ChangeLanguage(-1));
            nextLanguageButton.onClick.AddListener(() => ChangeLanguage(1));
        }

        /// <summary>
        /// 현재 언어 이름을 그 언어로 표시한다.
        /// </summary>
        public void Refresh()
        {
            languageValueText.text = Loc.GetLanguageName(Loc.CurrentLanguage);
        }

        /// <summary>
        /// 언어는 기본값으로 되돌리지 않는다.
        /// </summary>
        public void ResetToDefaults()
        {
        }

        /// <summary>
        /// 이전/다음 언어로 바꾼다. (키 설정 중에는 바꾸지 않음) 화면 갱신은 설정 화면이 언어 변경 알림으로 한다.
        /// </summary>
        private void ChangeLanguage(int direction)
        {
            if (RebindRowView.IsAnyRebinding)
                return;

            var languages = Loc.Languages;
            if (languages.Count == 0)
                return;

            int index = 0;
            for (int i = 0; i < languages.Count; i++)
            {
                if (languages[i] == Loc.CurrentLanguage)
                    index = i;
            }
            index = (index + direction + languages.Count) % languages.Count;
            Loc.SetLanguage(languages[index]);
        }
    }
}
