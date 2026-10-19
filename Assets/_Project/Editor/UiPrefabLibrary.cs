using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GuardVsMannequin.Editor
{
    /// <summary>
    /// UI를 프리팹으로 관리한다. 씬 생성 도구는 프리팹이 있으면 그것을 그대로 쓰고, 없을 때만 만들어 저장한다.
    /// 그래서 프리팹을 직접 고쳐도(이미지·폰트·배치 교체 등) "테스트 환경 생성"을 다시 실행해도 유지된다.
    /// 프리팹이 없을 때는 초기화 기준본(<see cref="UiPrefabDefaults"/>)을 먼저 쓰고, 기준본도 없으면 코드 기본 모양으로 만든다.
    /// 메뉴는 <see cref="UiPrefabMenus"/>.
    ///
    /// 프리팹 (Assets/_Project/Prefabs/UI)
    /// - SettingsPanel, CreditsPanel: 공용 패널. 각 화면 캔버스 안에 중첩 프리팹으로 들어간다. (한 번 고치면 모든 화면에 반영)
    /// - LobbyCanvas, RoomCanvas, HudCanvas: 화면별 캔버스
    /// 코드가 참조하는 오브젝트(버튼, 텍스트 등)를 지우면 해당 기능이 동작하지 않으니, 모양만 바꾸고 오브젝트는 남겨 두는 것이 안전하다.
    /// </summary>
    public static class UiPrefabLibrary
    {
        public const string Folder = "Assets/_Project/Prefabs/UI";
        public const string SettingsPanelPath = Folder + "/SettingsPanel.prefab";
        public const string CreditsPanelPath = Folder + "/CreditsPanel.prefab";
        public const string LobbyCanvasPath = Folder + "/LobbyCanvas.prefab";
        public const string RoomCanvasPath = Folder + "/RoomCanvas.prefab";
        public const string HudCanvasPath = Folder + "/HudCanvas.prefab";

        /// <summary>모든 UI 프리팹. 캔버스가 패널을 중첩하므로 패널을 먼저 둔다(복원 순서).</summary>
        public static readonly string[] AllPaths = { SettingsPanelPath, CreditsPanelPath, LobbyCanvasPath, RoomCanvasPath, HudCanvasPath };

        /// <summary>
        /// 캔버스처럼 씬 루트에 두는 UI 프리팹을 가져온다. 없으면 기준본을 복원하고, 기준본도 없으면 build로 씬에 만든 뒤 프리팹으로 저장한다.
        /// </summary>
        public static GameObject GetOrBuildRoot(string path, Func<GameObject> build)
        {
            GameObject existing = LoadOrRestore(path);
            if (existing != null)
                return existing;

            GameObject root = build();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, EnsureFolder(path));
            Object.DestroyImmediate(root);
            return prefab;
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
            return (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        }

        /// <summary>
        /// 저장 폴더를 만든다.
        /// </summary>
        /// <returns>받은 경로 그대로.</returns>
        public static string EnsureFolder(string path)
        {
            string folder = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(folder) && !AssetDatabase.IsValidFolder(folder))
            {
                Directory.CreateDirectory(folder);
                AssetDatabase.Refresh();
            }
            return path;
        }

        /// <summary>
        /// 프리팹을 불러온다. 없고 기준본이 있으면 (중첩 순서를 위해 없는 프리팹 전부를) 기준본으로 복원한 뒤 불러온다.
        /// </summary>
        private static GameObject LoadOrRestore(string path)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null || !UiPrefabDefaults.Exists(path))
                return existing;

            // 캔버스가 중첩한 패널이 먼저 있어야 참조가 이어지므로, 없는 프리팹을 패널부터 모두 복원한다.
            foreach (string other in AllPaths)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(other) == null)
                    UiPrefabDefaults.Restore(other);
            }
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
    }
}
