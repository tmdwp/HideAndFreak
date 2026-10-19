using System;
using System.IO;
using UnityEngine;

namespace GuardVsMannequin.Core
{
    /// <summary>
    /// 사람이 편집하는 텍스트 데이터 파일(문장, 크레딧, 게임 설정)을 읽는다.
    /// 우선순위: ① 빌드 폴더의 외부 파일(StreamingAssets/GameData) → ② 게임에 내장된 원본(Resources) → ③ 호출자의 코드 기본값.
    /// 파일이 없거나, 인코딩이 깨졌거나, 호출자의 해석에 실패하면 다음 순서로 넘어간다.
    /// 원본은 Assets/_Project/Resources 아래에 두고, 빌드 시 에디터 스크립트가 외부 폴더로 복사한다.
    /// </summary>
    public static class DataFiles
    {
        /// <summary>외부 데이터 폴더 이름. (StreamingAssets 아래)</summary>
        public const string ExternalFolderName = "GameData";

        /// <summary>빌드 후에도 편집할 수 있게 복사하는 Resources 하위 폴더.</summary>
        public static readonly string[] EditableFolders = { "Localization", "Credits", "Data" };

        /// <summary>텍스트를 해석해 결과를 만드는 함수. 해석할 수 없으면 false.</summary>
        public delegate bool TextParser<T>(string text, out T result);

        /// <summary>외부 데이터 폴더의 전체 경로.</summary>
        public static string ExternalRoot => Path.Combine(Application.streamingAssetsPath, ExternalFolderName);

        /// <summary>
        /// 외부 파일 → 내장 파일 순서로 읽고 해석한다. 둘 다 실패하면 false. (호출자가 코드 기본값을 쓴다)
        /// </summary>
        /// <param name="resourcePath">Resources 기준 경로(확장자 제외). 예: "Data/default_game_settings"</param>
        /// <param name="extension">외부 파일 확장자. 예: ".json"</param>
        /// <param name="parser">텍스트 해석 함수.</param>
        /// <param name="result">해석 결과.</param>
        public static bool TryLoad<T>(string resourcePath, string extension, TextParser<T> parser, out T result)
        {
            string externalPath = GetExternalPath(resourcePath, extension);
            if (TryReadExternal(externalPath, out string externalText))
            {
                if (TryParse(parser, externalText, out result))
                    return true;
                Debug.LogWarning($"[DataFiles] 외부 파일을 해석할 수 없어 내장 원본을 사용합니다: {externalPath}");
            }

            if (TryReadBuiltIn(resourcePath, out string builtInText))
            {
                if (TryParse(parser, builtInText, out result))
                    return true;
                Debug.LogError($"[DataFiles] 내장 원본을 해석할 수 없습니다: Resources/{resourcePath}");
            }

            result = default;
            return false;
        }

        /// <summary>
        /// 외부 파일 경로를 만든다.
        /// </summary>
        public static string GetExternalPath(string resourcePath, string extension)
        {
            return Path.Combine(ExternalRoot, resourcePath.Replace('/', Path.DirectorySeparatorChar) + extension);
        }

        /// <summary>
        /// 외부 파일을 UTF-8로 읽는다. 없거나 읽을 수 없거나 UTF-8이 아니면(깨진 글자 포함) false.
        /// </summary>
        private static bool TryReadExternal(string path, out string text)
        {
            text = null;
            if (!File.Exists(path))
                return false;

            try
            {
                text = File.ReadAllText(path, System.Text.Encoding.UTF8);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Debug.LogWarning($"[DataFiles] 외부 파일을 읽을 수 없습니다: {path} ({exception.Message})");
                return false;
            }

            // 엑셀 등에서 UTF-8이 아닌 형식(ANSI)으로 저장하면 한글이 대체 문자(U+FFFD)로 바뀐다.
            if (text.IndexOf('�') >= 0)
            {
                Debug.LogWarning($"[DataFiles] UTF-8로 저장되지 않은 파일이라 사용하지 않습니다: {path}");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 게임에 내장된 원본(Resources)을 읽는다.
        /// </summary>
        private static bool TryReadBuiltIn(string resourcePath, out string text)
        {
            TextAsset asset = Resources.Load<TextAsset>(resourcePath);
            text = asset != null ? asset.text : null;
            return asset != null;
        }

        /// <summary>
        /// 빈 파일이나 해석 중 예외도 실패로 처리한다.
        /// </summary>
        private static bool TryParse<T>(TextParser<T> parser, string text, out T result)
        {
            result = default;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            try
            {
                return parser(text, out result);
            }
            catch (Exception exception) when (exception is ArgumentException || exception is FormatException)
            {
                Debug.LogWarning($"[DataFiles] 형식 오류: {exception.Message}");
                return false;
            }
        }
    }
}
