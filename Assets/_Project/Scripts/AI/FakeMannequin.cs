using System;
using FishNet.Object;
using GuardVsMannequin.Core;
using GuardVsMannequin.Player;

namespace GuardVsMannequin.AI
{
    /// <summary>
    /// 가짜 마네킹의 피격 처리. 가드 입장에서는 실제 마네킹과 똑같이 맞고 사라진다.
    /// 생존 인원 집계와 수류탄 효과에는 포함되지 않는다. (PlayerIdentity가 없음)
    /// </summary>
    public class FakeMannequin : NetworkBehaviour, IDamageable
    {
        private bool isAlive = true;

        /// <summary>서버에서 공격받아 제거되어야 할 때.</summary>
        public event Action<FakeMannequin> ServerKilled;

        public bool IsAlive => isAlive;

        /// <summary>피해 규칙상 마네킹으로 취급한다.</summary>
        public PlayerRole Role => PlayerRole.Mannequin;

        /// <summary>
        /// 피해 규칙을 확인하고, 맞으면 한 번에 제거된다.
        /// </summary>
        [Server]
        public void TakeDamage(int amount, NetworkObject attacker)
        {
            if (!isAlive || amount <= 0)
                return;

            PlayerRole attackerRole = attacker != null && attacker.TryGetComponent(out PlayerIdentity attackerIdentity)
                ? attackerIdentity.Role
                : PlayerRole.None;

            if (!DamageRules.CanDamage(attackerRole, Role, GameServices.CurrentStateOrWaiting))
                return;

            isAlive = false;
            ServerKilled?.Invoke(this);
        }
    }
}
