using UnityEngine;

namespace GuardVsMannequin.Map
{
    /// <summary>
    /// 탐색 단계가 시작될 때 대기실의 가드가 옮겨질 맵 안의 출발 지점.
    /// 대기실이 맵과 떨어져 있는 맵에서 사용한다. (<see cref="GuardReleaseTeleporter"/>)
    /// </summary>
    public class GuardReleasePoint : MonoBehaviour
    {
        /// <summary>
        /// 씬 뷰에 위치와 방향을 표시한다.
        /// </summary>
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.3f, 0.6f, 1f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.9f, 0.4f);
            Gizmos.DrawLine(transform.position + Vector3.up * 0.9f, transform.position + Vector3.up * 0.9f + transform.forward);
        }
    }
}
