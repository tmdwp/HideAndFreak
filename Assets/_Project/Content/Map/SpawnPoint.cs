using GuardVsMannequin.Core;
using UnityEngine;

namespace GuardVsMannequin.Map
{
    /// <summary>
    /// 역할별 플레이어 스폰 위치.
    /// 가드용은 스폰 후보다. 실제 위치는 판마다 마네킹 스폰에서 먼 곳을 고른다. (GuardSpawnSelector)
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
