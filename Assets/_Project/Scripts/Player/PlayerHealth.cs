using System;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using GuardVsMannequin.Core;
using UnityEngine;

namespace GuardVsMannequin.Player
{
    /// <summary>
    /// 플레이어 체력을 서버 권한으로 관리하고 동기화한다.
    /// 다른 플레이어의 체력은 UI에 표시하지 않지만, 값 자체는 동기화된다.
    /// </summary>
    [RequireComponent(typeof(PlayerIdentity))]
    public class PlayerHealth : NetworkBehaviour, IDamageable
    {
        private readonly SyncVar<int> currentHealth = new();
        private readonly SyncVar<int> maxHealth = new();

        private PlayerIdentity identity;

        /// <summary>서버에서 체력이 0이 되었을 때. (사망자, 공격자)</summary>
        public event Action<PlayerHealth, NetworkObject> ServerDied;

        public int CurrentHealth => currentHealth.Value;
        public int MaxHealth => maxHealth.Value;
        public bool IsAlive => currentHealth.Value > 0;
        public PlayerRole Role => identity.Role;

        /// <summary>
        /// 같은 오브젝트의 PlayerIdentity를 캐시한다.
        /// </summary>
        private void Awake()
        {
            identity = GetComponent<PlayerIdentity>();
        }

        /// <summary>
        /// 스폰 직전에 최대 체력을 지정하고 가득 채운다.
        /// </summary>
        public void ServerInitialize(int newMaxHealth)
        {
            maxHealth.Value = Mathf.Max(1, newMaxHealth);
            currentHealth.Value = maxHealth.Value;
        }

        /// <summary>
        /// 최대 체력을 변경한다. 사냥 시간 체력 배율 적용에 사용한다. (Phase 3)
        /// </summary>
        /// <param name="newMaxHealth">새 최대 체력.</param>
        /// <param name="refill">true면 체력을 가득 채우고, false면 현재 비율을 유지한다.</param>
        [Server]
        public void SetMaxHealth(int newMaxHealth, bool refill)
        {
            if (!IsAlive)
                return;

            float ratio = maxHealth.Value > 0 ? (float)currentHealth.Value / maxHealth.Value : 1f;
            maxHealth.Value = Mathf.Max(1, newMaxHealth);
            currentHealth.Value = refill ? maxHealth.Value : Mathf.Max(1, Mathf.RoundToInt(maxHealth.Value * ratio));
        }

        /// <summary>
        /// 피해 규칙을 확인한 뒤 체력을 깎고, 0이 되면 사망 이벤트를 발생시킨다.
        /// </summary>
        [Server]
        public void TakeDamage(int amount, NetworkObject attacker)
        {
            if (!IsAlive || amount <= 0)
                return;

            PlayerRole attackerRole = attacker != null && attacker.TryGetComponent(out PlayerIdentity attackerIdentity)
                ? attackerIdentity.Role
                : PlayerRole.None;

            if (!DamageRules.CanDamage(attackerRole, Role, GameServices.CurrentStateOrWaiting))
                return;

            currentHealth.Value = Mathf.Max(0, currentHealth.Value - amount);
            if (currentHealth.Value == 0)
                ServerDied?.Invoke(this, attacker);
        }
    }
}
