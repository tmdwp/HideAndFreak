using System;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using GuardVsMannequin.Core;

namespace GuardVsMannequin.Player
{
    /// <summary>
    /// 플레이어 아바타의 역할과 표시 이름을 동기화한다.
    /// </summary>
    public class PlayerIdentity : NetworkBehaviour
    {
        private readonly SyncVar<PlayerRole> role = new(PlayerRole.None);
        private readonly SyncVar<string> displayName = new(string.Empty);

        /// <summary>서버에서 아바타가 스폰되었을 때. (레지스트리 등록용)</summary>
        public static event Action<PlayerIdentity> ServerSpawned;

        /// <summary>서버에서 아바타가 제거되었을 때. (사망, 접속 종료, 판 종료 모두 포함)</summary>
        public static event Action<PlayerIdentity> ServerDespawned;

        /// <summary>이 플레이어의 역할.</summary>
        public PlayerRole Role => role.Value;

        /// <summary>화면에 표시할 이름. (Phase 5에서 Steam 닉네임으로 교체)</summary>
        public string DisplayName => displayName.Value;

        /// <summary>
        /// 스폰 직전에 서버에서 역할과 이름을 지정한다.
        /// 스폰 전에 설정한 값은 스폰 데이터와 함께 전송된다.
        /// </summary>
        public void ServerInitialize(PlayerRole newRole, string newDisplayName)
        {
            role.Value = newRole;
            displayName.Value = newDisplayName;
        }

        /// <summary>
        /// 서버 스폰을 알린다.
        /// </summary>
        public override void OnStartServer()
        {
            base.OnStartServer();
            ServerSpawned?.Invoke(this);
        }

        /// <summary>
        /// 서버 제거를 알린다.
        /// </summary>
        public override void OnStopServer()
        {
            base.OnStopServer();
            ServerDespawned?.Invoke(this);
        }
    }
}
