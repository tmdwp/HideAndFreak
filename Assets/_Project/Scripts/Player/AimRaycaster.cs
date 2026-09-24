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
        private static readonly RaycastHit[] hitBuffer = new RaycastHit[32];

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
