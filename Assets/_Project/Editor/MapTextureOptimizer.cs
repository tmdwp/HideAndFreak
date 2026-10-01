using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GuardVsMannequin.Editor
{
    /// <summary>
    /// 맵 씬이 쓰는 텍스처의 PC(Standalone) 최대 크기를 줄여 빌드 용량과 맵 로딩 시간을 줄인다.
    /// 빌드 텍스처의 대부분이 2048 크기 맵 에셋 팩 텍스처라, 1024로 낮추면 용량이 약 1/4이 된다.
    /// - 원본 파일과 기본(Default) 설정은 바꾸지 않고 Standalone 플랫폼 덮어쓰기만 추가한다.
    /// - 이 도구가 바꾼 텍스처는 userData에 표시를 남겨, "되돌리기"가 그것만 원래대로 돌린다.
    /// - 에셋 팩 제작자가 이미 Standalone 설정을 지정한 텍스처, UI·스프라이트, _Project 폴더 텍스처는 건드리지 않는다.
    /// </summary>
    public static class MapTextureOptimizer
    {
        private const int TargetMaxSize = 1024;
        private const string StandalonePlatform = "Standalone";
        private const string Marker = "GVM_MapTextureLimit";
        private const string ProjectFolder = "Assets/_Project/";

        /// <summary>
        /// 모든 맵 씬이 쓰는 텍스처의 PC 최대 크기를 1024로 제한한다.
        /// </summary>
        [MenuItem("GuardVsMannequin/맵 최적화/맵 텍스처 크기 줄이기 (최대 1024)")]
        public static void LimitMapTextures()
        {
            List<string> texturePaths = CollectMapTexturePaths();
            if (!EditorUtility.DisplayDialog("맵 텍스처 크기 줄이기",
                    $"맵 씬이 쓰는 텍스처 {texturePaths.Count}개 중 {TargetMaxSize}보다 큰 것을 PC용 {TargetMaxSize}로 제한합니다.\n" +
                    "다시 가져오기(Reimport)에 몇 분 걸릴 수 있습니다. 원본은 바뀌지 않으며 '되돌리기' 메뉴로 원래대로 돌릴 수 있습니다.",
                    "진행", "취소"))
                return;

            int changed = ProcessTextures(texturePaths, "텍스처 크기 제한", TryLimit);
            Debug.Log($"[MapTextureOptimizer] 텍스처 {changed}개의 PC 최대 크기를 {TargetMaxSize}로 제한했습니다.");
            EditorUtility.DisplayDialog("맵 텍스처 크기 줄이기", $"텍스처 {changed}개를 {TargetMaxSize}로 제한했습니다.", "확인");
        }

        /// <summary>
        /// 이 도구가 추가한 PC 크기 제한을 모두 제거한다.
        /// </summary>
        [MenuItem("GuardVsMannequin/맵 최적화/맵 텍스처 크기 되돌리기")]
        public static void RestoreMapTextures()
        {
            var markedPaths = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is TextureImporter importer && importer.userData == Marker)
                    markedPaths.Add(path);
            }

            int restored = ProcessTextures(markedPaths, "텍스처 크기 되돌리기", TryRestore);
            Debug.Log($"[MapTextureOptimizer] 텍스처 {restored}개의 PC 크기 제한을 제거했습니다.");
            EditorUtility.DisplayDialog("맵 텍스처 크기 되돌리기", $"텍스처 {restored}개를 원래 크기로 되돌렸습니다.", "확인");
        }

        /// <summary>
        /// 맵 목록의 모든 씬이 참조하는 텍스처 경로를 모은다. (중복 제거)
        /// </summary>
        private static List<string> CollectMapTexturePaths()
        {
            var result = new HashSet<string>();
            foreach (MapCatalog.Entry map in MapCatalog.AllMaps)
            {
                if (!File.Exists(map.ScenePath))
                    continue;
                foreach (string dependency in AssetDatabase.GetDependencies(map.ScenePath, true))
                {
                    if (!dependency.StartsWith(ProjectFolder) && AssetImporter.GetAtPath(dependency) is TextureImporter)
                        result.Add(dependency);
                }
            }
            return new List<string>(result);
        }

        /// <summary>
        /// 텍스처마다 처리 함수를 실행하고, 바뀐 것만 한 번에 다시 가져온다. 바뀐 개수를 반환한다.
        /// </summary>
        private static int ProcessTextures(List<string> paths, string title, System.Func<TextureImporter, bool> process)
        {
            int changed = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                for (int i = 0; i < paths.Count; i++)
                {
                    if (EditorUtility.DisplayCancelableProgressBar(title, paths[i], (float)i / Mathf.Max(1, paths.Count)))
                        break;

                    if (AssetImporter.GetAtPath(paths[i]) is not TextureImporter importer || !process(importer))
                        continue;
                    importer.SaveAndReimport();
                    changed++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
            }
            return changed;
        }

        /// <summary>
        /// 3D용 텍스처(기본·노멀맵)이고 PC 설정이 비어 있으며 목표보다 크면 PC 최대 크기를 제한한다.
        /// </summary>
        private static bool TryLimit(TextureImporter importer)
        {
            if (importer.textureType != TextureImporterType.Default && importer.textureType != TextureImporterType.NormalMap)
                return false;
            if (!string.IsNullOrEmpty(importer.userData))
                return false;

            TextureImporterPlatformSettings standalone = importer.GetPlatformTextureSettings(StandalonePlatform);
            if (standalone.overridden)
                return false;

            TextureImporterPlatformSettings defaults = importer.GetDefaultPlatformTextureSettings();
            if (defaults.maxTextureSize <= TargetMaxSize)
                return false;

            // 기본 설정(압축 방식 등)을 그대로 복사하고 크기만 줄인다.
            defaults.name = StandalonePlatform;
            defaults.overridden = true;
            defaults.maxTextureSize = TargetMaxSize;
            importer.SetPlatformTextureSettings(defaults);
            importer.userData = Marker;
            return true;
        }

        /// <summary>
        /// 이 도구가 추가한 PC 설정을 끄고 표시를 지운다.
        /// </summary>
        private static bool TryRestore(TextureImporter importer)
        {
            if (importer.userData != Marker)
                return false;

            TextureImporterPlatformSettings standalone = importer.GetPlatformTextureSettings(StandalonePlatform);
            standalone.overridden = false;
            importer.SetPlatformTextureSettings(standalone);
            importer.userData = string.Empty;
            return true;
        }
    }
}
