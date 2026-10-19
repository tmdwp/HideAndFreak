using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.SceneManagement;

namespace GuardVsMannequin.Mods
{
    /// <summary>
    /// 게임에 들어 있는 씬(빌드 설정의 씬) 이름 목록.
    /// 모드 맵 씬이 이 이름과 같으면, 이름으로 씬을 불러올 때 PC마다 내장 씬과 모드 씬 중 다른 쪽이 열리거나
    /// 게스트가 씬을 찾지 못할 수 있으므로 모드 검사에서 막는다.
    /// </summary>
    public static class BuiltInScenes
    {
        private static HashSet<string> names;

        /// <summary>
        /// 플레이 모드에 들어갈 때 목록을 다시 읽게 한다. (도메인 재로드를 끈 채 빌드 설정을 바꾼 경우)
        /// </summary>
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            names = null;
        }

        /// <summary>
        /// 씬 이름이 내장 씬과 같은지 여부. (대소문자 무시 — 네트워크 씬 조회가 대소문자를 구분하지 않음)
        /// </summary>
        public static bool Contains(string sceneName)
        {
            if (names == null)
            {
                names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
                    names.Add(Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(i)));
            }
            return !string.IsNullOrEmpty(sceneName) && names.Contains(sceneName);
        }
    }
}
