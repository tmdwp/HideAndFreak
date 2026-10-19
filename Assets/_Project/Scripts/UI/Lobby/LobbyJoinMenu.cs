using GuardVsMannequin.Network;
using UnityEngine;
using UnityEngine.UI;

namespace GuardVsMannequin.UI
{
    /// <summary>
    /// 참가 화면(스팀 방 입장 / IP로 참가)과 스팀 방 입장 화면(방 코드·암호), IP로 참가 화면(주소)의 버튼.
    /// 초대로 들어가려는 방에 암호가 필요하면 스팀 방 입장 화면을 열어 암호를 받는다.
    /// </summary>
    public class LobbyJoinMenu : MonoBehaviour
    {
        [Header("참가 화면")]
        [SerializeField] private Button steamJoinMenuButton;
        [SerializeField] private Button directJoinMenuButton;

        [Header("스팀 방 입장 화면")]
        [SerializeField] private InputField roomCodeInput;
        [SerializeField] private InputField joinPasswordInput;
        [SerializeField] private Button joinSteamButton;

        [Header("IP로 참가 화면")]
        [SerializeField] private InputField addressInput;
        [SerializeField] private Button joinDirectButton;

        private ILobbyNavigator navigator;

        /// <summary>
        /// 버튼 동작을 연결하고 암호 요청 이벤트를 구독한다. 접속은 세션 서비스에 맡긴다.
        /// </summary>
        private void Start()
        {
            navigator = UiLookup.FindInParent<ILobbyNavigator>(this);
            steamJoinMenuButton.onClick.AddListener(() => navigator?.Show(LobbyScreen.SteamJoin));
            directJoinMenuButton.onClick.AddListener(() => navigator?.Show(LobbyScreen.DirectJoin));
            joinSteamButton.onClick.AddListener(() => SessionServices.Session?.JoinSteamRoom(roomCodeInput.text, joinPasswordInput.text));
            joinDirectButton.onClick.AddListener(() => SessionServices.Session?.JoinDirectRoom(addressInput.text));

            joinPasswordInput.contentType = InputField.ContentType.Password;

            if (SessionServices.Session != null)
                SessionServices.Session.PasswordRequired += OnPasswordRequired;
        }

        /// <summary>
        /// 이벤트 구독을 해제한다.
        /// </summary>
        private void OnDestroy()
        {
            if (SessionServices.Session != null)
                SessionServices.Session.PasswordRequired -= OnPasswordRequired;
        }

        /// <summary>
        /// Steam 연결과 진행 중인 작업 여부에 따라 버튼을 켜고 끈다.
        /// </summary>
        private void Update()
        {
            ISessionService session = SessionServices.Session;
            if (session == null)
                return;

            steamJoinMenuButton.interactable = session.CanOpenSteamMenu();
            joinSteamButton.interactable = session.CanConnectSteam();
            joinDirectButton.interactable = session.CanConnectDirect();
        }

        /// <summary>
        /// 방 코드를 채운 스팀 방 입장 화면을 열고 암호 입력칸에 초점을 둔다.
        /// </summary>
        private void OnPasswordRequired(string roomCode)
        {
            navigator?.Show(LobbyScreen.SteamJoin);
            roomCodeInput.text = roomCode;
            joinPasswordInput.text = string.Empty;
            joinPasswordInput.Select();
            joinPasswordInput.ActivateInputField();
        }
    }
}
