using FishNet.Object;
using GuardVsMannequin.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace GuardVsMannequin.Player.Mannequin
{
    /// <summary>
    /// 사냥 시간 진입 시 마네킹을 사냥 모드로 바꾼다.
    /// - 서버: 강제 변신 해제, 최대 체력 = 기본 체력 × huntHealthMultiplier (가득 채움)
    /// - 소유자: 1인칭 전환(본인 몸은 그림자만), 제한된 시야(HuntVisionEffect)
    /// 동기화된 게임 상태를 보고 각자 한 번씩 적용한다.
    /// </summary>
    [RequireComponent(typeof(MannequinCharacter), typeof(MannequinTransformer), typeof(PlayerHealth))]
    public class MannequinHuntingMode : NetworkBehaviour
    {
        [Tooltip("1인칭에서 본인 화면에 보이지 않게(그림자만) 할 몸 렌더러.")]
        [SerializeField] private Renderer[] bodyRenderers;

        private MannequinCharacter character;
        private MannequinTransformer transformer;
        private PlayerHealth health;
        private HuntVisionEffect huntVision;
        private bool serverApplied;
        private bool ownerApplied;

        /// <summary>
        /// 같은 오브젝트의 구성 요소를 캐시한다.
        /// </summary>
        private void Awake()
        {
            character = GetComponent<MannequinCharacter>();
            transformer = GetComponent<MannequinTransformer>();
            health = GetComponent<PlayerHealth>();
            huntVision = GetComponent<HuntVisionEffect>();
        }

        /// <summary>
        /// 사냥 시간이 되면 서버/소유자 측 변경을 한 번씩 적용한다.
        /// </summary>
        private void Update()
        {
            if (!this.IsNetworkReady() || GameServices.CurrentStateOrWaiting != GameState.Hunting)
                return;

            if (IsServerInitialized && !serverApplied)
                ApplyOnServer();

            if (IsOwner && !ownerApplied)
                ApplyOnOwner();
        }

        /// <summary>
        /// 변신을 해제하고 사냥 시간 체력을 적용한다.
        /// </summary>
        private void ApplyOnServer()
        {
            serverApplied = true;
            transformer.ForceUntransform();

            GameSettings settings = GameServices.State.Settings;
            int huntMaxHealth = Mathf.Max(1, Mathf.RoundToInt(settings.mannequinMaxHealth * settings.huntHealthMultiplier));
            health.SetMaxHealth(huntMaxHealth, refill: true);
        }

        /// <summary>
        /// 1인칭으로 전환한다. (사냥 시간 속도는 MoveSpeedController가 적용)
        /// </summary>
        private void ApplyOnOwner()
        {
            ownerApplied = true;
            character.SetFirstPerson(true);

            foreach (Renderer bodyRenderer in bodyRenderers)
                bodyRenderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;

            // 주변만 보이도록 시야를 제한한다. (본인 화면에만 적용)
            if (huntVision != null)
                huntVision.Activate();
        }
    }
}
