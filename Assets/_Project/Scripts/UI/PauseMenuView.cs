using GuardVsMannequin.Core;
using GuardVsMannequin.Network;
using GuardVsMannequin.Player;
using GuardVsMannequin.Settings;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GuardVsMannequin.UI
{
    /// <summary>
    /// ESC 메뉴(계속하기 / 설정 / 방 나가기). 룸과 게임 씬에서 사용한다.
    /// 열려 있는 동안 커서를 풀어 게임 입력을 막고, 닫으면 게임 중이면 다시 잠근다.
    /// 설정 화면이 열려 있을 때 ESC를 누르면 설정 화면만 닫는다.
    /// </summary>
    public class PauseMenuView : MonoBehaviour
    {
        private const string MenuActionName = "Menu";

        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button leaveButton;
        [SerializeField] private SettingsPanelView settingsPanel;

        [Tooltip("메뉴를 닫을 때 커서를 다시 잠글지 여부. (게임 씬: 켬, 룸 씬: 끔)")]
        [SerializeField] private bool lockCursorOnClose = true;

        private InputActionAsset runtimeActions;
        private InputAction menuAction;

        /// <summary>
        /// 메뉴 키 입력과 버튼을 준비하고 메뉴를 닫아 둔다.
        /// </summary>
        private void Awake()
        {
            runtimeActions = Instantiate(inputActions);
            OptionsStore.ApplyBindings(runtimeActions);
            menuAction = runtimeActions.FindAction(MenuActionName, true);

            resumeButton.onClick.AddListener(CloseMenu);
            settingsButton.onClick.AddListener(() => settingsPanel.Open());
            leaveButton.onClick.AddListener(() => SessionServices.Session?.Leave());

            menuPanel.SetActive(false);
            settingsPanel.gameObject.SetActive(false);
        }

        /// <summary>
        /// 메뉴 키를 켠다.
        /// </summary>
        private void OnEnable()
        {
            menuAction.performed += OnMenuPressed;
            menuAction.Enable();
        }

        /// <summary>
        /// 메뉴 키를 끈다.
        /// </summary>
        private void OnDisable()
        {
            menuAction.performed -= OnMenuPressed;
            menuAction.Disable();
        }

        /// <summary>
        /// 복제한 입력 에셋을 해제한다.
        /// </summary>
        private void OnDestroy()
        {
            if (runtimeActions != null)
                Destroy(runtimeActions);
        }

        /// <summary>
        /// ESC: 키 설정 중이면 무시, 설정 화면이 열려 있으면 설정만 닫고, 아니면 메뉴를 열고 닫는다.
        /// </summary>
        private void OnMenuPressed(InputAction.CallbackContext context)
        {
            if (RebindRowView.IsAnyRebinding)
                return;

            if (settingsPanel.IsOpen)
                settingsPanel.Close();
            else if (menuPanel.activeSelf)
                CloseMenu();
            else
                OpenMenu();
        }

        /// <summary>
        /// 메뉴를 열고 커서를 푼다.
        /// </summary>
        private void OpenMenu()
        {
            menuPanel.SetActive(true);
            GameCursor.SetLocked(false);
        }

        /// <summary>
        /// 메뉴를 닫는다. 게임 중(내 아바타가 있고 결과 화면이 아님)이면 커서를 다시 잠근다.
        /// </summary>
        private void CloseMenu()
        {
            menuPanel.SetActive(false);
            settingsPanel.gameObject.SetActive(false);

            bool isPlaying = LocalPlayerEvents.Current != null && GameServices.CurrentStateOrWaiting != GameState.Ended;
            if (lockCursorOnClose && isPlaying)
                GameCursor.SetLocked(true);
        }
    }
}
