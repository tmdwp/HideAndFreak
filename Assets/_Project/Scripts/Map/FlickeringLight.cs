using System.Collections.Generic;
using UnityEngine;

namespace GuardVsMannequin.Map
{
    /// <summary>
    /// 오래된 전구처럼 밝기가 미세하게 흔들리고, 가끔 짧게 꺼졌다 켜지는 조명. (로비 배경, 사냥 시간 맵 조명)
    /// - 대상: 기본은 이 오브젝트와 자식의 Light 전부. <see cref="SetTargets"/>로 지정할 수 있다. (조명 기구 하나의 여러 Light가 함께 깜빡임)
    /// - 기준 밝기: 대상의 원래 밝기 × <see cref="IntensityScale"/>. 꺼지면(비활성·제거) 원래 밝기로 되돌린다.
    /// </summary>
    public class FlickeringLight : MonoBehaviour
    {
        [Tooltip("원래 밝기에 곱할 배율. 1이면 원래 밝기를 기준으로 깜빡인다.")]
        [SerializeField] private float intensityScale = 1f;

        [Tooltip("평소 흔들림 폭(0~1). 기준 밝기에서 이 비율만큼 어두워질 수 있다.")]
        [Range(0f, 1f)]
        [SerializeField] private float noiseAmount = 0.25f;

        [Tooltip("평소 흔들림 속도.")]
        [SerializeField] private float noiseSpeed = 6f;

        [Tooltip("1초에 깜빡임이 시작될 확률.")]
        [SerializeField] private float blinkChancePerSecond = 0.2f;

        [Tooltip("한 번 깜빡일 때 꺼져 있는 시간 범위(초).")]
        [SerializeField] private Vector2 blinkDuration = new Vector2(0.05f, 0.2f);

        [Tooltip("꺼져 있을 때 남는 밝기 비율.")]
        [Range(0f, 1f)]
        [SerializeField] private float blinkFloor = 0.05f;

        private Light[] targets = System.Array.Empty<Light>();
        private float[] originalIntensities = System.Array.Empty<float>();
        private float noiseSeed;
        private float blinkRemaining;

        /// <summary>원래 밝기에 곱할 배율.</summary>
        public float IntensityScale
        {
            get => intensityScale;
            set => intensityScale = Mathf.Max(0f, value);
        }

        /// <summary>
        /// 이 오브젝트와 자식의 조명을 대상으로 잡는다.
        /// </summary>
        private void Awake()
        {
            noiseSeed = Random.value * 100f;
            SetTargets(GetComponentsInChildren<Light>(true));
        }

        /// <summary>
        /// 깜빡일 조명을 지정하고 지금 밝기를 원래 밝기로 저장한다. (이전 대상은 원래 밝기로 되돌린다)
        /// </summary>
        public void SetTargets(IReadOnlyList<Light> lights)
        {
            RestoreIntensities();
            targets = new Light[lights.Count];
            originalIntensities = new float[lights.Count];
            for (int i = 0; i < lights.Count; i++)
            {
                targets[i] = lights[i];
                originalIntensities[i] = lights[i] != null ? lights[i].intensity : 0f;
            }
        }

        /// <summary>
        /// 이번 프레임의 밝기를 정한다.
        /// </summary>
        private void Update()
        {
            ApplyRatio(intensityScale * NextRatio(Time.deltaTime));
        }

        /// <summary>
        /// 이번 프레임의 밝기 비율(0~1). 깜빡이는 중이면 바닥값, 아니면 펄린 노이즈로 흔들린다.
        /// </summary>
        private float NextRatio(float deltaTime)
        {
            if (blinkRemaining > 0f)
            {
                blinkRemaining -= deltaTime;
                return blinkFloor;
            }

            if (Random.value < blinkChancePerSecond * deltaTime)
            {
                blinkRemaining = Random.Range(blinkDuration.x, blinkDuration.y);
                return blinkFloor;
            }

            float noise = Mathf.PerlinNoise(Time.time * noiseSpeed, noiseSeed);
            return 1f - noiseAmount * noise;
        }

        /// <summary>
        /// 모든 대상의 밝기를 원래 밝기 × 비율로 정한다.
        /// </summary>
        private void ApplyRatio(float ratio)
        {
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] != null)
                    targets[i].intensity = originalIntensities[i] * ratio;
            }
        }

        /// <summary>
        /// 꺼질 때 원래 밝기로 되돌린다.
        /// </summary>
        private void OnDisable()
        {
            RestoreIntensities();
        }

        /// <summary>
        /// 대상의 밝기를 저장해 둔 원래 밝기로 되돌린다.
        /// </summary>
        private void RestoreIntensities()
        {
            ApplyRatio(1f);
        }
    }
}
