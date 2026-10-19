using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuardVsMannequin.Map
{
    /// <summary>
    /// 변신 사물의 새 배치를 계산한다. (값만 반환, 장면은 건드리지 않음)
    /// - 자리 섞기: 크기가 비슷한 사물끼리 원래 자리(바닥 가운데 점)를 서로 바꾼다. 자리가 원래 비슷한 크기의 사물이 있던 곳이라 겹치거나 뜰 위험이 적다.
    /// - 흩기: 바뀐 자리에서 조금 옮기고 세로축으로 돌린다. (쓸 수 있는지는 장면을 보는 쪽이 검사)
    /// 같은 시드와 같은 입력(번호순)이면 모든 PC에서 같은 결과가 나온다.
    /// </summary>
    public static class PropLayoutPlanner
    {
        /// <summary>
        /// 사물 하나의 원래 상태.
        /// </summary>
        public readonly struct Spot
        {
            /// <summary>사물 번호. (정렬 기준)</summary>
            public readonly int index;
            /// <summary>사물 겉모습 상자의 바닥 가운데 점.</summary>
            public readonly Vector3 bottomCenter;
            /// <summary>겉모습 상자 크기.</summary>
            public readonly Vector3 size;

            /// <summary>사물 하나의 원래 상태를 만든다.</summary>
            public Spot(int index, Vector3 bottomCenter, Vector3 size)
            {
                this.index = index;
                this.bottomCenter = bottomCenter;
                this.size = size;
            }

            /// <summary>바닥 면의 긴 변.</summary>
            public float Footprint => Mathf.Max(size.x, size.z, 0.01f);

            /// <summary>높이.</summary>
            public float Height => Mathf.Max(size.y, 0.01f);
        }

        /// <summary>
        /// 사물 하나의 새 배치. 자리는 spots 목록의 위치 번호다.
        /// </summary>
        public readonly struct Placement
        {
            /// <summary>옮겨 갈 자리(입력 목록의 위치).</summary>
            public readonly int spot;
            /// <summary>그 자리에서 흩을 수평 거리(m).</summary>
            public readonly Vector3 jitter;
            /// <summary>세로축으로 돌릴 각도(도).</summary>
            public readonly float yaw;

            /// <summary>새 배치를 만든다.</summary>
            public Placement(int spot, Vector3 jitter, float yaw)
            {
                this.spot = spot;
                this.jitter = jitter;
                this.yaw = yaw;
            }
        }

        /// <summary>
        /// 새 배치를 계산한다. 결과는 입력과 같은 순서다. (결과[i] = spots[i] 사물이 갈 곳)
        /// </summary>
        /// <param name="spots">사물 목록. 모든 PC에서 같은 순서여야 한다. (번호순)</param>
        /// <param name="seed">배치 시드.</param>
        /// <param name="sizeTolerance">자리를 바꿀 수 있는 크기 차이 배율(1 이상).</param>
        /// <param name="jitterDistance">흩는 최대 거리(m).</param>
        /// <param name="maxYaw">돌리는 최대 각도(도, ±).</param>
        public static Placement[] Plan(IReadOnlyList<Spot> spots, int seed, float sizeTolerance, float jitterDistance, float maxYaw)
        {
            var random = new System.Random(seed);
            var targets = new int[spots.Count];
            foreach (List<int> group in GroupBySize(spots, sizeTolerance))
            {
                // 그룹 안에서 자리를 섞는다. (Fisher-Yates)
                var shuffled = new List<int>(group);
                for (int i = shuffled.Count - 1; i > 0; i--)
                {
                    int j = random.Next(i + 1);
                    (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
                }
                for (int i = 0; i < group.Count; i++)
                    targets[group[i]] = shuffled[i];
            }

            // 흩기 값은 검사 결과와 관계없이 사물마다 같은 순서로 뽑아, 검사 결과가 달라도 다른 사물의 값이 밀리지 않게 한다.
            var result = new Placement[spots.Count];
            for (int i = 0; i < spots.Count; i++)
            {
                double angle = random.NextDouble() * Math.PI * 2.0;
                double distance = Math.Sqrt(random.NextDouble()) * jitterDistance;
                var jitter = new Vector3((float)(Math.Cos(angle) * distance), 0f, (float)(Math.Sin(angle) * distance));
                float yaw = (float)((random.NextDouble() * 2.0 - 1.0) * maxYaw);
                result[i] = new Placement(targets[i], jitter, yaw);
            }
            return result;
        }

        /// <summary>
        /// 바닥 넓이·높이가 서로 tolerance배 이내인 사물끼리 묶는다. (크기순으로 늘어놓고 그룹의 첫 사물 기준)
        /// </summary>
        private static List<List<int>> GroupBySize(IReadOnlyList<Spot> spots, float tolerance)
        {
            var order = new List<int>();
            for (int i = 0; i < spots.Count; i++)
                order.Add(i);
            // 크기가 같으면 번호순으로 둬 모든 PC에서 순서가 같게 한다.
            order.Sort((a, b) =>
            {
                int bySize = (spots[a].Footprint * spots[a].Height).CompareTo(spots[b].Footprint * spots[b].Height);
                return bySize != 0 ? bySize : spots[a].index.CompareTo(spots[b].index);
            });

            var groups = new List<List<int>>();
            List<int> current = null;
            foreach (int i in order)
            {
                if (current == null || !IsSimilar(spots[current[0]], spots[i], tolerance))
                {
                    current = new List<int>();
                    groups.Add(current);
                }
                current.Add(i);
            }
            return groups;
        }

        /// <summary>
        /// 두 사물의 바닥 넓이·높이가 모두 tolerance배 이내인지.
        /// </summary>
        private static bool IsSimilar(Spot a, Spot b, float tolerance)
        {
            return Ratio(a.Footprint, b.Footprint) <= tolerance && Ratio(a.Height, b.Height) <= tolerance;
        }

        /// <summary>
        /// 큰 값 ÷ 작은 값.
        /// </summary>
        private static float Ratio(float a, float b)
        {
            return Mathf.Max(a, b) / Mathf.Min(a, b);
        }
    }
}
