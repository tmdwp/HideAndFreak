using UnityEngine;

namespace GuardVsMannequin.UI
{
    /// <summary>
    /// 로비 배경 연출용 타원 순환 경로. 경로를 따라 간 거리로 위치와 진행 방향을 구한다.
    /// 타원은 각도로 나누면 속도가 들쭉날쭉하므로, 미리 잰 호 길이 표로 나눠 일정한 속도가 되게 한다.
    /// 경로 오브젝트는 크기(Scale)를 1로 둔다. (길이는 로컬 좌표로 잰다)
    /// </summary>
    public class LobbyChasePath : MonoBehaviour
    {
        private const int SampleCount = 128;

        [Tooltip("좌우(X) 반지름(m).")]
        [SerializeField] private float radiusX = 7f;

        [Tooltip("앞뒤(Z) 반지름(m).")]
        [SerializeField] private float radiusZ = 3f;

        [Tooltip("켜면 위에서 볼 때 시계 방향으로 돈다.")]
        [SerializeField] private bool clockwise;

        // cumulativeLengths[i] = 샘플 0부터 i까지의 길이 (마지막 = 전체 길이)
        private float[] cumulativeLengths;

        /// <summary>경로 한 바퀴 길이(m).</summary>
        public float Length
        {
            get
            {
                EnsureTable();
                return cumulativeLengths[SampleCount];
            }
        }

        /// <summary>
        /// 출발점에서 distance만큼 간 곳의 월드 위치와 진행 방향(수평)을 반환한다. 한 바퀴를 넘으면 다시 돈다.
        /// </summary>
        public Pose Evaluate(float distance)
        {
            EnsureTable();
            float wrapped = Mathf.Repeat(distance, Length);

            int index = System.Array.BinarySearch(cumulativeLengths, wrapped);
            if (index < 0)
                index = ~index - 1;
            index = Mathf.Clamp(index, 0, SampleCount - 1);

            float segment = cumulativeLengths[index + 1] - cumulativeLengths[index];
            float t = segment > 0f ? (wrapped - cumulativeLengths[index]) / segment : 0f;
            float angle = (index + t) / SampleCount * Mathf.PI * 2f;

            Vector3 position = transform.TransformPoint(LocalPoint(angle));
            Vector3 forward = Vector3.ProjectOnPlane(transform.TransformDirection(LocalTangent(angle)), Vector3.up);
            Quaternion rotation = forward.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(forward, Vector3.up) : transform.rotation;
            return new Pose(position, rotation);
        }

        /// <summary>
        /// 인스펙터 값이 바뀌면 길이 표를 다시 만든다.
        /// </summary>
        private void OnValidate()
        {
            cumulativeLengths = null;
        }

        /// <summary>
        /// 각도에 해당하는 로컬 좌표.
        /// </summary>
        private Vector3 LocalPoint(float angle)
        {
            float direction = clockwise ? -1f : 1f;
            return new Vector3(Mathf.Cos(angle) * radiusX, 0f, Mathf.Sin(angle) * radiusZ * direction);
        }

        /// <summary>
        /// 각도에 해당하는 로컬 진행 방향. (LocalPoint의 미분)
        /// </summary>
        private Vector3 LocalTangent(float angle)
        {
            float direction = clockwise ? -1f : 1f;
            return new Vector3(-Mathf.Sin(angle) * radiusX, 0f, Mathf.Cos(angle) * radiusZ * direction);
        }

        /// <summary>
        /// 호 길이 표가 없으면 만든다.
        /// </summary>
        private void EnsureTable()
        {
            if (cumulativeLengths != null)
                return;

            cumulativeLengths = new float[SampleCount + 1];
            Vector3 previous = LocalPoint(0f);
            for (int i = 1; i <= SampleCount; i++)
            {
                Vector3 current = LocalPoint((float)i / SampleCount * Mathf.PI * 2f);
                cumulativeLengths[i] = cumulativeLengths[i - 1] + Vector3.Distance(previous, current);
                previous = current;
            }
        }

        /// <summary>
        /// 씬 뷰에 경로를 그린다.
        /// </summary>
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.8f);
            Vector3 previous = transform.TransformPoint(LocalPoint(0f));
            for (int i = 1; i <= SampleCount; i++)
            {
                Vector3 current = transform.TransformPoint(LocalPoint((float)i / SampleCount * Mathf.PI * 2f));
                Gizmos.DrawLine(previous, current);
                previous = current;
            }
        }
    }
}
