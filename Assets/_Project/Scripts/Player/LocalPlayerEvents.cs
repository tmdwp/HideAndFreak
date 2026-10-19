using System;

namespace GuardVsMannequin.Player
{
    /// <summary>
    /// 로컬 플레이어 아바타의 생성/제거를 알린다.
    /// 씬 카메라, HUD 등이 플레이어 오브젝트를 직접 찾지 않도록 한다.
    /// </summary>
    public static class LocalPlayerEvents
    {
        /// <summary>로컬 플레이어 아바타가 생성되었을 때.</summary>
        public static event Action<PlayerIdentity> Spawned;

        /// <summary>로컬 플레이어 아바타가 제거되었을 때.</summary>
        public static event Action<PlayerIdentity> Despawned;

        /// <summary>현재 로컬 플레이어 아바타. 없으면 null.</summary>
        public static PlayerIdentity Current { get; private set; }

        /// <summary>
        /// 로컬 플레이어 생성을 알린다.
        /// </summary>
        public static void RaiseSpawned(PlayerIdentity identity)
        {
            Current = identity;
            Spawned?.Invoke(identity);
        }

        /// <summary>
        /// 로컬 플레이어 제거를 알린다.
        /// </summary>
        public static void RaiseDespawned(PlayerIdentity identity)
        {
            if (Current == identity)
                Current = null;
            Despawned?.Invoke(identity);
        }
    }
}
