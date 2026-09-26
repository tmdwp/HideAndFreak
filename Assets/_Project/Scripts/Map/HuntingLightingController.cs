using System.Collections;
using GuardVsMannequin.Audio;
using GuardVsMannequin.Core;
using GuardVsMannequin.Settings;
using UnityEngine;
using UnityEngine.Rendering;

namespace GuardVsMannequin.Map
{
    /// <summary>
    /// 사냥 시간 진입 연출. 별도의 텍스트/알림 없이 전원 차단음과 함께 맵의 모든 조명이 꺼진다.
    /// 동기화된 게임 상태만 보고 각 피어가 똑같이 연출하므로 네트워크 오브젝트가 필요 없다.
    /// 사냥 시간과 결과 화면 동안 어둡게 유지하고, 다음 판(대기/준비)에서 원래대로 되돌린다.
    /// </summary>
    public class HuntingLightingController : MonoBehaviour
    {
        [SerializeField] private NoiseLibrary noiseLibrary;

        [Tooltip("조명이 하나씩 꺼지는 데 걸리는 최대 지연(초). 한꺼번에 꺼지는 것보다 전원이 나가는 느낌을 준다.")]
        [SerializeField] private float maxOffDelay = 0.5f;

        [Tooltip("사냥 시간의 환경광. 완전히 검으면 손전등 밖이 아무것도 안 보인다.")]
        [SerializeField] private Color huntingAmbient = new Color(0.02f, 0.02f, 0.03f);

        private MapLight[] mapLights;
        private AudioSource audioSource;
        private bool isDark;

        private AmbientMode originalAmbientMode;
        private Color originalAmbientLight;
        private float originalReflectionIntensity;
        private Material originalSkybox;
        private Material darkSkybox;

        /// <summary>
        /// 원래 환경 설정을 저장하고, 전원 차단음용 2D 오디오 소스를 만든다.
        /// </summary>
        private void Awake()
        {
            mapLights = FindObjectsByType<MapLight>();

            originalAmbientMode = RenderSettings.ambientMode;
            originalAmbientLight = RenderSettings.ambientLight;
            originalReflectionIntensity = RenderSettings.reflectionIntensity;
            originalSkybox = RenderSettings.skybox;
            darkSkybox = CreateDarkSkybox();

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        /// <summary>
        /// 씬을 떠날 때 환경 설정을 되돌린다. (RenderSettings는 씬 전환 후에도 남을 수 있음)
        /// </summary>
        private void OnDestroy()
        {
            if (isDark)
                RestoreEnvironment();
            if (darkSkybox != null)
                Destroy(darkSkybox);
        }

        /// <summary>
        /// 상태에 맞게 어둡게 하거나 원래대로 되돌린다.
        /// </summary>
        private void Update()
        {
            GameState state = GameServices.CurrentStateOrWaiting;
            bool shouldBeDark = state == GameState.Hunting || (state == GameState.Ended && isDark);

            if (shouldBeDark && !isDark)
                BlackOut();
            else if (!shouldBeDark && isDark)
                RestoreLights();
        }

        /// <summary>
        /// 전원 차단음을 재생하고 조명을 조금씩 어긋나게 끄며 환경광을 낮춘다.
        /// </summary>
        private void BlackOut()
        {
            isDark = true;

            if (noiseLibrary != null)
                audioSource.PlayOneShot(noiseLibrary.GetClip(NoiseType.PowerDown), noiseLibrary.Get(NoiseType.PowerDown).volume * AudioVolumes.Sfx);

            foreach (MapLight mapLight in mapLights)
                StartCoroutine(TurnOffAfter(mapLight, Random.Range(0f, maxOffDelay)));

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = huntingAmbient;
            RenderSettings.reflectionIntensity = 0f;
            RenderSettings.skybox = darkSkybox;
        }

        /// <summary>
        /// 지정한 시간 뒤에 조명 하나를 끈다.
        /// </summary>
        private IEnumerator TurnOffAfter(MapLight mapLight, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (isDark && mapLight != null)
                mapLight.SetOn(false);
        }

        /// <summary>
        /// 모든 조명을 켜고 환경 설정을 되돌린다.
        /// </summary>
        private void RestoreLights()
        {
            isDark = false;
            StopAllCoroutines();

            foreach (MapLight mapLight in mapLights)
            {
                if (mapLight != null)
                    mapLight.SetOn(true);
            }

            RestoreEnvironment();
        }

        /// <summary>
        /// 노출을 0으로 낮춘 기본 절차적 스카이박스로 검은 하늘을 만든다.
        /// (스카이박스를 비우면 카메라 기본 배경색(푸른색)이 보일 수 있음)
        /// </summary>
        private static Material CreateDarkSkybox()
        {
            Shader shader = Shader.Find("Skybox/Procedural");
            if (shader == null)
                return null;

            var material = new Material(shader);
            material.SetFloat("_Exposure", 0f);
            return material;
        }

        /// <summary>
        /// 저장해 둔 환경광, 반사광, 스카이박스를 되돌린다.
        /// </summary>
        private void RestoreEnvironment()
        {
            RenderSettings.ambientMode = originalAmbientMode;
            RenderSettings.ambientLight = originalAmbientLight;
            RenderSettings.reflectionIntensity = originalReflectionIntensity;
            RenderSettings.skybox = originalSkybox;
        }
    }
}
