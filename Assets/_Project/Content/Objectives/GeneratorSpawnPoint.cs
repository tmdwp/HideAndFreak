using UnityEngine;

namespace GuardVsMannequin.Objectives
{
    /// <summary>
    /// 전원 장치가 생성될 수 있는 후보 위치.
    /// 설정된 전원 장치 수보다 많이 배치하면 매 판 서로 멀리 떨어진 후보가 골라진다(게임의 GeneratorSpawner).
    /// 위치 편집: 씬에서 이 오브젝트를 옮기거나 복제(Ctrl+D)하면 된다. 맵 제작 도우미(GameSettingTools > 맵 제작)에서 추가·재배치할 수 있다.
    /// </summary>
    public class GeneratorSpawnPoint : MonoBehaviour
    {
        private static readonly Vector3 GeneratorSize = new Vector3(1f, 1.6f, 0.8f);

        /// <summary>
        /// 씬 뷰에서 전원 장치 크기의 상자와 이름표로 후보 위치를 표시한다. (방향 포함)
        /// </summary>
        private void OnDrawGizmos()
        {
            Gizmos.matrix = Matrix4x4.TRS(transform.position + Vector3.up * GeneratorSize.y * 0.5f, transform.rotation, Vector3.one);
            Gizmos.color = new Color(1f, 0.2f, 0.1f, 0.35f);
            Gizmos.DrawCube(Vector3.zero, GeneratorSize);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(Vector3.zero, GeneratorSize);
            Gizmos.DrawLine(Vector3.zero, Vector3.forward);
            Gizmos.matrix = Matrix4x4.identity;

#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * (GeneratorSize.y + 0.3f), "전원 장치 후보");
#endif
        }
    }
}
