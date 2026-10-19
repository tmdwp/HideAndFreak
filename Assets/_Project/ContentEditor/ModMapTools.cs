using System.Linq;
using GuardVsMannequin.Core;
using GuardVsMannequin.Map;
using GuardVsMannequin.Objectives;
using GuardVsMannequin.Props;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace GuardVsMannequin.Editor
{
    /// <summary>
    /// 모드 맵을 만드는 도구 메뉴(Hide&amp;Freak &gt; 맵 도구). 모드 SDK에 들어가므로 게임 코드(네트워크·HUD) 없이 동작한다.
    /// 순서: 맵 뼈대 → 모델 배치 → 사물·벽·충돌체·조명 등록 → 지점 배치 → NavMesh 굽기 → 맵 검사 → 모드 내보내기
    /// </summary>
    public static class ModMapTools
    {
        private const string Menu = EditorMenus.Mod + "맵 도구/";

        /// <summary>
        /// 열린 씬에 맵 뼈대(Map: MapInfo·NavMeshSurface·대기 화면 위치, Environment, 지점 그룹)를 만든다.
        /// </summary>
        [MenuItem(Menu + "맵 뼈대 만들기 (열린 씬)")]
        private static void CreateSkeleton()
        {
            if (Object.FindAnyObjectByType<MapInfo>() != null)
            {
                EditorUtility.DisplayDialog("맵 뼈대", "이미 MapInfo가 있습니다.", "확인");
                return;
            }

            var map = new GameObject("Map");
            Undo.RegisterCreatedObjectUndo(map, "맵 뼈대");
            map.AddComponent<MapInfo>();
            NavMeshSurface surface = map.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;

            new GameObject("Environment").transform.SetParent(map.transform, false);
            foreach (string group in new[] { "MannequinSpawnPoints", "GuardSpawnPoints", "GeneratorSpawnPoints", "Lights" })
                new GameObject(group).transform.SetParent(map.transform, false);

            var overview = new GameObject("CameraOverview");
            overview.transform.SetParent(map.transform, false);
            overview.transform.SetPositionAndRotation(new Vector3(0f, 30f, -28f), Quaternion.Euler(50f, 0f, 0f));
            var info = new SerializedObject(map.GetComponent<MapInfo>());
            info.FindProperty("overviewPoint").objectReferenceValue = overview.transform;
            info.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(map.scene);
            Selection.activeGameObject = map;
            Debug.Log("[ModMapTools] 맵 뼈대를 만들었습니다. 맵 모델은 Map/Environment 아래에 넣으세요. (사람 키 1.8m 기준)");
        }

        /// <summary>선택한 오브젝트를 변신 사물로 등록한다. (여러 조각이면 부모를 선택)</summary>
        [MenuItem(Menu + "선택 → 변신 사물로 등록")]
        private static void RegisterProps() => RegisterSelected(false);

        /// <summary>선택한 오브젝트를 벽으로 등록한다. (변신하면 고정 크기 벽 조각)</summary>
        [MenuItem(Menu + "선택 → 벽으로 등록")]
        private static void RegisterWalls() => RegisterSelected(true);

        /// <summary>
        /// 선택한 오브젝트(자식 포함) 중 충돌체가 없는 메시에 MeshCollider를 붙인다.
        /// </summary>
        [MenuItem(Menu + "선택 → 충돌체 추가")]
        private static void AddColliders()
        {
            int added = 0;
            foreach (MeshFilter filter in Selection.gameObjects.SelectMany(g => g.GetComponentsInChildren<MeshFilter>(true)).Distinct())
            {
                if (filter.sharedMesh == null || filter.GetComponent<Collider>() != null)
                    continue;
                Undo.AddComponent<MeshCollider>(filter.gameObject).sharedMesh = filter.sharedMesh;
                added++;
            }
            Debug.Log($"[ModMapTools] 충돌체 {added}개 추가");
        }

        /// <summary>
        /// 열린 씬의 모든 조명을 사냥 시간에 꺼지는 MapLight로 등록하고 실시간 조명으로 바꾼다.
        /// </summary>
        [MenuItem(Menu + "모든 조명 등록 (사냥 시간에 꺼짐)")]
        private static void RegisterLights()
        {
            int registered = 0;
            foreach (Light target in Object.FindObjectsByType<Light>(FindObjectsInactive.Include))
            {
                Undo.RecordObject(target, "조명 등록");
                target.lightmapBakeType = LightmapBakeType.Realtime;
                if (target.GetComponent<MapLight>() != null)
                    continue;
                Undo.AddComponent<MapLight>(target.gameObject);
                registered++;
            }
            Debug.Log($"[ModMapTools] 조명 {registered}개 등록");
        }

        /// <summary>씬 뷰 가운데 아래 바닥에 마네킹 스폰 지점을 만든다.</summary>
        [MenuItem(Menu + "지점 추가/마네킹 스폰")]
        private static void AddMannequinSpawn() => AddPoint("MannequinSpawnPoints", "SpawnPoint_Mannequin", go => SetRole(go.AddComponent<SpawnPoint>(), PlayerRole.Mannequin));

        /// <summary>씬 뷰 가운데 아래 바닥에 가드 스폰 후보를 만든다.</summary>
        [MenuItem(Menu + "지점 추가/가드 스폰 후보")]
        private static void AddGuardSpawn() => AddPoint("GuardSpawnPoints", "SpawnPoint_Guard", go => SetRole(go.AddComponent<SpawnPoint>(), PlayerRole.Guard));

        /// <summary>씬 뷰 가운데 아래 바닥에 전원 장치 후보를 만든다.</summary>
        [MenuItem(Menu + "지점 추가/전원 장치 후보")]
        private static void AddGenerator() => AddPoint("GeneratorSpawnPoints", "GeneratorSpawnPoint", go => go.AddComponent<GeneratorSpawnPoint>());

        /// <summary>
        /// 열린 씬을 검사하고 결과를 Console에 쓴다.
        /// </summary>
        [MenuItem(Menu + "맵 검사 (열린 씬)")]
        private static void ValidateActive()
        {
            Scene scene = SceneManager.GetActiveScene();
            PropIndexer.AssignIndices(scene);
            var issues = ModMapValidator.Validate(scene);
            foreach (ModMapValidator.Issue issue in issues)
            {
                if (issue.isError)
                    Debug.LogError($"[맵 검사] {issue.message}", issue.context);
                else
                    Debug.LogWarning($"[맵 검사] {issue.message}", issue.context);
            }
            int errors = issues.Count(i => i.isError);
            Debug.Log($"[맵 검사] {scene.name}: 오류 {errors}개, 경고 {issues.Count - errors}개");
        }

        /// <summary>
        /// 선택한 오브젝트를 하나씩 등록한다. 선택 안에 다른 선택의 자식이 있으면 가장 위의 것만 등록한다.
        /// </summary>
        private static void RegisterSelected(bool isWall)
        {
            GameObject[] roots = Selection.gameObjects
                .Where(selected => !Selection.gameObjects.Any(other => other != selected && selected.transform.IsChildOf(other.transform)))
                .ToArray();
            int count = roots.Count(target => PropRegistrar.Register(target, isWall, absorbChildren: true));
            if (roots.Length > 0)
                EditorSceneManager.MarkSceneDirty(roots[0].scene);
            Debug.Log($"[ModMapTools] {(isWall ? "벽" : "변신 사물")} {count}개 등록 (번호는 씬 저장 때 매겨집니다)");
        }

        /// <summary>
        /// 씬 뷰 가운데에서 아래로 바닥을 찾아 지점을 만든다. (지점 그룹이 없으면 Map 아래에 만든다)
        /// </summary>
        private static void AddPoint(string groupName, string pointName, System.Action<GameObject> setup)
        {
            MapInfo mapInfo = Object.FindAnyObjectByType<MapInfo>();
            if (mapInfo == null)
            {
                EditorUtility.DisplayDialog("지점 추가", "먼저 '맵 뼈대 만들기'를 실행하세요.", "확인");
                return;
            }

            Transform group = mapInfo.transform.Find(groupName);
            if (group == null)
            {
                group = new GameObject(groupName).transform;
                group.SetParent(mapInfo.transform, false);
            }

            Vector3 position = Vector3.zero;
            SceneView view = SceneView.lastActiveSceneView;
            if (view != null)
            {
                position = view.pivot;
                if (Physics.Raycast(position + Vector3.up * 50f, Vector3.down, out RaycastHit hit, 200f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    position = hit.point;
            }

            var point = new GameObject(pointName);
            Undo.RegisterCreatedObjectUndo(point, "지점 추가");
            point.transform.SetParent(group, false);
            point.transform.position = position;
            setup(point);
            Selection.activeGameObject = point;
        }

        /// <summary>
        /// 스폰 지점의 역할을 정한다.
        /// </summary>
        private static void SetRole(SpawnPoint point, PlayerRole role)
        {
            var serialized = new SerializedObject(point);
            serialized.FindProperty("role").enumValueIndex = System.Array.IndexOf(System.Enum.GetValues(typeof(PlayerRole)), role);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
