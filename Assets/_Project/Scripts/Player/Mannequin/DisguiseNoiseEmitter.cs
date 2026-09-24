using FishNet.Object;
using GuardVsMannequin.Audio;
using GuardVsMannequin.Core;
using UnityEngine;

namespace GuardVsMannequin.Player.Mannequin
{
    /// <summary>
    /// 변신 중인 마네킹이 일정 간격마다 소음을 내게 한다. (서버에서 판단)
    /// 가드에게 변신한 마네킹의 위치 단서를 준다.
    /// </summary>
    [RequireComponent(typeof(MannequinTransformer), typeof(NoiseEmitter))]
    public class DisguiseNoiseEmitter : NetworkBehaviour
    {
        private MannequinTransformer transformer;
        private NoiseEmitter noiseEmitter;
        private float nextNoiseTime = float.MaxValue;

        /// <summary>
        /// 같은 오브젝트의 구성 요소를 캐시한다.
        /// </summary>
        private void Awake()
        {
            transformer = GetComponent<MannequinTransformer>();
            noiseEmitter = GetComponent<NoiseEmitter>();
        }

        /// <summary>
        /// 서버에서 변신 상태를 확인하고, 간격이 지나면 소음을 낸다.
        /// </summary>
        private void Update()
        {
            if (!this.IsServerReady())
                return;

            if (!transformer.IsDisguised)
            {
                nextNoiseTime = float.MaxValue;
                return;
            }

            // 변신 직후에는 한 간격을 기다린 뒤 첫 소음을 낸다.
            if (nextNoiseTime == float.MaxValue)
                ScheduleNext();

            if (Time.time >= nextNoiseTime)
            {
                noiseEmitter.Emit(NoiseType.DisguiseCreak);
                ScheduleNext();
            }
        }

        /// <summary>
        /// 다음 소음 시각을 무작위 간격으로 정한다. (규칙적이면 예측이 너무 쉬움)
        /// </summary>
        private void ScheduleNext()
        {
            nextNoiseTime = Time.time + Random.Range(GameConstants.DisguiseNoiseIntervalMin, GameConstants.DisguiseNoiseIntervalMax);
        }
    }
}
