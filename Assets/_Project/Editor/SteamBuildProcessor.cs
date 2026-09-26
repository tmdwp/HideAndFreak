using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace GuardVsMannequin.Editor
{
    /// <summary>
    /// Steam 멀티플레이 빌드에 필요한 설정을 빌드 전후에 자동으로 적용한다.
    /// - 빌드 전: Run In Background를 켠다. (창이 포커스를 잃어도 호스트가 멈추지 않도록)
    /// - 빌드 후: 프로젝트 루트의 steam_appid.txt를 실행 파일 옆에 복사한다.
    ///   (Steamworks.NET의 빌드 후처리는 이 파일을 복사하지 않아, 없으면 빌드에서 Steam 초기화가 실패함)
    /// </summary>
    public class SteamBuildProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private const string SteamAppIdFileName = "steam_appid.txt";

        /// <summary>다른 빌드 처리기보다 뒤에 실행한다.</summary>
        public int callbackOrder => 100;

        /// <summary>
        /// 빌드 전에 Run In Background를 켠다.
        /// </summary>
        public void OnPreprocessBuild(BuildReport report)
        {
            EnsureRunInBackground();
        }

        /// <summary>
        /// 데스크톱 빌드 후 steam_appid.txt를 실행 파일 옆에 복사한다.
        /// </summary>
        public void OnPostprocessBuild(BuildReport report)
        {
            BuildTarget target = report.summary.platform;
            if (target != BuildTarget.StandaloneWindows64 && target != BuildTarget.StandaloneWindows && target != BuildTarget.StandaloneLinux64)
                return;

            string source = Path.Combine(Directory.GetCurrentDirectory(), SteamAppIdFileName);
            if (!File.Exists(source))
            {
                Debug.LogWarning($"[SteamBuild] 프로젝트 루트에 {SteamAppIdFileName}이 없어 복사하지 못했습니다. 빌드에서 Steam 초기화가 실패할 수 있습니다.");
                return;
            }

            string buildDirectory = Path.GetDirectoryName(report.summary.outputPath);
            string destination = Path.Combine(buildDirectory, SteamAppIdFileName);
            File.Copy(source, destination, overwrite: true);
            Debug.Log($"[SteamBuild] {SteamAppIdFileName} 복사 완료: {destination}");
        }

        /// <summary>
        /// Run In Background가 꺼져 있으면 켠다. (테스트 환경 생성 툴에서도 호출)
        /// </summary>
        public static void EnsureRunInBackground()
        {
            if (PlayerSettings.runInBackground)
                return;

            PlayerSettings.runInBackground = true;
            Debug.Log("[SteamBuild] Run In Background를 켰습니다. (창이 포커스를 잃어도 네트워크 게임이 멈추지 않도록)");
        }
    }
}
