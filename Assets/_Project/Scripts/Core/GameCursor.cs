using UnityEngine;

namespace GuardVsMannequin.Core
{
    /// <summary>
    /// 마우스 커서 잠금을 한곳에서 관리한다.
    /// 잠겨 있을 때만 게임 입력(시점, 클릭, 이동)을 받고, 메뉴·결과 화면에서는 풀어서 UI를 누를 수 있게 한다.
    /// </summary>
    public static class GameCursor
    {
        /// <summary>커서가 잠겨 있는지(= 게임 입력을 받는 중인지) 여부.</summary>
        public static bool IsLocked => Cursor.lockState == CursorLockMode.Locked;

        /// <summary>
        /// 커서를 잠그거나 푼다. 잠그면 커서가 보이지 않는다.
        /// </summary>
        public static void SetLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
