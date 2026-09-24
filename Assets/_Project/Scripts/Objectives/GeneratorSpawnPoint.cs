using UnityEngine;

namespace GuardVsMannequin.Objectives
{
    /// <summary>
    /// 전원 장치가 생성될 수 있는 후보 위치.
    /// 설정된 전원 장치 수보다 많이 배치하면 매 판 무작위로 선택된다.
    /// </summary>
    public class GeneratorSpawnPoint : MonoBehaviour
    {
        /// <summary>
        /// 씬 뷰에서 후보 위치를 표시한다.
        /// </summary>
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 0.8f, new Vector3(1f, 1.6f, 0.8f));
        }
    }
}
