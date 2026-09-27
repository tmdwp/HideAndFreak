using System.IO;
using GuardVsMannequin.Core;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace GuardVsMannequin.Editor
{
    /// <summary>
    /// 빌드가 끝나면 편집용 데이터 파일(문장, 크레딧, 게임 설정)을 빌드 폴더의
    /// &lt;게임&gt;_Data/StreamingAssets/GameData 로 복사한다. 사용자는 이 파일을 고친 뒤 게임을 다시 켜면 된다.
    /// 원본은 Assets/_Project/Resources에 있으며 게임 안에도 내장되어, 복사본이 없거나 깨지면 내장 원본을 쓴다.
    /// </summary>
    public class DataFilesBuildProcessor : IPostprocessBuildWithReport
    {
        private const string ResourcesRoot = "Assets/_Project/Resources";
        private const string ReadmeFileName = "README.txt";

        public int callbackOrder => 0;

        /// <summary>
        /// Windows/Linux 독립 실행 빌드에서만 복사한다.
        /// </summary>
        public void OnPostprocessBuild(BuildReport report)
        {
            BuildTarget target = report.summary.platform;
            if (target != BuildTarget.StandaloneWindows && target != BuildTarget.StandaloneWindows64
                && target != BuildTarget.StandaloneLinux64)
            {
                Debug.LogWarning($"[DataFilesBuildProcessor] {target} 빌드는 데이터 파일 복사를 지원하지 않습니다. 내장 원본만 사용됩니다.");
                return;
            }

            string outputPath = report.summary.outputPath;
            string dataFolder = Path.Combine(Path.GetDirectoryName(outputPath), Path.GetFileNameWithoutExtension(outputPath) + "_Data");
            string destinationRoot = Path.Combine(dataFolder, "StreamingAssets", DataFiles.ExternalFolderName);

            int copied = 0;
            foreach (string folder in DataFiles.EditableFolders)
                copied += CopyFolder(Path.Combine(ResourcesRoot, folder), Path.Combine(destinationRoot, folder));

            WriteReadme(destinationRoot);
            Debug.Log($"[DataFilesBuildProcessor] 데이터 파일 {copied}개를 복사했습니다: {destinationRoot}");
        }

        /// <summary>
        /// 폴더의 데이터 파일(.meta 제외)을 복사한다. 하위 폴더도 포함한다.
        /// </summary>
        private static int CopyFolder(string source, string destination)
        {
            if (!Directory.Exists(source))
                return 0;

            int count = 0;
            foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                if (file.EndsWith(".meta"))
                    continue;

                string relative = file.Substring(source.Length).TrimStart('/', '\\');
                string target = Path.Combine(destination, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                File.Copy(file, target, overwrite: true);
                count++;
            }
            return count;
        }

        /// <summary>
        /// 데이터 폴더에 편집 안내문을 쓴다.
        /// </summary>
        private static void WriteReadme(string destinationRoot)
        {
            const string readme =
                "[한국어]\n" +
                "이 폴더의 파일을 고치고 게임을 다시 실행하면 반영됩니다. 반드시 UTF-8로 저장하세요.\n" +
                "- Localization/strings.csv : 화면 문장 (열 = key, ko, en, ... / 열을 추가하면 새 언어)\n" +
                "- Credits/credits.<언어 코드>.txt : 크레딧\n" +
                "- Data/default_game_settings.json : 방을 만들 때의 게임 설정 기본값\n" +
                "- Data/game_settings.csv : 룸 설정 항목의 조정 단위와 범위\n" +
                "파일을 지우거나 형식이 깨지면 게임에 내장된 원본을 사용합니다.\n\n" +
                "[English]\n" +
                "Edit the files in this folder and restart the game to apply changes. Save them as UTF-8.\n" +
                "If a file is missing or broken, the built-in original is used.\n";

            Directory.CreateDirectory(destinationRoot);
            File.WriteAllText(Path.Combine(destinationRoot, ReadmeFileName), readme, new System.Text.UTF8Encoding(true));
        }
    }
}
