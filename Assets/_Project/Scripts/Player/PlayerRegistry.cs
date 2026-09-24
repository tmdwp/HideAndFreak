using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using GuardVsMannequin.Core;

namespace GuardVsMannequin.Player
{
    /// <summary>
    /// 스폰된(= 살아 있는) 플레이어 아바타를 추적하고, 역할별 생존 인원을 동기화한다.
    /// 사망 시 아바타가 제거되므로 "스폰 중"과 "생존"은 같은 의미다.
    /// </summary>
    public class PlayerRegistry : NetworkBehaviour
    {
        private readonly SyncVar<int> aliveGuardCount = new();
        private readonly SyncVar<int> aliveMannequinCount = new();

        private readonly HashSet<PlayerIdentity> alivePlayers = new();

        /// <summary>생존 중인 가드 수.</summary>
        public int AliveGuardCount => aliveGuardCount.Value;

        /// <summary>생존 중인 마네킹 수.</summary>
        public int AliveMannequinCount => aliveMannequinCount.Value;

        /// <summary>생존 중인 아바타 목록. (서버 전용)</summary>
        public IReadOnlyCollection<PlayerIdentity> AlivePlayers => alivePlayers;

        /// <summary>
        /// 서버에서 아바타 스폰/제거 이벤트를 구독한다.
        /// </summary>
        public override void OnStartServer()
        {
            base.OnStartServer();
            PlayerIdentity.ServerSpawned += OnAvatarSpawned;
            PlayerIdentity.ServerDespawned += OnAvatarDespawned;
        }

        /// <summary>
        /// 이벤트 구독을 해제하고 목록을 비운다.
        /// </summary>
        public override void OnStopServer()
        {
            base.OnStopServer();
            PlayerIdentity.ServerSpawned -= OnAvatarSpawned;
            PlayerIdentity.ServerDespawned -= OnAvatarDespawned;
            alivePlayers.Clear();
        }

        /// <summary>스폰된 아바타를 등록한다.</summary>
        private void OnAvatarSpawned(PlayerIdentity identity)
        {
            if (alivePlayers.Add(identity))
                Recount();
        }

        /// <summary>제거된 아바타를 해제한다.</summary>
        private void OnAvatarDespawned(PlayerIdentity identity)
        {
            if (alivePlayers.Remove(identity))
                Recount();
        }

        /// <summary>
        /// 역할별 생존 인원을 다시 계산해 동기화한다.
        /// </summary>
        private void Recount()
        {
            int guards = 0;
            int mannequins = 0;
            foreach (PlayerIdentity identity in alivePlayers)
            {
                if (identity.Role == PlayerRole.Guard)
                    guards++;
                else if (identity.Role == PlayerRole.Mannequin)
                    mannequins++;
            }

            aliveGuardCount.Value = guards;
            aliveMannequinCount.Value = mannequins;
        }
    }
}
