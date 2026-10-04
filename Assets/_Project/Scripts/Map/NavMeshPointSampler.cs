using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace GuardVsMannequin.Map
{
    /// <summary>
    /// 미리 구운 NavMesh 위의 무작위 지점을 뽑는다. (가드 스폰 후보)
    /// 삼각형을 면적에 비례해 고르고 그 안의 무작위 점을 쓰므로 넓은 바닥에 고르게 퍼진다.
    /// </summary>
    public static class NavMeshPointSampler
    {
        /// <summary>
        /// NavMesh 위 무작위 지점을 count개 뽑는다. NavMesh가 없으면 빈 목록.
        /// </summary>
        /// <param name="center">뽑은 지점들의 평균 위치. (맵 중심으로 사용) NavMesh가 없으면 원점.</param>
        public static List<Vector3> Sample(int count, out Vector3 center)
        {
            var result = new List<Vector3>(count);
            center = Vector3.zero;

            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
            int triangleCount = triangulation.indices.Length / 3;
            if (triangleCount == 0)
                return result;

            // 면적 누적합으로 삼각형을 면적 비례로 고른다.
            var cumulativeArea = new float[triangleCount];
            float total = 0f;
            for (int i = 0; i < triangleCount; i++)
            {
                GetTriangle(triangulation, i, out Vector3 a, out Vector3 b, out Vector3 c);
                total += Vector3.Cross(b - a, c - a).magnitude * 0.5f;
                cumulativeArea[i] = total;
            }

            for (int n = 0; n < count; n++)
            {
                int index = System.Array.BinarySearch(cumulativeArea, Random.value * total);
                if (index < 0)
                    index = Mathf.Min(~index, triangleCount - 1);

                GetTriangle(triangulation, index, out Vector3 a, out Vector3 b, out Vector3 c);
                Vector3 point = RandomPointInTriangle(a, b, c);
                result.Add(point);
                center += point;
            }

            center /= result.Count;
            return result;
        }

        /// <summary>
        /// i번째 삼각형의 세 꼭짓점.
        /// </summary>
        private static void GetTriangle(NavMeshTriangulation triangulation, int i, out Vector3 a, out Vector3 b, out Vector3 c)
        {
            a = triangulation.vertices[triangulation.indices[i * 3]];
            b = triangulation.vertices[triangulation.indices[i * 3 + 1]];
            c = triangulation.vertices[triangulation.indices[i * 3 + 2]];
        }

        /// <summary>
        /// 삼각형 안의 균일한 무작위 점.
        /// </summary>
        private static Vector3 RandomPointInTriangle(Vector3 a, Vector3 b, Vector3 c)
        {
            float u = Random.value;
            float v = Random.value;
            if (u + v > 1f)
            {
                u = 1f - u;
                v = 1f - v;
            }
            return a + (b - a) * u + (c - a) * v;
        }
    }
}
