using UnityEngine;

namespace GuardVsMannequin.Player
{
    /// <summary>
    /// 조준 방향(화면 중앙 광선)을 제공한다.
    /// 도구는 카메라 구현(1인칭/3인칭)을 몰라도 조준할 수 있다(DIP).
    /// </summary>
    public interface IAimProvider
    {
        /// <summary>
        /// 현재 조준 광선을 반환한다.
        /// </summary>
        Ray GetAimRay();
    }
}
