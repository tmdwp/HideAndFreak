using UnityEngine;
using UnityEngine.UI;

namespace GuardVsMannequin.UI
{
    /// <summary>
    /// 로비 메인 화면의 버튼: 방 만들기 / 참가 / 설정 / 크레딧 / 종료.
    /// </summary>
    public class LobbyMainMenu : MonoBehaviour
    {
        [SerializeField] private Button createMenuButton;
        [SerializeField] private Button joinMenuButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button quitButton;

        /// <summary>
        /// 버튼 동작을 연결한다. 화면 이동은 같은 캔버스의 <see cref="ILobbyNavigator"/>에 맡긴다.
        /// </summary>
        private void Start()
        {
            ILobbyNavigator navigator = GetComponentInParent<ILobbyNavigator>();
            createMenuButton.onClick.AddListener(() => navigator.Show(LobbyScreen.Create));
            joinMenuButton.onClick.AddListener(() => navigator.Show(LobbyScreen.Join));
            settingsButton.onClick.AddListener(navigator.OpenSettings);
            creditsButton.onClick.AddListener(navigator.OpenCredits);
            quitButton.onClick.AddListener(Quit);
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
