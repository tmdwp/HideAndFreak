using System.Collections.Generic;
using System.IO;
using System.Linq;
using GuardVsMannequin.Cameras;
using GuardVsMannequin.Core;
using GuardVsMannequin.Map;
using GuardVsMannequin.Objectives;
using GuardVsMannequin.Player;
using GuardVsMannequin.Props;
using GuardVsMannequin.Room;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace GuardVsMannequin.Editor
{
    /// <summary>
    /// 외부 에셋의 데모 씬으로 게임 맵 씬을 만든다. (<see cref="MapCatalog"/>의 1번 이후 맵)
    /// 1. 원본 씬의 오브젝트를 가져와 게임에 필요 없는 스크립트·카메라·UI를 제거하고 머티리얼을 URP로 변환한다.
    /// 2. 충돌체가 없는 메시에 충돌체를 붙이고, 크기·이름으로 변신 가능 사물과 벽을 골라 맵 전용 사물 목록을 만든다.
    /// 3. NavMesh를 임시로 구워 플레이 가능한 바닥(실내 맵은 천장이 있는 곳)을 찾고,
    ///    마네킹 스폰 지점·전원 장치 후보·가드 출발 지점을 고르게 배치한다.
    /// 4. 조명을 사냥 시간에 꺼지는 MapLight로 등록하고, 부족하면 조명을 추가한다.
    /// 5. 맵 밖(아래)에 닫힌 가드 대기실을 만들고, 탐색 단계가 시작되면 가드를 출발 지점으로 옮긴다.
    /// 6. 투명 경계(벽·천장), 게임 시스템, HUD를 추가하고 저장한 뒤 빌드 설정과 룸 맵 목록에 등록한다.
    /// 자동 배치는 추정이므로, 생성 후 씬에서 위치를 확인하고 필요하면 옮긴다.
    /// "테스트 환경 생성"을 먼저 실행해 프리팹이 있어야 한다.
    /// </summary>
    public static class MapSceneBuilder
    {
        private const int FloorSampleCount = 600;
        private const int MannequinSpawnCount = 10;
        // 자동 크기 조정: 걸을 수 있는 바닥이 이보다 좁으면 환경을 키운다. (최대 10명 기준)
        private const float MinFloorArea = 800f;
        private const float MaxAutoScale = 2.5f;

        // 룸 설정의 전원 장치 수 최댓값(game_settings.csv)과 맞춘다.
        internal const int GeneratorCandidateCount = 12;
        private const int GuardReleaseCount = 5;
        private const int WaitingRoomSpawnCount = 5;

        private const float SpawnEdgeClearance = 0.5f;
        internal const float GeneratorEdgeClearance = 1.0f;
        private const float IndoorCeilingCheck = 12f;
        private const float WaitingRoomDepth = 60f;
        private const float LampSpacingArea = 70f;
        private const int ShadowLightLimit = 24;


        // 이름에 들어 있으면 변신 사물에서 제외하는 단어 (구조물·조명·바닥재 등)
        private static readonly string[] NonPropKeywords =
        {
            "floor", "wall", "ceil", "roof", "door", "window", "stair", "step", "light", "lamp", "bulb", "pipe", "beam",
            "column", "pillar", "frame", "trim", "ground", "terrain", "grass", "decal", "cable", "wire", "rail", "tile",
            "skirting", "baseboard", "molding", "moulding", "glass", "road", "building", "house", "fence", "stairs"
        };

        #region 메뉴

        [MenuItem("GuardVsMannequin/맵 생성/전체 맵 생성")]
        private static void BuildAllMaps()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            foreach (MapCatalog.Entry map in MapCatalog.Maps.Where(m => !string.IsNullOrEmpty(m.sourceScenePath)))
                BuildMap(map);
            FinishBuild();
        }

        [MenuItem("GuardVsMannequin/맵 생성/1 Abandoned Asylum")]
        private static void BuildAsylum() => BuildSingle(1);

        [MenuItem("GuardVsMannequin/맵 생성/2 Industrial")]
        private static void BuildIndustrial() => BuildSingle(2);

        [MenuItem("GuardVsMannequin/맵 생성/3 Horror Hospital")]
        private static void BuildHospital() => BuildSingle(3);

        [MenuItem("GuardVsMannequin/맵 생성/4 Alchemist House")]
        private static void BuildAlchemistHouse() => BuildSingle(4);

        /// <summary>
        /// 맵 하나를 만든다.
        /// </summary>
        private static void BuildSingle(int mapId)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            BuildMap(MapCatalog.Maps[mapId]);
            FinishBuild();
        }

        /// <summary>
        /// 빌드 설정, 룸의 맵 목록, 맵 이름 문구, 맵 설정 범위를 갱신하고 에셋을 저장한다.
        /// </summary>
        private static void FinishBuild()
        {
            MapRegistrySync.Sync();
        }

        #endregion

        /// <summary>
        /// 맵 씬 하나를 만들어 저장한다.
        /// </summary>
        private static void BuildMap(MapCatalog.Entry map)
        {
            if (!TestEnvironmentSetupTool.TryLoadScenePrefabs(out TestEnvironmentSetupTool.ScenePrefabs prefabs))
            {
                Debug.LogError("[MapSceneBuilder] 가드·마네킹 프리팹이 없습니다. 'GuardVsMannequin/테스트 환경 생성'을 먼저 실행하세요.");
                return;
            }
            if (!File.Exists(map.sourceScenePath))
            {
                Debug.LogError($"[MapSceneBuilder] 원본 씬을 찾을 수 없습니다: {map.sourceScenePath}");
                return;
            }

            Debug.Log($"[MapSceneBuilder] {map.displayName} 맵 생성 시작");
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SpectatorController spectator = TestEnvironmentSetupTool.CreateSceneCamera();

            Transform mapRoot = new GameObject("Map").transform;
            Transform environment = ImportEnvironment(map.sourceScenePath, scene, mapRoot);
            CleanEnvironment(environment);

            int converted = UrpMaterialConverter.ConvertRenderers(environment.GetComponentsInChildren<Renderer>(true));
            ConvertTerrains(environment);
            int colliders = AddMissingColliders(environment);
            Physics.SyncTransforms();

            NavMeshSurface navMeshSurface = mapRoot.gameObject.AddComponent<NavMeshSurface>();
            navMeshSurface.collectObjects = CollectObjects.Children;
            navMeshSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;

            // 작은 맵은 환경 전체를 키운다. (변신 사물 크기도 커진 상태로 등록되도록 사물 목록보다 먼저)
            float mapScale = ScaleEnvironment(map, environment, navMeshSurface);

            PropDatabase propDatabase = CreatePropDatabase(map, environment, out int propCount, out int wallCount);

            List<Vector3> floor = SampleFloorPoints(navMeshSurface, map.indoor);
            Bounds playArea = GetPlayArea(floor, environment);

            List<Vector3> mannequinSpawns = PickSpread(floor, MannequinSpawnCount, SpawnEdgeClearance);
            List<Vector3> generatorPoints = PickSpread(floor, GeneratorCandidateCount, GeneratorEdgeClearance);
            List<Vector3> guardReleases = PickSpread(floor, GuardReleaseCount, SpawnEdgeClearance);
            int addedLamps = SetupLights(mapRoot, environment, floor, map.indoor);

            // NavMesh는 서버가 실행 중에 다시 굽는다. 에디터에서 구운 데이터는 배치에만 쓰고 지운다.
            navMeshSurface.RemoveData();
            navMeshSurface.navMeshData = null;

            CreatePoints(mapRoot, "MannequinSpawnPoints", mannequinSpawns, playArea.center,
                (parent, position, rotation) => TestEnvironmentSetupTool.CreateSpawnPoint(parent, PlayerRole.Mannequin, position, rotation));
            CreatePoints(mapRoot, "GeneratorSpawnPoints", generatorPoints, playArea.center,
                (parent, position, rotation) => CreateMarker<GeneratorSpawnPoint>(parent, "GeneratorSpawnPoint", position, rotation));
            CreatePoints(mapRoot, "GuardReleasePoints", guardReleases, playArea.center,
                (parent, position, rotation) => CreateMarker<GuardReleasePoint>(parent, "GuardReleasePoint", position, rotation));

            float ceilingHeight = CreateBoundary(playArea, environment);
            CreateWaitingRoom(playArea);

            MapInfo mapInfo = mapRoot.gameObject.AddComponent<MapInfo>();
            var mapInfoSerialized = new SerializedObject(mapInfo);
            mapInfoSerialized.FindProperty("mapName").stringValue = map.displayName;
            mapInfoSerialized.FindProperty("ceilingHeight").floatValue = ceilingHeight;
            mapInfoSerialized.FindProperty("mannequinSkin").objectReferenceValue = AssetDatabase.LoadAssetAtPath<CharacterSkin>(map.mannequinSkinPath);
            mapInfoSerialized.FindProperty("propDatabase").objectReferenceValue = propDatabase;
            mapInfoSerialized.ApplyModifiedPropertiesWithoutUndo();

            TestEnvironmentSetupTool.GameSystemsRefs systems = TestEnvironmentSetupTool.CreateGameSystems(prefabs, navMeshSurface);
            TestEnvironmentSetupTool.CreateUi(systems, spectator);
            PlaceSceneCamera(spectator.transform, playArea);

            // 경로가 있어야 FishNet이 씬 ID를 만들므로 먼저 저장한다.
            EditorSceneManager.SaveScene(scene, map.ScenePath);
            TestEnvironmentSetupTool.GenerateNetworkSceneIds(scene);

            // 경계 밖·꺼진 오브젝트 제거, 정적 플래그, 오클루전 컬링 굽기 (저장된 경로가 필요)
            MapOptimizer.Optimize(scene, map.indoor);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log($"[MapSceneBuilder] {map.displayName} 완료: {map.ScenePath}\n" +
                      $"맵 크기 배율 {mapScale:F2}, 머티리얼 변환 {converted}개, 충돌체 추가 {colliders}개, 변신 사물 {propCount}개(벽 {wallCount}개), " +
                      $"바닥 샘플 {floor.Count}개, 조명 추가 {addedLamps}개. 스폰·전원 장치·출발 지점 위치를 씬에서 확인하세요.");
        }

        #region 환경 가져오기

        /// <summary>
        /// 원본 씬을 추가로 열어 모든 루트 오브젝트를 새 씬의 Map/Environment 아래로 옮기고 원본은 저장하지 않고 닫는다.
        /// 하늘과 환경광 설정도 가져온다.
        /// </summary>
        private static Transform ImportEnvironment(string sourcePath, Scene target, Transform mapRoot)
        {
            Scene source = EditorSceneManager.OpenScene(sourcePath, OpenSceneMode.Additive);

            SceneManager.SetActiveScene(source);
            Material skybox = RenderSettings.skybox;
            AmbientMode ambientMode = RenderSettings.ambientMode;
            Color ambientSky = RenderSettings.ambientSkyColor;
            Color ambientEquator = RenderSettings.ambientEquatorColor;
            Color ambientGround = RenderSettings.ambientGroundColor;
            float ambientIntensity = RenderSettings.ambientIntensity;
            SceneManager.SetActiveScene(target);

            RenderSettings.skybox = skybox;
            RenderSettings.ambientMode = ambientMode;
            RenderSettings.ambientSkyColor = ambientSky;
            RenderSettings.ambientEquatorColor = ambientEquator;
            RenderSettings.ambientGroundColor = ambientGround;
            RenderSettings.ambientIntensity = ambientIntensity;

            Transform environment = new GameObject("Environment").transform;
            environment.SetParent(mapRoot, false);
            foreach (GameObject root in source.GetRootGameObjects())
            {
                SceneManager.MoveGameObjectToScene(root, target);
                root.transform.SetParent(environment, true);
            }

            EditorSceneManager.CloseScene(source, true);
            return environment;
        }

        /// <summary>
        /// 게임에 방해되는 것을 제거한다.
        /// - 카메라·플레이어 컨트롤러·UI·이벤트 시스템 오브젝트
        /// - 누락된 스크립트, Unity 기본 컴포넌트가 아닌 스크립트(에셋 데모용 스크립트)
        /// - 물리 오브젝트는 움직이지 않게 고정 (피어마다 따로 시뮬레이션되면 위치가 어긋나므로)
        /// </summary>
        private static void CleanEnvironment(Transform environment)
        {
            // 프리팹 인스턴스 안의 오브젝트는 지울 수 없으므로 먼저 연결을 푼다. (생성된 맵은 원본 에셋과 독립)
            foreach (Transform child in environment.GetComponentsInChildren<Transform>(true))
            {
                if (child != null && PrefabUtility.IsOutermostPrefabInstanceRoot(child.gameObject))
                    PrefabUtility.UnpackPrefabInstance(child.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }

            foreach (Transform child in environment.GetComponentsInChildren<Transform>(true).ToArray())
            {
                if (child == null || child == environment)
                    continue;

                GameObject target = child.gameObject;
                if (target.GetComponent<Camera>() != null || target.GetComponent<CharacterController>() != null
                    || target.GetComponent<Canvas>() != null || target.GetComponent<UnityEngine.EventSystems.EventSystem>() != null)
                {
                    Object.DestroyImmediate(target);
                    continue;
                }

                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(target);
                foreach (MonoBehaviour behaviour in target.GetComponents<MonoBehaviour>())
                {
                    string ns = behaviour.GetType().Namespace ?? string.Empty;
                    if (!ns.StartsWith("UnityEngine"))
                        Object.DestroyImmediate(behaviour);
                }

                foreach (AudioListener listener in target.GetComponents<AudioListener>())
                    Object.DestroyImmediate(listener);
                foreach (Rigidbody body in target.GetComponents<Rigidbody>())
                    body.isKinematic = true;
            }
        }

        /// <summary>
        /// 지형 머티리얼이 URP용이 아니면 URP 지형 머티리얼로 바꾼다.
        /// </summary>
        private static void ConvertTerrains(Transform environment)
        {
            Terrain[] terrains = environment.GetComponentsInChildren<Terrain>(true);
            if (terrains.Length == 0)
                return;

            Shader terrainShader = Shader.Find("Universal Render Pipeline/Terrain/Lit");
            if (terrainShader == null)
                return;

            string path = $"{TestEnvironmentSetupTool.MaterialFolder}/M_TerrainLit.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(terrainShader);
                AssetDatabase.CreateAsset(material, path);
            }

            foreach (Terrain terrain in terrains)
            {
                if (terrain.materialTemplate == null || !terrain.materialTemplate.shader.name.StartsWith("Universal Render Pipeline"))
                    terrain.materialTemplate = material;
            }
        }

        /// <summary>
        /// 충돌체가 없는 메시에 메시 충돌체를 붙인다. (LOD는 가장 자세한 단계만, 아주 작은 장식은 제외)
        /// </summary>
        internal static int AddMissingColliders(Transform environment)
        {
            HashSet<Renderer> lowerLods = GetLowerLodRenderers(environment);
            int added = 0;
            foreach (MeshRenderer meshRenderer in environment.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (lowerLods.Contains(meshRenderer) || meshRenderer.GetComponent<Collider>() != null)
                    continue;

                MeshFilter filter = meshRenderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null)
                    continue;
                if (meshRenderer.bounds.size.magnitude < 0.1f)
                    continue;

                MeshCollider meshCollider = meshRenderer.gameObject.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = filter.sharedMesh;
                added++;
            }
            return added;
        }

        /// <summary>
        /// LOD 그룹에서 가장 자세한 단계(LOD0)가 아닌 렌더러를 모은다.
        /// </summary>
        private static HashSet<Renderer> GetLowerLodRenderers(Transform environment)
        {
            var result = new HashSet<Renderer>();
            foreach (LODGroup group in environment.GetComponentsInChildren<LODGroup>(true))
            {
                LOD[] lods = group.GetLODs();
                for (int i = 1; i < lods.Length; i++)
                {
                    foreach (Renderer lodRenderer in lods[i].renderers)
                    {
                        if (lodRenderer != null)
                            result.Add(lodRenderer);
                    }
                }
            }
            return result;
        }

        #endregion

        #region 맵 크기

        /// <summary>
        /// 환경을 키울 배율을 정해 적용한다. 적용한 배율을 반환한다.
        /// - MapCatalog의 scale이 0보다 크면 그 값을 그대로 쓴다.
        /// - 0이면 걸을 수 있는 바닥 넓이를 재서 <see cref="MinFloorArea"/>보다 좁을 때만 넓이가 맞도록 키운다. (최대 <see cref="MaxAutoScale"/>배)
        /// 조명의 범위도 같은 배율로 늘린다. (Light.range는 트랜스폼 크기의 영향을 받지 않으므로)
        /// </summary>
        private static float ScaleEnvironment(MapCatalog.Entry map, Transform environment, NavMeshSurface surface)
        {
            float factor = map.scale;
            if (factor <= 0f)
            {
                List<Vector3> floor = SampleFloorPoints(surface, map.indoor);
                float area = EstimateFloorArea(floor);
                factor = area > 0f && area < MinFloorArea ? Mathf.Clamp(Mathf.Sqrt(MinFloorArea / area), 1f, MaxAutoScale) : 1f;
                surface.RemoveData();
                surface.navMeshData = null;
                Debug.Log($"[MapSceneBuilder] {map.displayName}: 바닥 넓이 약 {area:F0}㎡ → 배율 {factor:F2}");
            }

            if (Mathf.Approximately(factor, 1f))
                return 1f;

            environment.localScale *= factor;
            foreach (Light sceneLight in environment.GetComponentsInChildren<Light>(true))
            {
                if (sceneLight.type != LightType.Directional)
                    sceneLight.range *= factor;
            }
            Physics.SyncTransforms();
            return factor;
        }

        #endregion

        #region 변신 사물

        /// <summary>
        /// 크기와 이름으로 변신 가능 사물과 벽을 골라 맵 전용 사물 목록에 등록한다.
        /// - 사물: 가장 긴 변 0.3~2.6m, 높이 2.4m 이하, 바닥 면적 4㎡ 미만, 구조물 이름이 아님
        /// - 벽: 높이 2m 이상, 두께 0.6m 이하, 길이 1m 이상 → 고정 크기 벽 조각으로 변신
        /// 조명 기구와 LOD 하위 단계는 제외한다.
        /// </summary>
        private static PropDatabase CreatePropDatabase(MapCatalog.Entry map, Transform environment, out int propCount, out int wallCount)
        {
            PropDatabase database = GetOrCreatePropDatabase(map.sceneName);
            database.Clear();

            var registrar = new PropRegistrar(database);
            HashSet<Renderer> lowerLods = GetLowerLodRenderers(environment);
            propCount = 0;
            wallCount = 0;

            foreach (MeshRenderer meshRenderer in environment.GetComponentsInChildren<MeshRenderer>(true))
            {
                MeshFilter filter = meshRenderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null || lowerLods.Contains(meshRenderer))
                    continue;
                if (meshRenderer.GetComponent<Collider>() == null || IsLightFixture(meshRenderer.transform))
                    continue;

                Vector3 size = meshRenderer.bounds.size;
                float longest = Mathf.Max(size.x, size.y, size.z);
                float thickness = Mathf.Min(size.x, size.z);
                float length = Mathf.Max(size.x, size.z);

                if (size.y >= 2f && thickness <= 0.6f && length >= 1f)
                {
                    if (registrar.RegisterWall(meshRenderer))
                        wallCount++;
                    continue;
                }

                bool isProp = longest >= 0.3f && longest <= 2.6f && size.y <= 2.4f && size.x * size.z < 4f
                              && !HasKeyword(meshRenderer.transform, NonPropKeywords);
                if (isProp && registrar.RegisterProp(meshRenderer))
                    propCount++;
            }

            registrar.Save();
            return database;
        }

        /// <summary>
        /// 맵 전용 사물 목록 에셋(ScriptableObjects/PropDatabase_&lt;씬 이름&gt;)을 가져오거나 만든다.
        /// </summary>
        internal static PropDatabase GetOrCreatePropDatabase(string sceneName)
        {
            string path = $"{TestEnvironmentSetupTool.SoFolder}/PropDatabase_{sceneName}.asset";
            PropDatabase database = AssetDatabase.LoadAssetAtPath<PropDatabase>(path);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<PropDatabase>();
                AssetDatabase.CreateAsset(database, path);
            }
            return database;
        }

        /// <summary>
        /// 조명이 붙은 오브젝트(조명 기구)인지 확인한다. 조명 기구는 변신 대상이 아니다.
        /// </summary>
        private static bool IsLightFixture(Transform target)
        {
            return target.GetComponentInChildren<Light>(true) != null
                   || (target.parent != null && target.parent.GetComponentInChildren<Light>(true) != null && target.parent.childCount <= 3);
        }

        /// <summary>
        /// 오브젝트나 부모 이름에 제외 단어가 들어 있는지 확인한다. (대소문자 무시)
        /// </summary>
        private static bool HasKeyword(Transform target, string[] keywords)
        {
            string name = (target.name + " " + (target.parent != null ? target.parent.name : string.Empty)).ToLowerInvariant();
            return keywords.Any(name.Contains);
        }

        #endregion

        #region 바닥 분석과 배치

        /// <summary>
        /// NavMesh를 구워 걸을 수 있는 바닥에서 점을 넓이에 비례해 뽑는다.
        /// 벽에 너무 붙은 점, (실내 맵이면) 천장이 없는 점을 버리고, 가장 넓게 이어진 영역의 점만 남긴다.
        /// (지붕 위처럼 떨어진 곳을 제외하기 위함)
        /// </summary>
        internal static List<Vector3> SampleFloorPoints(NavMeshSurface surface, bool indoor)
        {
            surface.BuildNavMesh();
            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
            List<Vector3> result;
            int triangleCount = triangulation.indices.Length / 3;
            if (triangleCount == 0)
            {
                Debug.LogError("[MapSceneBuilder] NavMesh를 만들지 못했습니다. 바닥에 충돌체가 있는지 확인하세요.");
                return new List<Vector3>();
            }

            // 삼각형 넓이의 누적 합으로 넓이 비례 추출
            var cumulative = new float[triangleCount];
            float total = 0f;
            for (int i = 0; i < triangleCount; i++)
            {
                Vector3 a = triangulation.vertices[triangulation.indices[i * 3]];
                Vector3 b = triangulation.vertices[triangulation.indices[i * 3 + 1]];
                Vector3 c = triangulation.vertices[triangulation.indices[i * 3 + 2]];
                total += Vector3.Cross(b - a, c - a).magnitude * 0.5f;
                cumulative[i] = total;
            }

            result = DrawPoints(triangulation, cumulative, total, indoor);

            // 천장이 없는 실내 데모 씬(위가 열린 전시용 방 등)은 천장 조건 없이 다시 뽑는다.
            if (indoor && result.Count < FloorSampleCount / 20)
            {
                Debug.LogWarning("[MapSceneBuilder] 천장이 있는 바닥이 거의 없어 천장 조건 없이 배치합니다.");
                result = DrawPoints(triangulation, cumulative, total, false);
            }

            return KeepLargestConnectedRegion(result);
        }

        /// <summary>
        /// 넓이 비례로 점을 뽑고, 벽에 너무 붙은 점과 (조건이 있으면) 천장이 없는 점을 버린다.
        /// </summary>
        private static List<Vector3> DrawPoints(NavMeshTriangulation triangulation, float[] cumulative, float total, bool requireCeiling)
        {
            var result = new List<Vector3>();
            var random = new System.Random(20260927);
            for (int n = 0; n < FloorSampleCount; n++)
            {
                float pick = (float)random.NextDouble() * total;
                int index = System.Array.BinarySearch(cumulative, pick);
                if (index < 0)
                    index = ~index;
                index = Mathf.Clamp(index, 0, cumulative.Length - 1);

                Vector3 point = RandomPointInTriangle(triangulation, index, random);
                if (NavMeshEdgeTooClose(point, SpawnEdgeClearance))
                    continue;
                if (requireCeiling && !Physics.Raycast(point + Vector3.up * 0.5f, Vector3.up, IndoorCeilingCheck))
                    continue;
                result.Add(point);
            }
            return result;
        }

        /// <summary>
        /// 삼각형 안의 무작위 점.
        /// </summary>
        private static Vector3 RandomPointInTriangle(NavMeshTriangulation triangulation, int triangle, System.Random random)
        {
            Vector3 a = triangulation.vertices[triangulation.indices[triangle * 3]];
            Vector3 b = triangulation.vertices[triangulation.indices[triangle * 3 + 1]];
            Vector3 c = triangulation.vertices[triangulation.indices[triangle * 3 + 2]];
            float r1 = Mathf.Sqrt((float)random.NextDouble());
            float r2 = (float)random.NextDouble();
            return (1f - r1) * a + r1 * (1f - r2) * b + r1 * r2 * c;
        }

        /// <summary>
        /// 경로가 이어지는 점이 가장 많은 영역만 남긴다.
        /// </summary>
        private static List<Vector3> KeepLargestConnectedRegion(List<Vector3> points)
        {
            if (points.Count < 2)
                return points;

            var path = new NavMeshPath();
            List<Vector3> best = points;
            int bestCount = -1;
            // 후보 기준점 몇 개로 각각 도달 가능한 점을 세고, 가장 많은 쪽을 고른다.
            int seedStep = Mathf.Max(1, points.Count / 12);
            for (int s = 0; s < points.Count; s += seedStep)
            {
                var reachable = new List<Vector3>();
                foreach (Vector3 point in points)
                {
                    if (NavMesh.CalculatePath(points[s], point, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete)
                        reachable.Add(point);
                }
                if (reachable.Count > bestCount)
                {
                    bestCount = reachable.Count;
                    best = reachable;
                }
                if (bestCount > points.Count / 2)
                    break;
            }
            return best;
        }

        /// <summary>
        /// 서로 최대한 멀리 떨어진 점을 개수만큼 고른다. (가장 먼 점 샘플링)
        /// 가장자리 여유가 부족한 점은 제외한다.
        /// </summary>
        internal static List<Vector3> PickSpread(List<Vector3> points, int count, float edgeClearance)
        {
            List<Vector3> candidates = points
                .Where(p => !NavMeshEdgeTooClose(p, edgeClearance))
                .ToList();
            if (candidates.Count == 0)
                candidates = new List<Vector3>(points);

            var chosen = new List<Vector3>();
            if (candidates.Count == 0 || count <= 0)
                return chosen;

            chosen.Add(candidates[candidates.Count / 2]);
            while (chosen.Count < count && chosen.Count < candidates.Count)
            {
                Vector3 farthest = candidates[0];
                float farthestDistance = -1f;
                foreach (Vector3 candidate in candidates)
                {
                    float nearest = chosen.Min(c => (c - candidate).sqrMagnitude);
                    if (nearest > farthestDistance)
                    {
                        farthestDistance = nearest;
                        farthest = candidate;
                    }
                }
                chosen.Add(farthest);
            }
            return chosen;
        }

        /// <summary>
        /// NavMesh 가장자리(벽)까지의 거리가 기준보다 가까운지 확인한다.
        /// </summary>
        private static bool NavMeshEdgeTooClose(Vector3 point, float clearance)
        {
            return NavMesh.FindClosestEdge(point, out NavMeshHit edge, NavMesh.AllAreas) && edge.distance < clearance;
        }

        /// <summary>
        /// 바닥 점의 범위로 플레이 영역을 정한다. 점이 없으면 환경 전체 범위를 쓴다.
        /// </summary>
        private static Bounds GetPlayArea(List<Vector3> floor, Transform environment)
        {
            if (floor.Count == 0)
                return CharacterAssetBuilder.GetRendererBounds(environment.gameObject);

            var bounds = new Bounds(floor[0], Vector3.zero);
            foreach (Vector3 point in floor)
                bounds.Encapsulate(point);
            bounds.Expand(new Vector3(4f, 0f, 4f));
            return bounds;
        }

        /// <summary>
        /// 점마다 오브젝트를 만들고 플레이 영역 중심을 바라보게 한다.
        /// </summary>
        internal static void CreatePoints(Transform mapRoot, string groupName, List<Vector3> positions, Vector3 center,
            System.Action<Transform, Vector3, Quaternion> create)
        {
            Transform group = new GameObject(groupName).transform;
            group.SetParent(mapRoot, false);
            foreach (Vector3 position in positions)
            {
                Vector3 toCenter = center - position;
                toCenter.y = 0f;
                Quaternion rotation = toCenter.sqrMagnitude > 0.01f ? Quaternion.LookRotation(toCenter) : Quaternion.identity;
                create(group, position, rotation);
            }
        }

        /// <summary>
        /// 컴포넌트 하나를 가진 표식 오브젝트를 만든다.
        /// </summary>
        internal static void CreateMarker<T>(Transform parent, string name, Vector3 position, Quaternion rotation) where T : Component
        {
            var marker = new GameObject(name);
            marker.transform.SetParent(parent, false);
            marker.transform.SetPositionAndRotation(position, rotation);
            marker.AddComponent<T>();
        }

        #endregion

        #region 조명

        /// <summary>
        /// 모든 조명을 실시간으로 바꾸고 사냥 시간에 꺼지는 MapLight로 등록한다.
        /// (다른 씬에서 옮겨 온 구운 조명은 라이트맵이 없어 빛을 내지 않으므로)
        /// 조명이 바닥 넓이에 비해 부족하면 천장 아래에 조명을 추가한다. 추가한 개수를 반환한다.
        /// </summary>
        private static int SetupLights(Transform mapRoot, Transform environment, List<Vector3> floor, bool indoor)
        {
            Transform lightsRoot = new GameObject("Lights").transform;
            lightsRoot.SetParent(mapRoot, false);

            Light[] existing = environment.GetComponentsInChildren<Light>(true);
            foreach (Light sceneLight in existing)
            {
                sceneLight.lightmapBakeType = LightmapBakeType.Realtime;
                if (existing.Length > ShadowLightLimit && sceneLight.type != LightType.Directional)
                    sceneLight.shadows = LightShadows.None;
                if (sceneLight.GetComponent<MapLight>() == null)
                    sceneLight.gameObject.AddComponent<MapLight>();
            }

            int wanted = Mathf.Max(4, Mathf.RoundToInt(floor.Count > 0 ? EstimateFloorArea(floor) / LampSpacingArea : 4f));
            int pointLights = existing.Count(l => l.type != LightType.Directional);
            int toAdd = Mathf.Max(0, wanted - pointLights);
            List<Vector3> lampSpots = PickSpread(floor, toAdd, 0f);
            foreach (Vector3 spot in lampSpots)
                CreateLamp(lightsRoot, spot, indoor);

            // 실외 맵에 방향광이 없으면 추가한다. (사냥 시간에 꺼짐)
            if (!indoor && !existing.Any(l => l.type == LightType.Directional))
            {
                var sun = new GameObject("Directional Light");
                sun.transform.SetParent(lightsRoot, false);
                sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                Light sunLight = sun.AddComponent<Light>();
                sunLight.type = LightType.Directional;
                sunLight.shadows = LightShadows.Soft;
                sun.AddComponent<MapLight>();
            }

            HuntingLightingController controller = lightsRoot.gameObject.AddComponent<HuntingLightingController>();
            TestEnvironmentSetupTool.SetField(controller, "noiseLibrary", AssetDatabase.LoadAssetAtPath<Object>(TestEnvironmentSetupTool.NoiseLibraryPath));
            return lampSpots.Count;
        }

        /// <summary>
        /// 바닥 점이 덮는 대략적인 넓이. (2m 격자 칸 수)
        /// </summary>
        internal static float EstimateFloorArea(List<Vector3> floor)
        {
            var cells = new HashSet<Vector2Int>();
            foreach (Vector3 point in floor)
                cells.Add(new Vector2Int(Mathf.FloorToInt(point.x / 2f), Mathf.FloorToInt(point.z / 2f)));
            return cells.Count * 4f;
        }

        /// <summary>
        /// 천장 바로 아래(천장이 없으면 3m 높이)에 포인트 조명을 만든다.
        /// </summary>
        private static void CreateLamp(Transform parent, Vector3 floorPoint, bool indoor)
        {
            float height = 3f;
            if (Physics.Raycast(floorPoint + Vector3.up * 0.5f, Vector3.up, out RaycastHit hit, IndoorCeilingCheck))
                height = Mathf.Max(1.8f, hit.distance + 0.5f - 0.3f);
            else if (indoor)
                height = 2.7f;

            var lamp = new GameObject("MapLamp");
            lamp.transform.SetParent(parent, false);
            lamp.transform.position = floorPoint + Vector3.up * height;

            Light point = lamp.AddComponent<Light>();
            point.type = LightType.Point;
            point.range = 10f;
            point.intensity = 2.5f;
            point.color = new Color(1f, 0.92f, 0.8f);
            point.shadows = LightShadows.None;
            lamp.AddComponent<MapLight>();
        }

        #endregion

        #region 경계와 대기실

        /// <summary>
        /// 플레이 영역을 둘러싸는 투명 벽과 천장(충돌체만)을 만든다. 맵 루트 밖에 두어 NavMesh와 변신 대상에서 제외한다.
        /// 맵 정보에 쓸 천장 높이(바닥에서 투명 천장까지)를 반환한다.
        /// </summary>
        internal static float CreateBoundary(Bounds playArea, Transform environment)
        {
            Bounds environmentBounds = CharacterAssetBuilder.GetRendererBounds(environment.gameObject);
            float floorY = playArea.min.y;
            float top = Mathf.Min(environmentBounds.max.y + 1f, floorY + GameConstants.GunRange - 10f);
            top = Mathf.Max(top, playArea.max.y + 4f);

            Transform boundary = new GameObject("Boundary (Invisible)").transform;
            float height = top - floorY + 10f;
            float centerY = floorY - 5f + height * 0.5f;
            Vector3 center = playArea.center;
            Vector3 size = playArea.size + new Vector3(2f, 0f, 2f);

            CreateInvisibleBox(boundary, "Ceiling", new Vector3(center.x, top + 0.25f, center.z), new Vector3(size.x, 0.5f, size.z));
            CreateInvisibleBox(boundary, "Wall_North", new Vector3(center.x, centerY, center.z + size.z * 0.5f), new Vector3(size.x, height, 0.5f));
            CreateInvisibleBox(boundary, "Wall_South", new Vector3(center.x, centerY, center.z - size.z * 0.5f), new Vector3(size.x, height, 0.5f));
            CreateInvisibleBox(boundary, "Wall_East", new Vector3(center.x + size.x * 0.5f, centerY, center.z), new Vector3(0.5f, height, size.z));
            CreateInvisibleBox(boundary, "Wall_West", new Vector3(center.x - size.x * 0.5f, centerY, center.z), new Vector3(0.5f, height, size.z));
            return top - floorY;
        }

        /// <summary>
        /// 보이지 않는 상자 충돌체를 만든다.
        /// </summary>
        private static void CreateInvisibleBox(Transform parent, string name, Vector3 position, Vector3 size)
        {
            var box = new GameObject(name);
            box.transform.SetParent(parent, false);
            box.transform.position = position;
            box.isStatic = true;
            box.AddComponent<BoxCollider>().size = size;
        }

        /// <summary>
        /// 맵 아래 떨어진 곳에 닫힌 가드 대기실을 만든다. (가드는 대기 시간 동안 맵을 볼 수 없다)
        /// 대기실 영역(이동 검증), 가드 스폰 지점, 탐색 단계 시작 시 가드를 맵으로 옮기는 장치를 포함한다.
        /// </summary>
        internal static void CreateWaitingRoom(Bounds playArea)
        {
            const float roomSize = 8f;
            const float roomHeight = 3f;
            const float wall = 0.5f;

            Material wallMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{TestEnvironmentSetupTool.MaterialFolder}/M_WaitingRoom.mat");
            Material floorMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{TestEnvironmentSetupTool.MaterialFolder}/M_Floor.mat");

            Transform room = new GameObject("WaitingRoom").transform;
            room.position = new Vector3(playArea.center.x, playArea.min.y - WaitingRoomDepth, playArea.center.z);

            float half = roomSize * 0.5f;
            TestEnvironmentSetupTool.CreateBlock(room, "Floor", new Vector3(0f, -0.25f, 0f), new Vector3(roomSize + wall * 2f, 0.5f, roomSize + wall * 2f), floorMaterial);
            TestEnvironmentSetupTool.CreateBlock(room, "Ceiling", new Vector3(0f, roomHeight + 0.25f, 0f), new Vector3(roomSize + wall * 2f, 0.5f, roomSize + wall * 2f), wallMaterial);
            TestEnvironmentSetupTool.CreateBlock(room, "Wall_North", new Vector3(0f, roomHeight * 0.5f, half + wall * 0.5f), new Vector3(roomSize + wall * 2f, roomHeight, wall), wallMaterial);
            TestEnvironmentSetupTool.CreateBlock(room, "Wall_South", new Vector3(0f, roomHeight * 0.5f, -half - wall * 0.5f), new Vector3(roomSize + wall * 2f, roomHeight, wall), wallMaterial);
            TestEnvironmentSetupTool.CreateBlock(room, "Wall_East", new Vector3(half + wall * 0.5f, roomHeight * 0.5f, 0f), new Vector3(wall, roomHeight, roomSize), wallMaterial);
            TestEnvironmentSetupTool.CreateBlock(room, "Wall_West", new Vector3(-half - wall * 0.5f, roomHeight * 0.5f, 0f), new Vector3(wall, roomHeight, roomSize), wallMaterial);

            var lightObject = new GameObject("RoomLight");
            lightObject.transform.SetParent(room, false);
            lightObject.transform.localPosition = new Vector3(0f, roomHeight - 0.3f, 0f);
            Light roomLight = lightObject.AddComponent<Light>();
            roomLight.type = LightType.Point;
            roomLight.range = 10f;
            roomLight.intensity = 2f;

            for (int i = 0; i < WaitingRoomSpawnCount; i++)
            {
                var local = new Vector3(-2f + (i % 3) * 2f, 0f, -1.5f + (i / 3) * 3f);
                TestEnvironmentSetupTool.CreateSpawnPoint(room, PlayerRole.Guard, room.position + local, Quaternion.identity);
            }

            var area = new SerializedObject(room.gameObject.AddComponent<WaitingRoomArea>());
            area.FindProperty("center").vector3Value = new Vector3(0f, roomHeight * 0.5f, 0f);
            area.FindProperty("size").vector3Value = new Vector3(roomSize + wall * 2f, roomHeight + 2f, roomSize + wall * 2f);
            area.ApplyModifiedPropertiesWithoutUndo();

            room.gameObject.AddComponent<GuardReleaseTeleporter>();
        }

        /// <summary>
        /// 대기 중 씬 카메라를 플레이 영역 위에서 내려다보게 한다.
        /// </summary>
        internal static void PlaceSceneCamera(Transform cameraTransform, Bounds playArea)
        {
            float distance = Mathf.Max(playArea.size.x, playArea.size.z) * 0.6f + 10f;
            Vector3 position = playArea.center + new Vector3(0f, distance * 0.8f, -distance * 0.6f);
            cameraTransform.SetPositionAndRotation(position, Quaternion.LookRotation(playArea.center - position));
        }

        #endregion
    }
}
