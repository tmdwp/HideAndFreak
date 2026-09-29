using GuardVsMannequin.Localization;
using GuardVsMannequin.Network;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GuardVsMannequin.UI
{
    /// <summary>
    /// 로비(메인 메뉴) 화면. 화면(패널)을 하나씩 보여주며 이동하고, 연결 처리는 세션 서비스에 맡긴다.
    /// 메인 ─ 방 만들기 ─ 스팀 방 만들기(암호) / 직접 방 만들기
    ///      ─ 참가     ─ 스팀 방 입장(방 코드, 암호) / IP로 참가(주소)
    ///      ─ 설정, 크레딧, 종료
    /// 각 화면의 "뒤로" 버튼이나 ESC로 이전 화면으로 돌아간다.
    /// 화면 요소는 Layout Group 없이 배치되어 있어 프리팹에서 위치·크기를 자유롭게 바꿀 수 있다.
    /// </summary>
    public class LobbyView : MonoBehaviour
    {
        [Header("화면")]
        [SerializeField] private GameObject mainMenu;
        [SerializeField] private GameObject createMenu;
        [SerializeField] private GameObject steamCreatePanel;
        [SerializeField] private GameObject joinMenu;
        [SerializeField] private GameObject steamJoinPanel;
        [SerializeField] private GameObject directJoinPanel;

        [Header("메인")]
        [SerializeField] private Button createMenuButton;
        [SerializeField] private Button joinMenuButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button quitButton;

        [Header("방 만들기")]
        [SerializeField] private Button steamCreateMenuButton;
        [SerializeField] private Button hostDirectButton;
        [SerializeField] private InputField hostPasswordInput;
        [SerializeField] private Button hostSteamButton;

        [Header("참가")]
        [SerializeField] private Button steamJoinMenuButton;
        [SerializeField] private Button directJoinMenuButton;
        [SerializeField] private InputField roomCodeInput;
        [SerializeField] private InputField joinPasswordInput;
        [SerializeField] private Button joinSteamButton;
        [SerializeField] private InputField addressInput;
        [SerializeField] private Button joinDirectButton;

        [Header("뒤로 (각 화면의 뒤로 버튼)")]
        [SerializeField] private Button[] backButtons;

        [Header("상태")]
        [SerializeField] private Text steamStatusText;
        [SerializeField] private Text noticeText;

        [Header("패널")]
        [SerializeField] private SettingsPanelView settingsPanel;
        [SerializeField] private CreditsView creditsPanel;

        private GameObject currentScreen;

        /// <summary>
        /// 버튼 동작을 연결하고 메인 화면을 보여준다. (게임에서 돌아왔을 때 커서가 잠겨 있을 수 있어 풀어 둔다)
        /// </summary>
        private void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            createMenuButton.onClick.AddListener(() => Show(createMenu));
            joinMenuButton.onClick.AddListener(() => Show(joinMenu));
            settingsButton.onClick.AddListener(() => settingsPanel.Open());
            creditsButton.onClick.AddListener(() => creditsPanel.Open());
            quitButton.onClick.AddListener(Quit);

            steamCreateMenuButton.onClick.AddListener(() => Show(steamCreatePanel));
            hostDirectButton.onClick.AddListener(() => SessionServices.Session?.HostDirectRoom());
            hostSteamButton.onClick.AddListener(() => SessionServices.Session?.HostSteamRoom(hostPasswordInput.text));

            steamJoinMenuButton.onClick.AddListener(() => Show(steamJoinPanel));
            directJoinMenuButton.onClick.AddListener(() => Show(directJoinPanel));
            joinSteamButton.onClick.AddListener(() => SessionServices.Session?.JoinSteamRoom(roomCodeInput.text, joinPasswordInput.text));
            joinDirectButton.onClick.AddListener(() => SessionServices.Session?.JoinDirectRoom(addressInput.text));

            foreach (Button back in backButtons)
                back.onClick.AddListener(Back);

            hostPasswordInput.contentType = InputField.ContentType.Password;
            joinPasswordInput.contentType = InputField.ContentType.Password;

            settingsPanel.gameObject.SetActive(false);
            creditsPanel.gameObject.SetActive(false);
            Show(mainMenu);

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
        /// 세션 상태에 맞게 버튼과 안내 문구를 갱신하고, ESC로 이전 화면으로 돌아간다.
        /// </summary>
        private void Update()
        {
            ISessionService session = SessionServices.Session;
            if (session == null)
            {
                noticeText.text = Loc.Get("lobby.noService");
                return;
            }

            bool idle = !session.IsBusy;
            steamCreateMenuButton.interactable = session.IsSteamAvailable;
            steamJoinMenuButton.interactable = session.IsSteamAvailable;
            hostSteamButton.interactable = idle && session.IsSteamAvailable;
            joinSteamButton.interactable = idle && session.IsSteamAvailable;
            hostDirectButton.interactable = idle;
            joinDirectButton.interactable = idle;

            steamStatusText.text = Loc.Get(session.IsSteamAvailable ? "lobby.steam.connected" : "lobby.steam.unavailable");
            noticeText.text = session.Notice;

            // 설정·크레딧이 열려 있으면 그쪽이 ESC를 처리한다.
            bool overlayOpen = settingsPanel.IsOpen || creditsPanel.gameObject.activeSelf;
            if (!overlayOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                Back();
        }

        /// <summary>
        /// 화면 하나만 보이게 한다.
        /// </summary>
        private void Show(GameObject screen)
        {
            currentScreen = screen;
            foreach (GameObject candidate in new[] { mainMenu, createMenu, steamCreatePanel, joinMenu, steamJoinPanel, directJoinPanel })
            {
                if (candidate != null)
                    candidate.SetActive(candidate == screen);
            }
        }

        /// <summary>
        /// 이전 화면으로 돌아간다. (세부 화면 → 방 만들기/참가 → 메인)
        /// </summary>
        private void Back()
        {
            if (currentScreen == steamCreatePanel)
                Show(createMenu);
            else if (currentScreen == steamJoinPanel || currentScreen == directJoinPanel)
                Show(joinMenu);
            else
                Show(mainMenu);
        }

        /// <summary>
        /// 초대로 들어가려는 방에 암호가 필요하면, 방 코드를 채운 스팀 방 입장 화면을 열어 암호를 받는다.
        /// </summary>
        private void OnPasswordRequired(string roomCode)
        {
            Show(steamJoinPanel);
            roomCodeInput.text = roomCode;
            joinPasswordInput.text = string.Empty;
            joinPasswordInput.Select();
            joinPasswordInput.ActivateInputField();
        }

        /// <summary>
        /// 게임을 종료한다. (에디터에서는 플레이 모드를 끝낸다)
        /// </summary>
        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
