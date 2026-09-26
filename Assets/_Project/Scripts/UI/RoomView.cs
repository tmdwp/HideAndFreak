using System.Collections.Generic;
using System.Text;
using FishNet;
using GuardVsMannequin.Core;
using GuardVsMannequin.Localization;
using GuardVsMannequin.Network;
using GuardVsMannequin.Room;
using UnityEngine;
using UnityEngine.UI;

namespace GuardVsMannequin.UI
{
    /// <summary>
    /// 룸(대기방) 화면.
    /// - 참가자 목록과 준비 상태, 방 코드(복사), 친구 초대
    /// - 게임 설정: 모두에게 보이고 호스트만 조정
    /// - 호스트: 시작 버튼 (전원 준비 + 인원 조건 충족 시 활성화) / 게스트: 준비 ↔ 준비 해제
    /// </summary>
    public class RoomView : MonoBehaviour
    {
        [Header("참가자")]
        [SerializeField] private Text memberListText;

        [Header("방 코드")]
        [SerializeField] private Text roomCodeText;
        [SerializeField] private Button copyCodeButton;
        [SerializeField] private Button inviteButton;

        [Header("게임 설정")]
        [SerializeField] private Text settingsHintText;
        [SerializeField] private SettingRowView[] settingRows;

        [Header("하단")]
        [SerializeField] private Button primaryButton;
        [SerializeField] private Text primaryButtonLabel;
        [SerializeField] private Button leaveButton;
        [SerializeField] private Text statusText;

        private readonly StringBuilder builder = new();
        private readonly List<KeyValuePair<int, RoomMember>> sortedMembers = new();

        /// <summary>로컬 플레이어가 호스트(서버)인지 여부.</summary>
        private static bool IsHost => InstanceFinder.IsServerStarted;

        /// <summary>
        /// 버튼 동작을 연결하고 커서를 보이게 한다.
        /// </summary>
        private void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            foreach (SettingRowView row in settingRows)
                row.Bind(OnAdjustSetting);

            primaryButton.onClick.AddListener(OnPrimaryClicked);
            leaveButton.onClick.AddListener(() => SessionServices.Session?.Leave());
            copyCodeButton.onClick.AddListener(CopyRoomCode);
            inviteButton.onClick.AddListener(() => SessionServices.Session?.OpenInviteDialog());
        }

        /// <summary>
        /// 방 상태를 화면에 반영한다.
        /// </summary>
        private void Update()
        {
            RoomState room = RoomState.Instance;
            if (room == null)
            {
                statusText.text = Loc.Get("room.status.loading");
                primaryButton.interactable = false;
                return;
            }

            RefreshMembers(room);
            RefreshRoomCode();
            RefreshSettings(room);
            RefreshPrimaryButton(room);
        }

        /// <summary>
        /// 참가자 목록을 호스트 먼저, 접속 순서대로 표시한다.
        /// </summary>
        private void RefreshMembers(RoomState room)
        {
            sortedMembers.Clear();
            sortedMembers.AddRange(room.Members);
            sortedMembers.Sort((a, b) => a.Value.isHost != b.Value.isHost ? (a.Value.isHost ? -1 : 1) : a.Key.CompareTo(b.Key));

            builder.Clear();
            builder.AppendLine(Loc.Format("room.members.count", sortedMembers.Count, GameConstants.MaxPlayers));
            foreach (KeyValuePair<int, RoomMember> pair in sortedMembers)
            {
                string name = room.GetDisplayName(pair.Key);
                string state = Loc.Get(pair.Value.isHost ? "room.member.host" : (pair.Value.isReady ? "room.member.ready" : "room.member.notReady"));
                builder.AppendLine($"{name}  —  {state}");
            }
            memberListText.text = builder.ToString();
        }

        /// <summary>
        /// 방 코드와 초대 버튼 상태를 갱신한다.
        /// </summary>
        private void RefreshRoomCode()
        {
            ISessionService session = SessionServices.Session;
            bool isSteamRoom = session != null && session.IsSteamRoom;

            roomCodeText.text = session != null ? Loc.Format("room.code", session.RoomCode) : string.Empty;
            copyCodeButton.interactable = isSteamRoom;
            inviteButton.interactable = isSteamRoom;
        }

        /// <summary>
        /// 설정 값을 표시하고 호스트에게만 조정 버튼을 켠다.
        /// </summary>
        private void RefreshSettings(RoomState room)
        {
            settingsHintText.text = Loc.Get(IsHost ? "room.settings.host" : "room.settings.guest");
            foreach (SettingRowView row in settingRows)
                row.Refresh(room.Settings, IsHost);
        }

        /// <summary>
        /// 호스트는 시작 버튼, 게스트는 준비 버튼으로 표시한다.
        /// </summary>
        private void RefreshPrimaryButton(RoomState room)
        {
            if (IsHost)
            {
                bool canStart = room.CanStartGame(out string reason);
                primaryButtonLabel.text = Loc.Get("room.start");
                primaryButton.interactable = canStart;
                statusText.text = canStart ? Loc.Get("room.status.allReady") : reason;
                return;
            }

            bool isReady = TryGetLocalMember(room, out RoomMember member) && member.isReady;
            primaryButtonLabel.text = Loc.Get(isReady ? "room.unready" : "room.ready");
            primaryButton.interactable = true;
            statusText.text = Loc.Get(isReady ? "room.status.waitingHost" : "room.status.pressReady");
        }

        /// <summary>
        /// 호스트는 게임을 시작하고, 게스트는 준비 상태를 바꾼다.
        /// </summary>
        private void OnPrimaryClicked()
        {
            RoomState room = RoomState.Instance;
            if (room == null)
                return;

            if (IsHost)
            {
                if (!room.TryStartGame(out string reason))
                    statusText.text = reason;
                return;
            }

            bool isReady = TryGetLocalMember(room, out RoomMember member) && member.isReady;
            room.SetReadyServerRpc(!isReady);
        }

        /// <summary>
        /// 호스트가 설정을 조정한다. (호스트는 서버이므로 직접 호출)
        /// </summary>
        private void OnAdjustSetting(GameSettingId id, int direction)
        {
            if (IsHost && RoomState.Instance != null)
                RoomState.Instance.AdjustSetting(id, direction);
        }

        /// <summary>
        /// 방 코드를 클립보드에 복사한다.
        /// </summary>
        private void CopyRoomCode()
        {
            ISessionService session = SessionServices.Session;
            if (session == null || !session.IsSteamRoom)
                return;

            GUIUtility.systemCopyBuffer = session.RoomCode;
            statusText.text = Loc.Get("room.status.codeCopied");
        }

        /// <summary>
        /// 로컬 플레이어의 참가자 정보를 찾는다.
        /// </summary>
        private static bool TryGetLocalMember(RoomState room, out RoomMember member)
        {
            int localId = InstanceFinder.ClientManager.Connection.ClientId;
            return room.Members.TryGetValue(localId, out member);
        }
    }
}
