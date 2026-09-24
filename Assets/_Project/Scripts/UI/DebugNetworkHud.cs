using GuardVsMannequin.Core;
using GuardVsMannequin.Network;
using GuardVsMannequin.Player;
using UnityEngine;

namespace GuardVsMannequin.UI
{
    /// <summary>
    /// Phase 0 테스트용 임시 HUD. (IMGUI)
    /// 호스트/참가/종료, 게임 시작, 현재 상태와 남은 시간을 보여준다.
    /// Phase 1(HUD)과 Phase 5(로비/룸 씬)에서 정식 UI로 대체한다.
    /// </summary>
    public class DebugNetworkHud : MonoBehaviour
    {
        [Tooltip("IConnectionService를 구현한 컴포넌트.")]
        [SerializeField] private MonoBehaviour connectionServiceComponent;
        [SerializeField] private GameStateMachine stateMachine;

        private IConnectionService connectionService;
        private string address = "localhost";
        private string message = string.Empty;

        /// <summary>
        /// 지정된 컴포넌트를 접속 서비스 인터페이스로 캐스팅한다.
        /// </summary>
        private void Awake()
        {
            connectionService = connectionServiceComponent as IConnectionService;
            if (connectionService == null)
                Debug.LogError("[DebugNetworkHud] connectionServiceComponent가 IConnectionService를 구현하지 않습니다.");
        }

        /// <summary>
        /// 접속 상태에 따라 알맞은 패널을 그린다.
        /// </summary>
        private void OnGUI()
        {
            if (connectionService == null)
                return;

            GUILayout.BeginArea(new Rect(10f, 10f, 280f, 400f), GUI.skin.box);

            if (!connectionService.IsServerRunning && !connectionService.IsClientRunning)
                DrawConnectPanel();
            else
                DrawSessionPanel();

            if (!string.IsNullOrEmpty(message))
                GUILayout.Label(message);

            GUILayout.EndArea();
        }

        /// <summary>
        /// 호스트 시작, 주소 입력 후 참가 버튼을 그린다.
        /// </summary>
        private void DrawConnectPanel()
        {
            if (GUILayout.Button("호스트 시작"))
            {
                message = string.Empty;
                connectionService.StartHost();
            }

            GUILayout.Space(8f);
            GUILayout.Label("호스트 주소");
            address = GUILayout.TextField(address);
            if (GUILayout.Button("참가"))
            {
                message = string.Empty;
                connectionService.StartClient(address);
            }
        }

        /// <summary>
        /// 현재 상태, 남은 시간, 역할과 호스트 전용 시작 버튼을 그린다.
        /// </summary>
        private void DrawSessionPanel()
        {
            GUILayout.Label(connectionService.IsServerRunning ? "[호스트]" : "[게스트]");

            // 씬 네트워크 오브젝트가 아직 스폰되지 않았다면(접속 중이거나 씬 ID 누락) 상태를 표시하지 않는다.
            if (!stateMachine.IsServerReady() && !stateMachine.IsClientReady())
            {
                GUILayout.Label("GameSystems 초기화 대기 중...\n(계속되면 씬 ID 재생성 메뉴 실행)");
                DrawDisconnectButton();
                return;
            }

            GUILayout.Label($"상태: {stateMachine.CurrentState}");

            if (stateMachine.RemainingTime > 0f)
                GUILayout.Label($"남은 시간: {stateMachine.RemainingTime:0.0}초");

            PlayerIdentity localPlayer = LocalPlayerEvents.Current;
            if (localPlayer != null)
                GUILayout.Label($"내 역할: {localPlayer.Role} ({localPlayer.DisplayName})");

            if (connectionService.IsServerRunning && stateMachine.CurrentState == GameState.Waiting)
            {
                if (GUILayout.Button("게임 시작"))
                    message = stateMachine.TryStartMatch(out string failReason) ? string.Empty : failReason;
            }

            DrawDisconnectButton();
            GUILayout.Label("ESC: 커서 잠금 전환");
        }

        /// <summary>
        /// 접속 종료 버튼을 그린다.
        /// </summary>
        private void DrawDisconnectButton()
        {
            GUILayout.Space(8f);
            if (GUILayout.Button("접속 종료"))
                connectionService.Stop();
        }
    }
}
