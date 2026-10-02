using System.Text;
using GuardVsMannequin.Core;
using GuardVsMannequin.Localization;
using GuardVsMannequin.Player;
using UnityEngine;
using UnityEngine.UI;

namespace GuardVsMannequin.UI
{
    /// <summary>
    /// HUD 하단: 살아 있으면 역할별 목표·내 체력·도구 쿨타임, 사망·미참가면 관전 정보.
    /// </summary>
    public class HudBottomPanel : MonoBehaviour, IHudSection
    {
        [SerializeField] private GameObject bottomPanel;
        [SerializeField] private Text objectiveText;
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

            if (context.LocalPlayer == null)
            {
                objectiveText.text = GetSpectatorText();
                statusText.text = string.Empty;
                return;
            }

            objectiveText.text = GetObjectiveText(context.LocalPlayer.Role, state == GameState.Hunting);

            builder.Clear();
            if (context.LocalHealth != null)
                builder.Append(Loc.Format("hud.health", context.LocalHealth.CurrentHealth, context.LocalHealth.MaxHealth));
            foreach (ITool tool in context.LocalTools)
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
            string header = Loc.Get(context.LastLocalRole == PlayerRole.None ? "hud.spectating" : "hud.spectatingDead");
            SpectatorViewPoint target = context.Spectator != null ? context.Spectator.CurrentTarget : null;
            if (target == null)
                return $"{header}\n{Loc.Get("hud.spectate.none")}";

            PlayerIdentity identity = target.Identity;
            return $"{header}\n{Loc.Format("hud.spectate.target", identity.DisplayName, HudText.GetRoleName(identity.Role))}";
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
    }
}
