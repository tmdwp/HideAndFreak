using UnityEngine;

namespace GuardVsMannequin.Map
{
    /// <summary>
    /// 가드 대기실의 영역(로컬 좌표 기준 상자). 가드 대기 시간 동안 가드가 이 안에 있어야 한다.
    /// 서버의 이동 검증(<see cref="Player.MovementValidator"/>)이 대기실 탈출을 막는 데 사용한다.
    /// 맵마다 대기실 오브젝트에 하나만 둔다.
    /// </summary>
    public class WaitingRoomArea : MonoBehaviour
    {
        [Tooltip("영역 중심. (이 오브젝트의 로컬 좌표)")]
        [SerializeField] private Vector3 center = new Vector3(0f, 2f, 0f);

        [Tooltip("영역 크기. (이 오브젝트의 로컬 좌표) 벽 두께와 약간의 여유를 포함한다.")]
        [SerializeField] private Vector3 size = new Vector3(8f, 6f, 8f);

        /// <summary>현재 맵의 대기실 영역. 없으면 null.</summary>
        public static WaitingRoomArea Current { get; private set; }

        /// <summary>
        /// 활성화되면 현재 맵의 대기실로 등록한다.
        /// </summary>
        private void OnEnable()
        {
            Current = this;
        }

        /// <summary>
        /// 비활성화되면 등록을 해제한다.
        /// </summary>
        private void OnDisable()
        {
            if (Current == this)
                Current = null;
        }

        /// <summary>
        /// 월드 좌표가 대기실 영역 안에 있는지 확인한다.
        /// </summary>
        public bool Contains(Vector3 worldPoint)
        {
            Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
            return new Bounds(center, size).Contains(localPoint);
        }

        /// <summary>
        /// 선택했을 때 씬 뷰에 영역을 표시한다.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.8f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(center, size);
        }
    }
}
