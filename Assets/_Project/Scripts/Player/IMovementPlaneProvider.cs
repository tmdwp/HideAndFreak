using UnityEngine;

namespace GuardVsMannequin.Player
{
    /// <summary>
    /// 현재 이동 평면의 법선을 제공한다. (바닥: 위쪽, 벽: 벽의 법선, 천장: 아래쪽)
    /// 이동 입력 변환과 카메라 제한이 벽타기 구현에 직접 의존하지 않도록 분리한다(DIP).
    /// </summary>
    public interface IMovementPlaneProvider
    {
        /// <summary>이동 평면의 법선(바깥 방향).</summary>
        Vector3 MovementPlaneNormal { get; }
    }
}
