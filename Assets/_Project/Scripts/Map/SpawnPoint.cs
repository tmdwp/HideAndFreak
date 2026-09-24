using GuardVsMannequin.Core;
using UnityEngine;

namespace GuardVsMannequin.Map
{
    /// <summary>
    /// 역할별 플레이어 스폰 위치.
    /// 가드용 스폰 지점은 반드시 대기실 안에 배치한다.
    /// </summary>
    public class SpawnPoint : MonoBehaviour
    {
        [SerializeField] private PlayerRole role = PlayerRole.Mannequin;

        /// <summary>이 지점에서 스폰될 역할.</summary>
        public PlayerRole Role => role;

        /// <summary>
        /// 씬 뷰에서 역할별 색으로 위치와 방향을 표시한다.
        /// </summary>
        private void OnDrawGizmos()
        {
            Gizmos.color = role == PlayerRole.Guard ? Color.cyan : Color.yellow;
            Gizmos.DrawWireSphere(transform.position + Vector3.up, 0.5f);
            Gizmos.DrawLine(transform.position + Vector3.up, transform.position + Vector3.up + transform.forward);
        }
    }
}
