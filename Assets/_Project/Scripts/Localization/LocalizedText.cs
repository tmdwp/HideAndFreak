using UnityEngine;
using UnityEngine.UI;

namespace GuardVsMannequin.Localization
{
    /// <summary>
    /// 고정 문구(제목, 버튼 이름 등)를 현재 언어로 표시한다. 언어가 바뀌면 바로 다시 표시한다.
    /// 상황에 따라 바뀌는 문구는 각 화면 코드가 <see cref="Loc"/>로 직접 채운다.
    /// </summary>
    [RequireComponent(typeof(Text))]
    public class LocalizedText : MonoBehaviour
    {
        [Tooltip("Resources/Localization/strings.csv의 key")]
        [SerializeField] private string key;

        private Text text;

        /// <summary>표시할 문장의 키.</summary>
        public string Key => key;

        /// <summary>
        /// 켜질 때 현재 언어로 표시하고 언어 변경을 구독한다.
        /// </summary>
        private void OnEnable()
        {
            text = GetComponent<Text>();
            Refresh();
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
        /// 현재 언어의 문장으로 바꾼다.
        /// </summary>
        private void Refresh()
        {
            text.text = Loc.Get(key);
        }
    }
}
