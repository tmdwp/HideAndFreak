using FishNet.Transporting;
using GuardVsMannequin.Localization;
using UnityEngine;

namespace GuardVsMannequin.Network
{
    /// <summary>
    /// Steam 로비와 트랜스포트 연결을 묶어 "방"을 만든다/참가한다/나간다.
    /// - Steam 방 만들기: 로비 생성 → 서버(Tugboat + Steam) 시작 → 로컬 클라이언트 접속
    /// - Steam 방 참가: 로비 입장 → 호스트 SteamID로 Steam 클라이언트 접속
    /// - 직접 방: Tugboat만 사용 (한 PC에서 여러 개를 띄워 테스트할 때)
    /// 서버가 시작되면 FishNet DefaultScene이 룸 씬을, 연결이 끊기면 로비 씬을 불러온다.
    /// </summary>
    public class SessionService : MonoBehaviour, ISessionService
    {
        [SerializeField] private TransportConnector connector;
        [SerializeField] private SteamLobbyService steamLobby;

        private bool isLeaving;
        private bool isRegistered;

        // 안내 문구는 key와 끼워 넣을 값으로 보관하고, 읽을 때 현재 언어로 만든다. (언어를 바꾸면 바로 반영)
        private string noticeKey = string.Empty;
        private string noticeDetail = string.Empty;

        public bool IsSteamAvailable => SteamBootstrap.IsInitialized && connector.HasSteamTransport;
        public bool IsBusy { get; private set; }
        public bool IsSteamRoom { get; private set; }
        public string Notice => string.IsNullOrEmpty(noticeKey) ? string.Empty : Loc.Format(noticeKey, noticeDetail);

        public string RoomCode => IsSteamRoom && steamLobby.CurrentLobbyId != 0
            ? steamLobby.CurrentLobbyId.ToString()
            : Loc.Get("session.directRoomCode");

        /// <summary>
        /// 첫 인스턴스만 서비스로 등록하고 이벤트를 연결한다. (로비 씬 재로딩 시 생기는 중복은 무시)
        /// </summary>
        private void Awake()
        {
            isRegistered = SessionServices.TryRegister(this);
            if (!isRegistered)
                return;

            steamLobby.LobbyCreated += OnLobbyCreated;
            steamLobby.JoinedLobby += OnJoinedLobby;
            steamLobby.JoinRequested += OnJoinRequested;
            steamLobby.Failed += OnLobbyFailed;
        }

        /// <summary>
        /// 클라이언트 연결 상태를 구독한다. (NetworkManager의 하위 매니저는 NetworkManager.Awake 이후에 준비됨)
        /// </summary>
        private void Start()
        {
            if (isRegistered)
                connector.NetworkManager.ClientManager.OnClientConnectionState += OnClientConnectionState;
        }

        /// <summary>
        /// 등록된 인스턴스라면 이벤트 연결을 해제한다.
        /// </summary>
        private void OnDestroy()
        {
            if (!isRegistered)
                return;

            SessionServices.Unregister(this);
            steamLobby.LobbyCreated -= OnLobbyCreated;
            steamLobby.JoinedLobby -= OnJoinedLobby;
            steamLobby.JoinRequested -= OnJoinRequested;
            steamLobby.Failed -= OnLobbyFailed;
            if (connector != null && connector.NetworkManager != null)
                connector.NetworkManager.ClientManager.OnClientConnectionState -= OnClientConnectionState;
        }

        /// <summary>
        /// Steam 로비를 먼저 만들고, 생성되면 서버를 시작한다.
        /// </summary>
        public void HostSteamRoom()
        {
            if (!CanStartNewSession())
                return;

            if (!IsSteamAvailable)
            {
                SetNotice("notice.steamUnavailable");
                return;
            }

            IsBusy = true;
            SetNotice("notice.creatingRoom");
            steamLobby.CreateLobby();
        }

        /// <summary>
        /// 방 코드(로비 ID)로 로비에 들어가고, 입장하면 호스트에 접속한다.
        /// </summary>
        public void JoinSteamRoom(string roomCode)
        {
            if (!CanStartNewSession())
                return;

            if (!IsSteamAvailable)
            {
                SetNotice("notice.steamUnavailable");
                return;
            }

            if (!ulong.TryParse(roomCode?.Trim(), out ulong lobbyId))
            {
                SetNotice("notice.invalidCode");
                return;
            }

            IsBusy = true;
            SetNotice("notice.joiningRoom");
            steamLobby.JoinLobby(lobbyId);
        }

        /// <summary>
        /// Tugboat만으로 방을 만든다.
        /// </summary>
        public void HostDirectRoom()
        {
            if (!CanStartNewSession())
                return;

            IsSteamRoom = false;
            if (connector.StartServer(withSteam: false))
                connector.StartLocalClient();
            else
                SetNotice("notice.serverStartFailedPort");
        }

        /// <summary>
        /// IP 주소로 직접 접속 방에 참가한다.
        /// </summary>
        public void JoinDirectRoom(string address)
        {
            if (!CanStartNewSession())
                return;

            IsSteamRoom = false;
            IsBusy = true;
            SetNotice("notice.joiningRoom");
            connector.StartDirectClient(address);
        }

        /// <summary>
        /// Steam 친구 초대 창을 연다.
        /// </summary>
        public void OpenInviteDialog()
        {
            if (IsSteamRoom)
                steamLobby.OpenInviteDialog();
        }

        /// <summary>
        /// Steam 로비의 참가 가능 여부를 바꾼다.
        /// </summary>
        public void SetRoomJoinable(bool joinable)
        {
            if (IsSteamRoom)
                steamLobby.SetJoinable(joinable);
        }

        /// <summary>
        /// 연결과 로비를 모두 정리한다. 로비 씬으로의 이동은 DefaultScene이 처리한다.
        /// </summary>
        public void Leave()
        {
            isLeaving = true;
            connector.StopAll();
            steamLobby.LeaveLobby();
            IsSteamRoom = false;
            IsBusy = false;
        }

        /// <summary>
        /// 안내 메시지를 지운다.
        /// </summary>
        public void ClearNotice()
        {
            SetNotice(string.Empty);
        }

        /// <summary>
        /// 안내 문구를 key(strings.csv)와 끼워 넣을 세부 정보로 정한다. 빈 key는 문구 없음.
        /// </summary>
        private void SetNotice(string key, string detail = "")
        {
            noticeKey = key ?? string.Empty;
            noticeDetail = detail ?? string.Empty;
        }

        /// <summary>
        /// 이미 방에 있거나 연결 중이면 새 작업을 막는다.
        /// </summary>
        private bool CanStartNewSession()
        {
            if (IsBusy || connector.IsServerRunning || connector.IsClientRunning)
            {
                SetNotice("notice.alreadyInRoom");
                return false;
            }

            isLeaving = false;
            SetNotice(string.Empty);
            return true;
        }

        /// <summary>
        /// 로비가 만들어지면 서버와 로컬 클라이언트를 시작한다.
        /// </summary>
        private void OnLobbyCreated(ulong lobbyId)
        {
            IsSteamRoom = true;
            if (connector.StartServer(withSteam: true))
            {
                connector.StartLocalClient();
                return;
            }

            IsBusy = false;
            SetNotice("notice.serverStartFailed");
            steamLobby.LeaveLobby();
        }

        /// <summary>
        /// 다른 사람의 로비에 들어가면 호스트에 Steam으로 접속한다.
        /// </summary>
        private void OnJoinedLobby(ulong hostSteamId)
        {
            IsSteamRoom = true;
            if (!connector.StartSteamClient(hostSteamId))
            {
                IsBusy = false;
                SetNotice("notice.hostConnectFailed");
                steamLobby.LeaveLobby();
            }
        }

        /// <summary>
        /// 친구 초대 수락 등으로 참가 요청이 오면, 다른 방에 있지 않을 때만 참가한다.
        /// </summary>
        private void OnJoinRequested(ulong lobbyId)
        {
            if (connector.IsClientRunning || connector.IsServerRunning)
            {
                SetNotice("notice.inviteWhileInRoom");
                return;
            }

            JoinSteamRoom(lobbyId.ToString());
        }

        /// <summary>
        /// 로비 작업 실패를 안내한다.
        /// </summary>
        private void OnLobbyFailed(string reasonKey, string detail)
        {
            IsBusy = false;
            SetNotice(reasonKey, detail);
        }

        /// <summary>
        /// 접속 성공 시 작업을 끝내고, 스스로 나가지 않았는데 끊기면 안내 메시지를 남긴다.
        /// (호스트가 방을 닫으면 게스트는 이 경로로 로비에 돌아간다)
        /// </summary>
        private void OnClientConnectionState(ClientConnectionStateArgs args)
        {
            if (args.ConnectionState == LocalConnectionState.Started)
            {
                IsBusy = false;
                SetNotice(string.Empty);
                return;
            }

            if (args.ConnectionState != LocalConnectionState.Stopped)
                return;

            bool wasConnecting = IsBusy;
            IsBusy = false;
            steamLobby.LeaveLobby();

            if (!isLeaving)
                SetNotice(wasConnecting ? "notice.joinFailed" : "notice.hostDisconnected");

            // 호스트가 아닌 경우에만 여기서 정리한다. (호스트는 Leave에서 서버까지 정리)
            if (!connector.IsServerRunning)
                IsSteamRoom = false;
            isLeaving = false;
        }
    }
}
