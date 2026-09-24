using GuardVsMannequin.Core;
using GuardVsMannequin.Objectives;
using GuardVsMannequin.Player;
using UnityEngine;

namespace GuardVsMannequin.UI
{
    /// <summary>
    /// Phase 1 최소 HUD. (IMGUI 임시 구현, 정식 UI는 마무리 단계에서 교체)
    /// 남은 시간, 남은 마네킹 수, 달성/목표 횟수, 역할별 목표, 내 체력, 도구 쿨타임, 결과를 표시한다.
    /// 다른 플레이어의 체력과 킬 로그는 기획에 따라 표시하지 않는다.
    /// </summary>
    public class GameHud : MonoBehaviour
    {
        [SerializeField] private GameStateMachine stateMachine;
        [SerializeField] private PlayerRegistry playerRegistry;
        [SerializeField] private ObjectiveTracker objectiveTracker;
        [SerializeField] private MatchResult matchResult;

        private PlayerIdentity localPlayer;
        private PlayerHealth localHealth;
        private ITool[] localTools = new ITool[0];
        private PlayerRole lastLocalRole = PlayerRole.None;

        private GUIStyle centerStyle;
        private GUIStyle bigCenterStyle;

        /// <summary>
        /// 로컬 플레이어 생성/제거 이벤트를 구독한다.
        /// </summary>
        private void Awake()
        {
            LocalPlayerEvents.Spawned += OnLocalPlayerSpawned;
            LocalPlayerEvents.Despawned += OnLocalPlayerDespawned;
        }

        /// <summary>
        /// 이벤트 구독을 해제한다.
        /// </summary>
        private void OnDestroy()
        {
            LocalPlayerEvents.Spawned -= OnLocalPlayerSpawned;
            LocalPlayerEvents.Despawned -= OnLocalPlayerDespawned;
        }

        /// <summary>
        /// 로컬 아바타의 체력과 도구를 캐시한다.
        /// </summary>
        private void OnLocalPlayerSpawned(PlayerIdentity identity)
        {
            localPlayer = identity;
            localHealth = identity.GetComponent<PlayerHealth>();
            localTools = identity.GetComponents<ITool>();
            lastLocalRole = identity.Role;
        }

        /// <summary>
        /// 로컬 아바타 참조를 비운다. 역할은 사망 후 안내를 위해 유지한다.
        /// </summary>
        private void OnLocalPlayerDespawned(PlayerIdentity identity)
        {
            if (localPlayer != identity)
                return;

            localPlayer = null;
            localHealth = null;
            localTools = new ITool[0];
        }

        /// <summary>
        /// 게임이 진행 중일 때만 HUD를 그린다.
        /// </summary>
        private void OnGUI()
        {
            // 네트워크 초기화 전(Play 직후, 접속 전)에는 FishNet 상태를 읽을 수 없다.
            if (!stateMachine.IsClientReady())
                return;

            GameState state = stateMachine.CurrentState;
            if (state == GameState.Waiting)
            {
                lastLocalRole = PlayerRole.None;
                return;
            }

            EnsureStyles();
            DrawTopPanel(state);
            DrawLocalPlayerPanel();
            DrawCrosshair();

            if (state == GameState.Ended)
                DrawResult();
        }

        /// <summary>
        /// 화면 상단에 단계, 남은 시간, 남은 마네킹 수, 달성 횟수를 표시한다.
        /// </summary>
        private void DrawTopPanel(GameState state)
        {
            string phase = state switch
            {
                GameState.Preparation => "가드 대기 시간",
                GameState.Playing => "탐색",
                GameState.Hunting => "사냥 시간",
                GameState.Ended => "종료",
                _ => string.Empty
            };

            string text = $"{phase}  {FormatTime(stateMachine.RemainingTime)}\n" +
                          $"남은 마네킹 {playerRegistry.AliveMannequinCount}   " +
                          $"목표 {objectiveTracker.DestroyedCount} / {objectiveTracker.RequiredCount}";

            GUI.Box(new Rect(Screen.width * 0.5f - 160f, 10f, 320f, 50f), text, centerStyle);
        }

        /// <summary>
        /// 화면 하단에 역할 목표, 체력, 도구 쿨타임을 표시한다.
        /// </summary>
        private void DrawLocalPlayerPanel()
        {
            Rect area = new Rect(Screen.width * 0.5f - 280f, Screen.height - 140f, 560f, 130f);
            GUILayout.BeginArea(area, GUI.skin.box);

            if (localPlayer == null)
            {
                GUILayout.Label(lastLocalRole == PlayerRole.None ? "관전 중" : "사망했습니다. (관전 모드는 Phase 4)", centerStyle);
                GUILayout.EndArea();
                return;
            }

            GUILayout.Label(GetObjectiveText(localPlayer.Role), centerStyle);
            if (localHealth != null)
                GUILayout.Label($"체력 {localHealth.CurrentHealth} / {localHealth.MaxHealth}", centerStyle);

            GUILayout.BeginHorizontal();
            foreach (ITool tool in localTools)
            {
                string status = tool.CooldownRemaining > 0f ? $"{tool.CooldownRemaining:0.0}초" : (tool.IsReady ? "준비" : "-");
                GUILayout.Label($"{tool.DisplayName}: {status}", centerStyle);
            }
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        /// <summary>
        /// 역할별 목표 안내 문구를 반환한다. (전원 장치 목표는 마네킹에게만 표시)
        /// </summary>
        private static string GetObjectiveText(PlayerRole role)
        {
            return role switch
            {
                PlayerRole.Guard => "[가드] 숨어 있는 마네킹을 찾아 제거하세요. (좌클릭: 사격)",
                PlayerRole.Mannequin => "[마네킹] 전원 장치를 찾아 파괴하세요.\n(좌클릭 유지: 파괴, 우클릭: 변신/해제, Q/E: 변신 중 회전, 1~6: 감정 표현)",
                _ => string.Empty
            };
        }

        /// <summary>
        /// 화면 중앙에 조준점을 그린다.
        /// </summary>
        private void DrawCrosshair()
        {
            if (localPlayer == null)
                return;

            GUI.Label(new Rect(Screen.width * 0.5f - 10f, Screen.height * 0.5f - 12f, 20f, 24f), "+", centerStyle);
        }

        /// <summary>
        /// 승패와 사유를 크게 표시한다.
        /// </summary>
        private void DrawResult()
        {
            string winner = matchResult.Winner switch
            {
                MatchWinner.Guard => "가드 승리",
                MatchWinner.Mannequin => "마네킹 승리",
                _ => "무승부"
            };

            string reason = matchResult.Reason switch
            {
                WinReason.AllMannequinsEliminated => "모든 마네킹이 제거되었습니다.",
                WinReason.ObjectiveFailed => "마네킹이 제한 시간 내에 목표를 달성하지 못했습니다.",
                WinReason.GuardsSurvived => "가드가 사냥 시간 동안 살아남았습니다.",
                WinReason.AllGuardsEliminated => "모든 가드가 제거되었습니다.",
                _ => string.Empty
            };

            bool isWinner = (matchResult.Winner == MatchWinner.Guard && lastLocalRole == PlayerRole.Guard)
                            || (matchResult.Winner == MatchWinner.Mannequin && lastLocalRole == PlayerRole.Mannequin);
            string personal = lastLocalRole == PlayerRole.None ? string.Empty : (isWinner ? "\n승리!" : "\n패배");

            GUI.Box(new Rect(Screen.width * 0.5f - 250f, Screen.height * 0.5f - 80f, 500f, 160f),
                $"{winner}\n{reason}{personal}", bigCenterStyle);
        }

        /// <summary>
        /// 초를 분:초 형식으로 바꾼다.
        /// </summary>
        private static string FormatTime(float seconds)
        {
            int total = Mathf.CeilToInt(Mathf.Max(0f, seconds));
            return $"{total / 60}:{total % 60:00}";
        }

        /// <summary>
        /// GUI 스타일을 한 번만 만든다. (GUI.skin은 OnGUI 안에서만 접근 가능)
        /// </summary>
        private void EnsureStyles()
        {
            if (centerStyle != null)
                return;

            centerStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 16 };
            bigCenterStyle = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontSize = 28 };
        }
    }
}
