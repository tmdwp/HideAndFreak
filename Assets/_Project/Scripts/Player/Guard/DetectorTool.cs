using System.Collections.Generic;
using FishNet.Object;
using GuardVsMannequin.Audio;
using GuardVsMannequin.Core;
using GuardVsMannequin.Localization;
using UnityEngine;

namespace GuardVsMannequin.Player.Guard
{
    /// <summary>
    /// 가드의 탐지기. 우클릭 시 서버가 반경 안의 모든 실제 마네킹(변신 여부 무관)에게 소음을 내게 한다.
    /// 가짜 마네킹은 반응하지 않는다. 쿨타임은 총과 공유하지 않는다.
    /// </summary>
    [RequireComponent(typeof(NoiseEmitter))]
    public class DetectorTool : NetworkBehaviour, ITool
    {
        [SerializeField] private DetectorPulseView pulseView;

        private readonly Collider[] overlapBuffer = new Collider[64];
        private readonly HashSet<PlayerIdentity> detected = new();
        private readonly ToolCooldown cooldown = new();

        private IPlayerInput playerInput;
        private NoiseEmitter noiseEmitter;

        public string DisplayName => Loc.Get("tool.detector");
        public float CooldownRemaining => cooldown.LocalRemaining;
        public bool IsReady => CooldownRemaining <= 0f && DamageRules.CanUseGuardTools(GameServices.CurrentStateOrWaiting);

        /// <summary>호스트 설정의 탐지기 쿨타임.</summary>
        private static float Cooldown => GameServices.CurrentSettingsOrDefault.detectorCooldown;

        /// <summary>
        /// 입력과 소음 구현을 캐시한다.
        /// </summary>
        private void Awake()
        {
            playerInput = GetComponent<IPlayerInput>();
            noiseEmitter = GetComponent<NoiseEmitter>();
        }

        /// <summary>
        /// 소유자만 우클릭 입력을 구독한다.
        /// </summary>
        public override void OnStartClient()
        {
            base.OnStartClient();
            if (IsOwner && playerInput != null)
                playerInput.SecondaryPressed += OnSecondaryPressed;
        }

        /// <summary>
        /// 입력 구독을 해제한다.
        /// </summary>
        public override void OnStopClient()
        {
            base.OnStopClient();
            if (IsOwner && playerInput != null)
                playerInput.SecondaryPressed -= OnSecondaryPressed;
        }

        /// <summary>
        /// 쿨타임이 끝났으면 서버에 탐지를 요청한다.
        /// </summary>
        private void OnSecondaryPressed()
        {
            if (!IsReady)
                return;

            cooldown.StartLocal(Cooldown);
            DetectServerRpc();
        }

        /// <summary>
        /// 서버에서 반경 안의 실제 마네킹을 찾아 소음을 내게 한다.
        /// </summary>
        [ServerRpc]
        private void DetectServerRpc()
        {
            if (!DamageRules.CanUseGuardTools(GameServices.CurrentStateOrWaiting))
                return;
            if (!cooldown.TryStartServer(Cooldown))
                return;

            noiseEmitter.Emit(NoiseType.DetectorPing);
            PulseObserversRpc();

            foreach (PlayerIdentity mannequin in FindMannequinsInRange())
            {
                if (mannequin.TryGetComponent(out NoiseEmitter mannequinNoise))
                    mannequinNoise.Emit(NoiseType.MannequinDetected);
            }
        }

        /// <summary>
        /// 반경 안의 살아 있는 실제 마네킹을 중복 없이 찾는다.
        /// </summary>
        private HashSet<PlayerIdentity> FindMannequinsInRange()
        {
            detected.Clear();
            int count = Physics.OverlapSphereNonAlloc(transform.position, GameTuning.DetectorRadius, overlapBuffer,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                PlayerIdentity identity = overlapBuffer[i].GetComponentInParent<PlayerIdentity>();
                if (identity != null && identity.Role == PlayerRole.Mannequin)
                    detected.Add(identity);
            }
            return detected;
        }

        /// <summary>
        /// 모든 클라이언트에 탐지 범위 파동을 표시한다.
        /// </summary>
        [ObserversRpc]
        private void PulseObserversRpc()
        {
            if (pulseView != null)
                pulseView.Play(GameTuning.DetectorRadius);
        }
    }
}
