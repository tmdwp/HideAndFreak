using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GuardVsMannequin.Map
{
    /// <summary>
    /// 가드 스폰 위치를 고르는 계산만 맡는다. (씬·네트워크에 의존하지 않음. 시야 판정은 생성자에서 받는다)
    /// 1. 후보 = 맵의 가드 스폰 지점 + NavMesh 위 무작위 지점 (갈 수 있는 곳만 미리 걸러서 받음)
    /// 2. 모든 마네킹 스폰 지점에서 최소 거리 이상 떨어지고, 마네킹 스폰 지점이 보이지 않으며,
    ///    이미 고른 가드 위치와도 떨어진 후보 중에서 무작위로 고른다.
    /// 3. 조건을 모두 만족하는 후보가 없으면(작은 맵) 점수가 가장 높은 후보를 쓴다. (안 보이는 곳 우선, 그다음 먼 곳)
    /// 4. 시점은 맵 중심 쪽을 향한다.
    /// </summary>
    public sealed class GuardSpawnSelector
    {
        private readonly List<Candidate> candidates = new();
        private readonly IReadOnlyList<Vector3> mannequinSpawns;
        private readonly Vector3 mapCenter;
        private readonly float minMannequinDistance;
        private readonly float minGuardSeparation;
        private readonly List<Vector3> chosen = new();

        /// <summary>
        /// 후보 위치와, 그곳에서 마네킹 스폰 지점이 보이는지 여부. (판마다 한 번만 계산)
        /// </summary>
        private readonly struct Candidate
        {
            public readonly Vector3 Position;
            public readonly bool SeesMannequins;

            public Candidate(Vector3 position, bool seesMannequins)
            {
                Position = position;
                SeesMannequins = seesMannequins;
            }
        }

        /// <param name="candidatePoints">가드 스폰 후보 위치. (맵 지점 + NavMesh 무작위 지점, 갈 수 있는 곳만)</param>
        /// <param name="mannequinSpawns">마네킹 스폰 지점 위치.</param>
        /// <param name="mapCenter">맵 중심. (시점 방향 기준)</param>
        /// <param name="minMannequinDistance">마네킹 스폰 지점과의 최소 거리(m).</param>
        /// <param name="minGuardSeparation">가드끼리의 최소 거리(m).</param>
        /// <param name="seesMannequins">후보 위치에서 마네킹 스폰 지점이 보이는지 판정. null이면 시야 조건을 쓰지 않는다.</param>
        public GuardSpawnSelector(IEnumerable<Vector3> candidatePoints, IReadOnlyList<Vector3> mannequinSpawns,
            Vector3 mapCenter, float minMannequinDistance, float minGuardSeparation, Func<Vector3, bool> seesMannequins)
        {
            foreach (Vector3 point in candidatePoints)
                candidates.Add(new Candidate(point, seesMannequins != null && seesMannequins(point)));
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
            Vector3 best = candidates[0].Position;
            float bestScore = float.MinValue;
            foreach (Candidate candidate in candidates)
            {
                float mannequinDistance = DistanceToNearest(candidate.Position, mannequinSpawns);
                float guardDistance = DistanceToNearest(candidate.Position, chosen);
                if (mannequinDistance >= minMannequinDistance && guardDistance >= minGuardSeparation && !candidate.SeesMannequins)
                    valid.Add(candidate.Position);

                // 조건을 만족하는 후보가 없을 때를 대비해 점수가 가장 높은 후보를 기억한다.
                // 마네킹이 안 보이는 곳을 거리보다 우선하고, 가드끼리 겹치는 것도 피한다.
                float score = Mathf.Min(mannequinDistance, guardDistance + minMannequinDistance);
                if (!candidate.SeesMannequins)
                    score += minMannequinDistance * 2f;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate.Position;
                }
            }

            Vector3 position = valid.Count > 0 ? valid[Random.Range(0, valid.Count)] : best;
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
