using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using GuardVsMannequin.Mods;
using UnityEditor;
using UnityEngine;

namespace GuardVsMannequin.Editor
{
    /// <summary>
    /// 모드 SDK 패키지(.unitypackage)를 만든다. (게임 프로젝트 전용 메뉴)
    /// 담는 것: 콘텐츠 어셈블리(맵·사물·외형 컴포넌트, FishNet·ECM2 무참조), 콘텐츠 에디터 도구(맵 도구, 맵 검사, 모드 내보내기),
    /// 게임과 같은 그래픽 설정(<see cref="ModSdkGraphicsExporter"/>), 예제 모드 템플릿(<see cref="ModSdkTemplateBuilder"/>).
    /// 게임 코드(GuardVsMannequin.Runtime)와 유료 에셋(ECM2)은 담지 않는다.
    /// 출력: &lt;프로젝트&gt;/Builds/ModSDK/ — 패키지, 제작 안내서(ModdingGuide.md), 필요 환경(requirements.txt)
    /// </summary>
    public static class ModSdkPackageExporter
    {
        private const string OutputFolder = "Builds/ModSDK";
        private const string GuidePath = "Docs/ModdingGuide.md";
        private static readonly string[] PackagedFolders = { "Assets/_Project/Content", "Assets/_Project/ContentEditor", ModSdkSettings.Root };
        private static readonly string[] RequiredPackages = { "com.unity.render-pipelines.universal", "com.unity.ai.navigation" };

        /// <summary>
        /// SDK 패키지와 안내 파일을 만든다.
        /// </summary>
        [MenuItem(EditorMenus.Root + "모드/모드 SDK 패키지 만들기")]
        public static void Export()
        {
            // 그래픽 설정은 게임 설정이 바뀌었을 수 있으므로 매번 새로 복사하고, 예제 템플릿은 없을 때만 만든다.
            if (!ModSdkGraphicsExporter.Export())
                return;
            // SDK의 모드 내보내기가 제작 Unity 버전을 확인할 수 있게 게임의 Unity 버전을 적어 둔다.
            File.WriteAllText(ModSdkSettings.GameUnityVersionPath, Application.unityVersion, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(ModSdkSettings.GameUnityVersionPath);
            // 모드 맵 씬 이름이 게임 내장 씬과 겹치지 않는지 SDK에서 확인할 수 있게 게임 씬 목록을 적어 둔다.
            string[] sceneNames = EditorBuildSettings.scenes.Where(scene => scene.enabled)
                .Select(scene => Path.GetFileNameWithoutExtension(scene.path)).ToArray();
            File.WriteAllLines(ModSdkSettings.GameSceneNamesPath, sceneNames, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(ModSdkSettings.GameSceneNamesPath);
            if (!File.Exists(ModSdkTemplateBuilder.ScenePath))
                ModSdkTemplateBuilder.Build();

            Directory.CreateDirectory(OutputFolder);
            string packagePath = Path.Combine(OutputFolder, $"HideAndFreak_ModSDK_{Application.version}.unitypackage");
            AssetDatabase.ExportPackage(PackagedFolders, packagePath, ExportPackageOptions.Recurse);

            if (File.Exists(GuidePath))
                File.Copy(GuidePath, Path.Combine(OutputFolder, "ModdingGuide.md"), true);
            File.WriteAllText(Path.Combine(OutputFolder, "requirements.txt"), BuildRequirements(), new UTF8Encoding(false));

            string fullPath = Path.GetFullPath(OutputFolder);
            Debug.Log($"[ModSdkPackageExporter] 모드 SDK를 만들었습니다: {fullPath}");
            EditorUtility.RevealInFinder(packagePath);
        }

        /// <summary>
        /// 모드 제작 프로젝트에 필요한 Unity 버전과 패키지 버전을 적는다. (번들은 같은 Unity 버전으로 만들어야 읽힌다)
        /// </summary>
        private static string BuildRequirements()
        {
            var text = new StringBuilder();
            text.AppendLine("Hide&Freak Mod SDK - System Requirements ");
            text.AppendLine($"Game Version: {Application.version}");
            text.AppendLine($"Unity Version: {UnityVersionRules.DescribeRange(Application.unityVersion)} (same {Application.unityVersion.Split('.')[0]}.{Application.unityVersion.Split('.')[1]} line, below game version. Newer versions are not allowed)");
            text.AppendLine($"  Recommended: {Application.unityVersion} (same as game version)");
            text.AppendLine("Render Pipeline: URP");
            text.AppendLine("Packages (install same versions in Package Manager):");

            string manifest = File.Exists("Packages/manifest.json") ? File.ReadAllText("Packages/manifest.json") : string.Empty;
            foreach (string package in RequiredPackages)
            {
                Match match = Regex.Match(manifest, $"\"{Regex.Escape(package)}\"\\s*:\\s*\"([^\"]+)\"");
                text.AppendLine($"  - {package} {(match.Success ? match.Groups[1].Value : "(same as game version)")}");
            }
            text.AppendLine("Build Target: Windows (StandaloneWindows64)");
            return text.ToString();
        }
    }
}
