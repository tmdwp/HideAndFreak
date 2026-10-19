using GuardVsMannequin.Props;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace GuardVsMannequin.Editor
{
    /// <summary>
    /// 씬의 변신 사물(<see cref="TransformableProp"/>)에 0부터 겹치지 않는 번호를 매긴다. 변신 상태는 이 번호로 동기화된다.
    /// 번호는 계층 순서로 정해지므로 같은 씬이면 언제나 같은 번호가 나온다.
    /// - 씬을 저장할 때: 저장 파일에 번호를 남긴다. (검사·인스펙터 확인용)
    /// - 빌드·플레이 모드에서 씬을 처리할 때: 저장을 잊었어도 실제 게임은 항상 올바른 번호를 쓴다.
    /// 맵 제작자는 번호를 신경 쓰지 않아도 된다.
    /// </summary>
    public sealed class PropIndexer : IProcessSceneWithReport
    {
        public int callbackOrder => 0;

        /// <summary>
        /// 씬 저장 직전에 번호를 매기도록 연결한다.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void HookSceneSaving()
        {
            EditorSceneManager.sceneSaving += (scene, _) => AssignIndices(scene);
        }

        /// <summary>
        /// 빌드(그리고 플레이 모드)에서 씬을 처리할 때 번호를 매긴다.
        /// </summary>
        public void OnProcessScene(Scene scene, BuildReport report)
        {
            AssignIndices(scene);
        }

        /// <summary>
        /// 씬의 변신 사물을 계층 순서대로 0, 1, 2… 로 번호 매긴다. 바뀐 개수를 반환한다.
        /// </summary>
        public static int AssignIndices(Scene scene)
        {
            int next = 0;
            int changed = 0;
            foreach (UnityEngine.GameObject root in scene.GetRootGameObjects())
            {
                foreach (TransformableProp prop in root.GetComponentsInChildren<TransformableProp>(true))
                {
                    if (prop.Index != next)
                    {
                        prop.SetIndex(next);
                        if (!EditorApplication.isPlayingOrWillChangePlaymode)
                            EditorUtility.SetDirty(prop);
                        changed++;
                    }
                    next++;
                }
            }
            return changed;
        }
    }
}
