using System.Collections;
using UnityEngine;

namespace GuardVsMannequin.Player.Guard
{
    /// <summary>
    /// 임시 수류탄 폭발 연출: 구가 폭발 반경까지 커지는 동안 빛이 밝았다가 꺼지고, 끝나면 오브젝트를 지운다.
    /// (폭발 이펙트 프리팹이 없을 때 <see cref="GrenadeView"/>가 사용)
    /// </summary>
    public class GrenadeBlastAnimator : MonoBehaviour
    {
        private const float PeakIntensity = 12f;

        /// <summary>
        /// 애니메이션을 시작한다.
        /// </summary>
        public void Run(Light flash, Transform sphere, float radius, float duration)
        {
            StartCoroutine(Animate(flash, sphere, radius, Mathf.Max(0.01f, duration)));
        }

        /// <summary>
        /// 시간에 따라 구 크기와 빛 세기를 바꾸고 끝나면 지운다.
        /// </summary>
        private IEnumerator Animate(Light flash, Transform sphere, float radius, float duration)
        {
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float progress = t / duration;
                sphere.localScale = Vector3.one * (radius * 2f * Mathf.Sqrt(progress));
                flash.intensity = Mathf.Lerp(PeakIntensity, 0f, progress);
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
