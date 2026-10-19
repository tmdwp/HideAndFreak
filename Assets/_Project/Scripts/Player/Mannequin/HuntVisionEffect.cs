using GuardVsMannequin.Core;
using UnityEngine;

namespace GuardVsMannequin.Player.Mannequin
{
    /// <summary>
    /// 사냥 시간 마네킹의 시야. 로컬 화면에만 적용된다.
    /// - 주변을 은은하게 비추는 개인 조명(본인 화면에만 켜짐)으로 가까운 곳은 보이게 한다.
    /// - 검은 선형 안개로 일정 거리 밖은 보이지 않게 해 시야 범위를 줄인다.
    /// 조명 범위·밝기와 안개 거리는 <see cref="GameTuning"/>(game_tuning.csv)에서 읽는다.
    /// 가드와 다른 플레이어의 화면에는 영향을 주지 않는다. (이 마네킹을 관전하는 사람에게는 같은 시야가 적용된다)
    /// </summary>
    public class HuntVisionEffect : MonoBehaviour, IViewEffect
    {
        [Tooltip("본인 화면에서만 켜지는 개인 조명. 다른 플레이어에게는 꺼진 상태로 남는다.")]
        [SerializeField] private Light visionLight;

        [SerializeField] private Color fogColor = Color.black;

        private bool isActive;
        private bool originalFog;
        private FogMode originalFogMode;
        private Color originalFogColor;
        private float originalFogStart;
        private float originalFogEnd;

        /// <summary>
        /// 개인 조명을 꺼 둔다.
        /// </summary>
        private void Awake()
        {
            if (visionLight != null)
                visionLight.enabled = false;
        }

        /// <summary>
        /// 사망(아바타 제거)이나 판 종료로 사라질 때 안개를 되돌린다.
        /// </summary>
        private void OnDestroy()
        {
            Deactivate();
        }

        /// <summary>
        /// 사냥 시간 시야를 켠다. (로컬 소유자, 또는 이 마네킹을 관전하는 클라이언트에서 호출)
        /// </summary>
        public void Activate()
        {
            if (isActive)
                return;
            isActive = true;

            originalFog = RenderSettings.fog;
            originalFogMode = RenderSettings.fogMode;
            originalFogColor = RenderSettings.fogColor;
            originalFogStart = RenderSettings.fogStartDistance;
            originalFogEnd = RenderSettings.fogEndDistance;

            float fogStart = GameTuning.HuntVisionFogStart;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogStartDistance = fogStart;
            RenderSettings.fogEndDistance = Mathf.Max(fogStart + 0.1f, GameTuning.HuntVisionFogEnd);

            if (visionLight != null)
            {
                visionLight.range = GameTuning.HuntVisionLightRange;
                visionLight.intensity = GameTuning.HuntVisionLightIntensity;
                visionLight.enabled = true;
            }
        }

        /// <summary>
        /// 사냥 시간 시야를 끄고 원래 안개 설정으로 되돌린다.
        /// </summary>
        public void Deactivate()
        {
            if (!isActive)
                return;
            isActive = false;

            RenderSettings.fog = originalFog;
            RenderSettings.fogMode = originalFogMode;
            RenderSettings.fogColor = originalFogColor;
            RenderSettings.fogStartDistance = originalFogStart;
            RenderSettings.fogEndDistance = originalFogEnd;

            if (visionLight != null)
                visionLight.enabled = false;
        }
    }
}
