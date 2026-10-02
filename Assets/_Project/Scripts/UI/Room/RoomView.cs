using UnityEngine;

namespace GuardVsMannequin.UI
{
    /// <summary>
    /// 룸(대기방) 화면 관리. 화면에 들어오면 커서를 보이게 한다.
    /// 각 영역은 따로 갱신된다:
    /// - <see cref="RoomMemberListView"/>: 참가자 목록과 준비 상태
    /// - <see cref="RoomCodeView"/>: 방 코드(복사), 친구 초대
    /// - <see cref="RoomSettingsView"/>: 게임 설정 (모두에게 보이고 호스트만 조정)
    /// - <see cref="RoomFooterView"/>: 시작/준비 버튼, 나가기, 상태 문구
    /// </summary>
    public class RoomView : MonoBehaviour
    {
        /// <summary>
        /// 커서를 보이게 한다. (게임에서 돌아왔을 때 잠겨 있을 수 있음)
        /// </summary>
        private void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
