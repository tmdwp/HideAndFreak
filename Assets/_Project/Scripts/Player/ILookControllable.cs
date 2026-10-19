using UnityEngine;

namespace GuardVsMannequin.Player
{
    /// <summary>
    /// 마우스 시점 입력을 받을 수 있는 캐릭터.
    /// 1인칭/3인칭 구현이 서로 달라도 입력 드라이버는 이 인터페이스만 사용한다(LSP, DIP).
    /// </summary>
    public interface ILookControllable
    {
        /// <summary>
        /// 감도가 적용된 시점 입력(도 단위)을 추가한다. (x: 좌우, y: 상하)
        /// </summary>
        void AddLookInput(Vector2 lookDelta);
    }
}
