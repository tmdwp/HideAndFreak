using System;
using GuardVsMannequin.Core;
using GuardVsMannequin.Room;
using UnityEngine;
using UnityEngine.UI;

namespace GuardVsMannequin.UI
{
    /// <summary>
    /// 게임 설정 한 줄(이름, 값, -/+ 버튼). 모든 플레이어에게 값이 보이고, 호스트만 조정할 수 있다.
    /// 값 입력 칸(valueInput)이 있으면 호스트는 값을 눌러 키보드로 직접 입력할 수 있다.
    /// - 누르면 단위 없는 숫자로 바뀌고, Enter 또는 다른 곳을 누르면 적용된다. (범위를 넘으면 가장 가까운 값으로 맞춤)
    /// - 숫자가 아니면 원래 값으로 돌아간다.
    /// 맵처럼 입력 칸이 없는 줄은 -/+ 버튼으로만 바꾼다.
    /// </summary>
    public class SettingRowView : MonoBehaviour
    {
        [SerializeField] private GameSettingId settingId;
        [SerializeField] private Text labelText;
        [SerializeField] private Text valueText;

        [Tooltip("값을 키보드로 입력하는 칸. 비우면 -/+ 버튼으로만 조정한다.")]
        [SerializeField] private InputField valueInput;

        [SerializeField] private Button decreaseButton;
        [SerializeField] private Button increaseButton;

        private Action<GameSettingId, float> onSetValue;

        /// <summary>이 줄이 다루는 설정 항목.</summary>
        public GameSettingId SettingId => settingId;
        private GameSettings lastSettings;
        private bool wasFocused;

        /// <summary>
        /// 버튼과 입력 칸에 조정 함수를 연결하고 이름을 표시한다.
        /// </summary>
        /// <param name="onAdjust">(항목, 방향 -1/+1)을 받는 조정 함수.</param>
        /// <param name="setValue">(항목, 입력한 값)을 받는 직접 지정 함수.</param>
        public void Bind(Action<GameSettingId, int> onAdjust, Action<GameSettingId, float> setValue)
        {
            onSetValue = setValue;
            labelText.text = GameSettingDefinitions.Get(settingId).Label;
            decreaseButton.onClick.AddListener(() => onAdjust(settingId, -1));
            increaseButton.onClick.AddListener(() => onAdjust(settingId, 1));

            if (valueInput != null)
            {
                // 숫자 전용 입력 형식은 코드로 넣은 표시 문자열("30초")의 단위까지 지워 버리므로 일반 형식을 쓰고,
                // 입력을 마칠 때 숫자인지 확인한다.
                valueInput.contentType = InputField.ContentType.Standard;
                valueInput.lineType = InputField.LineType.SingleLine;
                valueInput.characterLimit = 10;
                valueInput.onEndEdit.AddListener(OnEndEdit);
            }
        }

        /// <summary>
        /// 입력 칸을 누른 순간 단위 없는 숫자로 바꿔 바로 고칠 수 있게 한다.
        /// </summary>
        private void Update()
        {
            if (valueInput == null)
                return;

            bool focused = valueInput.isFocused;
            if (focused && !wasFocused)
            {
                valueInput.SetTextWithoutNotify(GameSettingDefinitions.FormatNumber(lastSettings, settingId));
                valueInput.selectionAnchorPosition = 0;
                valueInput.selectionFocusPosition = valueInput.text.Length;
            }
            wasFocused = focused;
        }

        /// <summary>
        /// 현재 설정 값을 표시하고, 호스트일 때만 버튼과 입력 칸을 켠다. (언어가 바뀌었을 수 있어 이름도 다시 표시)
        /// 입력 중에는 입력한 내용을 덮어쓰지 않는다.
        /// </summary>
        public void Refresh(GameSettings settings, bool editable)
        {
            lastSettings = settings;
            labelText.text = GameSettingDefinitions.Get(settingId).Label;
            decreaseButton.interactable = editable;
            increaseButton.interactable = editable;

            string formatted = GameSettingDefinitions.Format(settings, settingId);
            if (valueInput == null)
            {
                valueText.text = formatted;
                return;
            }

            valueInput.interactable = editable;
            if (!valueInput.isFocused)
                valueInput.SetTextWithoutNotify(formatted);
        }

        /// <summary>
        /// 입력을 마치면(Enter, 다른 곳 클릭) 숫자를 읽어 적용을 요청한다. 숫자가 아니면 원래 값을 다시 보여준다.
        /// </summary>
        private void OnEndEdit(string text)
        {
            if (GameSettingDefinitions.TryParseInput(text, out float value))
                onSetValue?.Invoke(settingId, value);

            // 서버가 보정한 값은 다음 갱신 때 표시된다. 그 전까지는 이전 값을 보여준다.
            valueInput.SetTextWithoutNotify(GameSettingDefinitions.Format(lastSettings, settingId));
        }
    }
}
