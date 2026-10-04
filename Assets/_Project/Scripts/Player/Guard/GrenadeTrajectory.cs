using UnityEngine;

namespace GuardVsMannequin.Player.Guard
{
    /// <summary>
    /// 수류탄 궤적 계산. 고정 초기 속도·중력의 포물선이라 시작점과 속도만 알면 모든 피어가 같은 궤적을 그린다.
    /// 서버는 <see cref="TryFindImpact"/>로 처음 닿는 지점을 정하고, 연출은 <see cref="GetPosition"/>으로 같은 궤적을 따라간다.
    /// </summary>
    public static class GrenadeTrajectory
    {
        /// <summary>충돌 검사 간격(초).</summary>
        private const float StepTime = 0.02f;

        /// <summary>수류탄 충돌 반경(m).</summary>
        public const float Radius = 0.1f;

        /// <summary>
        /// t초 뒤 위치.
        /// </summary>
        public static Vector3 GetPosition(Vector3 start, Vector3 velocity, float t)
        {
            return start + velocity * t + 0.5f * Physics.gravity * (t * t);
        }

        /// <summary>
        /// 궤적을 따라 처음 닿는 지점과 시간을 찾는다. 최대 비행 시간 안에 닿지 않으면 그 시점의 위치를 쓴다.
        /// </summary>
        /// <param name="ignoreRoot">무시할 오브젝트(던진 가드 자신).</param>
        /// <param name="impactPoint">폭발 지점. (닿은 면에서 살짝 떨어진 곳)</param>
        /// <param name="flightTime">폭발까지의 시간(초).</param>
        /// <returns>무언가에 닿았으면 true.</returns>
        public static bool TryFindImpact(Vector3 start, Vector3 velocity, float maxFlightTime, Transform ignoreRoot,
            out Vector3 impactPoint, out float flightTime)
        {
            var hits = new RaycastHit[8];
            Vector3 previous = start;
            for (float t = StepTime; t <= maxFlightTime; t += StepTime)
            {
                Vector3 next = GetPosition(start, velocity, t);
                Vector3 delta = next - previous;
                float distance = delta.magnitude;
                if (distance > 0.0001f)
                {
                    int count = Physics.SphereCastNonAlloc(previous, Radius, delta / distance, hits, distance,
                        Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                    if (TryNearestHit(hits, count, ignoreRoot, out RaycastHit hit))
                    {
                        // 시작부터 겹쳐 있으면(벽에 붙어 던짐) Unity는 point를 0으로 준다. 그 경우 직전 위치에서 터진다.
                        bool startedInside = hit.distance <= 0f && hit.point == Vector3.zero;
                        impactPoint = startedInside ? previous : hit.point + hit.normal * Radius;
                        flightTime = t - StepTime + StepTime * (hit.distance / distance);
                        return true;
                    }
                }
                previous = next;
            }

            impactPoint = GetPosition(start, velocity, maxFlightTime);
            flightTime = maxFlightTime;
            return false;
        }

        /// <summary>
        /// 무시할 오브젝트를 뺀 가장 가까운 충돌.
        /// </summary>
        private static bool TryNearestHit(RaycastHit[] hits, int count, Transform ignoreRoot, out RaycastHit nearest)
        {
            nearest = default;
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                if (ignoreRoot != null && hits[i].transform.IsChildOf(ignoreRoot))
                    continue;
                if (!found || hits[i].distance < nearest.distance)
                {
                    nearest = hits[i];
                    found = true;
                }
            }
            return found;
        }
    }
}
