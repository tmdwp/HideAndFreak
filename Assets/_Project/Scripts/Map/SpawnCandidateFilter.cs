using System.Collections.Generic;
using GuardVsMannequin.Player;
using UnityEngine;
using UnityEngine.AI;

namespace GuardVsMannequin.Map
{
    /// <summary>
    /// 가드 스폰 후보를 씬 정보(NavMesh·물리)로 검사한다. 계산 규칙은 <see cref="GuardSpawnSelector"/>가 맡고, 이 클래스는 장면 질의만 한다.
    /// - 갈 수 있는 곳: 마네킹 스폰 지점까지 NavMesh 경로가 이어진 후보만 남긴다. (투명 천장 위, 지붕 위, 경계 밖, 막힌 방 제외)
    /// - 머리 위 공간: 서 있을 공간이 없는 후보(가구 밑, 낮은 틈)를 뺀다.
    /// - 시야: 후보에서 마네킹 스폰 지점이 직접 보이는지 확인한다. (벽·사물이 가리면 안 보임, 플레이어는 무시)
    /// </summary>
    public static class SpawnCandidateFilter
    {
        // 서 있을 공간 검사용 캡슐 (가드 몸 크기 기준)
        private const float BodyRadius = 0.3f;
        private const float BodyHeight = 1.8f;
        private const float FloorClearance = 0.15f;

        // 시야 검사 높이: 가드 눈 → 마네킹 몸 가운데
        private const float GuardEyeHeight = 1.6f;
        private const float MannequinCenterHeight = 1.0f;

        // 후보를 NavMesh에 붙일 때 허용하는 거리(m)
        private const float NavMeshSnapDistance = 1.5f;

        private static readonly RaycastHit[] lineBuffer = new RaycastHit[16];
        private static readonly NavMeshPath path = new();

        /// <summary>
        /// 마네킹 스폰 지점까지 걸어서 갈 수 있고 서 있을 공간이 있는 후보만 남긴다.
        /// 기준 지점을 NavMesh에 붙일 수 없으면(NavMesh 없음) 경로 검사는 건너뛴다.
        /// </summary>
        /// <param name="candidates">검사할 후보 위치.</param>
        /// <param name="anchors">경로 기준 지점(마네킹 스폰 지점).</param>
        public static List<Vector3> FilterStandable(IEnumerable<Vector3> candidates, IReadOnlyList<Vector3> anchors)
        {
            bool hasAnchor = TryFindAnchor(anchors, out Vector3 anchor);
            var result = new List<Vector3>();
            foreach (Vector3 candidate in candidates)
            {
                if (hasAnchor && !IsReachable(candidate, anchor))
                    continue;
                if (!HasHeadroom(candidate))
                    continue;
                result.Add(candidate);
            }
            return result;
        }

        /// <summary>
        /// 후보에서 가드 눈높이로 봤을 때 마네킹 스폰 지점 중 하나라도 보이는지 확인한다.
        /// </summary>
        /// <param name="candidate">가드 스폰 후보(발 위치).</param>
        /// <param name="targets">마네킹 스폰 지점(발 위치).</param>
        /// <param name="maxDistance">이보다 먼 지점은 보이지 않는 것으로 친다(m).</param>
        public static bool CanSeeAny(Vector3 candidate, IReadOnlyList<Vector3> targets, float maxDistance)
        {
            Vector3 eye = candidate + Vector3.up * GuardEyeHeight;
            foreach (Vector3 target in targets)
            {
                Vector3 center = target + Vector3.up * MannequinCenterHeight;
                if ((center - eye).sqrMagnitude <= maxDistance * maxDistance && !IsBlocked(eye, center))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// NavMesh에 붙일 수 있는 첫 번째 기준 지점을 찾는다.
        /// </summary>
        private static bool TryFindAnchor(IReadOnlyList<Vector3> anchors, out Vector3 anchor)
        {
            foreach (Vector3 point in anchors)
            {
                if (NavMesh.SamplePosition(point, out NavMeshHit hit, NavMeshSnapDistance, NavMesh.AllAreas))
                {
                    anchor = hit.position;
                    return true;
                }
            }
            anchor = default;
            return false;
        }

        /// <summary>
        /// 후보를 NavMesh에 붙인 뒤 기준 지점까지 완전한 경로가 있는지 확인한다.
        /// </summary>
        private static bool IsReachable(Vector3 candidate, Vector3 anchor)
        {
            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, NavMeshSnapDistance, NavMesh.AllAreas))
                return false;
            return NavMesh.CalculatePath(hit.position, anchor, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete;
        }

        /// <summary>
        /// 발 위치에 가드가 설 수 있는 공간(캡슐)이 비어 있는지 확인한다. 바닥에 닿지 않도록 살짝 띄운다.
        /// </summary>
        private static bool HasHeadroom(Vector3 feet)
        {
            Vector3 bottom = feet + Vector3.up * (BodyRadius + FloorClearance);
            Vector3 top = feet + Vector3.up * (BodyHeight - BodyRadius);
            return !Physics.CheckCapsule(bottom, top, BodyRadius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        }

        /// <summary>
        /// 두 점 사이를 환경이나 사물이 가리는지 확인한다. 플레이어(이미 스폰된 아바타)는 무시한다.
        /// </summary>
        private static bool IsBlocked(Vector3 from, Vector3 to)
        {
            Vector3 delta = to - from;
            float distance = delta.magnitude;
            if (distance < 0.01f)
                return false;

            int count = Physics.RaycastNonAlloc(from, delta / distance, lineBuffer, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (lineBuffer[i].collider.GetComponentInParent<PlayerIdentity>() == null)
                    return true;
            }
            return false;
        }
    }
}
