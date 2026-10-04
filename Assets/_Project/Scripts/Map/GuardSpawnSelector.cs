using System.Collections.Generic;
using UnityEngine;

namespace GuardVsMannequin.Map
{
    /// <summary>
    /// 가드 스폰 위치를 고르는 계산만 맡는다. (씬·네트워크에 의존하지 않음)
    /// 1. 후보 = 맵의 가드 스폰 지점 + NavMesh 위 무작위 지점
    /// 2. 모든 마네킹 스폰 지점에서 최소 거리 이상 떨어지고, 이미 고른 가드 위치와도 떨어진 후보 중에서 무작위로 고른다.
    /// 3. 조건을 만족하는 후보가 없으면(작은 맵) 마네킹 스폰에서 가장 먼 후보를 쓴다.
    /// 4. 시점은 맵 중심 쪽을 향한다.
    /// </summary>
    public sealed class GuardSpawnSelector
    {
        private readonly List<Vector3> candidates = new();
        private readonly IReadOnlyList<Vector3> mannequinSpawns;
        private readonly Vector3 mapCenter;
        private readonly float minMannequinDistance;
        private readonly float minGuardSeparation;
        private readonly List<Vector3> chosen = new();

        /// <param name="guardPoints">맵의 가드 스폰 지점 위치.</param>
        /// <param name="navMeshPoints">NavMesh 위 무작위 지점.</param>
        /// <param name="mannequinSpawns">마네킹 스폰 지점 위치.</param>
        /// <param name="mapCenter">맵 중심. (시점 방향 기준)</param>
        /// <param name="minMannequinDistance">마네킹 스폰 지점과의 최소 거리(m).</param>
        /// <param name="minGuardSeparation">가드끼리의 최소 거리(m).</param>
        public GuardSpawnSelector(IEnumerable<Vector3> guardPoints, IEnumerable<Vector3> navMeshPoints, IReadOnlyList<Vector3> mannequinSpawns,
            Vector3 mapCenter, float minMannequinDistance, float minGuardSeparation)
        {
            candidates.AddRange(guardPoints);
            candidates.AddRange(navMeshPoints);
            this.mannequinSpawns = mannequinSpawns;
            this.mapCenter = mapCenter;
            this.minMannequinDistance = minMannequinDistance;
            this.minGuardSeparation = minGuardSeparation;
        }

        /// <summary>
        /// 다음 가드의 스폰 위치와 방향을 고른다. 후보가 하나도 없으면 false.
        /// </summary>
        public bool TrySelect(out Pose pose)
        {
            pose = default;
            if (candidates.Count == 0)
                return false;

            var valid = new List<Vector3>();
            Vector3 farthest = candidates[0];
            float farthestScore = float.MinValue;
            foreach (Vector3 candidate in candidates)
            {
                float mannequinDistance = DistanceToNearest(candidate, mannequinSpawns);
                float guardDistance = DistanceToNearest(candidate, chosen);
                if (mannequinDistance >= minMannequinDistance && guardDistance >= minGuardSeparation)
                    valid.Add(candidate);

                // 조건을 만족하는 후보가 없을 때를 대비해 "가장 먼" 후보를 기억한다. (가드끼리 겹침도 피함)
                float score = Mathf.Min(mannequinDistance, guardDistance + minMannequinDistance);
                if (score > farthestScore)
                {
                    farthestScore = score;
                    farthest = candidate;
                }
            }

            Vector3 position = valid.Count > 0 ? valid[Random.Range(0, valid.Count)] : farthest;
            chosen.Add(position);
            pose = new Pose(position, FaceTowards(position, mapCenter));
            return true;
        }

        /// <summary>
        /// 목록 중 가장 가까운 점까지의 수평 거리. 목록이 비면 무한대.
        /// </summary>
        private static float DistanceToNearest(Vector3 point, IReadOnlyList<Vector3> others)
        {
            float nearest = float.MaxValue;
            foreach (Vector3 other in others)
            {
                Vector2 delta = new(point.x - other.x, point.z - other.z);
                nearest = Mathf.Min(nearest, delta.magnitude);
            }
            return nearest;
        }

        /// <summary>
        /// 수평으로 목표 쪽을 바라보는 회전. 같은 위치면 기본 방향.
        /// </summary>
        private static Quaternion FaceTowards(Vector3 from, Vector3 to)
        {
            Vector3 direction = to - from;
            direction.y = 0f;
            return direction.sqrMagnitude > 0.01f ? Quaternion.LookRotation(direction) : Quaternion.identity;
        }
    }
}
