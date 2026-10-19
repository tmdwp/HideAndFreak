using System.Collections.Generic;
using UnityEngine;

namespace GuardVsMannequin.Mods
{
    /// <summary>
    /// 플레이어가 끈 모드 목록을 이 PC(PlayerPrefs)에 저장한다. 새로 설치한 모드는 켜진 상태로 시작한다.
    /// </summary>
    public static class ModPreferences
    {
        private const string DisabledKey = "Mods.Disabled";
        private const char Separator = '\n';

        /// <summary>
        /// 끈 모드 ID 목록을 읽는다.
        /// </summary>
        public static HashSet<string> LoadDisabled()
        {
            string saved = PlayerPrefs.GetString(DisabledKey, string.Empty);
            var result = new HashSet<string>();
            foreach (string id in saved.Split(Separator))
            {
                if (!string.IsNullOrWhiteSpace(id))
                    result.Add(id.Trim());
            }
            return result;
        }

        /// <summary>
        /// 모드를 켜거나 끈 상태를 저장한다.
        /// </summary>
        public static void SetEnabled(string modId, bool enabled)
        {
            HashSet<string> disabled = LoadDisabled();
            bool changed = enabled ? disabled.Remove(modId) : disabled.Add(modId);
            if (!changed)
                return;

            PlayerPrefs.SetString(DisabledKey, string.Join(Separator.ToString(), disabled));
            PlayerPrefs.Save();
        }
    }
}
