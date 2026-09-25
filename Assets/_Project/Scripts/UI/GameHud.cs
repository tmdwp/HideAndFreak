using GuardVsMannequin.Cameras;
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
        [SerializeField] private SpectatorController spectator;

        private PlayerIdentity localPlayer;
        private GameState previousState = GameState.Waiting;
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
            HandleStateTransition(state);
            if (state == GameState.Waiting)
            {
                lastLocalRole = PlayerRole.None;
                EnsureStyles();
                GUI.Label(new Rect(0f, Screen.height * 0.5f - 20f, Screen.width, 40f), "다른 플레이어를 기다리는 중...", centerStyle);
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
        /// 결과 화면에 들어서면 버튼을 누를 수 있도록 커서 잠금을 푼다.
        /// (커서가 풀리면 PlayerInputReader가 게임 입력을 막는다)
        /// </summary>
        private void HandleStateTransition(GameState state)
        {
            if (state == previousState)
                return;

            if (state == GameState.Ended)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            previousState = state;
        }

        /// <summary>
        /// 화면 상단에 단계, 남은 시간, 남은 마네킹 수, 달성 횟수를 표시한다.
        /// 사냥 시간 진입은 기획에 따라 텍스트 없이 조명 연출로만 알리므로, 탐색/사냥 시간에는 단계 이름을 표시하지 않는다.
        /// </summary>
        private void DrawTopPanel(GameState state)
        {
            string phase = state switch
            {
                GameState.Preparation => "가드 대기 시간",
                GameState.Ended => "종료",
                _ => string.Empty
            };

            string text = $"{phase}  {FormatTime(stateMachine.RemainingTime)}\n" +
                          $"남은 마네킹 {playerRegistry.AliveMannequinCount}   {GetObjectiveLine(state)}";

            GUI.Box(new Rect(Screen.width * 0.5f - 260f, 10f, 520f, 50f), text, centerStyle);
        }

        /// <summary>
        /// 목표 진행 문구를 만든다.
        /// 목표가 0회인 판은 달성 횟수 대신, 대기 시간 후 가드 보호 시간이 지나면 사냥 시간이 시작된다는 안내와 남은 보호 시간을 보여준다.
        /// </summary>
        private string GetObjectiveLine(GameState state)
        {
            GameSettings settings = stateMachine.Settings;
            if (!HuntTiming.IsTimedHunt(settings))
                return $"목표 {objectiveTracker.DestroyedCount} / {objectiveTracker.RequiredCount}";

            return state switch
            {
                GameState.Preparation =>
                    $"목표 없음 · 대기 시간 후 {GameConstants.ZeroObjectiveHuntDelay:0}초간 가드 보호, 이후 사냥 시간",
                GameState.Playing =>
                    $"목표 없음 · 가드 보호 시간 {Mathf.CeilToInt(Mathf.Max(0f, HuntTiming.GetProtectionRemaining(settings, stateMachine.RemainingTime)))}초 후 사냥 시간",
                _ => "목표 없음"
            };
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
                DrawSpectatorInfo();
                GUILayout.EndArea();
                return;
            }

            GUILayout.Label(GetObjectiveText(localPlayer.Role, stateMachine.CurrentState == GameState.Hunting), centerStyle);
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
        /// 사망 또는 미참가 상태에서 관전 대상과 전환 방법을 표시한다.
        /// </summary>
        private void DrawSpectatorInfo()
        {
            string header = lastLocalRole == PlayerRole.None ? "관전 중" : "사망했습니다 · 관전 중";
            SpectatorViewPoint target = spectator != null ? spectator.CurrentTarget : null;

            if (target == null)
            {
                GUILayout.Label($"{header}\n관전할 생존자가 없습니다.", centerStyle);
                return;
            }

            PlayerIdentity identity = target.Identity;
            GUILayout.Label($"{header}\n{identity.DisplayName} ({GetRoleName(identity.Role)})\n← / → : 관전 대상 전환", centerStyle);
        }

        /// <summary>
        /// 역할의 표시 이름을 반환한다.
        /// </summary>
        private static string GetRoleName(PlayerRole role)
        {
            return role switch
            {
                PlayerRole.Guard => "가드",
                PlayerRole.Mannequin => "마네킹",
                _ => "-"
            };
        }

        /// <summary>
        /// 역할별 목표와 조작 안내 문구를 반환한다. (전원 장치 목표는 마네킹에게만 표시)
        /// </summary>
        private static string GetObjectiveText(PlayerRole role, bool isHunting)
        {
            if (isHunting)
            {
                return role switch
                {
                    PlayerRole.Guard => "[가드] 살아남거나 마네킹을 모두 제거하세요.\n(좌클릭: 사격, 우클릭: 탐지기, E: 손전등)",
                    PlayerRole.Mannequin => "[마네킹] 가드를 모두 제거하세요.\n(좌클릭: 공격, 1~6: 감정 표현)",
                    _ => string.Empty
                };
            }

            return role switch
            {
                PlayerRole.Guard => "[가드] 숨어 있는 마네킹을 찾아 제거하세요.\n(좌클릭: 사격, 우클릭: 탐지기)",
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
        /// 결과 화면: 승패, 참가자별 결과, 자동 복귀 시간, (호스트) 바로 다음 판 준비 버튼을 표시한다.
        /// </summary>
        private void DrawResult()
        {
            Rect area = new Rect(Screen.width * 0.5f - 260f, Screen.height * 0.5f - 200f, 520f, 400f);
            GUILayout.BeginArea(area, GUI.skin.box);

            GUILayout.Label(GetResultHeadline(), bigCenterStyle);
            GUILayout.Space(10f);

            foreach (ParticipantResult participant in matchResult.Participants)
            {
                string status = participant.survived ? "생존" : "탈락";
                GUILayout.Label($"{participant.displayName}  ·  {GetRoleName(participant.role)}  ·  {status}", centerStyle);
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label($"{Mathf.CeilToInt(stateMachine.RemainingTime)}초 후 자동으로 룸으로 돌아갑니다.", centerStyle);

            // 서버(호스트)만 상태를 바꿀 수 있다. 대기 상태로 바뀌면 MatchSessionBridge가 모두를 룸으로 보낸다.
            if (stateMachine.IsServerReady() && GUILayout.Button("룸으로 이동", GUILayout.Height(36f)))
                stateMachine.ChangeState(GameState.Waiting);

            GUILayout.EndArea();
        }

        /// <summary>
        /// 승리 진영, 사유, 나의 승패를 한 문단으로 만든다.
        /// </summary>
        private string GetResultHeadline()
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

            return $"{winner}\n{reason}{personal}";
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
            bigCenterStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 26, wordWrap = true };
        }
    }
}
