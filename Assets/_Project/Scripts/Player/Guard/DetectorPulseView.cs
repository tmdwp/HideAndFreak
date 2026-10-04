using UnityEngine;

namespace GuardVsMannequin.Player.Guard
{
    /// <summary>
    /// 탐지기 작동 시 가드 발밑에서 퍼져 나가는 원을 표시한다. (정식 VFX 도입 전 임시 표현)
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    [System.Obsolete("Phase 7: 탐지기 제거. 가드 프리팹에서 지운 뒤 파일도 삭제한다.")]
    public class DetectorPulseView : MonoBehaviour
    {
        private const int Segments = 48;

        [SerializeField] private float duration = 0.6f;
        [SerializeField] private float heightOffset = 0.1f;

        private LineRenderer lineRenderer;
        private float maxRadius;
        private float elapsed = -1f;

        /// <summary>
        /// 원형 라인 렌더러를 준비하고 숨긴다.
        /// </summary>
        private void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.useWorldSpace = true;
            lineRenderer.loop = true;
            lineRenderer.positionCount = Segments;
            lineRenderer.enabled = false;
        }

        /// <summary>
        /// 지정한 반경까지 퍼지는 파동을 시작한다.
        /// </summary>
        public void Play(float radius)
        {
            maxRadius = radius;
            elapsed = 0f;
            lineRenderer.enabled = true;
        }

        /// <summary>
        /// 파동 반경을 키우고, 끝나면 숨긴다.
        /// </summary>
        private void Update()
        {
            if (elapsed < 0f)
                return;

            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            if (t >= 1f)
            {
                elapsed = -1f;
                lineRenderer.enabled = false;
                return;
            }

            UpdateCircle(maxRadius * t);
        }

        /// <summary>
        /// 가드 위치를 중심으로 원의 점을 갱신한다.
        /// </summary>
        private void UpdateCircle(float radius)
        {
            Vector3 center = transform.position + Vector3.up * heightOffset;
            for (int i = 0; i < Segments; i++)
            {
                float angle = i * Mathf.PI * 2f / Segments;
                lineRenderer.SetPosition(i, center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius);
            }
        }
    }
}
