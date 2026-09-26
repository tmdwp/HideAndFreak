using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GuardVsMannequin.Settings
{
    /// <summary>
    /// 개인 설정과 키 바인딩을 이 PC(PlayerPrefs)에 저장하고 불러온다.
    /// 설정이 바뀌면 이벤트로 알려, 입력·음량·감도를 쓰는 곳이 즉시 반영하게 한다.
    /// (호스트가 정하는 게임 설정 GameSettings와는 별개인, 이 PC만의 설정이다)
    /// </summary>
    public static class OptionsStore
    {
        private const string OptionsKey = "GuardVsMannequin.Options";
        private const string BindingsKey = "GuardVsMannequin.BindingOverrides";

        private static GameOptions current;
        private static string bindingOverridesJson;
        private static bool isLoaded;

        /// <summary>개인 설정이 바뀌었을 때.</summary>
        public static event Action OptionsChanged;

        /// <summary>키 바인딩이 바뀌었을 때.</summary>
        public static event Action BindingsChanged;

        /// <summary>현재 개인 설정.</summary>
        public static GameOptions Current
        {
            get
            {
                EnsureLoaded();
                return current;
            }
        }

        /// <summary>
        /// 개인 설정을 바꾸고 저장한 뒤 음량을 적용한다.
        /// </summary>
        public static void SetOptions(GameOptions options)
        {
            EnsureLoaded();
            options.mouseSensitivity = Mathf.Clamp(options.mouseSensitivity, GameOptions.MinSensitivity, GameOptions.MaxSensitivity);
            options.masterVolume = Mathf.Clamp01(options.masterVolume);
            options.bgmVolume = Mathf.Clamp01(options.bgmVolume);
            options.sfxVolume = Mathf.Clamp01(options.sfxVolume);

            current = options;
            PlayerPrefs.SetString(OptionsKey, JsonUtility.ToJson(current));
            PlayerPrefs.Save();

            ApplyMasterVolume();
            OptionsChanged?.Invoke();
        }

        /// <summary>
        /// 입력 에셋(복제본)에 저장된 키 바인딩을 적용한다. 입력 에셋을 만드는 곳마다 호출한다.
        /// </summary>
        public static void ApplyBindings(InputActionAsset actions)
        {
            EnsureLoaded();
            actions.RemoveAllBindingOverrides();
            if (!string.IsNullOrEmpty(bindingOverridesJson))
                actions.LoadBindingOverridesFromJson(bindingOverridesJson);
        }

        /// <summary>
        /// 편집한 입력 에셋의 키 바인딩을 저장하고 알린다.
        /// </summary>
        public static void SaveBindings(InputActionAsset editedActions)
        {
            EnsureLoaded();
            bindingOverridesJson = editedActions.SaveBindingOverridesAsJson();
            PlayerPrefs.SetString(BindingsKey, bindingOverridesJson);
            PlayerPrefs.Save();
            BindingsChanged?.Invoke();
        }

        /// <summary>
        /// 게임 시작 시 저장된 설정을 불러와 전체 음량을 바로 적용한다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void LoadOnStartup()
        {
            isLoaded = false;
            EnsureLoaded();
        }

        /// <summary>
        /// 저장된 값을 한 번만 불러오고 전체 음량을 적용한다.
        /// </summary>
        private static void EnsureLoaded()
        {
            if (isLoaded)
                return;
            isLoaded = true;

            current = GameOptions.CreateDefault();
            string json = PlayerPrefs.GetString(OptionsKey, string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    current = JsonUtility.FromJson<GameOptions>(json);
                }
                catch (ArgumentException)
                {
                    Debug.LogWarning("[OptionsStore] 저장된 설정을 읽지 못해 기본값을 사용합니다.");
                }
            }

            bindingOverridesJson = PlayerPrefs.GetString(BindingsKey, string.Empty);
            ApplyMasterVolume();
        }

        /// <summary>
        /// 전체 음량은 AudioListener 음량으로 적용한다.
        /// </summary>
        private static void ApplyMasterVolume()
        {
            AudioListener.volume = current.masterVolume;
        }
    }
}
