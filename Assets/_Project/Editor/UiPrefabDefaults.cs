using System.IO;
using UnityEditor;
using UnityEngine;

namespace GuardVsMannequin.Editor
{
    /// <summary>
    /// UI 프리팹의 초기화 기준본 저장소. (프로젝트 루트의 UiDefaults 폴더, Assets 밖이라 Unity가 불러오지 않음)
    /// 기준본은 .prefab과 .meta를 함께 보관한다. 되돌릴 때 파일 내용을 그대로 덮어쓰므로 GUID·내부 ID가 같아 씬의 프리팹 인스턴스가 그대로 갱신된다.
    /// </summary>
    public static class UiPrefabDefaults
    {
        /// <summary>초기화 기준본 폴더. (프로젝트 루트 기준)</summary>
        public const string Folder = "UiDefaults";

        /// <summary>프로젝트 루트 폴더 (Assets의 부모).</summary>
        private static string ProjectRoot => Path.GetDirectoryName(Application.dataPath);

        /// <summary>
        /// 프리팹의 기준본이 있는지 여부.
        /// </summary>
        public static bool Exists(string prefabPath)
        {
            return File.Exists(GetDefaultPath(prefabPath));
        }

        /// <summary>
        /// 지금 프리팹 파일(.prefab + .meta)을 기준본으로 복사한다.
        /// </summary>
        /// <returns>프리팹이 있어 저장했으면 true.</returns>
        public static bool Save(string prefabPath)
        {
            string source = Path.Combine(ProjectRoot, prefabPath);
            if (!File.Exists(source))
                return false;

            Directory.CreateDirectory(Path.Combine(ProjectRoot, Folder));
            File.Copy(source, GetDefaultPath(prefabPath), true);
            File.Copy(source + ".meta", GetDefaultPath(prefabPath) + ".meta", true);
            return true;
        }

        /// <summary>
        /// 기준본 내용으로 프리팹 파일을 덮어쓴다. .meta가 없을 때만 기준본의 .meta를 복사해 GUID를 원래대로 맞춘다.
        /// </summary>
        /// <returns>기준본이 있어 복원했으면 true.</returns>
        public static bool Restore(string prefabPath)
        {
            string source = GetDefaultPath(prefabPath);
            if (!File.Exists(source))
                return false;

            string target = Path.Combine(ProjectRoot, UiPrefabLibrary.EnsureFolder(prefabPath));
            File.Copy(source, target, true);
            if (!File.Exists(target + ".meta") && File.Exists(source + ".meta"))
                File.Copy(source + ".meta", target + ".meta");
            AssetDatabase.ImportAsset(prefabPath, ImportAssetOptions.ForceUpdate);
            return true;
        }

        /// <summary>
        /// 기준본을 모두 지운다.
        /// </summary>
        public static void DeleteAll()
        {
            string folder = Path.Combine(ProjectRoot, Folder);
            if (Directory.Exists(folder))
                Directory.Delete(folder, true);
        }

        /// <summary>
        /// 프리팹 경로에 해당하는 기준본 파일 경로.
        /// </summary>
        private static string GetDefaultPath(string prefabPath)
        {
            return Path.Combine(ProjectRoot, Folder, Path.GetFileName(prefabPath));
        }
    }
}
