using System;
using UnityEngine;

namespace GuardVsMannequin.Player
{
    /// <summary>
    /// 자기 자신의 충돌체를 무시하는 레이캐스트.
    /// 3인칭 카메라 광선이 자기 몸에 먼저 맞는 문제를 막는다.
    /// </summary>
    public static class AimRaycaster
    {
        /// <summary>클라이언트가 보낸 조준 원점이 아바타에서 이보다 멀면 비정상으로 본다(m). (3인칭 카메라 거리 포함)</summary>
        public const float MaxOriginOffset = 3f;

        private static readonly RaycastHit[] hitBuffer = new RaycastHit[32];

        /// <summary>
        /// 서버에서 클라이언트가 보낸 조준 원점을 검증한다. 아바타에서 너무 멀면(조작 의심) 아바타 눈 높이로 대체한다.
        /// </summary>
        /// <param name="origin">클라이언트가 보낸 원점.</param>
        /// <param name="owner">아바타 트랜스폼.</param>
        /// <param name="eyeHeight">대체할 눈 높이(m).</param>
        public static Vector3 SanitizeOrigin(Vector3 origin, Transform owner, float eyeHeight)
        {
            if ((origin - owner.position).sqrMagnitude > MaxOriginOffset * MaxOriginOffset)
                return owner.position + Vector3.up * eyeHeight;
            return origin;
        }

        /// <summary>
        /// ignoreRoot 하위의 충돌체와 트리거를 제외하고 가장 가까운 충돌을 찾는다.
        /// </summary>
        /// <param name="ray">검사할 광선.</param>
        /// <param name="maxDistance">최대 거리.</param>
        /// <param name="ignoreRoot">무시할 루트 트랜스폼(보통 자기 아바타).</param>
        /// <param name="hit">가장 가까운 충돌 정보.</param>
        /// <returns>충돌이 있는지 여부.</returns>
        public static bool TryRaycast(Ray ray, float maxDistance, Transform ignoreRoot, out RaycastHit hit)
        {
            int count = Physics.RaycastNonAlloc(ray, hitBuffer, maxDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            Array.Sort(hitBuffer, 0, count, HitDistanceComparer.Instance);

            for (int i = 0; i < count; i++)
            {
                if (ignoreRoot != null && hitBuffer[i].transform.IsChildOf(ignoreRoot))
                    continue;

                hit = hitBuffer[i];
                return true;
            }

            hit = default;
            return false;
        }

        /// <summary>
        /// 충돌 거리 오름차순 비교기.
        /// </summary>
        private sealed class HitDistanceComparer : System.Collections.Generic.IComparer<RaycastHit>
        {
            public static readonly HitDistanceComparer Instance = new();

            /// <summary>거리로 비교한다.</summary>
            public int Compare(RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance);
        }
    }
}
