using FishNet.Object;
using FishNet.Object.Synchronizing;
using GuardVsMannequin.Core;

namespace GuardVsMannequin.Player
{
    /// <summary>
    /// 수류탄에 맞은 효과(외곽선 표시, 감속)를 서버 Tick 기준 "끝나는 시각"으로 동기화한다.
    /// 다시 맞으면 끝 시각만 늦춘다(중첩 없음).
    /// 감속은 <see cref="IMoveSpeedModifier"/>로 <see cref="MoveSpeedController"/>가 곱한다.
    /// 서버에서도 같은 배율이 적용되므로 <see cref="MovementValidator"/>가 감속 중 허용 속도를 낮춰 검사한다.
    /// </summary>
    public class StatusEffects : NetworkBehaviour, IStatusEffects, IMoveSpeedModifier
    {
        private readonly SyncVar<uint> revealEndTick = new();
        private readonly SyncVar<uint> slowEndTick = new();

        /// <inheritdoc />
        public bool IsRevealed => IsActive(revealEndTick.Value);

        /// <inheritdoc />
        public bool IsSlowed => IsActive(slowEndTick.Value);

        /// <inheritdoc />
        public float SpeedMultiplier => IsSlowed ? GameTuning.GrenadeSlowMultiplier : 1f;

        /// <summary>
        /// 수류탄 효과를 건다. (서버 전용)
        /// </summary>
        [Server]
        public void ApplyGrenadeHit()
        {
            uint now = TimeManager.Tick;
            revealEndTick.Value = now + TimeManager.TimeToTicks(GameTuning.GrenadeRevealDuration);
            slowEndTick.Value = now + TimeManager.TimeToTicks(GameTuning.GrenadeSlowDuration);
        }

        /// <summary>
        /// 끝 시각이 아직 지나지 않았는지 확인한다. 네트워크 초기화 전에는 효과 없음.
        /// </summary>
        private bool IsActive(uint endTick)
        {
            if (endTick == 0 || TimeManager == null || !IsSpawned)
                return false;
            return TimeManager.Tick < endTick;
        }
    }
}
