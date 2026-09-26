using System;
using GuardVsMannequin.Localization;
using GuardVsMannequin.Settings;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GuardVsMannequin.UI
{
    /// <summary>
    /// 설정 화면: 언어, 마우스 감도, 음량(전체/배경 음악/효과음), 키 설정.
    /// 값을 바꾸는 즉시 저장되고 적용된다. 로비와 게임 중 ESC 메뉴에서 같은 화면을 쓴다.
    /// </summary>
    public class SettingsPanelView : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;

        [Header("언어")]
        [SerializeField] private Button previousLanguageButton;
        [SerializeField] private Button nextLanguageButton;
        [SerializeField] private Text languageValueText;

        [Header("감도와 음량")]
        [SerializeField] private Slider sensitivitySlider;
        [SerializeField] private Text sensitivityValueText;
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Text masterVolumeValueText;
        [SerializeField] private Slider bgmVolumeSlider;
        [SerializeField] private Text bgmVolumeValueText;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Text sfxVolumeValueText;

        [Header("키 설정")]
        [SerializeField] private RebindRowView[] rebindRows;

        [Header("버튼")]
        [SerializeField] private Button resetAllButton;
        [SerializeField] private Button closeButton;

        private InputActionAsset editingActions;
        private bool isInitialized;

        /// <summary>화면이 닫혔을 때.</summary>
        public event Action Closed;

        /// <summary>화면이 열려 있는지 여부.</summary>
        public bool IsOpen => gameObject.activeSelf;

        /// <summary>
        /// 처음 열릴 때 한 번 편집용 입력 에셋과 UI 이벤트를 준비한다.
        /// </summary>
        private void Initialize()
        {
            if (isInitialized)
                return;
            isInitialized = true;

            editingActions = Instantiate(inputActions);
            OptionsStore.ApplyBindings(editingActions);

            for (int i = 0; i < rebindRows.Length && i < KeyBindingCatalog.All.Count; i++)
                rebindRows[i].Bind(editingActions, KeyBindingCatalog.All[i].labelKey, OnBindingEdited);

            previousLanguageButton.onClick.AddListener(() => ChangeLanguage(-1));
            nextLanguageButton.onClick.AddListener(() => ChangeLanguage(1));
            Loc.LanguageChanged += OnLanguageChanged;

            sensitivitySlider.minValue = GameOptions.MinSensitivity;
            sensitivitySlider.maxValue = GameOptions.MaxSensitivity;
            foreach (Slider volume in new[] { masterVolumeSlider, bgmVolumeSlider, sfxVolumeSlider })
            {
                volume.minValue = 0f;
                volume.maxValue = 1f;
            }

            sensitivitySlider.onValueChanged.AddListener(_ => SaveOptionsFromSliders());
            masterVolumeSlider.onValueChanged.AddListener(_ => SaveOptionsFromSliders());
            bgmVolumeSlider.onValueChanged.AddListener(_ => SaveOptionsFromSliders());
            sfxVolumeSlider.onValueChanged.AddListener(_ => SaveOptionsFromSliders());
            resetAllButton.onClick.AddListener(ResetAll);
            closeButton.onClick.AddListener(Close);
        }

        /// <summary>
        /// 편집용 입력 에셋을 해제한다.
        /// </summary>
        private void OnDestroy()
        {
            Loc.LanguageChanged -= OnLanguageChanged;
            if (editingActions != null)
                Destroy(editingActions);
        }

        /// <summary>
        /// 화면을 열고 저장된 값을 표시한다.
        /// </summary>
        public void Open()
        {
            gameObject.SetActive(true);
            Initialize();
            LoadIntoControls();
        }

        /// <summary>
        /// 화면을 닫는다.
        /// </summary>
        public void Close()
        {
            if (RebindRowView.IsAnyRebinding)
                return;

            gameObject.SetActive(false);
            Closed?.Invoke();
        }

        /// <summary>
        /// 저장된 설정을 슬라이더와 키 목록에 표시한다. (저장 이벤트는 발생시키지 않음)
        /// </summary>
        private void LoadIntoControls()
        {
            GameOptions options = OptionsStore.Current;
            sensitivitySlider.SetValueWithoutNotify(options.mouseSensitivity);
            masterVolumeSlider.SetValueWithoutNotify(options.masterVolume);
            bgmVolumeSlider.SetValueWithoutNotify(options.bgmVolume);
            sfxVolumeSlider.SetValueWithoutNotify(options.sfxVolume);
            RefreshValueTexts(options);
            languageValueText.text = Loc.GetLanguageName(Loc.CurrentLanguage);

            OptionsStore.ApplyBindings(editingActions);
            foreach (RebindRowView row in rebindRows)
                row.Refresh();
        }

        /// <summary>
        /// 슬라이더 값을 저장한다. (감도와 음량은 바로 적용됨)
        /// </summary>
        private void SaveOptionsFromSliders()
        {
            GameOptions options = OptionsStore.Current;
            options.mouseSensitivity = sensitivitySlider.value;
            options.masterVolume = masterVolumeSlider.value;
            options.bgmVolume = bgmVolumeSlider.value;
            options.sfxVolume = sfxVolumeSlider.value;

            OptionsStore.SetOptions(options);
            RefreshValueTexts(OptionsStore.Current);
        }

        /// <summary>
        /// 슬라이더 옆 숫자를 갱신한다.
        /// </summary>
        private void RefreshValueTexts(GameOptions options)
        {
            sensitivityValueText.text = options.mouseSensitivity.ToString("0.00");
            masterVolumeValueText.text = Mathf.RoundToInt(options.masterVolume * 100f) + "%";
            bgmVolumeValueText.text = Mathf.RoundToInt(options.bgmVolume * 100f) + "%";
            sfxVolumeValueText.text = Mathf.RoundToInt(options.sfxVolume * 100f) + "%";
        }

        /// <summary>
        /// 키를 바꾸면 저장한다. 게임 중이면 입력에 바로 적용된다.
        /// </summary>
        private void OnBindingEdited()
        {
            OptionsStore.SaveBindings(editingActions);
        }

        /// <summary>
        /// 감도, 음량, 모든 키를 기본값으로 되돌린다. (언어는 유지)
        /// </summary>
        private void ResetAll()
        {
            if (RebindRowView.IsAnyRebinding)
                return;

            editingActions.RemoveAllBindingOverrides();
            OptionsStore.SaveBindings(editingActions);
            GameOptions defaults = GameOptions.CreateDefault();
            defaults.languageCode = OptionsStore.Current.languageCode;
            OptionsStore.SetOptions(defaults);
            LoadIntoControls();
        }

        /// <summary>
        /// 언어 목록(strings.csv 머리글 순서)에서 이전/다음 언어로 바꾼다.
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

        /// <summary>
        /// 언어가 바뀌면 코드로 채운 문장(언어 이름, 키 이름)을 다시 표시한다.
        /// </summary>
        private void OnLanguageChanged()
        {
            if (IsOpen)
                LoadIntoControls();
        }
    }
}
