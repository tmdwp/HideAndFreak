using System;
using UnityEngine;

namespace GuardVsMannequin.Core
{
    /// <summary>
    /// 방을 만들 때의 초기 게임 설정을 Resources/Data/default_game_settings.json에서 읽는다.
    /// JSON 파일만 고치면 코드 수정 없이 기본값을 바꿀 수 있다. 파일에 없는 항목은 코드 기본값을 쓴다.
    /// </summary>
    public static class DefaultGameSettings
    {
        private const string FilePath = "Data/default_game_settings";

        /// <summary>
        /// 초기 게임 설정을 불러온다. 파일이 없거나 형식이 틀리면 코드 기본값을 쓴다.
        /// </summary>
        public static GameSettings Load()
        {
            GameSettings settings = GameSettings.CreateDefault();

            TextAsset asset = Resources.Load<TextAsset>(FilePath);
            if (asset == null)
            {
                Debug.LogWarning($"[DefaultGameSettings] Resources/{FilePath}.json을 찾을 수 없어 코드 기본값을 사용합니다.");
                return settings;
            }

            try
            {
                // 파일에 적힌 항목만 덮어쓴다. (새 항목이 추가돼도 기존 파일이 깨지지 않도록)
                // 구조체는 박싱한 복사본을 덮어쓴 뒤 다시 꺼내야 값이 반영된다.
                object boxed = settings;
                JsonUtility.FromJsonOverwrite(asset.text, boxed);
                settings = (GameSettings)boxed;
            }
            catch (ArgumentException exception)
            {
                Debug.LogError($"[DefaultGameSettings] JSON 형식이 올바르지 않아 코드 기본값을 사용합니다: {exception.Message}");
                return GameSettings.CreateDefault();
            }

            return settings;
        }
    }
}
