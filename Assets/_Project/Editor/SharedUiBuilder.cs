using System.Linq;
using GuardVsMannequin.Settings;
using GuardVsMannequin.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GuardVsMannequin.Editor
{
    /// <summary>
    /// 여러 씬에서 함께 쓰는 uGUI 화면(설정, 크레딧, ESC 메뉴, 게임 HUD)을 만든다.
    /// </summary>
    public static class SharedUiBuilder
    {
        public const string InputActionsPath = "Assets/_Project/Input/GameInput.inputactions";

        /// <summary>
        /// 설정 화면 프리팹(없으면 기본 모양으로 만들어 저장)을 캔버스 안에 중첩 프리팹으로 넣는다. 처음에는 닫혀 있다.
        /// </summary>
        public static SettingsPanelView CreateSettingsPanel(Transform canvas)
        {
            GameObject prefab = UiPrefabLibrary.GetOrBuildPanel(UiPrefabLibrary.SettingsPanelPath, parent => BuildSettingsPanel(parent).gameObject);
            return UiPrefabLibrary.Instantiate(prefab, canvas).GetComponent<SettingsPanelView>();
        }

        /// <summary>
        /// 크레딧 화면 프리팹(없으면 기본 모양으로 만들어 저장)을 캔버스 안에 중첩 프리팹으로 넣는다. 처음에는 닫혀 있다.
        /// </summary>
        public static CreditsView CreateCreditsPanel(Transform canvas)
        {
            GameObject prefab = UiPrefabLibrary.GetOrBuildPanel(UiPrefabLibrary.CreditsPanelPath, parent => BuildCreditsPanel(parent).gameObject);
            return UiPrefabLibrary.Instantiate(prefab, canvas).GetComponent<CreditsView>();
        }

        /// <summary>
        /// 설정 화면의 기본 모양을 만든다. 탭(그래픽 / 사운드 / 조작)으로 페이지를 나눈다.
        /// </summary>
        private static SettingsPanelView BuildSettingsPanel(Transform canvas)
        {
            RectTransform panel = EditorUiFactory.CreatePanel(canvas, "SettingsPanel", new Vector2(0.2f, 0.06f), new Vector2(0.8f, 0.94f), new Color(0.06f, 0.07f, 0.09f, 0.98f));
            EditorUiFactory.AddVerticalLayout(panel, 10f, 28);

            Text title = EditorUiFactory.CreateText(panel, "Title", "options.title", 36, TextAnchor.MiddleLeft, EditorUiFactory.AccentColor);
            title.fontStyle = FontStyle.Bold;
            EditorUiFactory.SetHeight(title, 48f);

            // 탭 버튼 (순서 = 페이지 순서)
            RectTransform tabs = EditorUiFactory.CreateRow(panel, "Tabs", 50f, 8f);
            string[] tabKeys = { "options.tab.graphics", "options.tab.sound", "options.tab.controls" };
            Button[] tabButtons = tabKeys.Select(key => EditorUiFactory.CreateButton(tabs, key.Substring(key.LastIndexOf('.') + 1) + "Tab", key, EditorUiFactory.ButtonColor)).ToArray();
            foreach (Button tab in tabButtons)
                EditorUiFactory.SetFlexibleWidth(tab);

            // 페이지를 겹쳐 두고 하나만 켠다.
            RectTransform pagesRoot = EditorUiFactory.CreateRect(panel, "Pages");
            LayoutElement pagesLayout = pagesRoot.gameObject.AddComponent<LayoutElement>();
            pagesLayout.flexibleHeight = 1f;
            pagesLayout.minHeight = 300f;
            RectTransform graphicsPage = CreatePage(pagesRoot, "GraphicsPage");
            RectTransform soundPage = CreatePage(pagesRoot, "SoundPage");
            RectTransform controlsPage = CreatePage(pagesRoot, "ControlsPage");

            BuildGraphicsPage(graphicsPage);
            BuildSoundPage(soundPage);
            BuildControlsPage(controlsPage);

            RectTransform buttons = EditorUiFactory.CreateRow(panel, "Buttons", 54f, 12f);
            Button resetAll = EditorUiFactory.CreateButton(buttons, "ResetAllButton", "options.resetAll", EditorUiFactory.ButtonColor);
            Button close = EditorUiFactory.CreateButton(buttons, "CloseButton", "options.close", EditorUiFactory.AccentColor);
            EditorUiFactory.SetFlexibleWidth(resetAll);
            EditorUiFactory.SetFlexibleWidth(close);

            // 틀(탭·닫기)만 연결한다. 각 설정은 페이지에 붙은 설정 묶음(ISettingsSection)이 처리한다.
            SettingsPanelView view = panel.gameObject.AddComponent<SettingsPanelView>();
            TestEnvironmentSetupTool.SetArray(view, "tabButtons", tabButtons);
            TestEnvironmentSetupTool.SetArray(view, "pages", new Object[] { graphicsPage.gameObject, soundPage.gameObject, controlsPage.gameObject });
            TestEnvironmentSetupTool.SetField(view, "resetAllButton", resetAll);
            TestEnvironmentSetupTool.SetField(view, "closeButton", close);

            panel.gameObject.SetActive(false);
            return view;
        }

        /// <summary>
        /// 그래픽 페이지: 화면 모드(◀ ▶), 해상도(스크롤 목록), 언어(◀ ▶).
        /// </summary>
        private static void BuildGraphicsPage(RectTransform page)
        {
            CreateSelectorRow(page, "ScreenMode", "options.screenMode", out Button previousMode, out Text modeValue, out Button nextMode);

            RectTransform resolutionRow = EditorUiFactory.CreateRow(page, "ResolutionRow", 44f, 16f);
            Text resolutionLabel = EditorUiFactory.CreateText(resolutionRow, "Label", "options.resolution", 24, TextAnchor.MiddleLeft, EditorUiFactory.TextColor);
            EditorUiFactory.SetWidth(resolutionLabel, 200f);
            Dropdown resolution = EditorUiFactory.CreateDropdown(resolutionRow, "ResolutionDropdown", 24, 320f);
            EditorUiFactory.SetFlexibleWidth(resolution);

            // 언어: strings.csv에 열을 추가하면 목록에 나타남
            CreateSelectorRow(page, "Language", "options.language", out Button previousLanguage, out Text languageValue, out Button nextLanguage);

            DisplaySettingsSection display = page.gameObject.AddComponent<DisplaySettingsSection>();
            TestEnvironmentSetupTool.SetField(display, "previousScreenModeButton", previousMode);
            TestEnvironmentSetupTool.SetField(display, "nextScreenModeButton", nextMode);
            TestEnvironmentSetupTool.SetField(display, "screenModeValueText", modeValue);
            TestEnvironmentSetupTool.SetField(display, "resolutionDropdown", resolution);

            LanguageSettingsSection language = page.gameObject.AddComponent<LanguageSettingsSection>();
            TestEnvironmentSetupTool.SetField(language, "previousLanguageButton", previousLanguage);
            TestEnvironmentSetupTool.SetField(language, "nextLanguageButton", nextLanguage);
            TestEnvironmentSetupTool.SetField(language, "languageValueText", languageValue);
        }

        /// <summary>
        /// 사운드 페이지: 전체 / 배경 음악 / 효과음 음량.
        /// </summary>
        private static void BuildSoundPage(RectTransform page)
        {
            Slider master = CreateSliderRow(page, "MasterVolume", "options.masterVolume", out Text masterValue);
            Slider bgm = CreateSliderRow(page, "BgmVolume", "options.bgmVolume", out Text bgmValue);
            Slider sfx = CreateSliderRow(page, "SfxVolume", "options.sfxVolume", out Text sfxValue);

            SoundSettingsSection sound = page.gameObject.AddComponent<SoundSettingsSection>();
            TestEnvironmentSetupTool.SetField(sound, "masterVolumeSlider", master);
            TestEnvironmentSetupTool.SetField(sound, "masterVolumeValueText", masterValue);
            TestEnvironmentSetupTool.SetField(sound, "bgmVolumeSlider", bgm);
            TestEnvironmentSetupTool.SetField(sound, "bgmVolumeValueText", bgmValue);
            TestEnvironmentSetupTool.SetField(sound, "sfxVolumeSlider", sfx);
            TestEnvironmentSetupTool.SetField(sound, "sfxVolumeValueText", sfxValue);
        }

        /// <summary>
        /// 조작 키 페이지: 마우스 감도, 키 설정 목록(스크롤).
        /// </summary>
        private static void BuildControlsPage(RectTransform page)
        {
            Slider sensitivity = CreateSliderRow(page, "Sensitivity", "options.sensitivity", out Text sensitivityValue);
            Text keyHeader = EditorUiFactory.CreateText(page, "KeyHeader", "options.keyHeader", 24, TextAnchor.MiddleLeft, EditorUiFactory.TextColor);
            EditorUiFactory.SetHeight(keyHeader, 36f);

            RectTransform list = EditorUiFactory.CreateScrollList(page, "KeyList", 6f);
            EditorUiFactory.FillRemainingHeight(list, 200f);
            RebindRowView[] rows = KeyBindingCatalog.All.Select(entry => CreateRebindRow(list, entry)).ToArray();

            ControlSettingsSection controls = page.gameObject.AddComponent<ControlSettingsSection>();
            TestEnvironmentSetupTool.SetField(controls, "inputActions", AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath));
            TestEnvironmentSetupTool.SetField(controls, "sensitivitySlider", sensitivity);
            TestEnvironmentSetupTool.SetField(controls, "sensitivityValueText", sensitivityValue);
            TestEnvironmentSetupTool.SetArray(controls, "rebindRows", rows);
        }

        /// <summary>
        /// 크레딧 화면의 기본 모양을 만든다. (내용은 credits.&lt;언어&gt;.txt)
        /// </summary>
        private static CreditsView BuildCreditsPanel(Transform canvas)
        {
            RectTransform panel = EditorUiFactory.CreatePanel(canvas, "CreditsPanel", new Vector2(0.25f, 0.1f), new Vector2(0.75f, 0.9f), new Color(0.06f, 0.07f, 0.09f, 0.98f));
            EditorUiFactory.AddVerticalLayout(panel, 12f, 32);

            Text title = EditorUiFactory.CreateText(panel, "Title", "credits.title", 36, TextAnchor.MiddleCenter, EditorUiFactory.AccentColor);
            title.fontStyle = FontStyle.Bold;
            EditorUiFactory.SetHeight(title, 48f);

            // 에셋이 늘어도 넘치지 않도록 스크롤 영역에 둔다.
            Text body = EditorUiFactory.CreateScrollText(panel, "Body", string.Empty, 24, TextAnchor.UpperCenter, EditorUiFactory.TextColor);
            EditorUiFactory.FillRemainingHeight(body.transform.parent, 200f);

            Button close = EditorUiFactory.CreateButton(panel, "CloseButton", "credits.close", EditorUiFactory.AccentColor);
            EditorUiFactory.SetHeight(close, 54f);

            CreditsView view = panel.gameObject.AddComponent<CreditsView>();
            TestEnvironmentSetupTool.SetField(view, "bodyText", body);
            TestEnvironmentSetupTool.SetField(view, "closeButton", close);

            panel.gameObject.SetActive(false);
            return view;
        }

        /// <summary>
        /// ESC 메뉴(계속하기, 설정, 방 나가기)와 그 설정 화면을 만든다.
        /// </summary>
        /// <param name="canvas">메뉴를 올릴 캔버스.</param>
        /// <param name="lockCursorOnClose">메뉴를 닫을 때 커서를 다시 잠글지 여부. (게임 씬: true)</param>
        public static PauseMenuView CreatePauseMenu(Transform canvas, bool lockCursorOnClose)
        {
            RectTransform menu = EditorUiFactory.CreatePanel(canvas, "PauseMenu", Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.6f));

            RectTransform box = EditorUiFactory.CreatePanel(menu, "Box", new Vector2(0.38f, 0.3f), new Vector2(0.62f, 0.7f), EditorUiFactory.PanelColor);
            EditorUiFactory.AddVerticalLayout(box, 16f, 32);
            Text title = EditorUiFactory.CreateText(box, "Title", "pause.title", 36, TextAnchor.MiddleCenter, EditorUiFactory.AccentColor);
            title.fontStyle = FontStyle.Bold;
            EditorUiFactory.SetHeight(title, 50f);

            Button resume = EditorUiFactory.CreateButton(box, "ResumeButton", "pause.resume", EditorUiFactory.AccentColor);
            Button settings = EditorUiFactory.CreateButton(box, "SettingsButton", "pause.settings", EditorUiFactory.ButtonColor);
            Button leave = EditorUiFactory.CreateButton(box, "LeaveButton", "pause.leave", EditorUiFactory.ButtonColor);
            foreach (Button button in new[] { resume, settings, leave })
                EditorUiFactory.SetHeight(button, 60f);

            // 설정 화면은 메뉴 위에 그려지도록 나중에 만든다.
            SettingsPanelView settingsPanel = CreateSettingsPanel(canvas);

            PauseMenuView view = canvas.gameObject.AddComponent<PauseMenuView>();
            TestEnvironmentSetupTool.SetField(view, "inputActions", AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath));
            TestEnvironmentSetupTool.SetField(view, "menuPanel", menu.gameObject);
            TestEnvironmentSetupTool.SetField(view, "resumeButton", resume);
            TestEnvironmentSetupTool.SetField(view, "settingsButton", settings);
            TestEnvironmentSetupTool.SetField(view, "leaveButton", leave);
            TestEnvironmentSetupTool.SetField(view, "settingsPanel", settingsPanel);
            var serialized = new SerializedObject(view);
            serialized.FindProperty("lockCursorOnClose").boolValue = lockCursorOnClose;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            menu.gameObject.SetActive(false);
            return view;
        }

        /// <summary>
        /// 게임 HUD(상단 시간/목표, 조준점, 대기 문구, 하단 목표/상태, 결과 화면)를 만든다.
        /// 게임 상태 참조는 호출한 쪽에서 연결한다.
        /// </summary>
        public static GameHud CreateGameHud(Transform canvas)
        {
            // 상단: 시간, 남은 마네킹, 목표
            RectTransform top = EditorUiFactory.CreatePanel(canvas, "TopPanel", new Vector2(0.28f, 0.9f), new Vector2(0.72f, 0.985f), new Color(0f, 0f, 0f, 0.45f));
            Text topText = EditorUiFactory.CreateText(top, "TopText", string.Empty, 26, TextAnchor.MiddleCenter, EditorUiFactory.TextColor);
            EditorUiFactory.Stretch(topText.rectTransform);

            // 중앙: 조준점, 대기 문구
            Text crosshair = EditorUiFactory.CreateText(canvas, "Crosshair", "+", 34, TextAnchor.MiddleCenter, EditorUiFactory.TextColor);
            crosshair.rectTransform.sizeDelta = new Vector2(40f, 40f);
            crosshair.raycastTarget = false;
            Text waiting = EditorUiFactory.CreateText(canvas, "WaitingText", "hud.waiting", 32, TextAnchor.MiddleCenter, EditorUiFactory.TextColor);
            waiting.rectTransform.sizeDelta = new Vector2(900f, 60f);

            // 하단: 목표/관전 안내, 체력과 도구
            RectTransform bottom = EditorUiFactory.CreatePanel(canvas, "BottomPanel", new Vector2(0.18f, 0.02f), new Vector2(0.82f, 0.14f), new Color(0f, 0f, 0f, 0.45f));
            EditorUiFactory.AddVerticalLayout(bottom, 4f, 10);
            Text objective = EditorUiFactory.CreateText(bottom, "ObjectiveText", string.Empty, 22, TextAnchor.MiddleCenter, EditorUiFactory.TextColor);
            objective.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
            Text status = EditorUiFactory.CreateText(bottom, "StatusText", string.Empty, 22, TextAnchor.MiddleCenter, EditorUiFactory.NoticeColor);
            EditorUiFactory.SetHeight(status, 30f);

            // 결과
            RectTransform result = EditorUiFactory.CreatePanel(canvas, "ResultPanel", new Vector2(0.3f, 0.2f), new Vector2(0.7f, 0.82f), EditorUiFactory.PanelColor);
            EditorUiFactory.AddVerticalLayout(result, 12f, 28);
            Text headline = EditorUiFactory.CreateText(result, "Headline", string.Empty, 34, TextAnchor.MiddleCenter, EditorUiFactory.AccentColor);
            EditorUiFactory.SetHeight(headline, 150f);
            // 참가자가 많으면 스크롤한다. (최대 10명)
            Text participants = EditorUiFactory.CreateScrollText(result, "Participants", string.Empty, 24, TextAnchor.UpperCenter, EditorUiFactory.TextColor);
            EditorUiFactory.FillRemainingHeight(participants.transform.parent, 120f);
            Text countdown = EditorUiFactory.CreateText(result, "Countdown", string.Empty, 22, TextAnchor.MiddleCenter, EditorUiFactory.TextColor);
            EditorUiFactory.SetHeight(countdown, 32f);
            Button returnButton = EditorUiFactory.CreateButton(result, "ReturnToRoomButton", "result.returnToRoom", EditorUiFactory.AccentColor);
            EditorUiFactory.SetHeight(returnButton, 56f);

            GameHud hud = canvas.gameObject.AddComponent<GameHud>();
            TestEnvironmentSetupTool.SetField(hud, "topPanel", top.gameObject);
            TestEnvironmentSetupTool.SetField(hud, "topText", topText);
            TestEnvironmentSetupTool.SetField(hud, "crosshair", crosshair.gameObject);
            TestEnvironmentSetupTool.SetField(hud, "waitingText", waiting);
            TestEnvironmentSetupTool.SetField(hud, "bottomPanel", bottom.gameObject);
            TestEnvironmentSetupTool.SetField(hud, "objectiveText", objective);
            TestEnvironmentSetupTool.SetField(hud, "statusText", status);
            TestEnvironmentSetupTool.SetField(hud, "resultPanel", result.gameObject);
            TestEnvironmentSetupTool.SetField(hud, "resultHeadlineText", headline);
            TestEnvironmentSetupTool.SetField(hud, "resultParticipantsText", participants);
            TestEnvironmentSetupTool.SetField(hud, "resultCountdownText", countdown);
            TestEnvironmentSetupTool.SetField(hud, "returnToRoomButton", returnButton);
            return hud;
        }

        /// <summary>
        /// 설정 페이지 하나를 만든다. 부모 영역을 꽉 채우고 내용을 위에서부터 쌓는다.
        /// </summary>
        private static RectTransform CreatePage(Transform parent, string name)
        {
            RectTransform page = EditorUiFactory.CreateRect(parent, name);
            EditorUiFactory.Stretch(page);
            EditorUiFactory.AddVerticalLayout(page, 10f, 4);
            return page;
        }

        /// <summary>
        /// 이름 | ◀ | 값 | ▶ 형태의 선택 줄을 만든다. (값 문구는 설정 화면 코드가 채운다)
        /// </summary>
        private static void CreateSelectorRow(Transform parent, string name, string label, out Button previous, out Text value, out Button next)
        {
            RectTransform row = EditorUiFactory.CreateRow(parent, name + "Row", 44f, 16f);
            Text labelText = EditorUiFactory.CreateText(row, "Label", label, 24, TextAnchor.MiddleLeft, EditorUiFactory.TextColor);
            EditorUiFactory.SetWidth(labelText, 200f);
            previous = EditorUiFactory.CreateButton(row, "Previous" + name + "Button", "◀", EditorUiFactory.ButtonColor);
            EditorUiFactory.SetWidth(previous, 56f);
            value = EditorUiFactory.CreateText(row, "Value", "-", 24, TextAnchor.MiddleCenter, EditorUiFactory.TextColor);
            EditorUiFactory.SetFlexibleWidth(value);
            next = EditorUiFactory.CreateButton(row, "Next" + name + "Button", "▶", EditorUiFactory.ButtonColor);
            EditorUiFactory.SetWidth(next, 56f);
        }

        /// <summary>
        /// 이름 | 슬라이더 | 값 형태의 한 줄을 만든다.
        /// </summary>
        private static Slider CreateSliderRow(Transform parent, string name, string label, out Text valueText)
        {
            RectTransform row = EditorUiFactory.CreateRow(parent, name + "Row", 44f, 16f);
            Text labelText = EditorUiFactory.CreateText(row, "Label", label, 24, TextAnchor.MiddleLeft, EditorUiFactory.TextColor);
            EditorUiFactory.SetWidth(labelText, 200f);

            Slider slider = EditorUiFactory.CreateSlider(row, name + "Slider");
            EditorUiFactory.SetFlexibleWidth(slider);

            valueText = EditorUiFactory.CreateText(row, "Value", "-", 24, TextAnchor.MiddleRight, EditorUiFactory.TextColor);
            EditorUiFactory.SetWidth(valueText, 90f);
            return slider;
        }

        /// <summary>
        /// 키 설정 한 줄(이름 | 현재 키 | 변경 | 초기화)을 만든다.
        /// </summary>
        private static RebindRowView CreateRebindRow(Transform parent, KeyBindingCatalog.Entry entry)
        {
            RectTransform row = EditorUiFactory.CreateRow(parent, $"Rebind_{entry.actionName}_{entry.partName}", 42f, 10f);

            // 이름은 RebindRowView가 현재 언어로 채운다. (key를 넘기지 않음)
            Text label = EditorUiFactory.CreateText(row, "Label", entry.actionName, 22, TextAnchor.MiddleLeft, EditorUiFactory.TextColor);
            EditorUiFactory.SetFlexibleWidth(label);
            Text key = EditorUiFactory.CreateText(row, "Key", "-", 22, TextAnchor.MiddleCenter, EditorUiFactory.NoticeColor);
            EditorUiFactory.SetWidth(key, 260f);
            Button rebind = EditorUiFactory.CreateButton(row, "RebindButton", "options.rebind", EditorUiFactory.ButtonColor);
            EditorUiFactory.SetWidth(rebind, 110f);
            Button reset = EditorUiFactory.CreateButton(row, "ResetButton", "options.reset", EditorUiFactory.ButtonColor);
            EditorUiFactory.SetWidth(reset, 110f);

            RebindRowView view = row.gameObject.AddComponent<RebindRowView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("actionName").stringValue = entry.actionName;
            serialized.FindProperty("partName").stringValue = entry.partName ?? string.Empty;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            TestEnvironmentSetupTool.SetField(view, "labelText", label);
            TestEnvironmentSetupTool.SetField(view, "keyText", key);
            TestEnvironmentSetupTool.SetField(view, "rebindButton", rebind);
            TestEnvironmentSetupTool.SetField(view, "resetButton", reset);
            return view;
        }
    }
}
