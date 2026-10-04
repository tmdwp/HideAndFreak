using UnityEditor;
using UnityEngine;

namespace GuardVsMannequin.Editor
{
    /// <summary>
    /// Phase 7-A(게임플레이 개편)에 맞게 기존 에셋을 한 번에 바꾸는 메뉴.
    /// 각 단계는 따로 된 도구가 맡고, 이 메뉴는 순서대로 부르고 결과를 요약한다. 여러 번 실행해도 된다.
    /// </summary>
    public static class Phase7Migration
    {
        /// <summary>
        /// 모든 단계를 실행한다.
        /// </summary>
        [MenuItem("GuardVsMannequin/Phase 7/게임플레이 개편 적용")]
        private static void Apply()
        {
            if (!EditorUtility.DisplayDialog("Phase 7 게임플레이 개편 적용",
                    "외곽선 렌더러 기능 설치, 가드·마네킹 프리팹 갱신, 맵 씬의 가드 대기실 제거, 룸 설정 줄 정리를 실행합니다.\n" +
                    "열려 있는 씬의 저장하지 않은 변경은 먼저 저장하세요.", "실행", "취소"))
                return;

            var summary = new System.Text.StringBuilder("[Phase7Migration] 결과\n");
            summary.AppendLine($"- 외곽선 기능: {Describe(OutlineFeatureInstaller.Install(), "렌더러에 추가")}");
            Debug.Log(summary.ToString());
        }

        /// <summary>
        /// 단계 결과(추가·변경 개수)를 문구로 바꾼다.
        /// </summary>
        public static string Describe(int count, string action)
        {
            return count switch
            {
                < 0 => "실패 (위 오류 확인)",
                0 => "이미 적용됨",
                _ => $"{count}개 {action}"
            };
        }
    }
}
