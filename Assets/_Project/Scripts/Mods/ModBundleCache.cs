using System.Collections.Generic;
using UnityEngine;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

namespace GuardVsMannequin.Mods
{
    /// <summary>
    /// 모드 번들을 불러오고 해제한다.
    /// - 모드 맵 씬을 불러오기 직전에 그 모드(와 필요한 모드)의 에셋 번들, 그다음 씬 번들을 불러온다.
    ///   (씬이 외형·머티리얼 등 에셋 번들 안의 것을 참조하므로 에셋 번들이 먼저다)
    /// - 모드 맵 씬이 하나도 열려 있지 않으면 모든 모드 번들을 해제한다. (룸으로 돌아온 뒤)
    /// </summary>
    public static class ModBundleCache
    {
        private static readonly Dictionary<string, AssetBundle> loaded = new();

        /// <summary>
        /// 플레이 모드에 들어갈 때 기록을 비운다. (도메인 재로드를 끈 경우)
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            loaded.Clear();
        }

        /// <summary>
        /// 씬이 모드 맵이면 필요한 번들을 불러온다. 내장 씬이면 아무것도 하지 않는다. 실패하면 false.
        /// </summary>
        public static bool EnsureForScene(string sceneName)
        {
            MapEntry map = FindModMap(sceneName);
            if (map == null)
                return true;
            if (!ModService.Instance.TryGet(map.SourceId, out ModInfo mod) || mod.Status != ModStatus.Active)
            {
                Debug.LogError($"[ModBundleCache] 맵 '{map.Id}'의 모드 '{map.SourceId}'를 쓸 수 없습니다.");
                return false;
            }

            foreach (string dependencyId in mod.Manifest.dependencies)
            {
                if (ModService.Instance.TryGet(dependencyId, out ModInfo dependency))
                    Load(dependency.GetFilePath(dependency.Manifest.assetBundle));
            }
            Load(mod.GetFilePath(mod.Manifest.assetBundle));
            return Load(mod.GetFilePath(mod.Manifest.sceneBundle));
        }

        /// <summary>
        /// 모드 맵 씬이 하나도 열려 있지 않으면 불러온 모든 모드 번들을 해제한다.
        /// </summary>
        public static void UnloadIfUnused()
        {
            if (loaded.Count == 0)
                return;

            foreach (MapEntry map in ContentRegistry.Instance.Maps)
            {
                if (!map.IsBuiltIn && UnitySceneManager.GetSceneByName(map.SceneName).isLoaded)
                    return;
            }

            foreach (AssetBundle bundle in loaded.Values)
            {
                if (bundle != null)
                    bundle.Unload(true);
            }
            loaded.Clear();
        }

        /// <summary>
        /// 번들 파일 하나를 불러온다. 경로가 없으면(번들이 없는 모드) 건너뛴다. 이미 불러왔으면 그대로 쓴다.
        /// </summary>
        private static bool Load(string path)
        {
            if (string.IsNullOrEmpty(path) || loaded.ContainsKey(path))
                return true;

            AssetBundle bundle = AssetBundle.LoadFromFile(path);
            if (bundle == null)
            {
                Debug.LogError($"[ModBundleCache] 번들을 불러올 수 없습니다: {path} (Unity 버전·빌드 대상이 다르거나 파일이 손상됨)");
                return false;
            }
            loaded[path] = bundle;
            return true;
        }

        /// <summary>
        /// 씬 이름으로 모드 맵을 찾는다. 내장 맵이거나 없으면 null.
        /// </summary>
        private static MapEntry FindModMap(string sceneName)
        {
            foreach (MapEntry map in ContentRegistry.Instance.Maps)
            {
                if (!map.IsBuiltIn && map.SceneName == sceneName)
                    return map;
            }
            return null;
        }
    }
}
