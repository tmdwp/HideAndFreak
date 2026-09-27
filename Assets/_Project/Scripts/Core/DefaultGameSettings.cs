using UnityEngine;

namespace GuardVsMannequin.Core
{
    /// <summary>
    /// 방을 만들 때의 초기 게임 설정을 default_game_settings.json에서 읽는다.
    /// 빌드 폴더의 GameData/Data/default_game_settings.json을 먼저 보고, 없거나 깨졌으면 내장 원본을 쓴다(<see cref="DataFiles"/>).
    /// 파일에 없는 항목은 코드 기본값을 쓴다.
    /// </summary>
    public static class DefaultGameSettings
    {
        private const string FilePath = "Data/default_game_settings";
        private const string Extension = ".json";

        /// <summary>
        /// 초기 게임 설정을 불러온다. 외부·내장 파일을 모두 쓸 수 없으면 코드 기본값을 쓴다.
        /// </summary>
        public static GameSettings Load()
        {
            if (DataFiles.TryLoad<GameSettings>(FilePath, Extension, TryParse, out GameSettings settings))
                return settings;

            Debug.LogWarning($"[DefaultGameSettings] {FilePath}{Extension}을 사용할 수 없어 코드 기본값을 사용합니다.");
            return GameSettings.CreateDefault();
        }

        /// <summary>
        /// 코드 기본값 위에 JSON에 적힌 항목만 덮어쓴다. (새 항목이 추가돼도 기존 파일이 깨지지 않도록)
        /// 형식이 틀리면 JsonUtility가 ArgumentException을 던지고, DataFiles가 실패로 처리한다.
        /// </summary>
        private static bool TryParse(string json, out GameSettings settings)
        {
            // 구조체는 박싱한 복사본을 덮어쓴 뒤 다시 꺼내야 값이 반영된다.
            object boxed = GameSettings.CreateDefault();
            JsonUtility.FromJsonOverwrite(json, boxed);
            settings = (GameSettings)boxed;
            return true;
        }
    }
}
