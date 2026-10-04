using GuardVsMannequin.Core;
using GuardVsMannequin.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace GuardVsMannequin.UI
{
    /// <summary>
    /// HUD 상단 중앙: "단계 | 남은 시간(분:초)", 그 아래 "남은 마네킹 / 이번 판 마네킹".
    /// 단계 이름: 가드 대기 시간 / 가드 타임(탐색) / 헌트 타임(사냥) / 종료.
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

            int alive = context.PlayerRegistry.AliveMannequinCount;
            // 참가 인원이 아직 동기화되지 않았으면 생존 인원을 대신 쓴다.
            int total = Mathf.Max(alive, context.PlayerRegistry.MatchMannequinCount);

            topText.text = $"{HudText.GetPhaseName(state)}  |  {HudText.FormatTime(context.State.RemainingTime)}\n" +
                           Loc.Format("hud.remainingMannequins", alive, total);
        }
    }
}
