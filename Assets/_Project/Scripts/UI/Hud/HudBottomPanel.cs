using System.Text;
using GuardVsMannequin.Core;
using GuardVsMannequin.Localization;
using GuardVsMannequin.Player;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace GuardVsMannequin.UI
{
    /// <summary>
    /// HUD 하단: 살아 있으면 내 체력·도구 쿨타임, 사망·미참가면 관전 정보.
    /// (목표는 <see cref="HudObjectivePanel"/>, 조작 안내는 <see cref="HudControlsPanel"/>, 스태미나는 <see cref="HudStaminaBar"/>가 맡는다)
    /// </summary>
    public class HudBottomPanel : MonoBehaviour, IHudSection
    {
        [SerializeField] private GameObject bottomPanel;

        [Tooltip("관전 안내 문구. (살아 있는 동안은 비어 있음)")]
        [FormerlySerializedAs("objectiveText")]
        [SerializeField] private Text spectatorText;

        [SerializeField] private Text statusText;

        private readonly StringBuilder builder = new();
        private HudContext context;

        /// <inheritdoc />
        public void Bind(HudContext hudContext)
        {
            context = hudContext;
        }

        /// <inheritdoc />
        public void Hide()
        {
            bottomPanel.SetActive(false);
        }

        /// <inheritdoc />
        public void Refresh(GameState state)
        {
            bool visible = state != GameState.Waiting;
            bottomPanel.SetActive(visible);
            if (!visible)
                return;

            LocalPlayerTracker local = context.LocalPlayer;
            if (local.Player == null)
            {
                spectatorText.text = GetSpectatorText();
                statusText.text = string.Empty;
                return;
            }

            spectatorText.text = string.Empty;

            builder.Clear();
            if (local.Health != null)
                builder.Append(Loc.Format("hud.health", local.Health.CurrentHealth, local.Health.MaxHealth));
            foreach (ITool tool in local.Tools)
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
            string header = Loc.Get(context.LocalPlayer.LastRole == PlayerRole.None ? "hud.spectating" : "hud.spectatingDead");
            SpectatorViewPoint target = context.Spectator != null ? context.Spectator.CurrentTarget : null;
            if (target == null)
                return $"{header}\n{Loc.Get("hud.spectate.none")}";

            PlayerIdentity identity = target.Identity;
            return $"{header}\n{Loc.Format("hud.spectate.target", identity.DisplayName, HudText.GetRoleName(identity.Role))}";
        }
    }
}
