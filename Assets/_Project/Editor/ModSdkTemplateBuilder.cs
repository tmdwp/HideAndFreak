using System.Collections.Generic;
using System.IO;
using GuardVsMannequin.Core;
using GuardVsMannequin.Map;
using GuardVsMannequin.Objectives;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GuardVsMannequin.Editor
{
    /// <summary>
    /// 모드 SDK에 넣을 예제 모드 템플릿을 만든다. (기본 도형만 사용 — 에셋 팩 모델은 재배포할 수 없으므로)
    /// - 예제 맵 씬: 바닥·외벽(벽 조각)·변신 사물·조명(MapLight)·스폰 지점·전원 장치 후보·투명 천장·구운 NavMesh·대기 화면 위치
    /// - 예제 모드 설정(Mod Project)과 번역 파일
    /// 모드 제작자는 SDK를 가져온 뒤 예제 모드 설정을 선택하고 "모드 내보내기"를 눌러 전체 과정을 바로 시험해 볼 수 있다.
    /// 맵 씬에는 콘텐츠 어셈블리 컴포넌트만 쓴다. (게임 코드 의존 없음)
    /// </summary>
    public static class ModSdkTemplateBuilder
    {
        private const string SceneName = "Example_TemplateMap";
        private const float HalfSize = 18f;
        private const float WallHeight = 4f;
        private const float WallThickness = 0.4f;

        /// <summary>예제 맵 씬 경로.</summary>
        public static string ScenePath => $"{ModSdkSettings.TemplateFolder}/{SceneName}.unity";

        // 변신 사물 배치 (종류, 위치, 크기)
        private static readonly (PrimitiveType type, Vector3 position, Vector3 scale)[] PropLayout =
        {
            (PrimitiveType.Cube, new Vector3(-8f, 0.5f, 6f), new Vector3(1f, 1f, 1f)),
            (PrimitiveType.Cube, new Vector3(-5f, 0.6f, -9f), new Vector3(2f, 1.2f, 0.8f)),
            (PrimitiveType.Cube, new Vector3(7f, 0.4f, -5f), new Vector3(1.4f, 0.8f, 0.8f)),
            (PrimitiveType.Cylinder, new Vector3(9f, 0.6f, 7f), new Vector3(0.6f, 0.6f, 0.6f)),
            (PrimitiveType.Cylinder, new Vector3(-12f, 0.9f, -12f), new Vector3(0.9f, 0.9f, 0.9f)),
            (PrimitiveType.Sphere, new Vector3(2f, 0.4f, 2f), new Vector3(0.8f, 0.8f, 0.8f)),
            (PrimitiveType.Cube, new Vector3(13f, 0.9f, -13f), new Vector3(1.2f, 1.8f, 0.6f)),
            (PrimitiveType.Capsule, new Vector3(-2f, 1f, 12f), new Vector3(0.7f, 1f, 0.7f))
        };

        /// <summary>
        /// 예제 템플릿을 새로 만든다. (열린 씬은 저장 여부를 물은 뒤 닫히고, 끝나면 다시 연다)
        /// </summary>
        [MenuItem(EditorMenus.Root + "모드/모드 SDK 예제 템플릿 다시 만들기")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            string startScene = SceneManager.GetActiveScene().path;
            Directory.CreateDirectory(ModSdkSettings.TemplateFolder);
            AssetDatabase.Refresh();

            Dictionary<string, Material> materials = CreateMaterials();
            BuildScene(materials);
            CreateModProject();

            AssetDatabase.SaveAssets();
            if (!string.IsNullOrEmpty(startScene))
                EditorSceneManager.OpenScene(startScene, OpenSceneMode.Single);
            Debug.Log($"[ModSdkTemplateBuilder] 예제 모드 템플릿을 만들었습니다: {ModSdkSettings.TemplateFolder}");
        }

        /// <summary>
        /// 예제 맵 씬을 만들어 저장한다. (NavMesh는 구워서 씬 폴더에 저장)
        /// </summary>
        private static void BuildScene(Dictionary<string, Material> materials)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var map = new GameObject("Map");
            MapInfo mapInfo = map.AddComponent<MapInfo>();
            Transform environment = CreateChild(map.transform, "Environment");

            CreateBlock(environment, "Floor", new Vector3(0f, -0.25f, 0f), new Vector3(HalfSize * 2f, 0.5f, HalfSize * 2f), materials["Floor"]);
            float wallY = WallHeight * 0.5f;
            float length = HalfSize * 2f;
            RegisterWall(CreateBlock(environment, "Wall_North", new Vector3(0f, wallY, HalfSize), new Vector3(length, WallHeight, WallThickness), materials["Wall"]));
            RegisterWall(CreateBlock(environment, "Wall_South", new Vector3(0f, wallY, -HalfSize), new Vector3(length, WallHeight, WallThickness), materials["Wall"]));
            RegisterWall(CreateBlock(environment, "Wall_East", new Vector3(HalfSize, wallY, 0f), new Vector3(WallThickness, WallHeight, length), materials["Wall"]));
            RegisterWall(CreateBlock(environment, "Wall_West", new Vector3(-HalfSize, wallY, 0f), new Vector3(WallThickness, WallHeight, length), materials["Wall"]));
            // 가운데 칸막이: 시야를 가려 숨을 곳을 만든다.
            CreateBlock(environment, "Divider", new Vector3(0f, 1.25f, -3f), new Vector3(10f, 2.5f, 0.3f), materials["Wall"]);

            Transform props = CreateChild(environment, "Props");
            for (int i = 0; i < PropLayout.Length; i++)
            {
                GameObject prop = GameObject.CreatePrimitive(PropLayout[i].type);
                prop.name = $"Prop_{i:00}";
                prop.transform.SetParent(props, false);
                prop.transform.localPosition = PropLayout[i].position;
                prop.transform.localScale = PropLayout[i].scale;
                prop.GetComponent<Renderer>().sharedMaterial = materials["Prop"];
                prop.isStatic = true;
                PropRegistrar.Register(prop, isWall: false, absorbChildren: true);
            }

            // 투명 천장: 벽을 타고 오른 마네킹이 맵 밖으로 나가지 않게 한다.
            var ceiling = new GameObject("Ceiling (Invisible)");
            ceiling.transform.SetParent(map.transform, false);
            ceiling.transform.localPosition = new Vector3(0f, WallHeight + 0.25f, 0f);
            ceiling.AddComponent<BoxCollider>().size = new Vector3(length, 0.5f, length);

            CreateLights(map.transform, materials);
            CreatePoints(map.transform);

            var overview = new GameObject("CameraOverview");
            overview.transform.SetParent(map.transform, false);
            overview.transform.SetPositionAndRotation(new Vector3(0f, 26f, -24f), Quaternion.Euler(50f, 0f, 0f));
            var info = new SerializedObject(mapInfo);
            info.FindProperty("mapName").stringValue = "Example";
            info.FindProperty("ceilingHeight").floatValue = WallHeight;
            info.FindProperty("overviewPoint").objectReferenceValue = overview.transform;
            info.ApplyModifiedPropertiesWithoutUndo();

            ApplyRealtimeOnlyLighting(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            BakeNavMesh(scene, map);
            PropIndexer.AssignIndices(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>
        /// 천장 조명 4개와 방향광을 만든다. 모두 사냥 시간에 꺼지는 MapLight다.
        /// </summary>
        private static void CreateLights(Transform map, Dictionary<string, Material> materials)
        {
            Transform lights = CreateChild(map, "Lights");

            var sun = new GameObject("Directional Light");
            sun.transform.SetParent(lights, false);
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            Light sunLight = sun.AddComponent<Light>();
            sunLight.type = LightType.Directional;
            sunLight.intensity = 0.6f;
            sun.AddComponent<MapLight>();

            foreach (Vector3 position in new[] { new Vector3(-9f, 0f, -9f), new Vector3(9f, 0f, -9f), new Vector3(-9f, 0f, 9f), new Vector3(9f, 0f, 9f) })
            {
                var lamp = new GameObject("CeilingLamp");
                lamp.transform.SetParent(lights, false);
                lamp.transform.localPosition = position + Vector3.up * (WallHeight - 0.1f);

                GameObject bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bulb.name = "Bulb";
                Object.DestroyImmediate(bulb.GetComponent<Collider>());
                bulb.transform.SetParent(lamp.transform, false);
                bulb.transform.localScale = Vector3.one * 0.3f;
                Renderer bulbRenderer = bulb.GetComponent<Renderer>();
                bulbRenderer.sharedMaterial = materials["LampOn"];

                var pointObject = new GameObject("Light");
                pointObject.transform.SetParent(lamp.transform, false);
                pointObject.transform.localPosition = Vector3.down * 0.3f;
                Light point = pointObject.AddComponent<Light>();
                point.type = LightType.Point;
                point.range = 12f;
                point.intensity = 2f;
                point.lightmapBakeType = LightmapBakeType.Realtime;

                MapLight mapLight = lamp.AddComponent<MapLight>();
                var serialized = new SerializedObject(mapLight);
                serialized.FindProperty("bulbRenderer").objectReferenceValue = bulbRenderer;
                serialized.FindProperty("onMaterial").objectReferenceValue = materials["LampOn"];
                serialized.FindProperty("offMaterial").objectReferenceValue = materials["LampOff"];
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        /// <summary>
        /// 마네킹 스폰 9개, 가드 스폰 후보 5개, 전원 장치 후보 12개를 배치한다.
        /// </summary>
        private static void CreatePoints(Transform map)
        {
            Transform mannequins = CreateChild(map, "MannequinSpawnPoints");
            foreach (Vector3 position in Grid(-12f, 12f, 3, -12f, 4f, 3))
                CreateSpawnPoint(mannequins, PlayerRole.Mannequin, position);

            Transform guards = CreateChild(map, "GuardSpawnPoints");
            foreach (Vector3 position in new[] { new Vector3(15f, 0f, 15f), new Vector3(-15f, 0f, 15f), new Vector3(15f, 0f, 10f), new Vector3(-15f, 0f, 10f), new Vector3(0f, 0f, 15f) })
                CreateSpawnPoint(guards, PlayerRole.Guard, position);

            Transform generators = CreateChild(map, "GeneratorSpawnPoints");
            foreach (Vector3 position in Grid(-15f, 15f, 4, -15f, 15f, 3))
            {
                var point = new GameObject("GeneratorSpawnPoint");
                point.transform.SetParent(generators, false);
                Vector3 toCenter = new Vector3(-position.x, 0f, -position.z);
                point.transform.SetPositionAndRotation(position, Quaternion.LookRotation(toCenter.sqrMagnitude > 0.01f ? toCenter : Vector3.forward));
                point.AddComponent<GeneratorSpawnPoint>();
            }
        }

        /// <summary>
        /// x·z 범위를 나눈 격자 위치 목록.
        /// </summary>
        private static IEnumerable<Vector3> Grid(float xMin, float xMax, int xCount, float zMin, float zMax, int zCount)
        {
            for (int x = 0; x < xCount; x++)
            {
                for (int z = 0; z < zCount; z++)
                {
                    float px = xCount == 1 ? xMin : Mathf.Lerp(xMin, xMax, x / (float)(xCount - 1));
                    float pz = zCount == 1 ? zMin : Mathf.Lerp(zMin, zMax, z / (float)(zCount - 1));
                    yield return new Vector3(px, 0f, pz);
                }
            }
        }

        /// <summary>
        /// 역할이 정해진 스폰 지점을 만든다.
        /// </summary>
        private static void CreateSpawnPoint(Transform parent, PlayerRole role, Vector3 position)
        {
            var point = new GameObject($"SpawnPoint_{role}");
            point.transform.SetParent(parent, false);
            point.transform.SetPositionAndRotation(position, Quaternion.LookRotation(new Vector3(-position.x, 0f, -position.z + 0.01f)));
            SpawnPoint spawn = point.AddComponent<SpawnPoint>();
            var serialized = new SerializedObject(spawn);
            serialized.FindProperty("role").enumValueIndex = (int)role;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// NavMeshSurface를 붙여 굽고, 데이터를 씬 이름 폴더에 저장한다. (게임은 모드 맵의 NavMesh를 실행 중에 구울 수 없음)
        /// </summary>
        private static void BakeNavMesh(Scene scene, GameObject map)
        {
            NavMeshSurface surface = map.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
            Physics.SyncTransforms();
            surface.BuildNavMesh();
            if (surface.navMeshData == null)
            {
                Debug.LogWarning("[ModSdkTemplateBuilder] NavMesh를 굽지 못했습니다. 예제 씬에서 NavMeshSurface의 Bake를 누르세요.");
                return;
            }

            string folder = $"{ModSdkSettings.TemplateFolder}/{scene.name}";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder(ModSdkSettings.TemplateFolder, scene.name);
            string path = $"{folder}/NavMesh-Map.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(surface.navMeshData, path);
            EditorUtility.SetDirty(surface);
        }

        /// <summary>
        /// 예제 씬에 GI를 끈 조명 설정을 지정한다. (템플릿 폴더 안의 설정 에셋 — SDK 프로젝트에서도 그대로 쓰인다)
        /// </summary>
        private static void ApplyRealtimeOnlyLighting(Scene scene)
        {
            string path = $"{ModSdkSettings.TemplateFolder}/Lighting_RealtimeOnly.lighting";
            LightingSettings settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(path);
            if (settings == null)
            {
                settings = new LightingSettings { name = "Lighting_RealtimeOnly", bakedGI = false, realtimeGI = false };
                AssetDatabase.CreateAsset(settings, path);
            }
            Lightmapping.SetLightingSettingsForScene(scene, settings);
        }

        /// <summary>
        /// 예제 모드 설정(Mod Project)과 번역 파일을 만든다.
        /// </summary>
        private static void CreateModProject()
        {
            string stringsPath = $"{ModSdkSettings.TemplateFolder}/strings.csv";
            File.WriteAllText(stringsPath, "key,ko,en\nmod.example.template.example,예제 맵,Example Map\n", new System.Text.UTF8Encoding(false));
            AssetDatabase.ImportAsset(stringsPath);

            string projectPath = $"{ModSdkSettings.TemplateFolder}/ExampleModProject.asset";
            AssetDatabase.DeleteAsset(projectPath);
            var project = ScriptableObject.CreateInstance<ModProject>();
            project.modId = "example.template";
            project.modName = "Example Template";
            project.version = "1.0.0";
            project.author = "Hide&Freak";
            project.description = "모드 SDK 예제 맵. 이 설정을 복사해 자기 모드를 만드세요.";
            project.minGameVersion = PlayerSettings.bundleVersion;
            project.strings = AssetDatabase.LoadAssetAtPath<TextAsset>(stringsPath);
            project.maps.Add(new ModProject.MapSetting
            {
                scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath),
                mapKey = "example",
                displayName = "Example Map",
                minPlayers = 0,
                maxPlayers = 0,
                settings = { new ModProject.RangeSetting { id = "GuardCount", value = 1f, min = 1f, max = 2f } }
            });
            AssetDatabase.CreateAsset(project, projectPath);
        }

        /// <summary>
        /// 예제용 단색 URP 머티리얼을 만든다. (템플릿 폴더 안)
        /// </summary>
        private static Dictionary<string, Material> CreateMaterials()
        {
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            var colors = new Dictionary<string, Color>
            {
                { "Floor", new Color(0.35f, 0.35f, 0.38f) },
                { "Wall", new Color(0.55f, 0.52f, 0.48f) },
                { "Prop", new Color(0.6f, 0.45f, 0.3f) },
                { "LampOn", Color.white },
                { "LampOff", new Color(0.2f, 0.2f, 0.2f) }
            };

            var result = new Dictionary<string, Material>();
            foreach (KeyValuePair<string, Color> pair in colors)
            {
                string path = $"{ModSdkSettings.TemplateFolder}/M_Example{pair.Key}.mat";
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(lit);
                    AssetDatabase.CreateAsset(material, path);
                }
                material.SetColor("_BaseColor", pair.Value);
                if (pair.Key == "LampOn")
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", Color.white * 2f);
                }
                EditorUtility.SetDirty(material);
                result[pair.Key] = material;
            }
            return result;
        }

        /// <summary>
        /// 자식 오브젝트를 만든다.
        /// </summary>
        private static Transform CreateChild(Transform parent, string name)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false);
            return child;
        }

        /// <summary>
        /// 충돌체가 있는 정적 상자를 만든다.
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
        /// 외벽을 벽 조각으로 등록한다.
        /// </summary>
        private static void RegisterWall(GameObject wall)
        {
            PropRegistrar.Register(wall, isWall: true, absorbChildren: true);
        }
    }
}
