using System.Collections.Generic;
using System.IO;
using System.Linq;
using GuardVsMannequin.Localization;
using GuardVsMannequin.Mods;
using UnityEngine;
using UnityEngine.UI;

namespace GuardVsMannequin.UI
{
    /// <summary>
    /// 로비 "모드" 화면: 설치된 모드 목록(켜짐/꺼짐/오류와 이유), 누르면 켜고 끄기, 다시 읽기, 모드 폴더 열기.
    /// 모드 목록과 켜기/끄기는 <see cref="ModService"/>가 맡고, 이 화면은 보여주고 요청만 한다.
    /// </summary>
    public class LobbyModsMenu : MonoBehaviour
    {
        [Tooltip("메인 화면의 '모드' 버튼.")]
        [SerializeField] private Button openButton;

        [Tooltip("모드 줄을 넣을 스크롤 목록의 Content.")]
        [SerializeField] private RectTransform listContent;

        [Tooltip("모드 한 줄의 원본(꺼진 상태로 둔다). 모드마다 복제한다.")]
        [SerializeField] private Button rowTemplate;

        [Tooltip("켜진 모드 수와 모드 폴더 안내.")]
        [SerializeField] private Text summaryText;

        [SerializeField] private Button refreshButton;
        [SerializeField] private Button openFolderButton;

        private readonly List<GameObject> rows = new();

        /// <summary>
        /// 버튼을 연결하고 목록을 만든다. 모드 목록이 바뀌면 다시 만든다.
        /// </summary>
        private void Start()
        {
            ILobbyNavigator navigator = UiLookup.FindInParent<ILobbyNavigator>(this);
            openButton.onClick.AddListener(() => navigator?.Show(LobbyScreen.Mods));
            refreshButton.onClick.AddListener(() => ModService.Instance.Refresh());
            openFolderButton.onClick.AddListener(OpenModsFolder);

            rowTemplate.gameObject.SetActive(false);
            ModService.Instance.Changed += Rebuild;
            Loc.LanguageChanged += Rebuild;
            Rebuild();
        }

        /// <summary>
        /// 이벤트 연결을 해제한다.
        /// </summary>
        private void OnDestroy()
        {
            ModService.Instance.Changed -= Rebuild;
            Loc.LanguageChanged -= Rebuild;
        }

        /// <summary>
        /// 모드 줄을 다시 만든다. 오류 모드는 누를 수 없고 이유를 함께 보여준다.
        /// </summary>
        private void Rebuild()
        {
            foreach (GameObject row in rows)
                Destroy(row);
            rows.Clear();

            IReadOnlyList<ModInfo> mods = ModService.Instance.Mods;
            if (mods.Count == 0)
                AddRow(Loc.Get("lobby.mods.empty"), null);
            foreach (ModInfo mod in mods)
                AddRow(Describe(mod), mod.Status == ModStatus.Error ? null : mod);

            int active = mods.Count(m => m.Status == ModStatus.Active);
            summaryText.text = Loc.Format("lobby.mods.summary", active, mods.Count, ModScanner.SearchFolders[0]);
        }

        /// <summary>
        /// 줄 하나를 더한다. 모드가 null이면 누를 수 없는 안내 줄이다.
        /// </summary>
        private void AddRow(string label, ModInfo mod)
        {
            Button row = Instantiate(rowTemplate, listContent);
            row.gameObject.SetActive(true);
            row.GetComponentInChildren<Text>().text = label;
            row.interactable = mod != null;
            if (mod != null)
                row.onClick.AddListener(() => ModService.Instance.SetEnabled(mod.Id, mod.Status == ModStatus.Disabled));
            rows.Add(row.gameObject);
        }

        /// <summary>
        /// 모드 한 줄의 문구: [상태] 이름 v버전 (ID) — 오류면 이유.
        /// </summary>
        private static string Describe(ModInfo mod)
        {
            string state = mod.Status switch
            {
                ModStatus.Active => Loc.Get("lobby.mods.state.active"),
                ModStatus.Disabled => Loc.Get("lobby.mods.state.disabled"),
                _ => Loc.Get("lobby.mods.state.error")
            };
            string version = mod.Manifest?.version ?? "?";
            string line = $"{state} {mod.DisplayName} v{version}  ({mod.Id})";
            return mod.Status == ModStatus.Error ? $"{line}\n  └ {mod.Error}" : line;
        }

        /// <summary>
        /// 실행 파일 옆 모드 폴더를 탐색기로 연다. (없으면 만든다)
        /// </summary>
        private static void OpenModsFolder()
        {
            string folder = ModScanner.SearchFolders[0];
            try
            {
                Directory.CreateDirectory(folder);
                Application.OpenURL("file:///" + folder.Replace('\\', '/'));
            }
            catch (System.Exception exception) when (exception is IOException || exception is System.UnauthorizedAccessException)
            {
                Debug.LogWarning($"[LobbyModsMenu] 모드 폴더를 열 수 없습니다: {folder} ({exception.Message})");
            }
        }
    }
}
