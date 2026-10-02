using GuardVsMannequin.Localization;
using GuardVsMannequin.Network;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GuardVsMannequin.UI
{
    /// <summary>
    /// 로비(메인 메뉴) 화면 관리. 화면(패널)을 하나씩 보여주며 이동하고, 공통 상태 문구와 뒤로 가기(버튼·ESC)를 처리한다.
    /// 메인 ─ 방 만들기 ─ 스팀 방 만들기(암호) / 직접 방 만들기
    ///      ─ 참가     ─ 스팀 방 입장(방 코드, 암호) / IP로 참가(주소)
    ///      ─ 설정, 크레딧, 종료
    /// 각 화면의 버튼 동작은 메뉴 코드가 맡는다: <see cref="LobbyMainMenu"/>, <see cref="LobbyCreateMenu"/>, <see cref="LobbyJoinMenu"/>.
    /// 화면 요소는 Layout Group 없이 배치되어 있어 프리팹에서 위치·크기를 자유롭게 바꿀 수 있다.
    /// </summary>
    public class LobbyView : MonoBehaviour, ILobbyNavigator
    {
        [Header("화면")]
        [SerializeField] private GameObject mainMenu;
        [SerializeField] private GameObject createMenu;
        [SerializeField] private GameObject steamCreatePanel;
        [SerializeField] private GameObject joinMenu;
        [SerializeField] private GameObject steamJoinPanel;
        [SerializeField] private GameObject directJoinPanel;

        [Header("뒤로 (각 화면의 뒤로 버튼)")]
        [SerializeField] private Button[] backButtons;

        [Header("상태")]
        [SerializeField] private Text steamStatusText;
        [SerializeField] private Text noticeText;

        [Header("패널")]
        [SerializeField] private SettingsPanelView settingsPanel;
        [SerializeField] private CreditsView creditsPanel;

        private LobbyScreen currentScreen;

        /// <summary>
        /// 뒤로 버튼을 연결하고 메인 화면을 보여준다. (게임에서 돌아왔을 때 커서가 잠겨 있을 수 있어 풀어 둔다)
        /// </summary>
        private void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            foreach (Button back in backButtons)
                back.onClick.AddListener(Back);

            settingsPanel.gameObject.SetActive(false);
            creditsPanel.gameObject.SetActive(false);
            Show(LobbyScreen.Main);
        }

        /// <summary>
        /// 상태 문구를 갱신하고, ESC로 이전 화면으로 돌아간다.
        /// </summary>
        private void Update()
        {
            RefreshStatus();

            // 설정·크레딧이 열려 있으면 그쪽이 ESC를 처리한다.
            bool overlayOpen = settingsPanel.IsOpen || creditsPanel.gameObject.activeSelf;
            if (!overlayOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                Back();
        }

        /// <summary>
        /// Steam 연결 상태와 세션 안내 문구를 표시한다.
        /// </summary>
        private void RefreshStatus()
        {
            ISessionService session = SessionServices.Session;
            if (session == null)
            {
                noticeText.text = Loc.Get("lobby.noService");
                return;
            }

            steamStatusText.text = Loc.Get(session.IsSteamAvailable ? "lobby.steam.connected" : "lobby.steam.unavailable");
            noticeText.text = session.Notice;
        }

        /// <inheritdoc />
        public void Show(LobbyScreen screen)
        {
            currentScreen = screen;
            foreach (LobbyScreen candidate in new[] { LobbyScreen.Main, LobbyScreen.Create, LobbyScreen.SteamCreate, LobbyScreen.Join, LobbyScreen.SteamJoin, LobbyScreen.DirectJoin })
            {
                GameObject target = GetScreenObject(candidate);
                if (target != null)
                    target.SetActive(candidate == screen);
            }
        }

        /// <inheritdoc />
        public void Back()
        {
            Show(currentScreen switch
            {
                LobbyScreen.SteamCreate => LobbyScreen.Create,
                LobbyScreen.SteamJoin or LobbyScreen.DirectJoin => LobbyScreen.Join,
                _ => LobbyScreen.Main
            });
        }

        /// <inheritdoc />
        public void OpenSettings()
        {
            settingsPanel.Open();
        }

        /// <inheritdoc />
        public void OpenCredits()
        {
            creditsPanel.Open();
        }

        /// <summary>
        /// 화면 종류에 해당하는 패널 오브젝트를 반환한다.
        /// </summary>
        private GameObject GetScreenObject(LobbyScreen screen)
        {
            return screen switch
            {
                LobbyScreen.Main => mainMenu,
                LobbyScreen.Create => createMenu,
                LobbyScreen.SteamCreate => steamCreatePanel,
                LobbyScreen.Join => joinMenu,
                LobbyScreen.SteamJoin => steamJoinPanel,
                LobbyScreen.DirectJoin => directJoinPanel,
                _ => null
            };
        }
    }
}
