using GuardVsMannequin.Network;
using UnityEngine;
using UnityEngine.UI;

namespace GuardVsMannequin.UI
{
    /// <summary>
    /// 방 만들기 화면(스팀 방 만들기 / 직접 방 만들기)과 스팀 방 만들기 화면(암호 입력)의 버튼.
    /// </summary>
    public class LobbyCreateMenu : MonoBehaviour
    {
        [Header("방 만들기 화면")]
        [SerializeField] private Button steamCreateMenuButton;
        [SerializeField] private Button hostDirectButton;

        [Header("스팀 방 만들기 화면")]
        [SerializeField] private InputField hostPasswordInput;
        [SerializeField] private Button hostSteamButton;

        /// <summary>
        /// 버튼 동작을 연결한다. 방 생성은 세션 서비스에 맡긴다.
        /// </summary>
        private void Start()
        {
            ILobbyNavigator navigator = UiLookup.FindInParent<ILobbyNavigator>(this);
            steamCreateMenuButton.onClick.AddListener(() => navigator?.Show(LobbyScreen.SteamCreate));
            hostDirectButton.onClick.AddListener(() => SessionServices.Session?.HostDirectRoom());
            hostSteamButton.onClick.AddListener(() => SessionServices.Session?.HostSteamRoom(hostPasswordInput.text));

            hostPasswordInput.contentType = InputField.ContentType.Password;
        }

        /// <summary>
        /// Steam 연결과 진행 중인 작업 여부에 따라 버튼을 켜고 끈다.
        /// </summary>
        private void Update()
        {
            ISessionService session = SessionServices.Session;
            if (session == null)
                return;

            steamCreateMenuButton.interactable = session.CanOpenSteamMenu();
            hostSteamButton.interactable = session.CanConnectSteam();
            hostDirectButton.interactable = session.CanConnectDirect();
        }
    }
}
