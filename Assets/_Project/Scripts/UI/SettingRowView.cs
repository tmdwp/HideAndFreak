using System;
using GuardVsMannequin.Core;
using GuardVsMannequin.Room;
using UnityEngine;
using UnityEngine.UI;

namespace GuardVsMannequin.UI
{
    /// <summary>
    /// 게임 설정 한 줄(이름, 값, -/+ 버튼). 모든 플레이어에게 값이 보이고, 호스트만 조정할 수 있다.
    /// </summary>
    public class SettingRowView : MonoBehaviour
    {
        [SerializeField] private GameSettingId settingId;
        [SerializeField] private Text labelText;
        [SerializeField] private Text valueText;
        [SerializeField] private Button decreaseButton;
        [SerializeField] private Button increaseButton;

        /// <summary>
        /// 버튼을 눌렀을 때 호출할 조정 함수를 연결하고 이름을 표시한다.
        /// </summary>
        /// <param name="onAdjust">(항목, 방향 -1/+1)을 받는 조정 함수.</param>
        public void Bind(Action<GameSettingId, int> onAdjust)
        {
            labelText.text = GameSettingDefinitions.Get(settingId).label;
            decreaseButton.onClick.AddListener(() => onAdjust(settingId, -1));
            increaseButton.onClick.AddListener(() => onAdjust(settingId, 1));
        }

        /// <summary>
        /// 현재 설정 값을 표시하고, 호스트일 때만 버튼을 켠다.
        /// </summary>
        public void Refresh(GameSettings settings, bool editable)
        {
            valueText.text = GameSettingDefinitions.Format(settings, settingId);
            decreaseButton.interactable = editable;
            increaseButton.interactable = editable;
        }
    }
}
