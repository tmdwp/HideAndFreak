using System;
using GuardVsMannequin.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace GuardVsMannequin.UI
{
    /// <summary>
    /// 설정 화면의 틀: 탭 전환, 열기/닫기, "모두 기본값으로".
    /// 실제 설정은 하위의 설정 묶음(<see cref="ISettingsSection"/>)이 각자 처리한다.
    /// - 그래픽 탭: <see cref="DisplaySettingsSection"/>(화면 모드, 해상도), <see cref="LanguageSettingsSection"/>(언어)
    /// - 사운드 탭: <see cref="SoundSettingsSection"/>
    /// - 조작 키 탭: <see cref="ControlSettingsSection"/>
    /// 새 설정을 추가할 때는 ISettingsSection 컴포넌트를 원하는 페이지 안에 두면 자동으로 연결된다.
    /// 로비와 게임 중 ESC 메뉴에서 같은 화면을 쓴다.
    /// </summary>
    public class SettingsPanelView : MonoBehaviour
    {
        [Header("탭 (버튼과 페이지는 같은 순서)")]
        [SerializeField] private Button[] tabButtons;
        [SerializeField] private GameObject[] pages;
        [Tooltip("선택된 탭 버튼 색. 탭 버튼에 이미지를 넣었다면 흰색으로 두면 이미지 색 그대로 보인다.")]
        [SerializeField] private Color selectedTabColor = new Color(0.85f, 0.55f, 0.20f, 1f);
        [SerializeField] private Color normalTabColor = new Color(0.22f, 0.25f, 0.30f, 1f);

        [Header("버튼")]
        [SerializeField] private Button resetAllButton;
        [SerializeField] private Button closeButton;

        private ISettingsSection[] sections = Array.Empty<ISettingsSection>();
        private int currentPage;
        private bool isInitialized;

        /// <summary>화면이 닫혔을 때.</summary>
        public event Action Closed;

        /// <summary>화면이 열려 있는지 여부.</summary>
        public bool IsOpen => gameObject.activeSelf;

        /// <summary>
        /// 처음 열릴 때 한 번 설정 묶음을 찾아 준비하고 버튼을 연결한다. (꺼진 페이지 안의 묶음도 포함)
        /// </summary>
        private void Initialize()
        {
            if (isInitialized)
                return;
            isInitialized = true;

            sections = GetComponentsInChildren<ISettingsSection>(true);
            foreach (ISettingsSection section in sections)
                section.Initialize();

            for (int i = 0; i < tabButtons.Length; i++)
            {
                int pageIndex = i;
                tabButtons[i].onClick.AddListener(() => ShowPage(pageIndex));
            }

            resetAllButton.onClick.AddListener(ResetAll);
            closeButton.onClick.AddListener(Close);
            Loc.LanguageChanged += OnLanguageChanged;
        }

        /// <summary>
        /// 언어 변경 구독을 해제한다.
        /// </summary>
        private void OnDestroy()
        {
            Loc.LanguageChanged -= OnLanguageChanged;
        }

        /// <summary>
        /// 화면을 열고 저장된 값을 표시한다. 마지막으로 본 페이지를 다시 연다.
        /// </summary>
        public void Open()
        {
            gameObject.SetActive(true);
            Initialize();
            RefreshAll();
            ShowPage(currentPage);
        }

        /// <summary>
        /// 화면을 닫는다. (키 설정 중에는 닫지 않음)
        /// </summary>
        public void Close()
        {
            if (RebindRowView.IsAnyRebinding)
                return;

            gameObject.SetActive(false);
            Closed?.Invoke();
        }

        /// <summary>
        /// 페이지 하나만 보이게 하고 해당 탭을 강조한다. (키 설정 중에는 바꾸지 않음)
        /// </summary>
        private void ShowPage(int pageIndex)
        {
            if (RebindRowView.IsAnyRebinding || pages.Length == 0)
                return;

            currentPage = Mathf.Clamp(pageIndex, 0, pages.Length - 1);
            for (int i = 0; i < pages.Length; i++)
                pages[i].SetActive(i == currentPage);

            for (int i = 0; i < tabButtons.Length; i++)
            {
                if (tabButtons[i].targetGraphic != null)
                    tabButtons[i].targetGraphic.color = i == currentPage ? selectedTabColor : normalTabColor;
            }
        }

        /// <summary>
        /// 모든 설정 묶음이 저장된 값을 다시 표시한다.
        /// </summary>
        private void RefreshAll()
        {
            foreach (ISettingsSection section in sections)
                section.Refresh();
        }

        /// <summary>
        /// 모든 설정 묶음을 기본값으로 되돌린다. (언어·화면 설정은 각 묶음이 유지)
        /// </summary>
        private void ResetAll()
        {
            if (RebindRowView.IsAnyRebinding)
                return;

            foreach (ISettingsSection section in sections)
                section.ResetToDefaults();
            RefreshAll();
        }

        /// <summary>
        /// 언어가 바뀌면 코드로 채운 문장(언어 이름, 화면 모드, 키 이름)을 다시 표시한다.
        /// </summary>
        private void OnLanguageChanged()
        {
            if (IsOpen)
                RefreshAll();
        }
    }
}
