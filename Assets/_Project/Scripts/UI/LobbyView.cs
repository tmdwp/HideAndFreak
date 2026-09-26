using GuardVsMannequin.Localization;
using GuardVsMannequin.Network;
using UnityEngine;
using UnityEngine.UI;

namespace GuardVsMannequin.UI
{
    /// <summary>
    /// 로비(메인 메뉴) 화면. 방 만들기/참가와 안내 메시지를 담당하며, 연결 처리는 세션 서비스에 맡긴다.
    /// </summary>
    public class LobbyView : MonoBehaviour
    {
        [Header("Steam")]
        [SerializeField] private Button hostSteamButton;
        [SerializeField] private InputField roomCodeInput;
        [SerializeField] private Button joinSteamButton;
        [SerializeField] private Text steamStatusText;

        [Header("직접 접속 (개발/LAN)")]
        [SerializeField] private Button hostDirectButton;
        [SerializeField] private InputField addressInput;
        [SerializeField] private Button joinDirectButton;

        [Header("기타")]
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Text noticeText;

        [Header("패널")]
        [SerializeField] private SettingsPanelView settingsPanel;
        [SerializeField] private CreditsView creditsPanel;

        /// <summary>
        /// 버튼 동작을 연결하고 커서를 보이게 한다. (게임에서 돌아왔을 때 잠겨 있을 수 있음)
        /// </summary>
        private void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            hostSteamButton.onClick.AddListener(() => SessionServices.Session?.HostSteamRoom());
            joinSteamButton.onClick.AddListener(() => SessionServices.Session?.JoinSteamRoom(roomCodeInput.text));
            hostDirectButton.onClick.AddListener(() => SessionServices.Session?.HostDirectRoom());
            joinDirectButton.onClick.AddListener(() => SessionServices.Session?.JoinDirectRoom(addressInput.text));
            quitButton.onClick.AddListener(Quit);
            settingsButton.onClick.AddListener(() => settingsPanel.Open());
            creditsButton.onClick.AddListener(() => creditsPanel.Open());

            settingsPanel.gameObject.SetActive(false);
            creditsPanel.gameObject.SetActive(false);
        }

        /// <summary>
        /// 세션 상태에 맞게 버튼과 안내 문구를 갱신한다.
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
            hostSteamButton.interactable = idle && session.IsSteamAvailable;
            joinSteamButton.interactable = idle && session.IsSteamAvailable;
            hostDirectButton.interactable = idle;
            joinDirectButton.interactable = idle;

            steamStatusText.text = Loc.Get(session.IsSteamAvailable ? "lobby.steam.connected" : "lobby.steam.unavailable");
            noticeText.text = session.Notice;
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
