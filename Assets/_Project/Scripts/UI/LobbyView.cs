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

            // 설정과 크레딧은 Phase 6에서 구현한다.
            settingsButton.interactable = false;
            creditsButton.interactable = false;
        }

        /// <summary>
        /// 세션 상태에 맞게 버튼과 안내 문구를 갱신한다.
        /// </summary>
        private void Update()
        {
            ISessionService session = SessionServices.Session;
            if (session == null)
            {
                noticeText.text = "네트워크 서비스를 찾을 수 없습니다.";
                return;
            }

            bool idle = !session.IsBusy;
            hostSteamButton.interactable = idle && session.IsSteamAvailable;
            joinSteamButton.interactable = idle && session.IsSteamAvailable;
            hostDirectButton.interactable = idle;
            joinDirectButton.interactable = idle;

            steamStatusText.text = session.IsSteamAvailable
                ? "Steam 연결됨"
                : "Steam을 사용할 수 없습니다. (Steam을 실행하면 Steam 방을 만들 수 있습니다)";
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
