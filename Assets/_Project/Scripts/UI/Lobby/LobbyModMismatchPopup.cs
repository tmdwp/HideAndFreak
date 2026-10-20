using System.Collections.Generic;
using System.Text;
using GuardVsMannequin.Localization;
using GuardVsMannequin.Mods;
using GuardVsMannequin.Network;
using UnityEngine;
using UnityEngine.UI;

namespace GuardVsMannequin.UI
{
    /// <summary>
    /// 모드가 달라 방 입장이 거부되면 로비에 띄우는 경고 창. 경고문과 함께 부족한(없음·버전·파일이 다른) 모드 목록을 보여준다.
    /// 세션 서비스에 남은 거부 기록을 매 프레임 확인하므로, 연결이 끊기며 로비 씬을 다시 불러와도 창이 뜬다.
    /// 캔버스에 붙여 두고 창(popupPanel)은 꺼 둔다.
    /// </summary>
    public class LobbyModMismatchPopup : MonoBehaviour
    {
        [SerializeField] private GameObject popupPanel;
        [SerializeField] private Text missingListText;
        [SerializeField] private Button closeButton;

        [Tooltip("로비 '모드' 화면을 여는 버튼. 비우면 닫기만 있다.")]
        [SerializeField] private Button openModsButton;

        private ILobbyNavigator navigator;
        private string shownHostMods;

        /// <summary>
        /// 버튼을 연결하고 창을 닫아 둔다.
        /// </summary>
        private void Start()
        {
            navigator = UiLookup.FindInParent<ILobbyNavigator>(this);
            closeButton.onClick.AddListener(Close);
            if (openModsButton != null)
                openModsButton.onClick.AddListener(OpenModsScreen);
            popupPanel.SetActive(false);
        }

        /// <summary>
        /// 언어 변경 시 목록 문구를 다시 만든다.
        /// </summary>
        private void OnEnable()
        {
            Loc.LanguageChanged += RefreshList;
        }

        /// <summary>
        /// 언어 변경 구독을 해제한다.
        /// </summary>
        private void OnDisable()
        {
            Loc.LanguageChanged -= RefreshList;
        }

        /// <summary>
        /// 새 거부 기록이 있으면 창을 연다.
        /// </summary>
        private void Update()
        {
            ISessionService session = SessionServices.Session;
            if (session != null && session.TryTakeModRejection(out string hostMods))
                Open(hostMods);
        }

        /// <summary>
        /// 호스트 모드 목록으로 부족한 모드를 계산해 창을 연다.
        /// </summary>
        private void Open(string hostMods)
        {
            shownHostMods = hostMods ?? string.Empty;
            RefreshList();
            popupPanel.SetActive(true);
            popupPanel.transform.SetAsLastSibling();
        }

        /// <summary>
        /// 창을 닫는다.
        /// </summary>
        private void Close()
        {
            popupPanel.SetActive(false);
        }

        /// <summary>
        /// 창을 닫고 로비 "모드" 화면으로 간다.
        /// </summary>
        private void OpenModsScreen()
        {
            Close();
            navigator?.Show(LobbyScreen.Mods);
        }

        /// <summary>
        /// 부족한 모드 목록을 현재 언어로 채운다.
        /// </summary>
        private void RefreshList()
        {
            if (shownHostMods == null)
                return;

            List<MissingMod> missing = ModService.Instance.FindMissingFor(shownHostMods);
            missingListText.text = missing.Count > 0 ? FormatMissing(missing) : Loc.Get("lobby.modMismatch.unknown");

            ScrollRect scroll = missingListText.GetComponentInParent<ScrollRect>();
            if (scroll != null)
                scroll.verticalNormalizedPosition = 1f;
        }

        /// <summary>
        /// 부족한 모드를 한 줄씩 "• 이름 (ID 버전) - 이유" 형태로 만든다.
        /// </summary>
        private static string FormatMissing(List<MissingMod> missing)
        {
            var builder = new StringBuilder();
            foreach (MissingMod mod in missing)
            {
                if (builder.Length > 0)
                    builder.Append('\n');

                string name = mod.local != null ? mod.local.DisplayName : mod.id;
                builder.Append("• ").Append(name);
                if (name != mod.id)
                    builder.Append(" (").Append(mod.id).Append(')');
                if (!string.IsNullOrEmpty(mod.hostVersion))
                    builder.Append(" v").Append(mod.hostVersion);
                builder.Append("  -  ").Append(DescribeReason(mod));
            }
            return builder.ToString();
        }

        /// <summary>
        /// 모드를 맞출 수 없는 이유 문구.
        /// </summary>
        private static string DescribeReason(MissingMod mod)
        {
            return mod.reason switch
            {
                MissingModReason.VersionDiffers => Loc.Format("lobby.modMismatch.reason.version", mod.local.Manifest.version),
                MissingModReason.FilesDiffer => Loc.Get("lobby.modMismatch.reason.files"),
                MissingModReason.Unusable => Loc.Format("lobby.modMismatch.reason.error", mod.local.Error),
                _ => Loc.Get("lobby.modMismatch.reason.notInstalled")
            };
        }
    }
}
