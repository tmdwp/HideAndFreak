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
    /// 처음 모양으로 되돌리려면 메뉴 "UI 프리팹 초기화" 후 "테스트 환경 생성"을 실행한다.
    /// </summary>
    public static class UiPrefabLibrary
    {
        public const string Folder = "Assets/_Project/Prefabs/UI";
        public const string SettingsPanelPath = Folder + "/SettingsPanel.prefab";
        public const string CreditsPanelPath = Folder + "/CreditsPanel.prefab";
        public const string LobbyCanvasPath = Folder + "/LobbyCanvas.prefab";
        public const string RoomCanvasPath = Folder + "/RoomCanvas.prefab";
        public const string HudCanvasPath = Folder + "/HudCanvas.prefab";

        /// <summary>
        /// 캔버스처럼 씬 루트에 두는 UI 프리팹을 가져온다. 없으면 build로 씬에 만든 뒤 프리팹으로 저장한다.
        /// </summary>
        public static GameObject GetOrBuildRoot(string path, Func<GameObject> build)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
                return existing;

            GameObject root = build();
            return SaveAndDestroy(root, path);
        }

        /// <summary>
        /// 캔버스 안에 들어가는 패널 프리팹을 가져온다. 없으면 임시 캔버스 안에 만들어 저장한다.
        /// </summary>
        public static GameObject GetOrBuildPanel(string path, Func<Transform, GameObject> build)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
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
        /// UI 프리팹을 모두 지운다. 다음 "테스트 환경 생성" 때 코드 기본 모양으로 다시 만들어진다.
        /// (직접 고친 내용이 사라지므로 확인을 받는다)
        /// </summary>
        [MenuItem("GuardVsMannequin/UI/UI 프리팹 초기화 (기본 모양으로 되돌리기)")]
        private static void ResetPrefabs()
        {
            if (!EditorUtility.DisplayDialog("UI 프리팹 초기화",
                    "Assets/_Project/Prefabs/UI의 UI 프리팹을 지웁니다. 직접 고친 내용이 사라집니다.\n" +
                    "지운 뒤 '테스트 환경 생성'과 '맵 생성'을 다시 실행해야 씬에 반영됩니다.", "지우기", "취소"))
                return;

            foreach (string path in new[] { SettingsPanelPath, CreditsPanelPath, LobbyCanvasPath, RoomCanvasPath, HudCanvasPath })
                AssetDatabase.DeleteAsset(path);
            Debug.Log("[UiPrefabLibrary] UI 프리팹을 지웠습니다. '테스트 환경 생성'을 실행하세요.");
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
