using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using GuardVsMannequin.Core;
using GuardVsMannequin.Localization;
using GuardVsMannequin.Network;
using UnityEngine;

namespace GuardVsMannequin.Room
{
    /// <summary>
    /// 방 하나의 상태(참가자, 준비 여부, 게임 설정)를 서버 권한으로 관리하고 동기화한다.
    /// 전역(Global) 네트워크 오브젝트라 룸 ↔ 게임 씬을 오가도 유지되며, 서버가 멈추면 함께 사라진다.
    /// 게임 씬에는 <see cref="IRoomInfo"/>, <see cref="ISceneFlow"/>로만 노출된다.
    /// </summary>
    public class RoomState : NetworkBehaviour, IRoomInfo, ISceneFlow
    {
        private const int MaxDisplayNameLength = 24;

        [Tooltip("룸 씬 이름. (빌드 설정에 등록되어 있어야 함)")]
        [SerializeField] private string roomSceneName = "Room";

        [Tooltip("맵 ID 순서대로의 게임 씬 이름.")]
        [SerializeField] private string[] gameSceneNames = { "Game_Test", "Map_Asylum", "Map_Industrial", "Map_Hospital", "Map_AlchemistHouse" };

        private readonly SyncVar<GameSettings> settings = new();
        private readonly SyncDictionary<int, RoomMember> members = new();
        private readonly SyncVar<bool> isInGame = new();

        /// <summary>현재 방. 없으면 null. (서버와 클라이언트 모두)</summary>
        public static RoomState Instance { get; private set; }

        public GameSettings Settings => settings.Value;

        /// <summary>참가자 목록. (키: 클라이언트 ID)</summary>
        public SyncDictionary<int, RoomMember> Members => members;

        /// <summary>게임 씬에 있는지 여부. (룸 UI 표시용)</summary>
        public bool IsInGame => isInGame.Value;

        /// <summary>
        /// 서버에서 기본 설정을 적용하고, 이미 접속한 클라이언트를 참가자로 등록한다.
        /// </summary>
        public override void OnStartServer()
        {
            base.OnStartServer();
            // 방의 초기 설정은 Resources/Data/default_game_settings.json을 따른다.
            settings.Value = DefaultGameSettings.Load();

            // 접속 검증(RoomAuthenticator)을 통과한 연결만 참가자로 등록한다.
            foreach (NetworkConnection conn in ServerManager.Clients.Values)
            {
                if (conn.IsAuthenticated)
                    AddMember(conn);
            }

            ServerManager.OnAuthenticationResult += OnAuthenticationResult;
            ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
            ApplySettings(settings.Value);
        }

        /// <summary>
        /// 접속 상태 구독을 해제한다.
        /// </summary>
        public override void OnStopServer()
        {
            base.OnStopServer();
            ServerManager.OnAuthenticationResult -= OnAuthenticationResult;
            ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
        }

        /// <summary>
        /// 클라이언트에서 방을 등록하고, 서버에 내 표시 이름을 알린다.
        /// </summary>
        public override void OnStartClient()
        {
            base.OnStartClient();
            Instance = this;
            GameServices.RegisterRoom(this, this);
            SubmitNameServerRpc(LocalProfile.DisplayName);
        }

        /// <summary>
        /// 클라이언트에서 방 등록을 해제한다.
        /// </summary>
        public override void OnStopClient()
        {
            base.OnStopClient();
            if (Instance == this)
                Instance = null;
            GameServices.UnregisterRoom(this);
        }

        /// <summary>
        /// 클라이언트의 표시 이름을 반환한다.
        /// </summary>
        public string GetDisplayName(int clientId)
        {
            return members.TryGetValue(clientId, out RoomMember member) && !string.IsNullOrEmpty(member.displayName)
                ? member.displayName
                : $"Player {clientId}";
        }

        /// <summary>
        /// 게임을 시작할 수 있는지와 그 이유를 반환한다. (호스트 제외 전원 준비 + 인원 조건)
        /// </summary>
        public bool CanStartGame(out string reason)
        {
            if (!GameSettingsValidator.CanStart(members.Count))
            {
                reason = Loc.Format("room.reason.playerCount", GameConstants.MinPlayers, GameConstants.MaxPlayers, members.Count);
                return false;
            }

            foreach (RoomMember member in members.Values)
            {
                if (!member.isHost && !member.isReady)
                {
                    reason = Loc.Get("room.reason.notAllReady");
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }

        /// <summary>
        /// 설정 항목을 한 단계 조정한다. (호스트 전용, 호스트는 서버이므로 직접 호출)
        /// </summary>
        [Server]
        public void AdjustSetting(GameSettingId id, int direction)
        {
            if (isInGame.Value)
                return;
            ApplySettings(GameSettingDefinitions.Adjust(settings.Value, id, direction));
        }

        /// <summary>
        /// 설정 항목에 값을 직접 지정한다. 항목 범위와 인원 조건으로 보정된다. (호스트 전용, 키보드 입력)
        /// </summary>
        [Server]
        public void SetSettingValue(GameSettingId id, float value)
        {
            if (isInGame.Value || id == GameSettingId.Map)
                return;
            ApplySettings(GameSettingDefinitions.SetClamped(settings.Value, id, value));
        }

        /// <summary>
        /// 조건을 확인하고 모든 플레이어를 게임 씬으로 보낸다. (호스트 전용)
        /// </summary>
        [Server]
        public bool TryStartGame(out string reason)
        {
            if (isInGame.Value)
            {
                reason = Loc.Get("room.reason.inGame");
                return false;
            }
            if (!CanStartGame(out reason))
                return false;

            // 게임 중에는 새 참가를 막는다.
            SessionServices.Session?.SetRoomJoinable(false);
            isInGame.Value = true;
            LoadGlobalScene(GetGameSceneName());
            return true;
        }

        /// <summary>
        /// 게임이 끝나면 준비 상태를 초기화하고 모든 플레이어를 룸 씬으로 돌려보낸다. (서버 전용)
        /// </summary>
        [Server]
        public void ReturnToRoom()
        {
            if (!isInGame.Value)
                return;

            isInGame.Value = false;
            ResetReadyStates();
            SessionServices.Session?.SetRoomJoinable(true);
            LoadGlobalScene(roomSceneName);
        }

        /// <summary>
        /// 게스트가 준비/해제를 요청한다.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void SetReadyServerRpc(bool ready, NetworkConnection caller = null)
        {
            if (caller == null || !members.TryGetValue(caller.ClientId, out RoomMember member) || member.isHost)
                return;

            member.isReady = ready;
            members[caller.ClientId] = member;
        }

        /// <summary>
        /// 클라이언트가 자신의 표시 이름을 알린다. 비어 있으면 "Player N"을 쓴다.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        private void SubmitNameServerRpc(string displayName, NetworkConnection caller = null)
        {
            if (caller == null || !members.TryGetValue(caller.ClientId, out RoomMember member))
                return;

            string trimmed = SanitizeDisplayName(displayName);
            member.displayName = trimmed.Length > MaxDisplayNameLength ? trimmed.Substring(0, MaxDisplayNameLength) : trimmed;
            members[caller.ClientId] = member;
        }

        /// <summary>
        /// 표시 이름에서 UI 리치 텍스트 태그로 쓰일 수 있는 꺾쇠와 제어 문자를 제거한다.
        /// (이름에 &lt;size=200&gt; 같은 태그를 넣어 다른 사람의 화면을 깨뜨리는 것을 막는다)
        /// </summary>
        private static string SanitizeDisplayName(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return string.Empty;

            var builder = new System.Text.StringBuilder(displayName.Length);
            foreach (char c in displayName)
            {
                if (c == '<' || c == '>' || char.IsControl(c))
                    continue;
                builder.Append(c);
            }
            return builder.ToString().Trim();
        }

        /// <summary>
        /// 접속 검증을 통과하면 참가자로 등록하고, 인원에 맞게 설정을 다시 보정한다.
        /// </summary>
        private void OnAuthenticationResult(NetworkConnection conn, bool authenticated)
        {
            if (!authenticated)
                return;

            AddMember(conn);
            ApplySettings(settings.Value);
        }

        /// <summary>
        /// 접속이 끊기면 참가자에서 제거하고, 인원에 맞게 설정을 다시 보정한다.
        /// </summary>
        private void OnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
        {
            if (args.ConnectionState != RemoteConnectionState.Stopped)
                return;

            if (members.Remove(conn.ClientId))
                ApplySettings(settings.Value);
        }

        /// <summary>
        /// 참가자를 등록한다. 호스트 자신의 연결은 호스트로 표시한다.
        /// </summary>
        private void AddMember(NetworkConnection conn)
        {
            if (members.ContainsKey(conn.ClientId))
                return;

            members[conn.ClientId] = new RoomMember
            {
                displayName = string.Empty,
                isReady = false,
                isHost = conn.IsLocalClient
            };
        }

        /// <summary>
        /// 서버에서 호스트 표시를 확정한다.
        /// 호스트의 로컬 연결은 인증이 끝난 뒤에야 IsLocalClient로 식별되므로, 참가자 등록 시점에 놓칠 수 있다.
        /// </summary>
        private void Update()
        {
            if (!this.IsServerReady() || !ClientManager.Started)
                return;

            NetworkConnection hostConnection = ClientManager.Connection;
            if (hostConnection == null || !hostConnection.IsValid)
                return;

            if (members.TryGetValue(hostConnection.ClientId, out RoomMember host) && !host.isHost)
            {
                host.isHost = true;
                host.isReady = false;
                members[hostConnection.ClientId] = host;
            }
        }

        /// <summary>
        /// 현재 인원 기준으로 설정을 보정해 적용한다. (인원이 줄면 가드 수 등이 자동으로 줄어듦)
        /// </summary>
        private void ApplySettings(GameSettings newSettings)
        {
            settings.Value = GameSettingsValidator.Validate(newSettings, Mathf.Max(GameConstants.MinPlayers, members.Count));
        }

        /// <summary>
        /// 모든 게스트의 준비 상태를 해제한다.
        /// </summary>
        private void ResetReadyStates()
        {
            foreach (int clientId in new System.Collections.Generic.List<int>(members.Keys))
            {
                RoomMember member = members[clientId];
                member.isReady = false;
                members[clientId] = member;
            }
        }

        /// <summary>
        /// 설정된 맵 ID에 해당하는 게임 씬 이름을 반환한다.
        /// </summary>
        private string GetGameSceneName()
        {
            int index = Mathf.Clamp(settings.Value.mapId, 0, gameSceneNames.Length - 1);
            string sceneName = gameSceneNames[index];

            // 아직 만들지 않았거나 빌드 설정에 없는 맵이면 첫 번째 맵(테스트 맵)으로 대신한다.
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogWarning($"[RoomState] 맵 씬 '{sceneName}'이 빌드 설정에 없어 '{gameSceneNames[0]}'을 사용합니다. 'GuardVsMannequin/맵 생성'을 실행하세요.");
                return gameSceneNames[0];
            }
            return sceneName;
        }

        /// <summary>
        /// 모든 플레이어에게 씬을 전역으로 불러오고 이전 씬을 교체한다.
        /// </summary>
        private void LoadGlobalScene(string sceneName)
        {
            var loadData = new SceneLoadData(sceneName) { ReplaceScenes = ReplaceOption.All };
            SceneManager.LoadGlobalScenes(loadData);
        }
    }
}
