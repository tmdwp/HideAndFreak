using System.Text;
using GuardVsMannequin.Cameras;
using GuardVsMannequin.Core;
using GuardVsMannequin.Localization;
using GuardVsMannequin.Objectives;
using GuardVsMannequin.Player;
using UnityEngine;
using UnityEngine.UI;

namespace GuardVsMannequin.UI
{
    /// <summary>
    /// 게임 HUD (uGUI).
    /// 남은 시간, 남은 마네킹 수, 달성/목표 횟수, 역할별 목표, 내 체력, 도구 쿨타임, 관전 정보, 결과를 표시한다.
    /// 다른 플레이어의 체력과 킬 로그는 기획에 따라 표시하지 않는다.
    /// </summary>
    public class GameHud : MonoBehaviour
    {
        [Header("게임 상태")]
        [SerializeField] private GameStateMachine stateMachine;
        [SerializeField] private PlayerRegistry playerRegistry;
        [SerializeField] private ObjectiveTracker objectiveTracker;
        [SerializeField] private MatchResult matchResult;
        [SerializeField] private SpectatorController spectator;

        [Header("상단 / 중앙")]
        [SerializeField] private GameObject topPanel;
        [SerializeField] private Text topText;
        [SerializeField] private GameObject crosshair;
        [SerializeField] private Text waitingText;

        [Header("하단")]
        [SerializeField] private GameObject bottomPanel;
        [SerializeField] private Text objectiveText;
        [SerializeField] private Text statusText;

        [Header("결과")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private Text resultHeadlineText;
        [SerializeField] private Text resultParticipantsText;
        [SerializeField] private Text resultCountdownText;
        [SerializeField] private Button returnToRoomButton;

        private readonly StringBuilder builder = new();
        private PlayerIdentity localPlayer;
        private PlayerHealth localHealth;
        private ITool[] localTools = new ITool[0];
        private PlayerRole lastLocalRole = PlayerRole.None;
        private GameState previousState = GameState.Waiting;

        /// <summary>
        /// 로컬 플레이어 이벤트를 구독하고, 호스트 전용 버튼을 연결한다.
        /// </summary>
        private void Awake()
        {
            LocalPlayerEvents.Spawned += OnLocalPlayerSpawned;
            LocalPlayerEvents.Despawned += OnLocalPlayerDespawned;

            // 서버(호스트)만 상태를 바꿀 수 있다. 대기 상태로 바뀌면 MatchSessionBridge가 모두를 룸으로 보낸다.
            returnToRoomButton.onClick.AddListener(() =>
            {
                if (stateMachine.IsServerReady())
                    stateMachine.ChangeState(GameState.Waiting);
            });

            SetAllHidden();
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
        /// 상태에 맞게 HUD 요소를 켜고 내용을 갱신한다.
        /// </summary>
        private void Update()
        {
            // 네트워크 초기화 전에는 FishNet 상태를 읽을 수 없다.
            if (!stateMachine.IsClientReady())
            {
                SetAllHidden();
                return;
            }

            GameState state = stateMachine.CurrentState;
            HandleStateTransition(state);

            bool isWaiting = state == GameState.Waiting;
            waitingText.gameObject.SetActive(isWaiting);
            topPanel.SetActive(!isWaiting);
            bottomPanel.SetActive(!isWaiting);
            crosshair.SetActive(!isWaiting && localPlayer != null && state != GameState.Ended);
            resultPanel.SetActive(state == GameState.Ended);

            if (isWaiting)
            {
                lastLocalRole = PlayerRole.None;
                return;
            }

            RefreshTopPanel(state);
            RefreshBottomPanel(state);
            if (state == GameState.Ended)
                RefreshResult();
        }

        /// <summary>
        /// 모든 HUD 요소를 숨긴다.
        /// </summary>
        private void SetAllHidden()
        {
            topPanel.SetActive(false);
            bottomPanel.SetActive(false);
            crosshair.SetActive(false);
            waitingText.gameObject.SetActive(false);
            resultPanel.SetActive(false);
        }

        /// <summary>
        /// 결과 화면에 들어서면 버튼을 누를 수 있도록 커서 잠금을 푼다.
        /// </summary>
        private void HandleStateTransition(GameState state)
        {
            if (state == previousState)
                return;

            if (state == GameState.Ended)
                GameCursor.SetLocked(false);
            previousState = state;
        }

        /// <summary>
        /// 상단: 단계, 남은 시간, 남은 마네킹 수, 목표.
        /// 사냥 시간 진입은 기획에 따라 텍스트 없이 조명 연출로만 알리므로, 탐색/사냥 시간에는 단계 이름을 표시하지 않는다.
        /// </summary>
        private void RefreshTopPanel(GameState state)
        {
            string phase = state switch
            {
                GameState.Preparation => Loc.Get("hud.phase.preparation") + "  ",
                GameState.Ended => Loc.Get("hud.phase.ended") + "  ",
                _ => string.Empty
            };

            topText.text = $"{phase}{FormatTime(stateMachine.RemainingTime)}\n" +
                           $"{Loc.Format("hud.remainingMannequins", playerRegistry.AliveMannequinCount)}   {GetObjectiveLine(state)}";
        }

        /// <summary>
        /// 목표 진행 문구를 만든다.
        /// 목표가 0회인 판은 달성 횟수 대신, 대기 시간 후 가드 보호 시간이 지나면 사냥 시간이 시작된다는 안내와 남은 보호 시간을 보여준다.
        /// </summary>
        private string GetObjectiveLine(GameState state)
        {
            GameSettings settings = stateMachine.Settings;
            if (!HuntTiming.IsTimedHunt(settings))
                return Loc.Format("hud.objective.progress", objectiveTracker.DestroyedCount, objectiveTracker.RequiredCount);

            return state switch
            {
                GameState.Preparation =>
                    Loc.Format("hud.objective.none.preparation", GameConstants.ZeroObjectiveHuntDelay.ToString("0")),
                GameState.Playing =>
                    Loc.Format("hud.objective.none.playing", Mathf.CeilToInt(Mathf.Max(0f, HuntTiming.GetProtectionRemaining(settings, stateMachine.RemainingTime)))),
                _ => Loc.Get("hud.objective.none")
            };
        }

        /// <summary>
        /// 하단: 살아 있으면 역할 목표·체력·도구, 아니면 관전 정보.
        /// </summary>
        private void RefreshBottomPanel(GameState state)
        {
            if (localPlayer == null)
            {
                objectiveText.text = GetSpectatorText();
                statusText.text = string.Empty;
                return;
            }

            objectiveText.text = GetObjectiveText(localPlayer.Role, state == GameState.Hunting);

            builder.Clear();
            if (localHealth != null)
                builder.Append(Loc.Format("hud.health", localHealth.CurrentHealth, localHealth.MaxHealth));
            foreach (ITool tool in localTools)
            {
                string toolStatus = tool.CooldownRemaining > 0f
                    ? Loc.Format("hud.tool.cooldown", tool.CooldownRemaining.ToString("0.0"))
                    : Loc.Get(tool.IsReady ? "hud.tool.ready" : "hud.tool.unavailable");
                builder.Append($"     {tool.DisplayName}: {toolStatus}");
            }
            statusText.text = builder.ToString();
        }

        /// <summary>
        /// 사망 또는 미참가 상태의 관전 안내 문구를 만든다.
        /// </summary>
        private string GetSpectatorText()
        {
            string header = Loc.Get(lastLocalRole == PlayerRole.None ? "hud.spectating" : "hud.spectatingDead");
            SpectatorViewPoint target = spectator != null ? spectator.CurrentTarget : null;
            if (target == null)
                return $"{header}\n{Loc.Get("hud.spectate.none")}";

            PlayerIdentity identity = target.Identity;
            return $"{header}\n{Loc.Format("hud.spectate.target", identity.DisplayName, GetRoleName(identity.Role))}";
        }

        /// <summary>
        /// 결과 화면: 승패, 참가자별 결과, 자동 복귀 시간, (호스트) 룸으로 이동 버튼.
        /// </summary>
        private void RefreshResult()
        {
            resultHeadlineText.text = GetResultHeadline();

            builder.Clear();
            foreach (ParticipantResult participant in matchResult.Participants)
            {
                string status = Loc.Get(participant.survived ? "result.survived" : "result.eliminated");
                builder.AppendLine($"{participant.displayName}  ·  {GetRoleName(participant.role)}  ·  {status}");
            }
            resultParticipantsText.text = builder.ToString();

            resultCountdownText.text = Loc.Format("result.countdown", Mathf.CeilToInt(stateMachine.RemainingTime));
            returnToRoomButton.gameObject.SetActive(stateMachine.IsServerReady());
        }

        /// <summary>
        /// 승리 진영, 사유, 나의 승패를 한 문단으로 만든다.
        /// </summary>
        private string GetResultHeadline()
        {
            string winner = Loc.Get(matchResult.Winner switch
            {
                MatchWinner.Guard => "result.guardWin",
                MatchWinner.Mannequin => "result.mannequinWin",
                _ => "result.draw"
            });

            string reasonKey = matchResult.Reason switch
            {
                WinReason.AllMannequinsEliminated => "result.reason.allMannequins",
                WinReason.ObjectiveFailed => "result.reason.objectiveFailed",
                WinReason.GuardsSurvived => "result.reason.guardsSurvived",
                WinReason.AllGuardsEliminated => "result.reason.allGuards",
                _ => string.Empty
            };
            string reason = Loc.Get(reasonKey);

            bool isWinner = (matchResult.Winner == MatchWinner.Guard && lastLocalRole == PlayerRole.Guard)
                            || (matchResult.Winner == MatchWinner.Mannequin && lastLocalRole == PlayerRole.Mannequin);
            string personal = lastLocalRole == PlayerRole.None ? string.Empty : "\n" + Loc.Get(isWinner ? "result.victory" : "result.defeat");

            return $"{winner}\n{reason}{personal}";
        }

        /// <summary>
        /// 역할의 표시 이름을 반환한다.
        /// </summary>
        private static string GetRoleName(PlayerRole role)
        {
            return Loc.Get(role switch
            {
                PlayerRole.Guard => "role.guard",
                PlayerRole.Mannequin => "role.mannequin",
                _ => "role.none"
            });
        }

        /// <summary>
        /// 역할별 목표와 조작 안내 문구를 반환한다. (전원 장치 목표는 마네킹에게만 표시)
        /// </summary>
        private static string GetObjectiveText(PlayerRole role, bool isHunting)
        {
            string phase = isHunting ? "hunting" : "playing";
            return role switch
            {
                PlayerRole.Guard => Loc.Get("hud.guide.guard." + phase),
                PlayerRole.Mannequin => Loc.Get("hud.guide.mannequin." + phase),
                _ => string.Empty
            };
        }

        /// <summary>
        /// 초를 분:초 형식으로 바꾼다.
        /// </summary>
        private static string FormatTime(float seconds)
        {
            int total = Mathf.CeilToInt(Mathf.Max(0f, seconds));
            return $"{total / 60}:{total % 60:00}";
        }
    }
}
