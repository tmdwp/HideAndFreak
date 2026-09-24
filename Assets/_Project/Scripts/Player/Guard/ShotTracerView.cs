using UnityEngine;

namespace GuardVsMannequin.Player.Guard
{
    /// <summary>
    /// 사격 궤적과 탄착 지점을 잠깐 표시한다. (정식 VFX 도입 전 임시 표현)
    /// 궤적은 총구 위치에서 시작해, 1인칭 사격자 화면에서도 대각선으로 보이게 한다.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class ShotTracerView : MonoBehaviour
    {
        [SerializeField] private float tracerDuration = 0.15f;
        [SerializeField] private float impactDuration = 0.4f;

        [Tooltip("궤적 시작점. 카메라 오른쪽 아래에 두어 사격자 화면에서도 선이 보이게 한다.")]
        [SerializeField] private Transform muzzle;

        [Tooltip("탄착 지점에 잠깐 표시할 오브젝트. 실행 시 아바타에서 분리된다.")]
        [SerializeField] private Transform impactMarker;

        private LineRenderer lineRenderer;
        private float tracerHideTime;
        private float impactHideTime;

        /// <summary>
        /// 라인 렌더러를 월드 좌표 모드로 초기화하고, 탄착 표시를 아바타에서 분리한다.
        /// </summary>
        private void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = 2;
            lineRenderer.enabled = false;

            if (impactMarker != null)
            {
                // 아바타가 움직여도 탄착 표시는 맞은 자리에 남아야 하므로 부모에서 분리한다.
                impactMarker.SetParent(null, true);
                impactMarker.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 아바타와 함께 분리된 탄착 표시도 제거한다.
        /// </summary>
        private void OnDestroy()
        {
            if (impactMarker != null)
                Destroy(impactMarker.gameObject);
        }

        /// <summary>
        /// 표시 시간이 지나면 숨긴다.
        /// </summary>
        private void Update()
        {
            if (lineRenderer.enabled && Time.time >= tracerHideTime)
                lineRenderer.enabled = false;

            if (impactMarker != null && impactMarker.gameObject.activeSelf && Time.time >= impactHideTime)
                impactMarker.gameObject.SetActive(false);
        }

        /// <summary>
        /// 총구에서 끝점까지 궤적을 표시하고, 무언가에 맞았으면 탄착 지점을 표시한다.
        /// </summary>
        /// <param name="origin">서버가 판정에 사용한 원점. 총구가 없을 때 대신 사용한다.</param>
        /// <param name="endPoint">궤적 끝점.</param>
        /// <param name="didHit">사거리 안에서 충돌했는지 여부.</param>
        public void Show(Vector3 origin, Vector3 endPoint, bool didHit)
        {
            Vector3 start = muzzle != null ? muzzle.position : origin;
            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, endPoint);
            lineRenderer.enabled = true;
            tracerHideTime = Time.time + tracerDuration;

            if (didHit && impactMarker != null)
            {
                impactMarker.position = endPoint;
                impactMarker.gameObject.SetActive(true);
                impactHideTime = Time.time + impactDuration;
            }
        }
    }
}
