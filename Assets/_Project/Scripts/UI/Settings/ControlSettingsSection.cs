using GuardVsMannequin.Settings;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GuardVsMannequin.UI
{
    /// <summary>
    /// 조작 설정: 마우스 감도와 키 설정. 키는 편집용 입력 에셋 복제본에서 바꾸고 저장하며, 게임 중이면 바로 적용된다.
    /// </summary>
    public class ControlSettingsSection : MonoBehaviour, ISettingsSection
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private Slider sensitivitySlider;
        [SerializeField] private Text sensitivityValueText;
        [SerializeField] private RebindRowView[] rebindRows;

        private InputActionAsset editingActions;

        /// <summary>
        /// 편집용 입력 에셋을 만들고 키 설정 줄과 감도 슬라이더를 연결한다.
        /// </summary>
        public void Initialize()
        {
            editingActions = Instantiate(inputActions);
            OptionsStore.ApplyBindings(editingActions);

            // 줄 순서와 목록 순서가 달라도(프리팹에 줄을 나중에 추가한 경우) 액션 이름으로 짝짓는다.
            foreach (RebindRowView row in rebindRows)
            {
                if (KeyBindingCatalog.TryFind(row.ActionName, row.PartName, out KeyBindingCatalog.Entry entry))
                    row.Bind(editingActions, entry.labelKey, OnBindingEdited);
                else
                    Debug.LogWarning($"[ControlSettingsSection] 키 목록에 없는 줄입니다: {row.ActionName} {row.PartName}", row);
            }

            sensitivitySlider.minValue = GameOptions.MinSensitivity;
            sensitivitySlider.maxValue = GameOptions.MaxSensitivity;
            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        }

        /// <summary>
        /// 편집용 입력 에셋을 해제한다.
        /// </summary>
        private void OnDestroy()
        {
            if (editingActions != null)
                Destroy(editingActions);
        }

        /// <summary>
        /// 저장된 감도와 키를 표시한다.
        /// </summary>
        public void Refresh()
        {
            GameOptions options = OptionsStore.Current;
            sensitivitySlider.SetValueWithoutNotify(options.mouseSensitivity);
            sensitivityValueText.text = options.mouseSensitivity.ToString("0.00");

            if (editingActions == null)
                return;
            OptionsStore.ApplyBindings(editingActions);
            foreach (RebindRowView row in rebindRows)
                row.Refresh();
        }

        /// <summary>
        /// 감도와 모든 키를 기본값으로 되돌린다.
        /// </summary>
        public void ResetToDefaults()
        {
            editingActions.RemoveAllBindingOverrides();
            OptionsStore.SaveBindings(editingActions);

            GameOptions options = OptionsStore.Current;
            options.mouseSensitivity = GameOptions.CreateDefault().mouseSensitivity;
            OptionsStore.SetOptions(options);
            Refresh();
        }

        /// <summary>
        /// 감도를 저장한다.
        /// </summary>
        private void OnSensitivityChanged(float value)
        {
            GameOptions options = OptionsStore.Current;
            options.mouseSensitivity = value;
            OptionsStore.SetOptions(options);
            sensitivityValueText.text = OptionsStore.Current.mouseSensitivity.ToString("0.00");
        }

        /// <summary>
        /// 키를 바꾸면 저장한다.
        /// </summary>
        private void OnBindingEdited()
        {
            OptionsStore.SaveBindings(editingActions);
        }
    }
}
