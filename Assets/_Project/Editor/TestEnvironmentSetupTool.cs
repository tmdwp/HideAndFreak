using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ECM2;
using FishNet.Component.Transforming;
using FishNet.Managing;
using FishNet.Managing.Object;
using FishNet.Managing.Transporting;
using FishNet.Object;
using FishNet.Transporting.Tugboat;
using GuardVsMannequin.AI;
using GuardVsMannequin.Audio;
using GuardVsMannequin.Cameras;
using GuardVsMannequin.Core;
using GuardVsMannequin.Localization;
using GuardVsMannequin.Map;
using GuardVsMannequin.Network;
using GuardVsMannequin.Objectives;
using GuardVsMannequin.Player;
using GuardVsMannequin.Player.Guard;
using GuardVsMannequin.Player.Emotes;
using GuardVsMannequin.Player.Mannequin;
using GuardVsMannequin.Props;
using GuardVsMannequin.Room;
using GuardVsMannequin.Rules;
using GuardVsMannequin.UI;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace GuardVsMannequin.Editor
{
    /// <summary>
    /// 테스트 환경(머티리얼, 설정/사물 데이터, 프리팹, 테스트 씬)을 메뉴 한 번으로 생성한다.
    /// 다시 실행하면 프리팹과 씬을 덮어써서 항상 같은 결과를 만든다.
    /// (게임 설정·문장·크레딧은 Resources 폴더의 텍스트 파일이라 이 도구가 건드리지 않는다)
    /// </summary>
    public static class TestEnvironmentSetupTool
    {
        private const string RootFolder = "Assets/_Project";
        private const string MaterialFolder = RootFolder + "/Art/Materials";
        private const string PrefabFolder = RootFolder + "/Prefabs";
        private const string SoFolder = RootFolder + "/ScriptableObjects";
        private const string SceneFolder = RootFolder + "/Scenes";

        private const string InputActionsPath = RootFolder + "/Input/GameInput.inputactions";
        private const string PropDatabasePath = SoFolder + "/PropDatabase_Test.asset";
        private const string GuardPrefabPath = PrefabFolder + "/Guard.prefab";
        private const string MannequinPrefabPath = PrefabFolder + "/Mannequin.prefab";
        private const string GeneratorPrefabPath = PrefabFolder + "/Generator.prefab";
        private const string FakeMannequinPrefabPath = PrefabFolder + "/FakeMannequin.prefab";
        private const string NoiseLibraryPath = SoFolder + "/NoiseLibrary.asset";
        private const string ScenePath = SceneFolder + "/Game_Test.unity";
        private const string LobbyScenePath = SceneFolder + "/Lobby.unity";
        private const string RoomScenePath = SceneFolder + "/Room.unity";
        private const string RoomStatePrefabPath = PrefabFolder + "/RoomState.prefab";
        private const string DefaultPrefabObjectsPath = "Assets/DefaultPrefabObjects.asset";

        private const float MapHalfSize = 20f;
        private const float WallHeight = 4f;
        private const float WallThickness = 0.5f;

        /// <summary>벽으로 변신할 때 사용하는 고정 크기 벽 조각.</summary>
        private static readonly Vector3 WallPieceScale = new Vector3(1.2f, 2f, 0.25f);

        /// <summary>테스트 맵에 배치할 변신 가능 사물(종류, 위치, 크기).</summary>
        private static readonly (PrimitiveType type, Vector3 position, Vector3 scale)[] PropLayout =
        {
            (PrimitiveType.Cube, new Vector3(-8f, 0.5f, 5f), new Vector3(1f, 1f, 1f)),
            (PrimitiveType.Cube, new Vector3(-6f, 0.75f, -7f), new Vector3(2f, 1.5f, 1f)),
            (PrimitiveType.Cube, new Vector3(5f, 0.4f, -4f), new Vector3(1.5f, 0.8f, 0.8f)),
            (PrimitiveType.Cube, new Vector3(0f, 1f, 8f), new Vector3(3f, 2f, 0.6f)),
            (PrimitiveType.Cylinder, new Vector3(8f, 0.6f, 3f), new Vector3(0.6f, 0.6f, 0.6f)),
            (PrimitiveType.Cylinder, new Vector3(-12f, 1f, -12f), new Vector3(1f, 1f, 1f)),
            (PrimitiveType.Cylinder, new Vector3(3f, 0.5f, -13f), new Vector3(0.5f, 0.5f, 0.5f)),
            (PrimitiveType.Sphere, new Vector3(-3f, 0.4f, 0f), new Vector3(0.8f, 0.8f, 0.8f)),
            (PrimitiveType.Sphere, new Vector3(10f, 0.3f, -10f), new Vector3(0.6f, 0.6f, 0.6f)),
            (PrimitiveType.Cube, new Vector3(-15f, 0.9f, 10f), new Vector3(1.2f, 1.8f, 0.6f)),
        };

        /// <summary>전원 장치 후보 위치. (대기실 바깥)</summary>
        private static readonly Vector3[] GeneratorPositions =
        {
            new Vector3(-18f, 0f, -18f), new Vector3(-18f, 0f, 5f), new Vector3(-10f, 0f, 18f),
            new Vector3(0f, 0f, -18f), new Vector3(5f, 0f, 3f), new Vector3(18f, 0f, -18f),
            new Vector3(18f, 0f, 0f), new Vector3(-5f, 0f, 12f)
        };

        /// <summary>
        /// 전체 테스트 환경을 생성한다.
        /// </summary>
        [MenuItem("GuardVsMannequin/테스트 환경 생성")]
        public static void BuildAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EnsureFolders();

            // 텍스트 데이터 파일(문장, 설정 항목)을 수정한 뒤 바로 실행해도 최신 내용으로 UI를 만든다.
            Loc.Reload();
            GameSettingDefinitions.ClearCache();

            Dictionary<string, Material> materials = CreateMaterials();
            InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (inputActions == null)
            {
                Debug.LogError($"[TestSetup] 입력 에셋을 찾을 수 없습니다: {InputActionsPath}");
                return;
            }

            PropDatabase propDatabase = CreatePropDatabase(materials, out int[] layoutPropIds, out int wallPieceId);
            NoiseLibrary noiseLibrary = CreateNoiseLibrary();

            var prefabs = new ScenePrefabs
            {
                guard = CreateGuardPrefab(inputActions, materials, noiseLibrary),
                mannequin = CreateMannequinPrefab(inputActions, materials, propDatabase, noiseLibrary),
                generator = CreateGeneratorPrefab(materials, noiseLibrary),
                fakeMannequin = CreateFakeMannequinPrefab(materials)
            };
            NetworkObject roomStatePrefab = CreateRoomStatePrefab();
            RegisterSpawnablePrefabs(prefabs.guard, prefabs.mannequin, prefabs.generator, prefabs.fakeMannequin, roomStatePrefab);

            // 게임 씬 → 룸 씬 → 로비 씬(시작 씬) 순서로 만든다. 마지막에 로비 씬이 열린 상태로 끝난다.
            CreateTestScene(prefabs, materials, layoutPropIds, wallPieceId, noiseLibrary);
            MenuSceneBuilder.BuildRoomScene(RoomScenePath, AssetDatabase.LoadAssetAtPath<NetworkObject>(RoomStatePrefabPath));
            MenuSceneBuilder.BuildLobbyScene(LobbyScenePath, RoomScenePath,
                AssetDatabase.LoadAssetAtPath<DefaultPrefabObjects>(DefaultPrefabObjectsPath));

            SetBuildScenes(LobbyScenePath, RoomScenePath, ScenePath);
            KeepLinearFogShaderVariants();
            SteamBuildProcessor.EnsureRunInBackground();

            AssetDatabase.SaveAssets();
            Debug.Log("[TestSetup] 테스트 환경 생성 완료. 로비 씬에서 Play 하세요: " + LobbyScenePath);
        }

        /// <summary>
        /// 룸 상태 프리팹을 만든다. 전역(Global) 네트워크 오브젝트라 씬이 바뀌어도 유지된다.
        /// (기본 게임 설정은 Resources/Data/default_game_settings.json에서 읽는다)
        /// </summary>
        private static NetworkObject CreateRoomStatePrefab()
        {
            var root = new GameObject("RoomState");
            NetworkObject networkObject = root.AddComponent<NetworkObject>();
            var serialized = new SerializedObject(networkObject);
            serialized.FindProperty("_isGlobal").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            root.AddComponent<RoomState>();
            return SavePrefab(root, RoomStatePrefabPath);
        }

        #region 에셋

        /// <summary>
        /// 필요한 폴더를 만든다.
        /// </summary>
        private static void EnsureFolders()
        {
            foreach (string folder in new[] { MaterialFolder, PrefabFolder, SoFolder, SceneFolder })
                Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 테스트용 단색 URP 머티리얼을 만든다.
        /// </summary>
        private static Dictionary<string, Material> CreateMaterials()
        {
            var litColors = new Dictionary<string, Color>
            {
                { "Guard", new Color(0.2f, 0.35f, 0.8f) },
                { "Mannequin", new Color(0.9f, 0.8f, 0.65f) },
                { "Visor", new Color(0.1f, 0.1f, 0.1f) },
                { "Floor", new Color(0.45f, 0.45f, 0.45f) },
                { "Wall", new Color(0.75f, 0.75f, 0.72f) },
                { "WaitingRoom", new Color(0.55f, 0.65f, 0.8f) },
                { "Door", new Color(0.8f, 0.3f, 0.2f) },
                { "Prop", new Color(0.55f, 0.4f, 0.3f) },
                { "Generator", new Color(0.2f, 0.6f, 0.3f) },
                { "GeneratorDestroyed", new Color(0.15f, 0.15f, 0.15f) }
            };
            var unlitColors = new Dictionary<string, Color>
            {
                { "Tracer", new Color(1f, 0.9f, 0.3f) },
                { "ProgressBackground", new Color(0.1f, 0.1f, 0.1f) },
                { "ProgressFill", new Color(1f, 0.3f, 0.2f) }
            };

            var result = new Dictionary<string, Material>();
            CreateMaterialSet(litColors, Shader.Find("Universal Render Pipeline/Lit"), result);
            CreateMaterialSet(unlitColors, Shader.Find("Universal Render Pipeline/Unlit"), result);

            // 조명 기구 전구: 켜짐은 발광, 꺼짐은 어두운 유리
            CreateMaterialSet(new Dictionary<string, Color> { { "LampOff", new Color(0.2f, 0.2f, 0.22f) } }, Shader.Find("Universal Render Pipeline/Lit"), result);
            CreateMaterialSet(new Dictionary<string, Color> { { "LampOn", new Color(1f, 0.97f, 0.9f) } }, Shader.Find("Universal Render Pipeline/Lit"), result);
            Material lampOn = result["LampOn"];
            lampOn.EnableKeyword("_EMISSION");
            lampOn.SetColor("_EmissionColor", new Color(1f, 0.95f, 0.85f) * 3f);
            lampOn.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(lampOn);
            return result;
        }

        /// <summary>
        /// 셰이더와 색 목록으로 머티리얼을 만들거나 갱신한다.
        /// </summary>
        private static void CreateMaterialSet(Dictionary<string, Color> colors, Shader shader, Dictionary<string, Material> result)
        {
            foreach (KeyValuePair<string, Color> pair in colors)
            {
                string path = $"{MaterialFolder}/M_{pair.Key}.mat";
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(shader);
                    AssetDatabase.CreateAsset(material, path);
                }
                material.shader = shader;
                material.SetColor("_BaseColor", pair.Value);
                EditorUtility.SetDirty(material);
                result[pair.Key] = material;
            }
        }

        /// <summary>
        /// 씬에서 스폰하는 네트워크 프리팹 묶음.
        /// </summary>
        private struct ScenePrefabs
        {
            public NetworkObject guard;
            public NetworkObject mannequin;
            public NetworkObject generator;
            public NetworkObject fakeMannequin;
        }

        /// <summary>
        /// 소음 라이브러리를 만든다. 이미 있으면 사용자가 지정한 클립과 값은 유지하고 새 소음 종류만 추가한다.
        /// </summary>
        private static NoiseLibrary CreateNoiseLibrary()
        {
            NoiseLibrary library = AssetDatabase.LoadAssetAtPath<NoiseLibrary>(NoiseLibraryPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<NoiseLibrary>();
                AssetDatabase.CreateAsset(library, NoiseLibraryPath);
            }

            if (library.AddMissingDefaults())
                EditorUtility.SetDirty(library);
            return library;
        }

        /// <summary>
        /// 테스트 맵의 사물 배치와 벽 조각으로 PropDatabase를 만든다.
        /// </summary>
        /// <param name="materials">사물 머티리얼.</param>
        /// <param name="layoutPropIds">PropLayout 순서대로 배정된 PropId.</param>
        /// <param name="wallPieceId">벽 조각 PropId.</param>
        private static PropDatabase CreatePropDatabase(Dictionary<string, Material> materials, out int[] layoutPropIds, out int wallPieceId)
        {
            PropDatabase database = AssetDatabase.LoadAssetAtPath<PropDatabase>(PropDatabasePath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<PropDatabase>();
                AssetDatabase.CreateAsset(database, PropDatabasePath);
            }
            database.Clear();

            layoutPropIds = new int[PropLayout.Length];
            for (int i = 0; i < PropLayout.Length; i++)
            {
                layoutPropIds[i] = database.Add(new PropDefinition
                {
                    displayName = $"{PropLayout[i].type}_{i:00}",
                    mesh = GetPrimitiveMesh(PropLayout[i].type),
                    materials = new[] { materials["Prop"] },
                    scale = PropLayout[i].scale
                });
            }

            wallPieceId = database.Add(new PropDefinition
            {
                displayName = "WallPiece",
                mesh = GetPrimitiveMesh(PrimitiveType.Cube),
                materials = new[] { materials["Wall"] },
                scale = WallPieceScale
            });

            EditorUtility.SetDirty(database);
            return database;
        }

        /// <summary>
        /// Unity 기본 도형의 메시를 가져온다.
        /// </summary>
        private static Mesh GetPrimitiveMesh(PrimitiveType type)
        {
            GameObject temp = GameObject.CreatePrimitive(type);
            Mesh mesh = temp.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(temp);
            return mesh;
        }

        /// <summary>
        /// 스폰할 프리팹을 FishNet의 DefaultPrefabObjects에 등록한다.
        /// </summary>
        private static void RegisterSpawnablePrefabs(params NetworkObject[] prefabs)
        {
            var collection = AssetDatabase.LoadAssetAtPath<DefaultPrefabObjects>(DefaultPrefabObjectsPath);
            if (collection == null)
            {
                Debug.LogWarning("[TestSetup] DefaultPrefabObjects를 찾지 못했습니다. FishNet이 자동으로 생성/등록하는지 확인하세요.");
                return;
            }

            foreach (NetworkObject prefab in prefabs)
                collection.AddObject(prefab, checkForDuplicates: true, initializeAdded: false);

            // 같은 경로에 프리팹을 덮어쓰면 NetworkObject가 새로 만들어져 AssetPathHash가 0이 된다.
            // 이미 등록된 프리팹이라 FishNet 자동 생성기가 해시를 다시 계산하지 않으므로 직접 갱신한다.
            RefreshAssetPathHashes(collection);
            EditorUtility.SetDirty(collection);
        }

        /// <summary>
        /// FishNet 내부의 DefaultPrefabObjects.SetAssetPathHashes(int)를 호출해 모든 프리팹의 해시를 다시 계산한다.
        /// 공개 API가 없어 리플렉션을 사용하며, 실패하면 FishNet 메뉴를 안내한다.
        /// </summary>
        private static void RefreshAssetPathHashes(DefaultPrefabObjects collection)
        {
            MethodInfo method = typeof(DefaultPrefabObjects).GetMethod("SetAssetPathHashes", BindingFlags.Instance | BindingFlags.NonPublic,
                null, new[] { typeof(int) }, null);

            if (method == null)
            {
                Debug.LogError("[TestSetup] FishNet 프리팹 해시 갱신 메서드를 찾지 못했습니다. " +
                               "Tools > Fish-Networking > Utility > Reserialize NetworkObjects에서 프리팹을 재직렬화하세요.");
                return;
            }

            method.Invoke(collection, new object[] { 0 });
            AssetDatabase.SaveAssets();
        }

        #endregion

        #region 프리팹

        /// <summary>
        /// 1인칭 가드 프리팹을 만든다.
        /// </summary>
        private static NetworkObject CreateGuardPrefab(InputActionAsset inputActions, Dictionary<string, Material> materials, NoiseLibrary noiseLibrary)
        {
            GameObject root = CreateAvatarRoot("Guard", inputActions, out GuardCharacter character, out PlayerInputReader inputReader, out PlayerMovementDriver driver);

            Renderer body = CreateVisual(root.transform, PrimitiveType.Capsule, "Body", new Vector3(0f, 0.9f, 0f), new Vector3(0.8f, 0.9f, 0.8f), materials["Guard"]);
            Renderer visor = CreateVisual(root.transform, PrimitiveType.Cube, "Visor", new Vector3(0f, 1.55f, 0.3f), new Vector3(0.5f, 0.15f, 0.2f), materials["Visor"]);

            Transform cameraPivot = new GameObject("CameraPivot").transform;
            cameraPivot.SetParent(root.transform, false);
            cameraPivot.localPosition = new Vector3(0f, 1.65f, 0f);
            Camera playerCamera = CreatePlayerCamera(cameraPivot, Vector3.zero);

            SetField(character, "cameraPivot", cameraPivot);
            ConfigureOwnership(root, character, playerCamera, new Behaviour[] { inputReader, driver }, new[] { body, visor });
            AddMoveSpeedController(root, PlayerRole.Guard);

            // 사격: 조준 → 소음 → 총 → 궤적 표시
            root.AddComponent<CameraAimProvider>();
            AddNoiseEmitter(root, noiseLibrary);
            GunTool gun = root.AddComponent<GunTool>();
            SetField(gun, "tracerView", CreateTracer(root.transform, cameraPivot, materials["Tracer"]));

            // 탐지기: 반경 파동 표시
            DetectorTool detector = root.AddComponent<DetectorTool>();
            SetField(detector, "pulseView", CreateDetectorPulse(root.transform, materials["Tracer"]));

            // Phase 3: 상하 시선 동기화, 사냥 시간 손전등
            AddViewSync(root, cameraPivot, cameraPivot, isThirdPerson: false);
            FlashlightTool flashlight = root.AddComponent<FlashlightTool>();
            SetField(flashlight, "spotLight", CreateFlashlight(cameraPivot));

            return SavePrefab(root, GuardPrefabPath);
        }

        /// <summary>
        /// 카메라 피벗 아래에 손전등용 스포트라이트를 만든다. (기본 꺼짐)
        /// </summary>
        private static Light CreateFlashlight(Transform cameraPivot)
        {
            var flashlightObject = new GameObject("Flashlight");
            flashlightObject.transform.SetParent(cameraPivot, false);
            flashlightObject.transform.localPosition = new Vector3(0.2f, -0.15f, 0.2f);

            Light spot = flashlightObject.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.range = 25f;
            spot.spotAngle = 55f;
            spot.innerSpotAngle = 30f;
            spot.intensity = 8f;
            spot.color = new Color(1f, 0.95f, 0.85f);
            spot.shadows = LightShadows.Soft;
            spot.enabled = false;
            return spot;
        }

        /// <summary>
        /// 3인칭 마네킹 프리팹을 만든다.
        /// </summary>
        private static NetworkObject CreateMannequinPrefab(InputActionAsset inputActions, Dictionary<string, Material> materials,
            PropDatabase propDatabase, NoiseLibrary noiseLibrary)
        {
            GameObject root = CreateAvatarRoot("Mannequin", inputActions, out MannequinCharacter character, out PlayerInputReader inputReader, out PlayerMovementDriver driver);

            // 가짜 마네킹과 완전히 같은 외형이어야 한다. (CreateMannequinVisual 공용)
            Transform visual = CreateMannequinVisual(root.transform, materials, out Renderer body, out Renderer face);

            Transform followTarget = new GameObject("FollowTarget").transform;
            followTarget.SetParent(root.transform, false);
            followTarget.localPosition = new Vector3(0f, 1.5f, 0f);
            Camera playerCamera = CreatePlayerCamera(root.transform, new Vector3(0f, 2.5f, -3.5f));

            SetField(character, "followTarget", followTarget);
            ConfigureOwnership(root, character, playerCamera, new Behaviour[] { inputReader, driver }, new Renderer[0]);
            AddMoveSpeedController(root, PlayerRole.Mannequin);

            // 변신: 조준 → 변신 상태 → 외형 적용, 상호작용
            root.AddComponent<CameraAimProvider>();
            MannequinTransformer transformer = root.AddComponent<MannequinTransformer>();
            SetField(transformer, "propDatabase", propDatabase);

            PropDisguiseView disguiseView = root.AddComponent<PropDisguiseView>();
            SetField(disguiseView, "characterMovement", root.GetComponent<CharacterMovement>());
            SetArray(disguiseView, "bodyRenderers", new Object[] { body, face });

            root.AddComponent<MannequinInteractor>();

            // Phase 2: 소음(탐지/변신 중 주기 소음), 벽타기, 감정 표현
            AddNoiseEmitter(root, noiseLibrary);
            root.AddComponent<DisguiseNoiseEmitter>();
            root.AddComponent<WallClimbAbility>();
            root.AddComponent<DisguiseRotationAbility>();
            AddEmotes(root, visual);

            // Phase 3: 사냥 시간 모드(1인칭, 체력 배율, 속도), 근접 공격
            MannequinHuntingMode huntingMode = root.AddComponent<MannequinHuntingMode>();
            SetArray(huntingMode, "bodyRenderers", new Object[] { body, face });

            // 사냥 시간 시야: 눈 높이의 개인 조명(본인 화면에만 켜짐) + 로컬 안개
            HuntVisionEffect huntVision = root.AddComponent<HuntVisionEffect>();
            SetField(huntVision, "visionLight", CreateHuntVisionLight(followTarget));
            root.AddComponent<MannequinAttackTool>();

            // Phase 4: 시선 동기화와 관전 시점 (상하 시선을 적용할 피벗은 없음)
            AddViewSync(root, null, followTarget, isThirdPerson: true);

            return SavePrefab(root, MannequinPrefabPath);
        }

        /// <summary>
        /// 게임 설정의 이동 속도를 적용하는 컴포넌트를 역할과 함께 추가한다.
        /// </summary>
        private static void AddMoveSpeedController(GameObject root, PlayerRole role)
        {
            MoveSpeedController speedController = root.AddComponent<MoveSpeedController>();
            var serialized = new SerializedObject(speedController);
            serialized.FindProperty("role").enumValueIndex = (int)role;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // 소유자 권한 이동을 서버에서 검증한다. (스피드핵, 순간이동, 대기실 탈출 방지)
            root.AddComponent<MovementValidator>();
        }

        /// <summary>
        /// 시선 동기화(PlayerViewSync)와 관전 시점(SpectatorViewPoint)을 추가한다.
        /// </summary>
        /// <param name="root">아바타 루트.</param>
        /// <param name="pitchPivot">원격 피어에서 상하 시선을 적용할 피벗. (가드의 카메라 피벗, 없으면 null)</param>
        /// <param name="eyePoint">관전 카메라의 눈 위치.</param>
        /// <param name="isThirdPerson">평소 3인칭으로 보는 역할인지 여부.</param>
        private static void AddViewSync(GameObject root, Transform pitchPivot, Transform eyePoint, bool isThirdPerson)
        {
            PlayerViewSync viewSync = root.AddComponent<PlayerViewSync>();
            SetField(viewSync, "pitchPivot", pitchPivot);

            SpectatorViewPoint viewPoint = root.AddComponent<SpectatorViewPoint>();
            SetField(viewPoint, "eyePoint", eyePoint);
            var serialized = new SerializedObject(viewPoint);
            serialized.FindProperty("isThirdPerson").boolValue = isThirdPerson;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// 사냥 시간 마네킹의 개인 조명을 만든다. 그림자 없이 주변만 은은하게 비춘다. (기본 꺼짐)
        /// </summary>
        private static Light CreateHuntVisionLight(Transform followTarget)
        {
            var lightObject = new GameObject("HuntVisionLight");
            lightObject.transform.SetParent(followTarget, false);

            Light point = lightObject.AddComponent<Light>();
            point.type = LightType.Point;
            point.range = 8f;
            point.intensity = 1.5f;
            point.color = new Color(0.65f, 0.75f, 0.95f);
            point.shadows = LightShadows.None;
            point.enabled = false;
            return point;
        }

        /// <summary>
        /// 가짜 마네킹 프리팹을 만든다. 외형, 충돌체 크기, 감정 표현은 실제 마네킹과 같다.
        /// NavMeshAgent는 서버에서만 켜지고, 위치는 서버 권한 NetworkTransform으로 동기화된다.
        /// </summary>
        private static NetworkObject CreateFakeMannequinPrefab(Dictionary<string, Material> materials)
        {
            var root = new GameObject("FakeMannequin");

            CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0f, 0.9f, 0f);
            capsule.radius = 0.4f;
            capsule.height = 1.8f;

            NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
            agent.radius = 0.4f;
            agent.height = 1.8f;
            // 실제 속도는 FakeMannequinBrain이 게임 설정(마네킹 이동 속도)으로 다시 맞춘다.
            agent.speed = 1.5f;
            agent.angularSpeed = 540f;
            agent.acceleration = 20f;
            agent.stoppingDistance = 0.1f;
            agent.enabled = false;

            root.AddComponent<NetworkObject>();
            root.AddComponent<NetworkTransform>();
            root.AddComponent<FakeMannequin>();

            Transform visual = CreateMannequinVisual(root.transform, materials, out _, out _);
            AddEmotes(root, visual);
            root.AddComponent<FakeMannequinBrain>();

            return SavePrefab(root, FakeMannequinPrefabPath);
        }

        /// <summary>
        /// 마네킹 외형(본체 + 얼굴 표시)을 감정 표현용 컨테이너 아래에 만든다.
        /// 실제/가짜 마네킹이 같은 함수를 써서 겉모습이 완전히 같도록 한다.
        /// </summary>
        private static Transform CreateMannequinVisual(Transform parent, Dictionary<string, Material> materials, out Renderer body, out Renderer face)
        {
            Transform visual = new GameObject("Visual").transform;
            visual.SetParent(parent, false);

            body = CreateVisual(visual, PrimitiveType.Capsule, "Body", new Vector3(0f, 0.9f, 0f), new Vector3(0.8f, 0.9f, 0.8f), materials["Mannequin"]);
            face = CreateVisual(visual, PrimitiveType.Cube, "FaceMarker", new Vector3(0f, 1.5f, 0.3f), new Vector3(0.3f, 0.1f, 0.2f), materials["Visor"]);
            return visual;
        }

        /// <summary>
        /// 감정 표현 뷰와 네트워크 컨트롤러를 추가한다.
        /// </summary>
        private static void AddEmotes(GameObject root, Transform visual)
        {
            ProceduralEmoteView emoteView = root.AddComponent<ProceduralEmoteView>();
            SetField(emoteView, "body", visual);
            root.AddComponent<EmoteController>();
        }

        /// <summary>
        /// 소음 재생 컴포넌트를 추가하고 라이브러리를 연결한다.
        /// </summary>
        private static void AddNoiseEmitter(GameObject root, NoiseLibrary noiseLibrary)
        {
            NoiseEmitter emitter = root.AddComponent<NoiseEmitter>();
            SetField(emitter, "library", noiseLibrary);
        }

        /// <summary>
        /// 탐지기 반경 파동 표시용 LineRenderer를 만든다.
        /// </summary>
        private static DetectorPulseView CreateDetectorPulse(Transform parent, Material material)
        {
            var pulseObject = new GameObject("DetectorPulse");
            pulseObject.transform.SetParent(parent, false);

            LineRenderer line = pulseObject.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.startWidth = 0.06f;
            line.endWidth = 0.06f;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.enabled = false;

            return pulseObject.AddComponent<DetectorPulseView>();
        }

        /// <summary>
        /// 모든 아바타에 공통인 네트워크/이동/입력/체력 컴포넌트를 가진 루트를 만든다.
        /// 입력과 드라이버는 소유자에게만 켜지도록 비활성 상태로 둔다.
        /// PlayerMovementDriver는 [RequireComponent(typeof(Character))]이므로, 실제 캐릭터를 먼저 추가해야
        /// Unity가 기본 Character를 자동으로 덧붙이지 않는다.
        /// </summary>
        private static GameObject CreateAvatarRoot<TCharacter>(string name, InputActionAsset inputActions, out TCharacter character,
            out PlayerInputReader inputReader, out PlayerMovementDriver driver) where TCharacter : Character
        {
            var root = new GameObject(name);

            CharacterMovement movement = root.AddComponent<CharacterMovement>();
            movement.SetDimensions(0.4f, 1.8f);

            character = root.AddComponent<TCharacter>();
            ConfigureCharacter(character);

            root.AddComponent<NetworkObject>();
            root.AddComponent<NetworkTransform>();
            root.AddComponent<PlayerIdentity>();
            root.AddComponent<PlayerHealth>();

            inputReader = root.AddComponent<PlayerInputReader>();
            SetField(inputReader, "inputActions", inputActions);
            inputReader.enabled = false;

            driver = root.AddComponent<PlayerMovementDriver>();
            driver.enabled = false;

            if (root.GetComponents<Character>().Length != 1)
                Debug.LogError($"[TestSetup] {name} 프리팹에 Character 컴포넌트가 여러 개 있습니다.");
            return root;
        }

        /// <summary>
        /// 이 게임에서 쓰지 않는 점프/앉기를 끈다.
        /// </summary>
        private static void ConfigureCharacter(Character character)
        {
            var serialized = new SerializedObject(character);
            serialized.FindProperty("_canEverJump").boolValue = false;
            serialized.FindProperty("_canEverCrouch").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// 소유권 설정 컴포넌트를 추가하고 참조를 연결한다.
        /// </summary>
        private static void ConfigureOwnership(GameObject root, Character character, Camera playerCamera, Behaviour[] ownerOnly, Renderer[] hiddenForOwner)
        {
            PlayerOwnershipSetup ownership = root.AddComponent<PlayerOwnershipSetup>();
            SetField(ownership, "character", character);
            SetField(ownership, "playerCamera", playerCamera);
            SetArray(ownership, "ownerOnlyBehaviours", ownerOnly);
            SetArray(ownership, "hiddenForOwner", hiddenForOwner);
        }

        /// <summary>
        /// 사격 궤적(LineRenderer), 총구 위치, 탄착 표시를 만든다.
        /// 총구는 카메라 피벗의 오른쪽 아래에 두어 1인칭 화면에서도 궤적이 대각선으로 보이게 한다.
        /// </summary>
        private static ShotTracerView CreateTracer(Transform parent, Transform cameraPivot, Material material)
        {
            var tracerObject = new GameObject("ShotTracer");
            tracerObject.transform.SetParent(parent, false);

            LineRenderer line = tracerObject.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.startWidth = 0.05f;
            line.endWidth = 0.02f;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.enabled = false;

            Transform muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(cameraPivot, false);
            muzzle.localPosition = new Vector3(0.25f, -0.2f, 0.4f);

            Renderer impact = CreateVisual(parent, PrimitiveType.Sphere, "ImpactMarker", Vector3.zero, Vector3.one * 0.12f, material);
            impact.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            impact.gameObject.SetActive(false);

            ShotTracerView view = tracerObject.AddComponent<ShotTracerView>();
            SetField(view, "muzzle", muzzle);
            SetField(view, "impactMarker", impact.transform);
            return view;
        }

        /// <summary>
        /// 전원 장치 프리팹을 만든다. (충돌체, 본체, 마네킹 전용 진행도 바)
        /// </summary>
        private static NetworkObject CreateGeneratorPrefab(Dictionary<string, Material> materials, NoiseLibrary noiseLibrary)
        {
            var root = new GameObject("Generator");

            BoxCollider box = root.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.8f, 0f);
            box.size = new Vector3(1f, 1.6f, 0.8f);

            root.AddComponent<NetworkObject>();
            root.AddComponent<Generator>();
            AddNoiseEmitter(root, noiseLibrary);

            // 가짜 마네킹이 전원 장치를 통과하지 않도록 NavMesh에 구멍을 낸다.
            NavMeshObstacle obstacle = root.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.center = box.center;
            obstacle.size = box.size;
            obstacle.carving = true;

            Renderer body = CreateVisual(root.transform, PrimitiveType.Cube, "Body", new Vector3(0f, 0.8f, 0f), new Vector3(1f, 1.6f, 0.8f), materials["Generator"]);

            // 진행도 바: 배경 + 왼쪽 끝이 피벗인 채움 막대
            Transform barRoot = new GameObject("ProgressBar").transform;
            barRoot.SetParent(root.transform, false);
            barRoot.localPosition = new Vector3(0f, 2.1f, 0f);
            Renderer background = CreateVisual(barRoot, PrimitiveType.Cube, "Background", Vector3.zero, new Vector3(1.1f, 0.16f, 0.02f), materials["ProgressBackground"]);
            background.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            Transform fillPivot = new GameObject("FillPivot").transform;
            fillPivot.SetParent(barRoot, false);
            fillPivot.localPosition = new Vector3(-0.5f, 0f, -0.02f);
            Renderer fill = CreateVisual(fillPivot, PrimitiveType.Cube, "Fill", new Vector3(0.5f, 0f, 0f), new Vector3(1f, 0.1f, 0.02f), materials["ProgressFill"]);
            fill.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            GeneratorView view = root.AddComponent<GeneratorView>();
            SetField(view, "bodyRenderer", body);
            SetField(view, "destroyedMaterial", materials["GeneratorDestroyed"]);
            SetField(view, "barRoot", barRoot);
            SetField(view, "barFill", fillPivot);

            return SavePrefab(root, GeneratorPrefabPath);
        }

        /// <summary>
        /// 충돌체가 없는 표시용 도형을 만든다.
        /// </summary>
        private static Renderer CreateVisual(Transform parent, PrimitiveType type, string name, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject visual = GameObject.CreatePrimitive(type);
            visual.name = name;
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = localPosition;
            visual.transform.localScale = localScale;

            Renderer renderer = visual.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            return renderer;
        }

        /// <summary>
        /// 소유자에게만 켜질 플레이어 카메라를 만든다. (기본 비활성)
        /// </summary>
        private static Camera CreatePlayerCamera(Transform parent, Vector3 localPosition)
        {
            var cameraObject = new GameObject("PlayerCamera");
            cameraObject.transform.SetParent(parent, false);
            cameraObject.transform.localPosition = localPosition;

            Camera playerCamera = cameraObject.AddComponent<Camera>();
            playerCamera.nearClipPlane = 0.05f;
            playerCamera.fieldOfView = 70f;
            playerCamera.GetUniversalAdditionalCameraData().renderPostProcessing = true; // 저체력 시야 효과
            cameraObject.AddComponent<AudioListener>();

            cameraObject.SetActive(false);
            return playerCamera;
        }

        /// <summary>
        /// 씬의 임시 오브젝트를 프리팹으로 저장하고 삭제한다.
        /// </summary>
        private static NetworkObject SavePrefab(GameObject root, string path)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<NetworkObject>();
        }

        #endregion

        #region 씬

        /// <summary>
        /// 테스트 씬을 새로 만들어 저장한다.
        /// </summary>
        private static void CreateTestScene(ScenePrefabs prefabs, Dictionary<string, Material> materials,
            int[] layoutPropIds, int wallPieceId, NoiseLibrary noiseLibrary)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            SpectatorController spectator = CreateSceneCamera();

            Transform mapRoot = new GameObject("Map").transform;
            CreateLighting(mapRoot, materials, noiseLibrary);
            MapInfo mapInfo = mapRoot.gameObject.AddComponent<MapInfo>();
            var mapInfoSerialized = new SerializedObject(mapInfo);
            mapInfoSerialized.FindProperty("ceilingHeight").floatValue = WallHeight;
            mapInfoSerialized.ApplyModifiedPropertiesWithoutUndo();

            CreateFloorAndOuterWalls(mapRoot, materials, wallPieceId);
            CreateInvisibleCeiling();
            CreateWaitingRoom(mapRoot, materials, wallPieceId);
            CreateProps(mapRoot, materials["Prop"], layoutPropIds);
            CreateMannequinSpawnPoints(mapRoot);
            CreateGeneratorSpawnPoints(mapRoot);
            NavMeshSurface navMeshSurface = CreateNavMeshSurface(mapRoot);

            GameSystemsRefs systems = CreateGameSystems(prefabs, navMeshSurface);
            CreateUi(systems, spectator);

            // 경로가 없는 새 씬에서는 FishNet이 씬 ID를 만들지 않으므로, 먼저 저장해 경로를 확정한 뒤 ID를 생성한다.
            EditorSceneManager.SaveScene(scene, ScenePath);
            GenerateNetworkSceneIds(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>
        /// 열린 씬의 NetworkObject에 FishNet 씬 ID를 강제로 생성한다.
        /// 씬 ID가 0이면 서버가 씬 오브젝트를 스폰하지 않는다.
        /// </summary>
        [MenuItem("GuardVsMannequin/현재 씬의 네트워크 씬 ID 재생성")]
        private static void GenerateNetworkSceneIdsForActiveScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            GenerateNetworkSceneIds(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>
        /// FishNet 내부의 NetworkObject.CreateSceneId(Scene, bool, out int)를 호출한다.
        /// 공개 API가 없어 리플렉션을 사용하며, 실패하면 FishNet 메뉴를 안내한다.
        /// </summary>
        private static void GenerateNetworkSceneIds(Scene scene)
        {
            MethodInfo method = typeof(NetworkObject).GetMethod("CreateSceneId", BindingFlags.Static | BindingFlags.NonPublic,
                null, new[] { typeof(Scene), typeof(bool), typeof(int).MakeByRefType() }, null);

            if (method == null)
            {
                Debug.LogError("[TestSetup] FishNet 씬 ID 생성 메서드를 찾지 못했습니다. " +
                               "Tools > Fish-Networking > Utility > Reserialize NetworkObjects에서 씬을 재직렬화하세요.");
                return;
            }

            object[] args = { scene, true, 0 };
            method.Invoke(null, args);
            Debug.Log($"[TestSetup] 네트워크 씬 ID {(int)args[2]}개 생성: {scene.path}");
        }

        /// <summary>
        /// 맵 조명을 만든다. 방향광과 천장 조명 기구 모두 사냥 시간에 꺼지는 MapLight이다.
        /// 조명 기구는 충돌체가 없고 변신 대상이 아니다.
        /// </summary>
        private static void CreateLighting(Transform mapRoot, Dictionary<string, Material> materials, NoiseLibrary noiseLibrary)
        {
            Transform lightsRoot = new GameObject("Lights").transform;
            lightsRoot.SetParent(mapRoot, false);

            var sunObject = new GameObject("Directional Light");
            sunObject.transform.SetParent(lightsRoot, false);
            sunObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            Light sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.2f;
            sun.shadows = LightShadows.Soft;
            sunObject.AddComponent<MapLight>();

            // 천장 조명 4x4 (대기실 포함)
            float[] grid = { -15f, -5f, 5f, 15f };
            foreach (float x in grid)
            {
                foreach (float z in grid)
                    CreateCeilingLamp(lightsRoot, new Vector3(x, WallHeight - 0.05f, z), materials["LampOn"], materials["LampOff"]);
            }

            HuntingLightingController controller = lightsRoot.gameObject.AddComponent<HuntingLightingController>();
            SetField(controller, "noiseLibrary", noiseLibrary);
        }

        /// <summary>
        /// 발광 전구와 포인트 라이트로 된 천장 조명 기구 하나를 만든다.
        /// </summary>
        private static void CreateCeilingLamp(Transform parent, Vector3 position, Material onMaterial, Material offMaterial)
        {
            var lamp = new GameObject("CeilingLamp");
            lamp.transform.SetParent(parent, false);
            lamp.transform.localPosition = position;

            Renderer bulb = CreateVisual(lamp.transform, PrimitiveType.Cube, "Bulb", Vector3.zero, new Vector3(0.8f, 0.08f, 0.8f), onMaterial);
            bulb.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var lightObject = new GameObject("PointLight");
            lightObject.transform.SetParent(lamp.transform, false);
            lightObject.transform.localPosition = Vector3.down * 0.2f;
            Light point = lightObject.AddComponent<Light>();
            point.type = LightType.Point;
            point.range = 12f;
            point.intensity = 3f;
            point.color = new Color(1f, 0.96f, 0.88f);
            point.shadows = LightShadows.None;

            MapLight mapLight = lamp.AddComponent<MapLight>();
            SetField(mapLight, "bulbRenderer", bulb);
            SetField(mapLight, "onMaterial", onMaterial);
            SetField(mapLight, "offMaterial", offMaterial);
        }

        /// <summary>
        /// 아바타가 없을 때 쓰는 씬 카메라를 만든다.
        /// 대기 중에는 맵을 내려다보고, 사망/미참가 상태에서는 생존자의 시야로 관전한다.
        /// </summary>
        private static SpectatorController CreateSceneCamera()
        {
            var cameraObject = new GameObject("SceneCamera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 30f, -28f), Quaternion.Euler(50f, 0f, 0f));

            Camera sceneCamera = cameraObject.AddComponent<Camera>();
            sceneCamera.nearClipPlane = 0.05f;
            sceneCamera.fieldOfView = 70f;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<SceneCameraSwitcher>();

            SpectatorController spectator = cameraObject.AddComponent<SpectatorController>();
            SetField(spectator, "inputActions", AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath));
            return spectator;
        }

        /// <summary>
        /// 바닥(변신 불가)과 맵 외곽 벽(벽 조각으로 변신 가능)을 만든다.
        /// </summary>
        private static void CreateFloorAndOuterWalls(Transform parent, Dictionary<string, Material> materials, int wallPieceId)
        {
            float size = MapHalfSize * 2f;
            CreateBlock(parent, "Floor", new Vector3(0f, -0.25f, 0f), new Vector3(size, 0.5f, size), materials["Floor"]);

            float wallY = WallHeight * 0.5f;
            AddTransformable(CreateBlock(parent, "Wall_North", new Vector3(0f, wallY, MapHalfSize), new Vector3(size, WallHeight, WallThickness), materials["Wall"]), wallPieceId);
            AddTransformable(CreateBlock(parent, "Wall_South", new Vector3(0f, wallY, -MapHalfSize), new Vector3(size, WallHeight, WallThickness), materials["Wall"]), wallPieceId);
            AddTransformable(CreateBlock(parent, "Wall_East", new Vector3(MapHalfSize, wallY, 0f), new Vector3(WallThickness, WallHeight, size), materials["Wall"]), wallPieceId);
            AddTransformable(CreateBlock(parent, "Wall_West", new Vector3(-MapHalfSize, wallY, 0f), new Vector3(WallThickness, WallHeight, size), materials["Wall"]), wallPieceId);
        }

        /// <summary>
        /// 천장이 개방된 맵용 투명 천장(충돌체만)을 만든다.
        /// 벽을 타고 오른 마네킹이 맵 밖으로 나가지 않고 천장으로 이어서 이동하게 한다.
        /// 맵 루트 밖에 두어 NavMesh 굽기 대상과 변신 대상에서 제외한다.
        /// </summary>
        private static void CreateInvisibleCeiling()
        {
            float size = MapHalfSize * 2f;
            var ceiling = new GameObject("Ceiling (Invisible)");
            ceiling.transform.position = new Vector3(0f, WallHeight + 0.25f, 0f);
            ceiling.isStatic = true;

            BoxCollider box = ceiling.AddComponent<BoxCollider>();
            box.size = new Vector3(size, 0.5f, size);
        }

        /// <summary>
        /// 북동쪽 모서리(x, z: 12~20)에 가드 대기실, 여닫히는 문, 가드 스폰 지점을 만든다.
        /// </summary>
        private static void CreateWaitingRoom(Transform parent, Dictionary<string, Material> materials, int wallPieceId)
        {
            Transform room = new GameObject("WaitingRoom").transform;
            room.SetParent(parent, false);

            const float roomMin = 12f;
            const float doorCenterZ = 16f;
            const float doorWidth = 2f;
            float wallY = WallHeight * 0.5f;
            Material wallMaterial = materials["WaitingRoom"];

            // 남쪽 벽(z = 12): 문 없음
            AddTransformable(CreateBlock(room, "Wall_South", new Vector3((roomMin + MapHalfSize) * 0.5f, wallY, roomMin), new Vector3(MapHalfSize - roomMin, WallHeight, WallThickness), wallMaterial), wallPieceId);

            // 서쪽 벽(x = 12): 가운데에 문
            float lowerLength = doorCenterZ - doorWidth * 0.5f - roomMin;
            float upperStart = doorCenterZ + doorWidth * 0.5f;
            float upperLength = MapHalfSize - upperStart;
            AddTransformable(CreateBlock(room, "Wall_West_Lower", new Vector3(roomMin, wallY, roomMin + lowerLength * 0.5f), new Vector3(WallThickness, WallHeight, lowerLength), wallMaterial), wallPieceId);
            AddTransformable(CreateBlock(room, "Wall_West_Upper", new Vector3(roomMin, wallY, upperStart + upperLength * 0.5f), new Vector3(WallThickness, WallHeight, upperLength), wallMaterial), wallPieceId);

            // 문: 움직이므로 정적 오브젝트가 아니며, 변신 대상이 아니다.
            GameObject door = CreateBlock(room, "Door", new Vector3(roomMin, wallY, doorCenterZ), new Vector3(WallThickness * 0.8f, WallHeight, doorWidth), materials["Door"]);
            door.isStatic = false;
            door.AddComponent<WaitingRoomDoor>();

            // 가드 대기 시간 동안 가드가 머물러야 하는 영역 (서버 이동 검증용). 벽 두께만큼 여유를 둔다.
            float roomCenter = (roomMin + MapHalfSize) * 0.5f;
            float roomSize = MapHalfSize - roomMin + WallThickness * 2f;
            var area = new SerializedObject(room.gameObject.AddComponent<WaitingRoomArea>());
            area.FindProperty("center").vector3Value = new Vector3(roomCenter, WallHeight * 0.5f, roomCenter);
            area.FindProperty("size").vector3Value = new Vector3(roomSize, WallHeight + 2f, roomSize);
            area.ApplyModifiedPropertiesWithoutUndo();

            // 가드 최대 인원(5명)만큼 스폰 지점
            for (int i = 0; i < GameConstants.MaxPlayers / 2; i++)
            {
                Vector3 position = new Vector3(14f + (i % 3) * 2f, 0f, 14f + (i / 3) * 3f);
                CreateSpawnPoint(room, PlayerRole.Guard, position, Quaternion.Euler(0f, -90f, 0f));
            }
        }

        /// <summary>
        /// 변신 대상이 될 테스트용 사물을 배치한다.
        /// </summary>
        private static void CreateProps(Transform parent, Material material, int[] layoutPropIds)
        {
            Transform props = new GameObject("Props").transform;
            props.SetParent(parent, false);

            for (int i = 0; i < PropLayout.Length; i++)
            {
                GameObject prop = GameObject.CreatePrimitive(PropLayout[i].type);
                prop.name = $"Prop_{i:00}";
                prop.transform.SetParent(props, false);
                prop.transform.localPosition = PropLayout[i].position;
                prop.transform.localScale = PropLayout[i].scale;
                prop.GetComponent<Renderer>().sharedMaterial = material;
                prop.isStatic = true;
                AddTransformable(prop, layoutPropIds[i]);
            }
        }

        /// <summary>
        /// 맵 곳곳에 마네킹 스폰 지점을 배치한다.
        /// </summary>
        private static void CreateMannequinSpawnPoints(Transform parent)
        {
            Transform points = new GameObject("MannequinSpawnPoints").transform;
            points.SetParent(parent, false);

            var positions = new[]
            {
                new Vector3(-15f, 0f, -15f), new Vector3(-15f, 0f, 0f), new Vector3(-15f, 0f, 15f),
                new Vector3(0f, 0f, -15f), new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 14f),
                new Vector3(15f, 0f, -15f), new Vector3(15f, 0f, 0f), new Vector3(8f, 0f, 8f)
            };

            foreach (Vector3 position in positions)
                CreateSpawnPoint(points, PlayerRole.Mannequin, position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
        }

        /// <summary>
        /// 전원 장치 후보 위치를 배치한다. 맵 중앙을 바라보도록 회전한다.
        /// </summary>
        private static void CreateGeneratorSpawnPoints(Transform parent)
        {
            Transform points = new GameObject("GeneratorSpawnPoints").transform;
            points.SetParent(parent, false);

            foreach (Vector3 position in GeneratorPositions)
            {
                var pointObject = new GameObject("GeneratorSpawnPoint");
                pointObject.transform.SetParent(points, false);
                Vector3 toCenter = -position;
                toCenter.y = 0f;
                pointObject.transform.SetPositionAndRotation(position, toCenter.sqrMagnitude > 0.01f ? Quaternion.LookRotation(toCenter) : Quaternion.identity);
                pointObject.AddComponent<GeneratorSpawnPoint>();
            }
        }

        /// <summary>
        /// 가짜 마네킹용 NavMeshSurface를 맵 루트에 추가한다.
        /// 맵의 충돌체만 대상으로 하며, 굽기는 서버가 준비 단계에 런타임으로 수행한다.
        /// </summary>
        private static NavMeshSurface CreateNavMeshSurface(Transform mapRoot)
        {
            NavMeshSurface surface = mapRoot.gameObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            return surface;
        }

        /// <summary>
        /// GameSystems에서 UI가 참조할 컴포넌트 묶음.
        /// </summary>
        private struct GameSystemsRefs
        {
            public GameStateMachine stateMachine;
            public PlayerRegistry playerRegistry;
            public ObjectiveTracker objectiveTracker;
            public MatchResult matchResult;
        }

        /// <summary>
        /// 게임 진행에 필요한 서버 시스템을 가진 씬 네트워크 오브젝트를 만든다.
        /// </summary>
        private static GameSystemsRefs CreateGameSystems(ScenePrefabs prefabs, NavMeshSurface navMeshSurface)
        {
            var systems = new GameObject("GameSystems");
            systems.AddComponent<NetworkObject>();

            GameStateMachine stateMachine = systems.AddComponent<GameStateMachine>();
            SpawnPointRegistry spawnPoints = systems.AddComponent<SpawnPointRegistry>();
            PlayerRegistry playerRegistry = systems.AddComponent<PlayerRegistry>();
            PlayerSpawner playerSpawner = systems.AddComponent<PlayerSpawner>();
            ObjectiveTracker objectiveTracker = systems.AddComponent<ObjectiveTracker>();
            GeneratorSpawner generatorSpawner = systems.AddComponent<GeneratorSpawner>();
            MatchResult matchResult = systems.AddComponent<MatchResult>();
            MatchRules matchRules = systems.AddComponent<MatchRules>();
            FakeMannequinSpawner fakeSpawner = systems.AddComponent<FakeMannequinSpawner>();
            MatchSessionBridge sessionBridge = systems.AddComponent<MatchSessionBridge>();
            SetField(sessionBridge, "stateMachine", stateMachine);

            SetField(stateMachine, "matchRulesComponent", matchRules);

            SetField(playerSpawner, "stateMachine", stateMachine);
            SetField(playerSpawner, "spawnPoints", spawnPoints);
            SetField(playerSpawner, "guardPrefab", prefabs.guard);
            SetField(playerSpawner, "mannequinPrefab", prefabs.mannequin);
            SetField(playerSpawner, "matchResult", matchResult);

            SetField(generatorSpawner, "stateMachine", stateMachine);
            SetField(generatorSpawner, "objectiveTracker", objectiveTracker);
            SetField(generatorSpawner, "generatorPrefab", prefabs.generator);

            SetField(fakeSpawner, "stateMachine", stateMachine);
            SetField(fakeSpawner, "fakeMannequinPrefab", prefabs.fakeMannequin);
            SetField(fakeSpawner, "navMeshSurface", navMeshSurface);

            SetField(matchResult, "stateMachine", stateMachine);

            SetField(matchRules, "playerRegistry", playerRegistry);
            SetField(matchRules, "objectiveTracker", objectiveTracker);
            SetField(matchRules, "matchResult", matchResult);

            return new GameSystemsRefs
            {
                stateMachine = stateMachine,
                playerRegistry = playerRegistry,
                objectiveTracker = objectiveTracker,
                matchResult = matchResult
            };
        }

        /// <summary>
        /// 게임 HUD와 저체력 효과 볼륨을 만든다. (접속은 로비/룸 씬에서 처리하므로 게임 씬에는 NetworkManager가 없다)
        /// 씬 네트워크 오브젝트는 클라이언트에서 스폰 전까지 비활성이므로 별도 오브젝트에 둔다.
        /// </summary>
        private static void CreateUi(GameSystemsRefs systems, SpectatorController spectator)
        {
            // uGUI HUD와 ESC 메뉴(게임 중에는 닫을 때 커서를 다시 잠금)
            EditorUiFactory.CreateEventSystem();
            Canvas canvas = EditorUiFactory.CreateCanvas("HudCanvas");
            canvas.gameObject.AddComponent<OsFontApplier>();

            GameHud gameHud = SharedUiBuilder.CreateGameHud(canvas.transform);
            SharedUiBuilder.CreatePauseMenu(canvas.transform, lockCursorOnClose: true);

            SetField(gameHud, "stateMachine", systems.stateMachine);
            SetField(gameHud, "playerRegistry", systems.playerRegistry);
            SetField(gameHud, "objectiveTracker", systems.objectiveTracker);
            SetField(gameHud, "matchResult", systems.matchResult);
            SetField(gameHud, "spectator", spectator);

            // 저체력 시야 효과용 전역 볼륨 (런타임에 프로필 생성)
            new GameObject("LowHealthVolume").AddComponent<LowHealthVignette>();
        }

        /// <summary>
        /// 충돌체가 있는 정적 블록을 만든다.
        /// </summary>
        private static GameObject CreateBlock(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.localPosition = position;
            block.transform.localScale = scale;
            block.GetComponent<Renderer>().sharedMaterial = material;
            block.isStatic = true;
            return block;
        }

        /// <summary>
        /// 오브젝트를 변신 가능 사물로 지정한다.
        /// </summary>
        private static void AddTransformable(GameObject target, int propId)
        {
            TransformableProp prop = target.AddComponent<TransformableProp>();
            prop.SetPropId(propId);
        }

        /// <summary>
        /// 역할이 지정된 스폰 지점을 만든다.
        /// </summary>
        private static void CreateSpawnPoint(Transform parent, PlayerRole role, Vector3 position, Quaternion rotation)
        {
            var pointObject = new GameObject($"SpawnPoint_{role}");
            pointObject.transform.SetParent(parent, false);
            pointObject.transform.SetPositionAndRotation(position, rotation);

            SpawnPoint point = pointObject.AddComponent<SpawnPoint>();
            var serialized = new SerializedObject(point);
            serialized.FindProperty("role").enumValueIndex = (int)role;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// 빌드에 선형 안개 셰이더 변형이 포함되도록 Graphics 설정의 안개 스트리핑을 Custom(Linear 유지)으로 바꾼다.
        /// 기본값(Automatic)은 씬에서 안개를 쓰지 않으면 빌드에서 안개 셰이더를 제거해,
        /// 런타임에 켜는 사냥 시간 시야 안개가 빌드에서만 동작하지 않는다.
        /// </summary>
        private static void KeepLinearFogShaderVariants()
        {
            Object graphicsSettings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset").FirstOrDefault();
            if (graphicsSettings == null)
            {
                Debug.LogWarning("[TestSetup] GraphicsSettings를 찾지 못했습니다. Project Settings > Graphics > Fog Modes를 Custom(Linear)으로 설정하세요.");
                return;
            }

            var serialized = new SerializedObject(graphicsSettings);
            serialized.FindProperty("m_FogStripping").intValue = 1; // 1 = Custom
            serialized.FindProperty("m_FogKeepLinear").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// 빌드 설정의 씬 순서를 정한다. (로비 → 룸 → 게임)
        /// </summary>
        private static void SetBuildScenes(params string[] orderedPaths)
        {
            // 지정한 씬을 순서대로 앞에 두고(첫 번째가 시작 씬), 나머지 기존 씬은 뒤에 유지한다.
            List<EditorBuildSettingsScene> others = EditorBuildSettings.scenes.Where(s => !orderedPaths.Contains(s.path)).ToList();
            List<EditorBuildSettingsScene> scenes = orderedPaths.Select(path => new EditorBuildSettingsScene(path, true)).ToList();
            scenes.AddRange(others);
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        #endregion

        #region SerializedObject 헬퍼

        /// <summary>
        /// private 직렬화 필드에 오브젝트 참조를 지정한다.
        /// </summary>
        internal static void SetField(Object target, string fieldName, Object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);
            if (property == null)
            {
                Debug.LogError($"[TestSetup] {target.GetType().Name}.{fieldName} 필드를 찾을 수 없습니다.");
                return;
            }
            // C# 참조는 있지만 Unity 오브젝트가 이미 내려간(파괴된) 경우 참조가 조용히 비는 것을 막는다.
            if (value == null && !ReferenceEquals(value, null))
                Debug.LogError($"[TestSetup] {target.GetType().Name}.{fieldName}에 연결할 오브젝트가 이미 해제되었습니다. 경로로 다시 불러와야 합니다.");

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// private 직렬화 배열 필드에 오브젝트 참조 목록을 지정한다.
        /// </summary>
        internal static void SetArray(Object target, string fieldName, Object[] values)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        #endregion
    }
}
