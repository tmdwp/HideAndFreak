using System.Collections.Generic;
using GuardVsMannequin.Localization;
using GuardVsMannequin.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace GuardVsMannequin.UI
{
    /// <summary>
    /// 화면 설정: 화면 모드(◀ ▶)와 해상도(누르면 펼쳐지는 스크롤 목록).
    /// 바꾸는 즉시 적용·저장된다(<see cref="DisplaySettings"/>). "모두 기본값으로"로는 되돌리지 않는다.
    /// </summary>
    public class DisplaySettingsSection : MonoBehaviour, ISettingsSection
    {
        [SerializeField] private Button previousScreenModeButton;
        [SerializeField] private Button nextScreenModeButton;
        [SerializeField] private Text screenModeValueText;

        [Tooltip("해상도 목록. 누르면 스크롤 목록이 펼쳐진다.")]
        [SerializeField] private Dropdown resolutionDropdown;

        private readonly List<Vector2Int> resolutions = new();

        /// <summary>
        /// 버튼과 목록 이벤트를 연결한다.
        /// </summary>
        public void Initialize()
        {
            previousScreenModeButton.onClick.AddListener(() => ChangeScreenMode(-1));
            nextScreenModeButton.onClick.AddListener(() => ChangeScreenMode(1));
            resolutionDropdown.onValueChanged.AddListener(OnResolutionSelected);
        }

        /// <summary>
        /// 현재 화면 모드와, 모니터가 지원하는 해상도 목록을 표시한다.
        /// </summary>
        public void Refresh()
        {
            GameOptions options = OptionsStore.Current;
            screenModeValueText.text = Loc.Get(DisplaySettings.Modes[DisplaySettings.ClampMode(options.screenMode)].labelKey);

            resolutions.Clear();
            resolutions.AddRange(DisplaySettings.GetResolutions());
            var labels = new List<string>();
            foreach (Vector2Int size in resolutions)
                labels.Add($"{size.x} × {size.y}");

            resolutionDropdown.ClearOptions();
            resolutionDropdown.AddOptions(labels);

            int selected = resolutions.IndexOf(GetSelectedResolution(options));
            resolutionDropdown.SetValueWithoutNotify(selected >= 0 ? selected : resolutions.Count - 1);
            resolutionDropdown.RefreshShownValue();
        }

        /// <summary>
        /// 화면 설정은 기본값으로 되돌리지 않는다. (바꾸면 화면이 깜빡이므로)
        /// </summary>
        public void ResetToDefaults()
        {
        }

        /// <summary>
        /// 화면 모드를 이전/다음으로 바꾸고 바로 적용한다.
        /// </summary>
        private void ChangeScreenMode(int direction)
        {
            GameOptions options = OptionsStore.Current;
            int count = DisplaySettings.Modes.Length;
            options.screenMode = (DisplaySettings.ClampMode(options.screenMode) + direction + count) % count;
            KeepCurrentResolution(ref options);
            OptionsStore.SetOptions(options);
            Refresh();
        }

        /// <summary>
        /// 목록에서 고른 해상도를 적용한다.
        /// </summary>
        private void OnResolutionSelected(int index)
        {
            if (index < 0 || index >= resolutions.Count)
                return;

            GameOptions options = OptionsStore.Current;
            options.screenWidth = resolutions[index].x;
            options.screenHeight = resolutions[index].y;
            OptionsStore.SetOptions(options);
        }

        /// <summary>
        /// 저장된 해상도. 고른 적이 없으면 현재 화면 크기.
        /// </summary>
        private static Vector2Int GetSelectedResolution(GameOptions options)
        {
            return options.screenWidth > 0 && options.screenHeight > 0
                ? new Vector2Int(options.screenWidth, options.screenHeight)
                : new Vector2Int(Screen.width, Screen.height);
        }

        /// <summary>
        /// 해상도를 고른 적이 없으면 지금 크기를 저장해, 모드만 바꿔도 크기가 유지되게 한다.
        /// </summary>
        private static void KeepCurrentResolution(ref GameOptions options)
        {
            if (options.screenWidth > 0 && options.screenHeight > 0)
                return;
            options.screenWidth = Screen.width;
            options.screenHeight = Screen.height;
        }
    }
}
