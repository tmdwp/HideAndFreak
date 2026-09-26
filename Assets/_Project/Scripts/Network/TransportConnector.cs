using FishNet.Managing;
using FishNet.Transporting;
using FishNet.Transporting.Multipass;
using FishNet.Transporting.Tugboat;
using UnityEngine;

namespace GuardVsMannequin.Network
{
    /// <summary>
    /// Multipass 트랜스포트로 서버/클라이언트 연결을 시작하고 끝낸다.
    /// - 서버: Tugboat(직접 접속)는 항상, Steam(FishySteamworks)은 Steam 방일 때만 연다.
    /// - 호스트의 로컬 클라이언트: 항상 Tugboat로 자기 자신(localhost)에 접속한다.
    /// - 참가자: Steam 방이면 FishySteamworks(호스트 SteamID), 직접 접속이면 Tugboat(IP 주소).
    /// Steam 방에서는 Tugboat를 루프백에만 열어, 방 코드 없이 IP로 직접 들어오는 경로를 막는다.
    /// FishySteamworks는 Assembly-CSharp에 있어 타입 이름으로 찾는다.
    /// </summary>
    public class TransportConnector : MonoBehaviour
    {
        private const string SteamTransportTypeName = "FishySteamworks";
        private const string LocalAddress = "127.0.0.1";
        private const string LoopbackIpv4 = "127.0.0.1";
        private const string LoopbackIpv6 = "::1";

        [SerializeField] private NetworkManager networkManager;

        private Multipass multipass;
        private Transport tugboat;
        private Transport steamTransport;
        private string defaultIpv4BindAddress;
        private string defaultIpv6BindAddress;

        /// <summary>서버가 실행 중인지 여부.</summary>
        public bool IsServerRunning => networkManager.IsServerStarted;

        /// <summary>클라이언트가 실행 중인지 여부.</summary>
        public bool IsClientRunning => networkManager.IsClientStarted;

        /// <summary>Steam 트랜스포트를 사용할 수 있는지 여부.</summary>
        public bool HasSteamTransport
        {
            get
            {
                EnsureTransports();
                return steamTransport != null;
            }
        }

        /// <summary>연결에 사용하는 NetworkManager.</summary>
        public NetworkManager NetworkManager => networkManager;

        /// <summary>Steam 트랜스포트 존재 여부를 확인하기 전에 트랜스포트를 찾아 둔다.</summary>
        private void Start()
        {
            EnsureTransports();
        }

        /// <summary>
        /// Multipass 안의 Tugboat와 FishySteamworks를 한 번만 찾는다.
        /// NetworkManager의 하위 매니저는 NetworkManager.Awake에서 준비되므로 Awake에서 찾지 않는다.
        /// </summary>
        private void EnsureTransports()
        {
            if (multipass != null)
                return;

            multipass = networkManager.TransportManager.GetTransport<Multipass>();
            if (multipass == null)
            {
                Debug.LogError("[TransportConnector] NetworkManager의 트랜스포트가 Multipass가 아닙니다.");
                return;
            }

            foreach (Transport transport in multipass.Transports)
            {
                if (transport is Tugboat)
                    tugboat = transport;
                else if (transport.GetType().Name == SteamTransportTypeName)
                    steamTransport = transport;
            }

            if (tugboat == null)
                Debug.LogError("[TransportConnector] Multipass에 Tugboat가 없습니다.");
            else
            {
                // 직접 방에서 되돌릴 수 있도록 인스펙터에 설정된 바인드 주소를 기억한다.
                defaultIpv4BindAddress = tugboat.GetServerBindAddress(IPAddressType.IPv4);
                defaultIpv6BindAddress = tugboat.GetServerBindAddress(IPAddressType.IPv6);
            }
            if (steamTransport == null)
                Debug.LogWarning("[TransportConnector] Multipass에 FishySteamworks가 없어 Steam 방을 만들 수 없습니다.");
        }

        /// <summary>
        /// 서버를 시작한다. Steam 방이면 Steam 트랜스포트도 함께 열고, Tugboat는 호스트 전용(루프백)으로 연다.
        /// </summary>
        /// <returns>서버 시작 요청에 성공했는지 여부.</returns>
        public bool StartServer(bool withSteam)
        {
            EnsureTransports();
            ApplyTugboatBinding(loopbackOnly: withSteam);
            bool started = multipass.StartConnection(true, tugboat.Index);
            if (withSteam && steamTransport != null)
                started &= multipass.StartConnection(true, steamTransport.Index);
            return started;
        }

        /// <summary>
        /// Tugboat 서버의 바인드 주소를 정한다. 루프백 전용이면 같은 PC(호스트 자신)만 접속할 수 있다.
        /// </summary>
        private void ApplyTugboatBinding(bool loopbackOnly)
        {
            tugboat.SetServerBindAddress(loopbackOnly ? LoopbackIpv4 : defaultIpv4BindAddress, IPAddressType.IPv4);
            tugboat.SetServerBindAddress(loopbackOnly ? LoopbackIpv6 : defaultIpv6BindAddress, IPAddressType.IPv6);
        }

        /// <summary>
        /// 호스트의 로컬 클라이언트를 자기 서버에 접속시킨다.
        /// </summary>
        public bool StartLocalClient()
        {
            return StartDirectClient(LocalAddress);
        }

        /// <summary>
        /// IP 주소로 Tugboat 클라이언트를 시작한다.
        /// </summary>
        public bool StartDirectClient(string address)
        {
            EnsureTransports();
            multipass.SetClientTransport(tugboat);
            tugboat.SetClientAddress(string.IsNullOrWhiteSpace(address) ? LocalAddress : address.Trim());
            return multipass.StartConnection(false);
        }

        /// <summary>
        /// 호스트 SteamID로 Steam 클라이언트를 시작한다.
        /// </summary>
        public bool StartSteamClient(ulong hostSteamId)
        {
            EnsureTransports();
            if (steamTransport == null)
                return false;

            multipass.SetClientTransport(steamTransport);
            steamTransport.SetClientAddress(hostSteamId.ToString());
            return multipass.StartConnection(false);
        }

        /// <summary>
        /// 클라이언트와 모든 서버 연결을 끝낸다.
        /// </summary>
        public void StopAll()
        {
            EnsureTransports();
            if (networkManager.IsClientStarted)
                networkManager.ClientManager.StopConnection();

            foreach (Transport transport in multipass.Transports)
                multipass.StopConnection(true, transport.Index);
        }
    }
}
