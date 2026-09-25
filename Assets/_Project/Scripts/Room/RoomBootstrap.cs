using FishNet;
using FishNet.Object;
using UnityEngine;

namespace GuardVsMannequin.Room
{
    /// <summary>
    /// 룸 씬에 배치한다. 서버에서 방 상태 오브젝트가 아직 없으면 한 번만 스폰한다.
    /// (게임 후 룸으로 돌아왔을 때는 이미 있는 방 상태를 그대로 사용)
    /// </summary>
    public class RoomBootstrap : MonoBehaviour
    {
        [Tooltip("전역(Global)으로 설정된 RoomState 프리팹.")]
        [SerializeField] private NetworkObject roomStatePrefab;

        /// <summary>
        /// 서버라면 방 상태를 준비한다.
        /// </summary>
        private void Start()
        {
            if (!InstanceFinder.IsServerStarted || FindAnyObjectByType<RoomState>() != null)
                return;

            NetworkObject instance = Instantiate(roomStatePrefab);
            InstanceFinder.ServerManager.Spawn(instance);
        }
    }
}
