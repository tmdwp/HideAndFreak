using UnityEngine;

namespace GuardVsMannequin.Player.Guard
{
    /// <summary>
    /// 총알 이펙트를 끝점까지 직선으로 이동시키고, 도착하면 명중 이펙트를 남긴 뒤 사라진다.
    /// (보이는 연출 전용. 판정은 서버 히트스캔이 이미 처리했다)
    /// </summary>
    public class BulletEffect : MonoBehaviour
    {
        // 도착 후 꼬리(Trail) 파티클이 자연스럽게 사라질 시간
        private const float FadeOutTime = 0.6f;
        private const float HitLifetime = 2f;

        private Vector3 target;
        private float speed;
        private GameObject hitPrefab;
        private bool arrived;

        /// <summary>
        /// 이동을 시작한다.
        /// </summary>
        /// <param name="targetPoint">도착 지점.</param>
        /// <param name="moveSpeed">이동 속도(m/s).</param>
        /// <param name="hitEffectPrefab">도착 시 남길 이펙트. 없으면 null.</param>
        public void Launch(Vector3 targetPoint, float moveSpeed, GameObject hitEffectPrefab)
        {
            target = targetPoint;
            speed = Mathf.Max(1f, moveSpeed);
            hitPrefab = hitEffectPrefab;
        }

        /// <summary>
        /// 끝점을 향해 이동하고, 도착하면 명중 이펙트를 만든 뒤 파티클 방출을 멈춘다.
        /// </summary>
        private void Update()
        {
            if (arrived)
                return;

            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            if ((transform.position - target).sqrMagnitude > 0.0001f)
                return;

            arrived = true;
            if (hitPrefab != null)
            {
                GameObject hit = Instantiate(hitPrefab, target, Quaternion.LookRotation(-transform.forward));
                Destroy(hit, HitLifetime);
            }

            foreach (ParticleSystem particles in GetComponentsInChildren<ParticleSystem>())
                particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            Destroy(gameObject, FadeOutTime);
        }
    }
}
