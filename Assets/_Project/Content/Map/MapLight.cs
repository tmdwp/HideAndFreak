using System.Collections.Generic;
using UnityEngine;

namespace GuardVsMannequin.Map
{
    /// <summary>
    /// 맵의 조명 기구 하나. Light 컴포넌트와 발광 머티리얼을 함께 켜고 끈다.
    /// 조명 기구는 변신 대상이 아니다. (TransformableProp을 붙이지 않음)
    /// </summary>
    public class MapLight : MonoBehaviour
    {
        [Tooltip("켜고 끌 조명. 비어 있으면 자식에서 찾는다.")]
        [SerializeField] private Light[] lights;

        [Tooltip("전구 외형. 켜짐/꺼짐 머티리얼로 교체한다. (없어도 됨)")]
        [SerializeField] private Renderer bulbRenderer;
        [SerializeField] private Material onMaterial;
        [SerializeField] private Material offMaterial;

        /// <summary>현재 켜져 있는지 여부.</summary>
        public bool IsOn { get; private set; } = true;

        /// <summary>이 조명 기구가 켜고 끄는 조명. (사냥 시간 깜빡임 대상)</summary>
        public IReadOnlyList<Light> Lights => lights;

        /// <summary>
        /// 조명 목록이 비어 있으면 자식에서 수집한다.
        /// </summary>
        private void Awake()
        {
            if (lights == null || lights.Length == 0)
                lights = GetComponentsInChildren<Light>(true);
        }

        /// <summary>
        /// 조명과 전구 외형을 켜거나 끈다.
        /// </summary>
        public void SetOn(bool on)
        {
            IsOn = on;
            foreach (Light mapLight in lights)
                mapLight.enabled = on;

            if (bulbRenderer != null)
            {
                Material material = on ? onMaterial : offMaterial;
                if (material != null)
                    bulbRenderer.sharedMaterial = material;
            }
        }
    }
}
