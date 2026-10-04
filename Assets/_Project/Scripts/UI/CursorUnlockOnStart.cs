using UnityEngine;

namespace GuardVsMannequin.UI
{
    /// <summary>
    /// 메뉴 화면(로비, 룸)에 들어오면 커서를 풀고 보이게 한다. (게임에서 돌아왔을 때 잠겨 있을 수 있음)
    /// </summary>
    public class CursorUnlockOnStart : MonoBehaviour
    {
        /// <summary>
        /// 커서 잠금을 풀고 보이게 한다.
        /// </summary>
        private void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
