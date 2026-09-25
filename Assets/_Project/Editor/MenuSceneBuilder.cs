using System;
using System.Linq;
using FishNet.Component.Scenes;
using FishNet.Managing;
using FishNet.Managing.Object;
using FishNet.Managing.Observing;
using FishNet.Observing;
using FishNet.Managing.Transporting;
using FishNet.Object;
using FishNet.Transporting;
using FishNet.Transporting.Multipass;
using FishNet.Transporting.Tugboat;
using GuardVsMannequin.Network;
using GuardVsMannequin.Room;
using GuardVsMannequin.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GuardVsMannequin.Editor
{
    /// <summary>
    /// 로비 씬(메인 메뉴 + 네트워크 루트)과 룸 씬(대기방)을 만든다.
    /// </summary>
    public static class MenuSceneBuilder
    {
        private const string FishySteamworksTypeName = "FishySteamworks.FishySteamworks";
        private const string SceneConditionPath = "Assets/FishNet/Runtime/Observing/Conditions/ScriptableObjects/SceneCondition.asset";

        /// <summary>
        /// 로비 씬을 만든다. 씬이 바뀌어도 유지되는 네트워크 루트(NetworkManager와 세션 서비스)를 포함한다.
        /// </summary>
        /// <param name="scenePath">저장할 경로.</param>
        /// <param name="roomScenePath">서버 시작 시 불러올 룸 씬 경로.</param>
        /// <param name="spawnablePrefabs">스폰 가능한 프리팹 목록.</param>
        public static void BuildLobbyScene(string scenePath, string roomScenePath, DefaultPrefabObjects spawnablePrefabs)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateMenuCamera();
            CreateNetworkRoot(scenePath, roomScenePath, spawnablePrefabs);
            EditorUiFactory.CreateEventSystem();
            CreateLobbyUi();

            EditorSceneManager.SaveScene(scene, scenePath);
        }

        /// <summary>
        /// 룸 씬을 만든다. 서버에서 방 상태를 스폰하는 부트스트랩과 룸 UI를 포함한다.
        /// </summary>
        public static void BuildRoomScene(string scenePath, NetworkObject roomStatePrefab)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateMenuCamera();
            EditorUiFactory.CreateEventSystem();

            RoomBootstrap bootstrap = new GameObject("RoomBootstrap").AddComponent<RoomBootstrap>();
            TestEnvironmentSetupTool.SetField(bootstrap, "roomStatePrefab", roomStatePrefab);

            CreateRoomUi();

            EditorSceneManager.SaveScene(scene, scenePath);
        }

        #region 네트워크 루트

        /// <summary>
        /// NetworkManager + Multipass(Tugboat, FishySteamworks) + DefaultScene + Steam/세션 서비스를 가진 루트를 만든다.
        /// NetworkManager가 DontDestroyOnLoad로 만들기 때문에 같은 오브젝트의 서비스도 씬 전환 후 유지된다.
        /// </summary>
        private static void CreateNetworkRoot(string lobbyScenePath, string roomScenePath, DefaultPrefabObjects spawnablePrefabs)
        {
            var root = new GameObject("NetworkRoot");

            NetworkManager networkManager = root.AddComponent<NetworkManager>();
            if (spawnablePrefabs != null)
                networkManager.SpawnablePrefabs = spawnablePrefabs;

            // 씬 오브젝트는 그 씬을 불러온 클라이언트에게만 보이게 한다.
            // 없으면 호스트가 게임 씬을 먼저 불러온 순간 로딩 중인 게스트에게 스폰 정보를 보내
            // 게스트가 GameSystems를 찾지 못한다(SceneId not found → HUD/문 동작 안 함).
            // 전역 오브젝트(RoomState)는 FishNet이 조건에서 자동으로 제외한다.
            ObserverManager observerManager = root.AddComponent<ObserverManager>();
            TestEnvironmentSetupTool.SetArray(observerManager, "_defaultConditions",
                new UnityEngine.Object[] { AssetDatabase.LoadAssetAtPath<ObserverCondition>(SceneConditionPath) });

            TransportManager transportManager = root.AddComponent<TransportManager>();
            Tugboat tugboat = root.AddComponent<Tugboat>();
            Transport steamTransport = AddFishySteamworks(root);

            Multipass multipass = root.AddComponent<Multipass>();
            Transport[] transports = steamTransport != null ? new Transport[] { tugboat, steamTransport } : new Transport[] { tugboat };
            TestEnvironmentSetupTool.SetArray(multipass, "_transports", transports);
            transportManager.Transport = multipass;

            // 서버가 시작되면 룸 씬을, 연결이 끊기면 로비 씬을 불러온다.
            DefaultScene defaultScene = root.AddComponent<DefaultScene>();
            var sceneSettings = new SerializedObject(defaultScene);
            sceneSettings.FindProperty("_offlineScene").stringValue = lobbyScenePath;
            sceneSettings.FindProperty("_onlineScene").stringValue = roomScenePath;
            sceneSettings.FindProperty("_startInOffline").boolValue = false;
            sceneSettings.ApplyModifiedPropertiesWithoutUndo();

            root.AddComponent<SteamBootstrap>();
            SteamLobbyService steamLobby = root.AddComponent<SteamLobbyService>();

            TransportConnector connector = root.AddComponent<TransportConnector>();
            TestEnvironmentSetupTool.SetField(connector, "networkManager", networkManager);

            SessionService session = root.AddComponent<SessionService>();
            TestEnvironmentSetupTool.SetField(session, "connector", connector);
            TestEnvironmentSetupTool.SetField(session, "steamLobby", steamLobby);
        }

        /// <summary>
        /// FishySteamworks 트랜스포트를 추가하고 P2P(SteamID 접속)를 켠다.
        /// FishySteamworks는 Assembly-CSharp에 있어 타입 이름으로 찾는다. 없으면 경고 후 null.
        /// </summary>
        private static Transport AddFishySteamworks(GameObject root)
        {
            Type steamType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(FishySteamworksTypeName))
                .FirstOrDefault(type => type != null);

            if (steamType == null)
            {
                Debug.LogWarning("[TestSetup] FishySteamworks를 찾지 못했습니다. Steam 방 없이 직접 접속만 사용합니다.");
                return null;
            }

            var transport = (Transport)root.AddComponent(steamType);
            var serialized = new SerializedObject(transport);
            SerializedProperty peerToPeer = serialized.FindProperty("_peerToPeer");
            if (peerToPeer != null)
            {
                peerToPeer.boolValue = true;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            return transport;
        }

        /// <summary>
        /// 메뉴 화면용 카메라를 만든다.
        /// </summary>
        private static void CreateMenuCamera()
        {
            var cameraObject = new GameObject("MenuCamera");
            cameraObject.tag = "MainCamera";
            Camera menuCamera = cameraObject.AddComponent<Camera>();
            menuCamera.clearFlags = CameraClearFlags.SolidColor;
            menuCamera.backgroundColor = new Color(0.05f, 0.05f, 0.07f);
            cameraObject.AddComponent<AudioListener>();
        }

        #endregion

        #region 로비 UI

        /// <summary>
        /// 로비 화면(제목, Steam 방, 직접 접속, 기타 메뉴, 안내 문구)을 만든다.
        /// </summary>
        private static void CreateLobbyUi()
        {
            Canvas canvas = EditorUiFactory.CreateCanvas("LobbyCanvas");
            canvas.gameObject.AddComponent<OsFontApplier>();

            Text title = EditorUiFactory.CreateText(canvas.transform, "Title", "가드 vs 마네킹", 72, TextAnchor.MiddleCenter, EditorUiFactory.TextColor);
            title.rectTransform.anchorMin = new Vector2(0f, 0.82f);
            title.rectTransform.anchorMax = new Vector2(1f, 0.97f);
            title.rectTransform.offsetMin = title.rectTransform.offsetMax = Vector2.zero;

            RectTransform panel = EditorUiFactory.CreatePanel(canvas.transform, "MenuPanel", new Vector2(0.3f, 0.06f), new Vector2(0.7f, 0.8f), EditorUiFactory.PanelColor);
            EditorUiFactory.AddVerticalLayout(panel, 14f, 32);

            // Steam
            AddHeader(panel, "Steam");
            Text steamStatus = AddLine(panel, "SteamStatus", "Steam 상태", 20, EditorUiFactory.TextColor);
            Button hostSteam = EditorUiFactory.CreateButton(panel, "HostSteamButton", "방 만들기", EditorUiFactory.AccentColor);
            EditorUiFactory.SetHeight(hostSteam, 60f);

            RectTransform codeRow = EditorUiFactory.CreateRow(panel, "RoomCodeRow", 56f, 10f);
            InputField roomCode = EditorUiFactory.CreateInputField(codeRow, "RoomCodeInput", "방 코드 입력");
            EditorUiFactory.SetFlexibleWidth(roomCode);
            Button joinSteam = EditorUiFactory.CreateButton(codeRow, "JoinSteamButton", "코드로 참가", EditorUiFactory.ButtonColor);
            EditorUiFactory.SetWidth(joinSteam, 200f);
            AddLine(panel, "InviteHint", "친구 초대는 Steam 친구 목록이나 오버레이에서 수락하면 자동으로 참가합니다.", 18, new Color(0.7f, 0.7f, 0.7f));

            // 직접 접속
            AddHeader(panel, "직접 접속 (개발 / LAN)");
            Button hostDirect = EditorUiFactory.CreateButton(panel, "HostDirectButton", "직접 방 만들기", EditorUiFactory.ButtonColor);
            EditorUiFactory.SetHeight(hostDirect, 52f);

            RectTransform addressRow = EditorUiFactory.CreateRow(panel, "AddressRow", 52f, 10f);
            InputField address = EditorUiFactory.CreateInputField(addressRow, "AddressInput", "호스트 IP (비우면 localhost)");
            EditorUiFactory.SetFlexibleWidth(address);
            Button joinDirect = EditorUiFactory.CreateButton(addressRow, "JoinDirectButton", "IP로 참가", EditorUiFactory.ButtonColor);
            EditorUiFactory.SetWidth(joinDirect, 200f);

            // 기타
            RectTransform menuRow = EditorUiFactory.CreateRow(panel, "MenuRow", 52f, 10f);
            Button settingsButton = EditorUiFactory.CreateButton(menuRow, "SettingsButton", "설정", EditorUiFactory.ButtonColor);
            Button creditsButton = EditorUiFactory.CreateButton(menuRow, "CreditsButton", "크레딧", EditorUiFactory.ButtonColor);
            Button quitButton = EditorUiFactory.CreateButton(menuRow, "QuitButton", "종료", EditorUiFactory.ButtonColor);
            foreach (Button button in new[] { settingsButton, creditsButton, quitButton })
                EditorUiFactory.SetFlexibleWidth(button);

            Text notice = AddLine(panel, "Notice", string.Empty, 22, EditorUiFactory.NoticeColor);
            EditorUiFactory.SetHeight(notice, 60f);

            LobbyView view = canvas.gameObject.AddComponent<LobbyView>();
            TestEnvironmentSetupTool.SetField(view, "hostSteamButton", hostSteam);
            TestEnvironmentSetupTool.SetField(view, "roomCodeInput", roomCode);
            TestEnvironmentSetupTool.SetField(view, "joinSteamButton", joinSteam);
            TestEnvironmentSetupTool.SetField(view, "steamStatusText", steamStatus);
            TestEnvironmentSetupTool.SetField(view, "hostDirectButton", hostDirect);
            TestEnvironmentSetupTool.SetField(view, "addressInput", address);
            TestEnvironmentSetupTool.SetField(view, "joinDirectButton", joinDirect);
            TestEnvironmentSetupTool.SetField(view, "settingsButton", settingsButton);
            TestEnvironmentSetupTool.SetField(view, "creditsButton", creditsButton);
            TestEnvironmentSetupTool.SetField(view, "quitButton", quitButton);
            TestEnvironmentSetupTool.SetField(view, "noticeText", notice);
        }

        #endregion

        #region 룸 UI

        /// <summary>
        /// 룸 화면(참가자·방 코드 / 게임 설정 / 하단 버튼)을 만든다.
        /// </summary>
        private static void CreateRoomUi()
        {
            Canvas canvas = EditorUiFactory.CreateCanvas("RoomCanvas");
            canvas.gameObject.AddComponent<OsFontApplier>();

            // 왼쪽: 참가자와 방 코드
            RectTransform left = EditorUiFactory.CreatePanel(canvas.transform, "MembersPanel", new Vector2(0.03f, 0.16f), new Vector2(0.45f, 0.95f), EditorUiFactory.PanelColor);
            EditorUiFactory.AddVerticalLayout(left, 14f, 28);
            AddHeader(left, "참가자");
            Text memberList = AddLine(left, "MemberList", string.Empty, 26, EditorUiFactory.TextColor);
            EditorUiFactory.SetHeight(memberList, 420f);

            Text roomCode = AddLine(left, "RoomCode", "방 코드:", 22, EditorUiFactory.TextColor);
            RectTransform codeRow = EditorUiFactory.CreateRow(left, "CodeButtons", 52f, 10f);
            Button copyButton = EditorUiFactory.CreateButton(codeRow, "CopyCodeButton", "코드 복사", EditorUiFactory.ButtonColor);
            Button inviteButton = EditorUiFactory.CreateButton(codeRow, "InviteButton", "친구 초대", EditorUiFactory.ButtonColor);
            EditorUiFactory.SetFlexibleWidth(copyButton);
            EditorUiFactory.SetFlexibleWidth(inviteButton);

            // 오른쪽: 게임 설정
            RectTransform right = EditorUiFactory.CreatePanel(canvas.transform, "SettingsPanel", new Vector2(0.47f, 0.16f), new Vector2(0.97f, 0.95f), EditorUiFactory.PanelColor);
            EditorUiFactory.AddVerticalLayout(right, 6f, 24);
            Text settingsHint = AddLine(right, "SettingsHint", "게임 설정", 28, EditorUiFactory.TextColor);

            SettingRowView[] rows = GameSettingDefinitions.All.Select(definition => CreateSettingRow(right, definition.id)).ToArray();

            // 하단: 상태, 나가기, 시작/준비
            RectTransform bottom = EditorUiFactory.CreatePanel(canvas.transform, "BottomBar", new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.13f), EditorUiFactory.PanelColor);
            HorizontalLayoutGroup bottomLayout = bottom.gameObject.AddComponent<HorizontalLayoutGroup>();
            bottomLayout.padding = new RectOffset(20, 20, 14, 14);
            bottomLayout.spacing = 16f;
            bottomLayout.childControlWidth = true;
            bottomLayout.childControlHeight = true;
            bottomLayout.childForceExpandWidth = false;

            Text status = EditorUiFactory.CreateText(bottom, "Status", string.Empty, 24, TextAnchor.MiddleLeft, EditorUiFactory.NoticeColor);
            EditorUiFactory.SetFlexibleWidth(status);
            Button leaveButton = EditorUiFactory.CreateButton(bottom, "LeaveButton", "나가기", EditorUiFactory.ButtonColor);
            EditorUiFactory.SetWidth(leaveButton, 220f);
            Button primaryButton = EditorUiFactory.CreateButton(bottom, "PrimaryButton", "준비", EditorUiFactory.AccentColor, out Text primaryLabel);
            EditorUiFactory.SetWidth(primaryButton, 320f);

            RoomView view = canvas.gameObject.AddComponent<RoomView>();
            TestEnvironmentSetupTool.SetField(view, "memberListText", memberList);
            TestEnvironmentSetupTool.SetField(view, "roomCodeText", roomCode);
            TestEnvironmentSetupTool.SetField(view, "copyCodeButton", copyButton);
            TestEnvironmentSetupTool.SetField(view, "inviteButton", inviteButton);
            TestEnvironmentSetupTool.SetField(view, "settingsHintText", settingsHint);
            TestEnvironmentSetupTool.SetArray(view, "settingRows", rows);
            TestEnvironmentSetupTool.SetField(view, "primaryButton", primaryButton);
            TestEnvironmentSetupTool.SetField(view, "primaryButtonLabel", primaryLabel);
            TestEnvironmentSetupTool.SetField(view, "leaveButton", leaveButton);
            TestEnvironmentSetupTool.SetField(view, "statusText", status);
        }

        /// <summary>
        /// 설정 한 줄(이름 | - | 값 | +)을 만든다.
        /// </summary>
        private static SettingRowView CreateSettingRow(Transform parent, GameSettingId id)
        {
            RectTransform row = EditorUiFactory.CreateRow(parent, $"Row_{id}", 40f, 8f);

            Text label = EditorUiFactory.CreateText(row, "Label", id.ToString(), 22, TextAnchor.MiddleLeft, EditorUiFactory.TextColor);
            EditorUiFactory.SetFlexibleWidth(label);

            Button decrease = EditorUiFactory.CreateButton(row, "Decrease", "-", EditorUiFactory.ButtonColor);
            EditorUiFactory.SetWidth(decrease, 48f);

            Text value = EditorUiFactory.CreateText(row, "Value", "-", 22, TextAnchor.MiddleCenter, EditorUiFactory.TextColor);
            EditorUiFactory.SetWidth(value, 140f);

            Button increase = EditorUiFactory.CreateButton(row, "Increase", "+", EditorUiFactory.ButtonColor);
            EditorUiFactory.SetWidth(increase, 48f);

            SettingRowView view = row.gameObject.AddComponent<SettingRowView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("settingId").enumValueIndex = (int)id;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            TestEnvironmentSetupTool.SetField(view, "labelText", label);
            TestEnvironmentSetupTool.SetField(view, "valueText", value);
            TestEnvironmentSetupTool.SetField(view, "decreaseButton", decrease);
            TestEnvironmentSetupTool.SetField(view, "increaseButton", increase);
            return view;
        }

        #endregion

        /// <summary>
        /// 구역 제목을 추가한다.
        /// </summary>
        private static void AddHeader(Transform parent, string content)
        {
            Text header = EditorUiFactory.CreateText(parent, "Header", content, 30, TextAnchor.MiddleLeft, EditorUiFactory.AccentColor);
            header.fontStyle = FontStyle.Bold;
            EditorUiFactory.SetHeight(header, 40f);
        }

        /// <summary>
        /// 한 줄 텍스트를 추가한다.
        /// </summary>
        private static Text AddLine(Transform parent, string name, string content, int fontSize, Color color)
        {
            Text text = EditorUiFactory.CreateText(parent, name, content, fontSize, TextAnchor.MiddleLeft, color);
            EditorUiFactory.SetHeight(text, fontSize + 14f);
            return text;
        }
    }
}
