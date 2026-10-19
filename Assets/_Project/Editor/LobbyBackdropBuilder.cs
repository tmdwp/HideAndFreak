using System.IO;
using System.Linq;
using GuardVsMannequin.Core;
using GuardVsMannequin.Map;
using GuardVsMannequin.Player;
using GuardVsMannequin.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace GuardVsMannequin.Editor
{
    /// <summary>
    /// 로비 배경 연출(어두운 방에서 가드가 구울에게 쫓기는 장면)을 만든다.
    /// - 프리팹(LobbyChaseRoom): 방(바닥·벽·천장·기둥·상자), 깜빡이는 전구, 비상등, 가드(손전등)·구울, 순환 경로, 카메라 위치(CameraPoint)
    /// - 씬에 넣기: 프리팹을 넣고, 메뉴 카메라를 CameraPoint로 옮기고, 환경광·안개를 어둡게 한다.
    /// 프리팹이 이미 있으면 그대로 쓰므로 직접 고친 배치는 유지된다. (처음 모양으로 되돌리려면 "다시 만들기")
    /// 외형은 "캐릭터·무기 에셋 생성"이 만든 Skin_Guard / Skin_Ghoul을 쓴다.
    /// </summary>
    public static class LobbyBackdropBuilder
    {
        public const string PrefabPath = "Assets/_Project/Prefabs/Lobby/LobbyChaseRoom.prefab";
        private const string MaterialFolder = "Assets/_Project/Art/Materials";
        private const string RootName = "LobbyChaseRoom";
        private const string CameraPointName = "CameraPoint";

        // 방 크기(m)
        private const float RoomWidth = 24f;
        private const float RoomDepth = 16f;
        private const float RoomHeight = 4f;
        private const float WallThickness = 0.3f;

        // 경로 중심(방 기준)과 카메라
        private static readonly Vector3 PathCenter = new Vector3(0f, 0f, 1f);
        private static readonly Vector3 CameraPosition = new Vector3(0f, 2.4f, -7f);
        private static readonly Vector3 CameraLookAt = new Vector3(0f, 0.7f, 1f);
        private const float CameraFieldOfView = 55f;

        // 어두운 환경
        private static readonly Color AmbientColor = new Color(0.035f, 0.035f, 0.045f);
        private static readonly Color FogColor = new Color(0.01f, 0.01f, 0.015f);
        private const float FogDensity = 0.05f;

        /// <summary>
        /// 지금 열린 씬(로비)에 배경 연출을 넣는다. 이미 있으면 지우고 다시 넣는다.
        /// </summary>
        [MenuItem(EditorMenus.Root + "로비/쫓기는 배경 연출 넣기 (열린 씬)")]
        public static void AddToOpenScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            AddToScene(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[LobbyBackdropBuilder] '{scene.name}' 씬에 배경 연출을 넣었습니다. 씬을 저장하세요.");
        }

        /// <summary>
        /// 프리팹을 처음 모양으로 다시 만든다. 씬에 들어간 프리팹 인스턴스는 자동으로 바뀐다. (카메라·환경은 "넣기" 메뉴로)
        /// </summary>
        [MenuItem(EditorMenus.Root + "로비/쫓기는 배경 프리팹 다시 만들기")]
        public static void RebuildPrefab()
        {
            if (!EditorUtility.DisplayDialog("배경 프리팹 다시 만들기",
                    "LobbyChaseRoom 프리팹을 처음 모양으로 덮어씁니다. 직접 고친 배치는 사라집니다.", "다시 만들기", "취소"))
                return;
            BuildPrefab();
            Debug.Log($"[LobbyBackdropBuilder] 배경 프리팹을 다시 만들었습니다: {PrefabPath}");
        }

        /// <summary>
        /// 씬에 배경 프리팹(없으면 만든다)을 넣고, 카메라를 CameraPoint로 옮기고, 환경을 어둡게 한다.
        /// 환경광·안개(RenderSettings)는 활성 씬에 적용되므로 대상 씬이 활성 씬이어야 한다. (씬 저장은 호출한 쪽이 한다)
        /// </summary>
        internal static void AddToScene(Scene scene)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
                prefab = BuildPrefab();

            foreach (GameObject existing in scene.GetRootGameObjects().Where(root => root.name == RootName))
                Object.DestroyImmediate(existing);

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.transform.SetAsFirstSibling();
            PlaceCamera(scene, instance.transform.Find(CameraPointName));
            ApplyDarkEnvironment();
        }

        #region 씬 설정

        /// <summary>
        /// 씬의 MainCamera를 카메라 위치로 옮기고 화각·배경색을 맞춘다.
        /// </summary>
        private static void PlaceCamera(Scene scene, Transform cameraPoint)
        {
            Camera menuCamera = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Camera>(true))
                .FirstOrDefault(candidate => candidate.CompareTag("MainCamera"));
            if (menuCamera == null || cameraPoint == null)
            {
                Debug.LogWarning("[LobbyBackdropBuilder] MainCamera 또는 CameraPoint를 찾지 못해 카메라를 옮기지 않았습니다.");
                return;
            }

            menuCamera.transform.SetPositionAndRotation(cameraPoint.position, cameraPoint.rotation);
            menuCamera.fieldOfView = CameraFieldOfView;
            menuCamera.clearFlags = CameraClearFlags.SolidColor;
            menuCamera.backgroundColor = Color.black;
            EditorUtility.SetDirty(menuCamera.transform);
            EditorUtility.SetDirty(menuCamera);
        }

        /// <summary>
        /// 하늘 없이 아주 어두운 환경광과 짙은 안개를 지정한다. (활성 씬)
        /// </summary>
        private static void ApplyDarkEnvironment()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = AmbientColor;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = FogColor;
            RenderSettings.fogDensity = FogDensity;
        }

        #endregion

        #region 프리팹

        /// <summary>
        /// 배경 프리팹을 만들어 저장하고 반환한다. 같은 경로에 있으면 덮어쓴다. (GUID 유지)
        /// </summary>
        private static GameObject BuildPrefab()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            Directory.CreateDirectory(MaterialFolder);
            AssetDatabase.Refresh();

            var root = new GameObject(RootName);
            BuildRoom(root.transform);
            BuildLights(root.transform);

            LobbyChasePath path = new GameObject("ChasePath").AddComponent<LobbyChasePath>();
            path.transform.SetParent(root.transform, false);
            path.transform.localPosition = PathCenter;

            Transform guard = CreateCharacter(root.transform, "Guard", PlayerRole.Guard, CharacterAssetBuilder.GuardSkinPath, out _);
            AddFlashlight(guard);
            Transform ghoul = CreateCharacter(root.transform, "Ghoul", PlayerRole.Mannequin, CharacterAssetBuilder.GhoulSkinPath, out CharacterSkinView ghoulSkin);

            LobbyChaseLoop chase = root.AddComponent<LobbyChaseLoop>();
            TestEnvironmentSetupTool.SetField(chase, "path", path);
            TestEnvironmentSetupTool.SetField(chase, "runner", guard);
            TestEnvironmentSetupTool.SetField(chase, "chaser", ghoul);
            TestEnvironmentSetupTool.SetField(chase, "chaserSkin", ghoulSkin);

            // 씬 뷰에서도 시작 위치가 보이도록 미리 놓는다. (실행 중에는 LobbyChaseLoop가 옮긴다)
            SetPose(guard, path.Evaluate(0f));
            SetPose(ghoul, path.Evaluate(-new SerializedObject(chase).FindProperty("baseGap").floatValue));

            var cameraPoint = new GameObject(CameraPointName).transform;
            cameraPoint.SetParent(root.transform, false);
            cameraPoint.localPosition = CameraPosition;
            cameraPoint.localRotation = Quaternion.LookRotation(CameraLookAt - CameraPosition, Vector3.up);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        /// <summary>
        /// 바닥·천장·벽 4면과 가운데 기둥 2개, 벽가 상자를 만든다.
        /// </summary>
        private static void BuildRoom(Transform root)
        {
            Material floor = LoadOrCreateMaterial("M_LobbyFloor", new Color(0.09f, 0.09f, 0.09f), 0.35f);
            Material wall = LoadOrCreateMaterial("M_LobbyWall", new Color(0.16f, 0.16f, 0.15f), 0.1f);
            Material prop = LoadOrCreateMaterial("M_LobbyProp", new Color(0.18f, 0.13f, 0.09f), 0.15f);

            Transform room = new GameObject("Room").transform;
            room.SetParent(root, false);

            float halfWidth = RoomWidth * 0.5f;
            float halfDepth = RoomDepth * 0.5f;
            CreateBlock(room, "Floor", new Vector3(0f, -0.1f, 0f), new Vector3(RoomWidth, 0.2f, RoomDepth), floor);
            CreateBlock(room, "Ceiling", new Vector3(0f, RoomHeight + 0.1f, 0f), new Vector3(RoomWidth, 0.2f, RoomDepth), wall);
            CreateBlock(room, "Wall_Back", new Vector3(0f, RoomHeight * 0.5f, halfDepth), new Vector3(RoomWidth, RoomHeight, WallThickness), wall);
            CreateBlock(room, "Wall_Front", new Vector3(0f, RoomHeight * 0.5f, -halfDepth), new Vector3(RoomWidth, RoomHeight, WallThickness), wall);
            CreateBlock(room, "Wall_Left", new Vector3(-halfWidth, RoomHeight * 0.5f, 0f), new Vector3(WallThickness, RoomHeight, RoomDepth), wall);
            CreateBlock(room, "Wall_Right", new Vector3(halfWidth, RoomHeight * 0.5f, 0f), new Vector3(WallThickness, RoomHeight, RoomDepth), wall);

            // 경로 안쪽 기둥: 뒤쪽을 지나는 캐릭터가 잠깐 가려져 깊이감이 생긴다.
            CreateBlock(room, "Pillar_L", PathCenter + new Vector3(-3.2f, RoomHeight * 0.5f, 0f), new Vector3(0.6f, RoomHeight, 0.6f), wall);
            CreateBlock(room, "Pillar_R", PathCenter + new Vector3(3.2f, RoomHeight * 0.5f, 0f), new Vector3(0.6f, RoomHeight, 0.6f), wall);

            CreateBlock(room, "Crate_1", new Vector3(-9.5f, 0.5f, 6.5f), Vector3.one, prop);
            CreateBlock(room, "Crate_2", new Vector3(-8.4f, 0.4f, 6.8f), Vector3.one * 0.8f, prop);
            CreateBlock(room, "Crate_3", new Vector3(-9.3f, 1.25f, 6.6f), Vector3.one * 0.5f, prop);
            CreateBlock(room, "Crate_4", new Vector3(9.8f, 0.6f, 5.8f), new Vector3(1.4f, 1.2f, 1.2f), prop);
        }

        /// <summary>
        /// 천장의 깜빡이는 전구(따뜻한 색)와 뒤쪽 구석의 붉은 비상등을 만든다.
        /// </summary>
        private static void BuildLights(Transform root)
        {
            Light lamp = CreateLight(root, "CeilingLamp", LightType.Point, PathCenter + new Vector3(0f, RoomHeight - 0.3f, 0f),
                new Color(1f, 0.82f, 0.6f), 2.2f, 9f);
            lamp.gameObject.AddComponent<FlickeringLight>();

            CreateLight(root, "EmergencyLight", LightType.Point, new Vector3(RoomWidth * 0.5f - 1.5f, 2.6f, RoomDepth * 0.5f - 0.6f),
                new Color(1f, 0.12f, 0.08f), 0.9f, 6f);
        }

        /// <summary>
        /// 가드 가슴 높이에 앞을 비추는 손전등(그림자 있는 스포트라이트)을 붙인다.
        /// </summary>
        private static void AddFlashlight(Transform guard)
        {
            Light flashlight = CreateLight(guard, "Flashlight", LightType.Spot, new Vector3(0.2f, 1.35f, 0.35f),
                new Color(1f, 0.96f, 0.88f), 4f, 14f);
            flashlight.transform.localRotation = Quaternion.Euler(8f, 0f, 0f);
            flashlight.spotAngle = 50f;
            flashlight.innerSpotAngle = 25f;
            flashlight.shadows = LightShadows.Soft;
        }

        /// <summary>
        /// 외형(<see cref="CharacterSkinView"/>)을 가진 캐릭터를 만든다. 모델은 실행할 때 Visual 아래에 붙는다.
        /// </summary>
        private static Transform CreateCharacter(Transform root, string name, PlayerRole role, string skinPath, out CharacterSkinView skinView)
        {
            var character = new GameObject(name).transform;
            character.SetParent(root, false);
            var visual = new GameObject("Visual").transform;
            visual.SetParent(character, false);

            CharacterSkin skin = AssetDatabase.LoadAssetAtPath<CharacterSkin>(skinPath);
            if (skin == null)
                Debug.LogWarning($"[LobbyBackdropBuilder] 외형이 없습니다: {skinPath} — 메뉴 \"캐릭터·무기 에셋 생성\"을 먼저 실행하세요.");

            skinView = character.gameObject.AddComponent<CharacterSkinView>();
            var serialized = new SerializedObject(skinView);
            serialized.FindProperty("role").intValue = (int)role;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            TestEnvironmentSetupTool.SetField(skinView, "defaultSkin", skin);
            TestEnvironmentSetupTool.SetField(skinView, "visualRoot", visual);
            return character;
        }

        /// <summary>
        /// 충돌체 없는 상자를 만든다.
        /// </summary>
        private static void CreateBlock(Transform parent, string name, Vector3 position, Vector3 size, Material material)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            Object.DestroyImmediate(block.GetComponent<Collider>());
            block.transform.SetParent(parent, false);
            block.transform.localPosition = position;
            block.transform.localScale = size;
            block.GetComponent<Renderer>().sharedMaterial = material;
        }

        /// <summary>
        /// 조명을 만든다.
        /// </summary>
        private static Light CreateLight(Transform parent, string name, LightType type, Vector3 position, Color color, float intensity, float range)
        {
            Light light = new GameObject(name).AddComponent<Light>();
            light.transform.SetParent(parent, false);
            light.transform.localPosition = position;
            light.type = type;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
            return light;
        }

        /// <summary>
        /// URP Lit 단색 머티리얼을 불러오고, 없으면 만든다. (만든 뒤 고친 값은 유지)
        /// </summary>
        private static Material LoadOrCreateMaterial(string name, Color color, float smoothness)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
                return material;

            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        /// <summary>
        /// 트랜스폼의 위치·회전을 지정한다.
        /// </summary>
        private static void SetPose(Transform target, Pose pose)
        {
            target.SetPositionAndRotation(pose.position, pose.rotation);
        }

        #endregion
    }
}
