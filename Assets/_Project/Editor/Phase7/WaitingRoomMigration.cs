#pragma warning disable CS0618 // 지울 대상인 예전 대기실 컴포넌트를 찾기 위해 [Obsolete] 형식을 쓴다.
using System.IO;
using GuardVsMannequin.Core;
using GuardVsMannequin.Map;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GuardVsMannequin.Editor
{
    /// <summary>
    /// 기존 맵 씬에서 가드 대기실을 지우고 가드 출발 지점을 가드 스폰 후보로 바꾼다. (Phase 7-A2)
    /// - "WaitingRoom" 오브젝트(벽·문·조명·대기실 스폰 지점·영역·순간이동 장치) 삭제
    /// - 대기실 문(WaitingRoomDoor)이 따로 있으면 삭제
    /// - GuardReleasePoint → SpawnPoint(Guard), 그룹 이름 "GuardSpawnPoints"
    /// - 미리 구운 NavMesh가 있으면 다시 굽는다. (대기실 바닥이 가드 무작위 스폰 후보에 섞이지 않도록)
    /// 대상: 맵 목록(MapCatalog.AllMaps)의 모든 씬. 이미 바뀐 씬은 건너뛴다.
    /// </summary>
    public static class WaitingRoomMigration
    {
        private const string WaitingRoomName = "WaitingRoom";

        /// <summary>
        /// 모든 맵 씬을 바꾼다.
        /// </summary>
        /// <returns>바꾼 씬 수.</returns>
        public static int Run()
        {
            string previousScene = SceneManager.GetActiveScene().path;
            int changedScenes = 0;
            foreach (MapCatalog.Entry map in MapCatalog.AllMaps)
            {
                if (!File.Exists(map.ScenePath))
                    continue;

                Scene scene = EditorSceneManager.OpenScene(map.ScenePath, OpenSceneMode.Single);
                if (!MigrateScene(scene, out string report))
                    continue;

                EditorSceneManager.SaveScene(scene);
                changedScenes++;
                Debug.Log($"[WaitingRoomMigration] {map.sceneName}: {report}");
            }

            if (!string.IsNullOrEmpty(previousScene) && File.Exists(previousScene))
                EditorSceneManager.OpenScene(previousScene, OpenSceneMode.Single);
            return changedScenes;
        }

        /// <summary>
        /// 씬 하나를 바꾼다. 바꿀 것이 없으면 false.
        /// </summary>
        private static bool MigrateScene(Scene scene, out string report)
        {
            int removedRooms = 0, removedDoors = 0, convertedPoints = 0;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                {
                    if (child != null && child.name == WaitingRoomName)
                    {
                        Object.DestroyImmediate(child.gameObject);
                        removedRooms++;
                    }
                }
            }

            foreach (WaitingRoomDoor door in Object.FindObjectsByType<WaitingRoomDoor>(FindObjectsInactive.Include))
            {
                Object.DestroyImmediate(door.gameObject);
                removedDoors++;
            }

            // 대기실과 함께 지워지지 않은 영역·순간이동 장치(다른 오브젝트에 붙은 경우)
            foreach (WaitingRoomArea area in Object.FindObjectsByType<WaitingRoomArea>(FindObjectsInactive.Include))
                Object.DestroyImmediate(area);
            foreach (GuardReleaseTeleporter teleporter in Object.FindObjectsByType<GuardReleaseTeleporter>(FindObjectsInactive.Include))
                Object.DestroyImmediate(teleporter);

            foreach (GuardReleasePoint point in Object.FindObjectsByType<GuardReleasePoint>(FindObjectsInactive.Include))
            {
                ConvertToGuardSpawn(point);
                convertedPoints++;
            }

            bool changed = removedRooms + removedDoors + convertedPoints > 0;
            bool rebaked = changed && RebakeNavMeshIfBaked(scene);
            report = $"대기실 {removedRooms}개·문 {removedDoors}개 삭제, 출발 지점 {convertedPoints}개 → 가드 스폰 후보" +
                     (rebaked ? ", NavMesh 다시 구움" : string.Empty);
            return changed;
        }

        /// <summary>
        /// 출발 지점을 같은 위치·방향의 가드 스폰 지점으로 바꾼다.
        /// </summary>
        private static void ConvertToGuardSpawn(GuardReleasePoint point)
        {
            GameObject target = point.gameObject;
            Object.DestroyImmediate(point);

            SpawnPoint spawn = target.AddComponent<SpawnPoint>();
            var serialized = new SerializedObject(spawn);
            serialized.FindProperty("role").enumValueIndex = (int)PlayerRole.Guard;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            target.name = "SpawnPoint_Guard";

            Transform group = target.transform.parent;
            if (group != null && group.name == "GuardReleasePoints")
                group.name = MapSceneBuilder.GuardSpawnGroup;
        }

        /// <summary>
        /// 에디터에서 미리 구운 NavMesh가 있으면 다시 굽는다. (실행 중에 굽는 맵은 그대로 둔다)
        /// </summary>
        private static bool RebakeNavMeshIfBaked(Scene scene)
        {
            Transform mapRoot = MapAuthoringTools.FindMapRoot(scene);
            NavMeshSurface surface = mapRoot != null ? mapRoot.GetComponent<NavMeshSurface>() : null;
            if (surface == null || surface.navMeshData == null)
                return false;
            return MapOptimizer.BakeNavMesh(scene, mapRoot);
        }
    }
}
