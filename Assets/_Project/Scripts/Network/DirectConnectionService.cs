using FishNet.Managing;
using UnityEngine;

namespace GuardVsMannequin.Network
{
    /// <summary>
    /// 주소를 직접 입력해 접속하는 개발용 구현. (NetworkManager에 설정된 Tugboat 트랜스포트 사용)
    /// </summary>
    public class DirectConnectionService : MonoBehaviour, IConnectionService
    {
        [SerializeField] private NetworkManager networkManager;

        public bool IsServerRunning => networkManager.IsServerStarted;
        public bool IsClientRunning => networkManager.IsClientStarted;

        /// <summary>
        /// 서버를 시작한 뒤 같은 프로세스에서 클라이언트로 접속한다.
        /// </summary>
        public void StartHost()
        {
            if (networkManager.ServerManager.StartConnection())
                networkManager.ClientManager.StartConnection();
        }

        /// <summary>
        /// 지정한 주소로 클라이언트 접속을 시작한다.
        /// </summary>
        public void StartClient(string address)
        {
            networkManager.ClientManager.StartConnection(address);
        }

        /// <summary>
        /// 클라이언트와 서버 접속을 모두 종료한다.
        /// </summary>
        public void Stop()
        {
            if (networkManager.IsClientStarted)
                networkManager.ClientManager.StopConnection();
            if (networkManager.IsServerStarted)
                networkManager.ServerManager.StopConnection(true);
        }
    }
}
