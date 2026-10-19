using GuardVsMannequin.Player;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GuardVsMannequin.UI
{
    /// <summary>
    /// 로컬 플레이어의 잔여 체력이 낮을수록 화면 가장자리부터 점점 어두워지게 한다.
    /// 전역 URP Volume(비네트 + 노출 감소)을 런타임에 만들어 제어한다. (본인 화면에만 적용)
    /// 카메라의 Post Processing이 켜져 있어야 보인다.
    /// </summary>
    public class LowHealthVignette : MonoBehaviour
    {
        [Tooltip("체력이 거의 없을 때의 비네트 세기(0~1).")]
        [SerializeField] private float maxVignetteIntensity = 0.6f;

        [Tooltip("체력이 거의 없을 때 낮출 노출(EV).")]
        [SerializeField] private float maxExposureDrop = 1.5f;

        [Tooltip("효과 변화 속도. 맞았을 때 서서히 어두워진다.")]
        [SerializeField] private float blendSpeed = 3f;

        private Volume volume;
        private VolumeProfile profile;
        private Vignette vignette;
        private ColorAdjustments colorAdjustments;
        private float currentDamage;

        /// <summary>
        /// 비네트와 색 보정 오버라이드를 가진 전역 볼륨을 만든다.
        /// </summary>
        private void Awake()
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();

            vignette = profile.Add<Vignette>(true);
            vignette.color.Override(Color.black);
            vignette.smoothness.Override(0.8f);
            vignette.intensity.Override(0f);

            colorAdjustments = profile.Add<ColorAdjustments>(true);
            colorAdjustments.postExposure.Override(0f);

            volume = gameObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 100f;
            volume.sharedProfile = profile;
        }

        /// <summary>
        /// 런타임에 만든 프로필을 해제한다.
        /// </summary>
        private void OnDestroy()
        {
            if (profile != null)
                Destroy(profile);
        }

        /// <summary>
        /// 로컬 플레이어의 체력 비율로 효과 세기를 갱신한다. 아바타가 없으면 효과를 끈다.
        /// </summary>
        private void Update()
        {
            float targetDamage = 0f;
            PlayerIdentity localPlayer = LocalPlayerEvents.Current;
            if (localPlayer != null && localPlayer.TryGetComponent(out PlayerHealth health) && health.MaxHealth > 0)
                targetDamage = 1f - Mathf.Clamp01((float)health.CurrentHealth / health.MaxHealth);

            currentDamage = Mathf.MoveTowards(currentDamage, targetDamage, blendSpeed * Time.deltaTime);
            vignette.intensity.value = currentDamage * maxVignetteIntensity;
            colorAdjustments.postExposure.value = -currentDamage * maxExposureDrop;
        }
    }
}
