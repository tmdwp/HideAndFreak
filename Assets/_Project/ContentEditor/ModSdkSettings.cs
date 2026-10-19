using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace GuardVsMannequin.Editor
{
    /// <summary>
    /// 모드 SDK에 함께 들어가는 게임 그래픽 설정(URP)과 예제 템플릿의 위치, 그리고 SDK 프로젝트에 그 설정을 적용하는 메뉴.
    /// 모드 번들의 셰이더는 제작 프로젝트의 URP 설정으로 만들어지므로, 게임과 같은 설정을 써야 게임에서 똑같이 보인다.
    /// </summary>
    public static class ModSdkSettings
    {
        /// <summary>SDK 전용 파일 폴더. (게임 그래픽 설정, 예제 템플릿)</summary>
        public const string Root = "Assets/_Project/ModSdk";

        /// <summary>게임 그래픽 설정 폴더.</summary>
        public const string SettingsFolder = Root + "/Settings";

        /// <summary>게임과 같은 URP 설정.</summary>
        public const string PipelineAssetPath = SettingsFolder + "/HF_RPAsset.asset";

        /// <summary>URP 렌더러 설정. (게임 전용 렌더러 기능은 뺀 것)</summary>
        public const string RendererPath = SettingsFolder + "/HF_Renderer.asset";

        /// <summary>기본 볼륨 프로필.</summary>
        public const string VolumeProfilePath = SettingsFolder + "/HF_VolumeProfile.asset";

        /// <summary>예제 모드 템플릿 폴더.</summary>
        public const string TemplateFolder = Root + "/Template";

        /// <summary>SDK를 만든 게임의 Unity 버전을 적은 파일. (모드 내보내기 전에 제작 Unity 버전을 확인)</summary>
        public const string GameUnityVersionPath = SettingsFolder + "/game_unity_version.txt";

        /// <summary>SDK를 만든 게임에 들어 있는 씬 이름 목록 파일(한 줄에 하나). 모드 맵 씬 이름이 겹치는지 확인한다.</summary>
        public const string GameSceneNamesPath = SettingsFolder + "/game_scene_names.txt";

        /// <summary>
        /// 게임에 들어 있는 씬 이름. 목록 파일이 있으면(SDK 프로젝트) 그것을, 없으면(게임 프로젝트 자신) 빌드 설정의 켜진 씬을 쓴다.
        /// </summary>
        public static string[] GameSceneNames
        {
            get
            {
                string fullPath = System.IO.Path.GetFullPath(GameSceneNamesPath);
                if (System.IO.File.Exists(fullPath))
                    return System.IO.File.ReadAllLines(fullPath).Select(line => line.Trim()).Where(line => line.Length > 0).ToArray();
                return EditorBuildSettings.scenes.Where(scene => scene.enabled)
                    .Select(scene => System.IO.Path.GetFileNameWithoutExtension(scene.path)).ToArray();
            }
        }

        /// <summary>
        /// 씬 이름이 게임에 들어 있는 씬과 같은지 여부. (대소문자 무시)
        /// </summary>
        public static bool IsGameSceneName(string sceneName)
        {
            return GameSceneNames.Any(name => string.Equals(name, sceneName, System.StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// SDK를 만든 게임의 Unity 버전. 파일이 없으면(게임 프로젝트 자신) 이 에디터의 버전.
        /// </summary>
        public static string GameUnityVersion
        {
            get
            {
                string fullPath = System.IO.Path.GetFullPath(GameUnityVersionPath);
                return System.IO.File.Exists(fullPath) ? System.IO.File.ReadAllText(fullPath).Trim() : Application.unityVersion;
            }
        }

        /// <summary>
        /// 이 프로젝트의 그래픽 설정을 게임과 같게 맞춘다. (URP 설정, 모든 품질 단계, 선형 색 공간, Windows 64비트)
        /// </summary>
        [MenuItem(EditorMenus.Mod + "게임 그래픽 설정 적용")]
        public static void ApplyGameGraphics()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(PipelineAssetPath);
            if (pipeline == null)
            {
                EditorUtility.DisplayDialog("게임 그래픽 설정", $"설정 파일이 없습니다: {PipelineAssetPath}\nSDK 패키지를 다시 가져오세요.", "확인");
                return;
            }

            GraphicsSettings.defaultRenderPipeline = pipeline;
            int current = QualitySettings.GetQualityLevel();
            for (int level = 0; level < QualitySettings.names.Length; level++)
            {
                QualitySettings.SetQualityLevel(level, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(current, false);
            PlayerSettings.colorSpace = ColorSpace.Linear;

            AssetDatabase.SaveAssets();
            Debug.Log("[ModSdkSettings] 게임 그래픽 설정(URP·품질 단계·선형 색 공간)을 적용했습니다.");
            EditorUtility.DisplayDialog("게임 그래픽 설정", "게임과 같은 그래픽 설정을 적용했습니다.\n모드를 내보내기 전에 한 번만 하면 됩니다.", "확인");
        }

        // 게임 프로젝트 자신의 URP 설정 (게임 프로젝트에서 모드를 시험 삼아 내보낼 때)
        private const string GameProjectPipelinePath = "Assets/Settings/PC_RPAsset.asset";

        /// <summary>
        /// 그래픽 설정이 게임과 같은지 확인한다. (모드 내보내기 전에 확인)
        /// SDK 설정을 쓰고 있거나, 게임 프로젝트 자신의 설정을 쓰고 있으면 true.
        /// </summary>
        public static bool IsGameGraphicsApplied()
        {
            RenderPipelineAsset current = GraphicsSettings.defaultRenderPipeline;
            string currentPath = current != null ? AssetDatabase.GetAssetPath(current) : string.Empty;
            return currentPath == PipelineAssetPath || currentPath == GameProjectPipelinePath;
        }
    }
}
