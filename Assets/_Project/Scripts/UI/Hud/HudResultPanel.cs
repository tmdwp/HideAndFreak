using System.Text;
using GuardVsMannequin.Core;
using GuardVsMannequin.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace GuardVsMannequin.UI
{
    /// <summary>
    /// HUD 결과 화면: 승패와 사유, 참가자별 결과, 자동 복귀 시간, (호스트) 룸으로 이동 버튼.
    /// </summary>
    public class HudResultPanel : MonoBehaviour, IHudSection
    {
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private Text resultHeadlineText;
        [SerializeField] private Text resultParticipantsText;
        [SerializeField] private Text resultCountdownText;
        [SerializeField] private Button returnToRoomButton;

        private readonly StringBuilder builder = new();
        private HudContext context;

        /// <summary>
        /// 룸으로 이동 버튼을 연결한다.
        /// 서버(호스트)만 상태를 바꿀 수 있다. 대기 상태로 바뀌면 MatchSessionBridge가 모두를 룸으로 보낸다.
        /// </summary>
        public void Bind(HudContext hudContext)
        {
            context = hudContext;
            returnToRoomButton.onClick.AddListener(() =>
            {
                if (context.StateMachine.IsServerReady())
                    context.StateMachine.ChangeState(GameState.Waiting);
            });
        }

        /// <inheritdoc />
        public void Hide()
        {
            resultPanel.SetActive(false);
        }

        /// <inheritdoc />
        public void Refresh(GameState state)
        {
            bool visible = state == GameState.Ended;
            resultPanel.SetActive(visible);
            if (!visible)
                return;

            resultHeadlineText.text = GetResultHeadline();

            builder.Clear();
            foreach (ParticipantResult participant in context.MatchResult.Participants)
            {
                string status = Loc.Get(participant.survived ? "result.survived" : "result.eliminated");
                builder.AppendLine($"{participant.displayName}  ·  {HudText.GetRoleName(participant.role)}  ·  {status}");
            }
            resultParticipantsText.text = builder.ToString();

            resultCountdownText.text = Loc.Format("result.countdown", Mathf.CeilToInt(context.StateMachine.RemainingTime));
            returnToRoomButton.gameObject.SetActive(context.StateMachine.IsServerReady());
        }

        /// <summary>
        /// 승리 진영, 사유, 나의 승패를 한 문단으로 만든다.
        /// </summary>
        private string GetResultHeadline()
        {
            MatchResult matchResult = context.MatchResult;
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

            PlayerRole localRole = context.LastLocalRole;
            bool isWinner = (matchResult.Winner == MatchWinner.Guard && localRole == PlayerRole.Guard)
                            || (matchResult.Winner == MatchWinner.Mannequin && localRole == PlayerRole.Mannequin);
            string personal = localRole == PlayerRole.None ? string.Empty : "\n" + Loc.Get(isWinner ? "result.victory" : "result.defeat");

            return $"{winner}\n{reason}{personal}";
        }
    }
}
