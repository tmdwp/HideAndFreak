using GuardVsMannequin.Core;
using GuardVsMannequin.Player;
using UnityEngine;
using UnityEngine.UI;

namespace GuardVsMannequin.UI
{
    /// <summary>
    /// HUD 하단 중앙: 남은 스태미나 막대. 채움 막대의 오른쪽 앵커를 남은 비율로 줄인다. (스프라이트 없이 동작)
    /// 스태미나를 다 써서 다시 달릴 수 없는 동안에는 색을 바꾼다.
    /// </summary>
    public class HudStaminaBar : MonoBehaviour, IHudSection
    {
        [SerializeField] private GameObject staminaPanel;

        [Tooltip("채움 막대. 왼쪽 끝 기준으로 남은 비율만큼 늘어난다.")]
        [SerializeField] private RectTransform fill;

        [SerializeField] private Graphic fillGraphic;
        [SerializeField] private Color normalColor = new(0.95f, 0.8f, 0.3f, 1f);
        [SerializeField] private Color exhaustedColor = new(0.85f, 0.3f, 0.25f, 1f);

        private HudContext context;

        /// <inheritdoc />
        public void Bind(HudContext hudContext)
        {
            context = hudContext;
        }

        /// <inheritdoc />
        public void Hide()
        {
            staminaPanel.SetActive(false);
        }

        /// <inheritdoc />
        public void Refresh(GameState state)
        {
            bool isInMatch = state is GameState.Preparation or GameState.Playing or GameState.Hunting;
            IStaminaSource stamina = context.LocalPlayer.Stamina;
            bool visible = isInMatch && stamina != null;
            staminaPanel.SetActive(visible);
            if (!visible)
                return;

            fill.anchorMin = new Vector2(0f, fill.anchorMin.y);
            fill.anchorMax = new Vector2(Mathf.Clamp01(stamina.Stamina), fill.anchorMax.y);
            if (fillGraphic != null)
                fillGraphic.color = stamina.IsExhausted ? exhaustedColor : normalColor;
        }
    }
}
