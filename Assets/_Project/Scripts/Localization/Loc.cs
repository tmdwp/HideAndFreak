using System;
using System.Collections.Generic;
using GuardVsMannequin.Core;
using GuardVsMannequin.Settings;
using UnityEngine;

namespace GuardVsMannequin.Localization
{
    /// <summary>
    /// 화면에 보이는 문장을 언어별로 제공한다.
    /// 문장은 Resources/Localization/strings.csv(열: key, ko, en, ...)에서 읽는다.
    /// 새 언어는 CSV에 열을 하나 추가하는 것만으로 설정 화면의 언어 목록에 나타난다.
    /// 번역이 비어 있으면 첫 번째 언어(ko)로, 키 자체가 없으면 키 이름을 보여준다.
    /// </summary>
    public static class Loc
    {
        private const string TablePath = "Localization/strings";
        private const string LanguageNameKey = "language.name";

        private static readonly Dictionary<string, Dictionary<string, string>> table = new();
        private static readonly List<string> languages = new();
        private static bool isLoaded;
        private static string currentLanguage;

        /// <summary>언어가 바뀌었을 때.</summary>
        public static event Action LanguageChanged;

        /// <summary>사용할 수 있는 언어 코드 목록. (CSV 머리글 순서)</summary>
        public static IReadOnlyList<string> Languages
        {
            get
            {
                EnsureLoaded();
                return languages;
            }
        }

        /// <summary>현재 언어 코드.</summary>
        public static string CurrentLanguage
        {
            get
            {
                EnsureLoaded();
                return currentLanguage;
            }
        }

        /// <summary>
        /// 현재 언어의 문장을 반환한다.
        /// </summary>
        public static string Get(string key)
        {
            EnsureLoaded();
            return Lookup(key, currentLanguage);
        }

        /// <summary>
        /// 문장 표에 키가 있는지 확인한다.
        /// </summary>
        public static bool HasKey(string key)
        {
            EnsureLoaded();
            return !string.IsNullOrEmpty(key) && table.ContainsKey(key);
        }

        /// <summary>
        /// 현재 언어의 문장에 값을 끼워 넣어 반환한다. ({0}, {1} ...)
        /// </summary>
        public static string Format(string key, params object[] args)
        {
            string pattern = Get(key);
            try
            {
                return string.Format(pattern, args);
            }
            catch (FormatException)
            {
                Debug.LogWarning($"[Loc] 형식이 맞지 않는 문장입니다: {key} = {pattern}");
                return pattern;
            }
        }

        /// <summary>
        /// 언어의 표시 이름(그 언어로 쓴 이름, 예: 한국어, English)을 반환한다.
        /// </summary>
        public static string GetLanguageName(string languageCode)
        {
            EnsureLoaded();
            return Lookup(LanguageNameKey, languageCode);
        }

        /// <summary>
        /// 언어를 바꾸고 설정에 저장한 뒤 알린다.
        /// </summary>
        public static void SetLanguage(string languageCode)
        {
            EnsureLoaded();
            if (!languages.Contains(languageCode) || languageCode == currentLanguage)
                return;

            currentLanguage = languageCode;
            GameOptions options = OptionsStore.Current;
            options.languageCode = languageCode;
            OptionsStore.SetOptions(options);
            LanguageChanged?.Invoke();
        }

        /// <summary>
        /// 문장 표를 다시 읽는다. (에디터 도구가 CSV 수정 내용을 바로 반영할 때 사용)
        /// </summary>
        public static void Reload()
        {
            isLoaded = false;
            table.Clear();
            languages.Clear();
            EnsureLoaded();
            LanguageChanged?.Invoke();
        }

        /// <summary>
        /// 문장 표를 한 번만 불러오고, 저장된 언어(없으면 시스템 언어)를 고른다.
        /// </summary>
        private static void EnsureLoaded()
        {
            if (isLoaded)
                return;
            isLoaded = true;

            TextAsset asset = Resources.Load<TextAsset>(TablePath);
            if (asset == null)
            {
                Debug.LogError($"[Loc] 문장 파일을 찾을 수 없습니다: Resources/{TablePath}.csv");
                languages.Add("ko");
                currentLanguage = "ko";
                return;
            }

            LoadTable(asset.text);
            currentLanguage = ChooseInitialLanguage();
        }

        /// <summary>
        /// CSV를 읽어 키별·언어별 문장 표를 만든다.
        /// </summary>
        private static void LoadTable(string csv)
        {
            List<string[]> rows = CsvParser.Parse(csv);
            if (rows.Count == 0)
                return;

            string[] header = rows[0];
            for (int column = 1; column < header.Length; column++)
                languages.Add(header[column]);

            for (int rowIndex = 1; rowIndex < rows.Count; rowIndex++)
            {
                string[] row = rows[rowIndex];
                if (row.Length == 0 || string.IsNullOrEmpty(row[0]))
                    continue;

                var translations = new Dictionary<string, string>();
                for (int column = 1; column < header.Length && column < row.Length; column++)
                {
                    if (!string.IsNullOrEmpty(row[column]))
                        translations[header[column]] = row[column];
                }
                table[row[0]] = translations;
            }
        }

        /// <summary>
        /// 저장된 언어가 있으면 그것을, 없으면 시스템 언어(한국어면 ko, 아니면 en)를 고른다.
        /// </summary>
        private static string ChooseInitialLanguage()
        {
            string saved = OptionsStore.Current.languageCode;
            if (!string.IsNullOrEmpty(saved) && languages.Contains(saved))
                return saved;

            string system = Application.systemLanguage == SystemLanguage.Korean ? "ko" : "en";
            return languages.Contains(system) ? system : languages[0];
        }

        /// <summary>
        /// 문장을 찾는다. 번역이 없으면 첫 번째 언어, 키가 없으면 키 이름을 반환한다.
        /// </summary>
        private static string Lookup(string key, string languageCode)
        {
            if (string.IsNullOrEmpty(key))
                return string.Empty;
            if (!table.TryGetValue(key, out Dictionary<string, string> translations))
                return key;
            if (translations.TryGetValue(languageCode, out string text))
                return text;
            return languages.Count > 0 && translations.TryGetValue(languages[0], out string fallback) ? fallback : key;
        }
    }
}
