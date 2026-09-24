using GuardVsMannequin.Core;
using UnityEngine;

namespace GuardVsMannequin.Map
{
    /// <summary>
    /// 가드 대기실 문. 가드 대기 시간(Preparation)과 접속 대기 중에는 닫혀 있고,
    /// 탐색 단계부터 열린다. 동기화된 게임 상태만 보고 움직이므로 별도 네트워크 오브젝트가 필요 없다.
    /// </summary>
    public class WaitingRoomDoor : MonoBehaviour
    {
        [Tooltip("열렸을 때의 위치 이동량(로컬 기준). 기본은 위로 올라가 천장 속으로 사라진다.")]
        [SerializeField] private Vector3 openOffset = new Vector3(0f, 4.2f, 0f);
        [SerializeField] private float moveSpeed = 3f;

        private Vector3 closedPosition;

        /// <summary>
        /// 닫힌 위치를 저장한다.
        /// </summary>
        private void Awake()
        {
            closedPosition = transform.localPosition;
        }

        /// <summary>
        /// 현재 상태에 맞는 위치로 문을 이동시킨다. 충돌체도 함께 이동한다.
        /// </summary>
        private void Update()
        {
            Vector3 target = IsOpenState(GameServices.CurrentStateOrWaiting) ? closedPosition + openOffset : closedPosition;
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, target, moveSpeed * Time.deltaTime);
        }

        /// <summary>
        /// 문이 열려 있어야 하는 상태인지 확인한다.
        /// </summary>
        private static bool IsOpenState(GameState state)
        {
            return state == GameState.Playing || state == GameState.Hunting || state == GameState.Ended;
        }
    }
}
