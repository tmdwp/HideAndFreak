using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GuardVsMannequin.Editor
{
    /// <summary>
    /// UI를 프리팹으로 관리한다. 씬 생성 도구는 프리팹이 있으면 그것을 그대로 쓰고, 없을 때만 코드로 만들어 저장한다.
    /// 그래서 프리팹을 직접 고쳐도(이미지·폰트·배치 교체 등) "테스트 환경 생성"을 다시 실행해도 유지된다.
    ///
    /// 프리팹 (Assets/_Project/Prefabs/UI)
    /// - SettingsPanel, CreditsPanel: 공용 패널. 각 화면 캔버스 안에 중첩 프리팹으로 들어간다. (한 번 고치면 모든 화면에 반영)
    /// - LobbyCanvas, RoomCanvas, HudCanvas: 화면별 캔버스
    /// 코드가 참조하는 오브젝트(버튼, 텍스트 등)를 지우면 해당 기능이 동작하지 않으니, 모양만 바꾸고 오브젝트는 남겨 두는 것이 안전하다.
    ///
    /// 초기화 기준본 (프로젝트 루트의 UiDefaults 폴더, Assets 밖이라 Unity가 불러오지 않음)
    /// - "현재 UI를 초기화 기준으로 저장": 지금 프리팹(.prefab + .meta)을 기준본으로 복사한다.
    /// - "UI 프리팹 초기화": 기준본이 있으면 그 내용으로 덮어쓴다. GUID·내부 ID가 같아 씬의 프리팹 인스턴스가 그대로 갱신된다.
    ///   기준본이 없는 프리팹은 지워서 다음 "테스트 환경 생성" 때 코드 기본 모양으로 다시 만든다.
    /// - 프리팹이 없어 새로 만들어야 할 때도 기준본이 있으면 코드 대신 기준본을 쓴다.
    /// </summary>
    public static class UiPrefabLibrary
    {
        public const string Folder = "Assets/_Project/Prefabs/UI";
        public const string SettingsPanelPath = Folder + "/SettingsPanel.prefab";
        public const string CreditsPanelPath = Folder + "/CreditsPanel.prefab";
        public const string LobbyCanvasPath = Folder + "/LobbyCanvas.prefab";
        public const string RoomCanvasPath = Folder + "/RoomCanvas.prefab";
        public const string HudCanvasPath = Folder + "/HudCanvas.prefab";

        /// <summary>초기화 기준본 폴더. (프로젝트 루트 기준)</summary>
        public const string DefaultsFolder = "UiDefaults";

        /// <summary>모든 UI 프리팹. 캔버스가 패널을 중첩하므로 패널을 먼저 둔다(복원 순서).</summary>
        private static readonly string[] AllPaths = { SettingsPanelPath, CreditsPanelPath, LobbyCanvasPath, RoomCanvasPath, HudCanvasPath };

        /// <summary>프로젝트 루트 폴더 (Assets의 부모).</summary>
        private static string ProjectRoot => Path.GetDirectoryName(Application.dataPath);

        /// <summary>
        /// 캔버스처럼 씬 루트에 두는 UI 프리팹을 가져온다. 없으면 기준본을 복원하고, 기준본도 없으면 build로 씬에 만든 뒤 프리팹으로 저장한다.
        /// </summary>
        public static GameObject GetOrBuildRoot(string path, Func<GameObject> build)
        {
            GameObject existing = LoadOrRestore(path);
            if (existing != null)
                return existing;

            GameObject root = build();
            return SaveAndDestroy(root, path);
        }

        /// <summary>
        /// 캔버스 안에 들어가는 패널 프리팹을 가져온다. 없으면 기준본을 복원하고, 기준본도 없으면 임시 캔버스 안에 만들어 저장한다.
        /// </summary>
        public static GameObject GetOrBuildPanel(string path, Func<Transform, GameObject> build)
        {
            GameObject existing = LoadOrRestore(path);
            if (existing != null)
                return existing;

            Canvas temp = EditorUiFactory.CreateCanvas("TempCanvas");
            GameObject panel = build(temp.transform);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(panel, EnsureFolder(path));
            Object.DestroyImmediate(temp.gameObject);
            return prefab;
        }

        /// <summary>
        /// 프리팹 인스턴스(연결 유지)를 부모 아래 또는 씬 루트에 만든다.
        /// </summary>
        public static GameObject Instantiate(GameObject prefab, Transform parent = null)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            return instance;
        }

        /// <summary>
        /// 지금 UI 프리팹을 초기화 기준본으로 저장한다. 이후 "UI 프리팹 초기화"는 이 모양으로 되돌린다.
        /// </summary>
        [MenuItem("GuardVsMannequin/UI/현재 UI를 초기화 기준으로 저장")]
        private static void SaveDefaults()
        {
            if (!EditorUtility.DisplayDialog("초기화 기준 저장",
                    "지금 UI 프리팹을 초기화 기준본으로 저장합니다.\n이후 'UI 프리팹 초기화'를 하면 이 모양으로 되돌아갑니다.", "저장", "취소"))
                return;

            // 프리팹 편집 화면에서 저장하지 않은 변경도 파일에 쓴다.
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory(Path.Combine(ProjectRoot, DefaultsFolder));

            int saved = 0;
            foreach (string path in AllPaths)
            {
                string source = Path.Combine(ProjectRoot, path);
                if (!File.Exists(source))
                    continue;
                File.Copy(source, GetDefaultPath(path), true);
                File.Copy(source + ".meta", GetDefaultPath(path) + ".meta", true);
                saved++;
            }
            Debug.Log($"[UiPrefabLibrary] UI 프리팹 {saved}개를 초기화 기준본({DefaultsFolder}/)으로 저장했습니다.");
        }

        /// <summary>
        /// UI 프리팹을 초기화 기준본으로 되돌린다. 기준본이 없는 프리팹은 지워서 다음 "테스트 환경 생성" 때 코드 기본 모양으로 만든다.
        /// (기준본 이후에 고친 내용이 사라지므로 확인을 받는다)
        /// </summary>
        [MenuItem("GuardVsMannequin/UI/UI 프리팹 초기화 (저장한 기준으로 되돌리기)")]
        private static void ResetPrefabs()
        {
            if (!EditorUtility.DisplayDialog("UI 프리팹 초기화",
                    "UI 프리팹을 '현재 UI를 초기화 기준으로 저장'으로 저장한 모양으로 되돌립니다. 그 뒤에 고친 내용은 사라집니다.\n" +
                    "기준본이 없는 프리팹은 지워지며, '테스트 환경 생성'과 '맵 생성'을 다시 실행하면 코드 기본 모양으로 만들어집니다.", "되돌리기", "취소"))
                return;

            int restored = 0, deleted = 0;
            foreach (string path in AllPaths)
            {
                if (RestoreDefault(path))
                    restored++;
                else if (AssetDatabase.DeleteAsset(path))
                    deleted++;
            }
            Debug.Log($"[UiPrefabLibrary] 기준본으로 되돌림 {restored}개, 기준본이 없어 지움 {deleted}개." +
                      (deleted > 0 ? " 지운 프리팹은 '테스트 환경 생성'을 실행하면 다시 만들어집니다." : string.Empty));
        }

        /// <summary>
        /// 코드 기본 모양으로 되돌린다. 기준본은 그대로 두고 프리팹만 지운다.
        /// </summary>
        [MenuItem("GuardVsMannequin/UI/UI 프리팹 초기화 (코드 기본 모양, 기준본 무시)")]
        private static void ResetPrefabsToCodeDefaults()
        {
            if (!EditorUtility.DisplayDialog("코드 기본 모양으로 초기화",
                    "UI 프리팹을 지우고, '테스트 환경 생성' 때 기준본 대신 코드 기본 모양으로 다시 만듭니다.\n" +
                    $"그러려면 {DefaultsFolder} 폴더(기준본)도 지워야 합니다. 함께 지울까요?", "프리팹과 기준본 지우기", "취소"))
                return;

            foreach (string path in AllPaths)
                AssetDatabase.DeleteAsset(path);
            string defaults = Path.Combine(ProjectRoot, DefaultsFolder);
            if (Directory.Exists(defaults))
                Directory.Delete(defaults, true);
            Debug.Log("[UiPrefabLibrary] UI 프리팹과 기준본을 지웠습니다. '테스트 환경 생성'과 '맵 생성'을 실행하세요.");
        }

        /// <summary>
        /// 프리팹을 불러온다. 없고 기준본이 있으면 (중첩 순서를 위해 없는 프리팹 전부를) 기준본으로 복원한 뒤 불러온다.
        /// </summary>
        private static GameObject LoadOrRestore(string path)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null || !File.Exists(GetDefaultPath(path)))
                return existing;

            // 캔버스가 중첩한 패널이 먼저 있어야 참조가 이어지므로, 없는 프리팹을 패널부터 모두 복원한다.
            foreach (string other in AllPaths)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(other) == null)
                    RestoreDefault(other);
            }
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        /// <summary>
        /// 기준본 내용으로 프리팹 파일을 덮어쓴다. .meta가 없을 때만 기준본의 .meta를 복사해 GUID를 원래대로 맞춘다.
        /// </summary>
        /// <returns>기준본이 있어 복원했으면 true.</returns>
        private static bool RestoreDefault(string path)
        {
            string source = GetDefaultPath(path);
            if (!File.Exists(source))
                return false;

            string target = Path.Combine(ProjectRoot, EnsureFolder(path));
            File.Copy(source, target, true);
            if (!File.Exists(target + ".meta") && File.Exists(source + ".meta"))
                File.Copy(source + ".meta", target + ".meta");
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            return true;
        }

        /// <summary>
        /// 프리팹 경로에 해당하는 기준본 파일 경로.
        /// </summary>
        private static string GetDefaultPath(string path)
        {
            return Path.Combine(ProjectRoot, DefaultsFolder, Path.GetFileName(path));
        }

        /// <summary>
        /// 씬의 임시 오브젝트를 프리팹으로 저장하고 지운다.
        /// </summary>
        private static GameObject SaveAndDestroy(GameObject root, string path)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, EnsureFolder(path));
            Object.DestroyImmediate(root);
            return prefab;
        }

        /// <summary>
        /// 저장 폴더를 만든다.
        /// </summary>
        private static string EnsureFolder(string path)
        {
            string folder = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(folder) && !AssetDatabase.IsValidFolder(folder))
            {
                Directory.CreateDirectory(folder);
                AssetDatabase.Refresh();
            }
            return path;
        }
    }
}
