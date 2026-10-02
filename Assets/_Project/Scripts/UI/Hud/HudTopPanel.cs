using GuardVsMannequin.Core;
using GuardVsMannequin.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace GuardVsMannequin.UI
{
    /// <summary>
    /// HUD 상단: 단계, 남은 시간, 남은 마네킹 수, 목표 진행.
    /// 사냥 시간 진입은 기획에 따라 텍스트 없이 조명 연출로만 알리므로, 탐색/사냥 시간에는 단계 이름을 표시하지 않는다.
    /// </summary>
    public class HudTopPanel : MonoBehaviour, IHudSection
    {
        [SerializeField] private GameObject topPanel;
        [SerializeField] private Text topText;

        private HudContext context;

        /// <inheritdoc />
        public void Bind(HudContext hudContext)
        {
            context = hudContext;
        }

        /// <inheritdoc />
        public void Hide()
        {
            topPanel.SetActive(false);
        }

        /// <inheritdoc />
        public void Refresh(GameState state)
        {
            bool visible = state != GameState.Waiting;
            topPanel.SetActive(visible);
            if (!visible)
                return;

            string phase = state switch
            {
                GameState.Preparation => Loc.Get("hud.phase.preparation") + "  ",
                GameState.Ended => Loc.Get("hud.phase.ended") + "  ",
                _ => string.Empty
            };

            topText.text = $"{phase}{HudText.FormatTime(context.StateMachine.RemainingTime)}\n" +
                           $"{Loc.Format("hud.remainingMannequins", context.PlayerRegistry.AliveMannequinCount)}   {GetObjectiveLine(state)}";
        }

        /// <summary>
        /// 목표 진행 문구를 만든다.
        /// 목표가 0회인 판은 달성 횟수 대신, 대기 시간 후 가드 보호 시간이 지나면 사냥 시간이 시작된다는 안내와 남은 보호 시간을 보여준다.
        /// </summary>
        private string GetObjectiveLine(GameState state)
        {
            GameSettings settings = context.StateMachine.Settings;
            if (!HuntTiming.IsTimedHunt(settings))
                return Loc.Format("hud.objective.progress", context.ObjectiveTracker.DestroyedCount, context.ObjectiveTracker.RequiredCount);

            return state switch
            {
                GameState.Preparation =>
                    Loc.Format("hud.objective.none.preparation", GameConstants.ZeroObjectiveHuntDelay.ToString("0")),
                GameState.Playing =>
                    Loc.Format("hud.objective.none.playing", Mathf.CeilToInt(Mathf.Max(0f, HuntTiming.GetProtectionRemaining(settings, context.StateMachine.RemainingTime)))),
                _ => Loc.Get("hud.objective.none")
            };
        }
    }
}
