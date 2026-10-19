using GuardVsMannequin.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace GuardVsMannequin.UI
{
    /// <summary>
    /// 사운드 설정: 전체 / 배경 음악 / 효과음 음량. 바꾸는 즉시 적용·저장된다.
    /// </summary>
    public class SoundSettingsSection : MonoBehaviour, ISettingsSection
    {
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Text masterVolumeValueText;
        [SerializeField] private Slider bgmVolumeSlider;
        [SerializeField] private Text bgmVolumeValueText;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Text sfxVolumeValueText;

        /// <summary>
        /// 슬라이더 범위와 이벤트를 연결한다.
        /// </summary>
        public void Initialize()
        {
            foreach (Slider volume in new[] { masterVolumeSlider, bgmVolumeSlider, sfxVolumeSlider })
            {
                volume.minValue = 0f;
                volume.maxValue = 1f;
                volume.onValueChanged.AddListener(_ => Save());
            }
        }

        /// <summary>
        /// 저장된 음량을 표시한다. (저장 이벤트는 발생시키지 않음)
        /// </summary>
        public void Refresh()
        {
            GameOptions options = OptionsStore.Current;
            masterVolumeSlider.SetValueWithoutNotify(options.masterVolume);
            bgmVolumeSlider.SetValueWithoutNotify(options.bgmVolume);
            sfxVolumeSlider.SetValueWithoutNotify(options.sfxVolume);
            RefreshTexts(options);
        }

        /// <summary>
        /// 음량을 기본값으로 되돌린다.
        /// </summary>
        public void ResetToDefaults()
        {
            GameOptions defaults = GameOptions.CreateDefault();
            GameOptions options = OptionsStore.Current;
            options.masterVolume = defaults.masterVolume;
            options.bgmVolume = defaults.bgmVolume;
            options.sfxVolume = defaults.sfxVolume;
            OptionsStore.SetOptions(options);
            Refresh();
        }

        /// <summary>
        /// 슬라이더 값을 저장한다.
        /// </summary>
        private void Save()
        {
            GameOptions options = OptionsStore.Current;
            options.masterVolume = masterVolumeSlider.value;
            options.bgmVolume = bgmVolumeSlider.value;
            options.sfxVolume = sfxVolumeSlider.value;
            OptionsStore.SetOptions(options);
            RefreshTexts(OptionsStore.Current);
        }

        /// <summary>
        /// 슬라이더 옆 퍼센트를 갱신한다.
        /// </summary>
        private void RefreshTexts(GameOptions options)
        {
            masterVolumeValueText.text = Mathf.RoundToInt(options.masterVolume * 100f) + "%";
            bgmVolumeValueText.text = Mathf.RoundToInt(options.bgmVolume * 100f) + "%";
            sfxVolumeValueText.text = Mathf.RoundToInt(options.sfxVolume * 100f) + "%";
        }
    }
}
