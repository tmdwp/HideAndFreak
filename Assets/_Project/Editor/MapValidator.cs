using System.Collections.Generic;
using System.Linq;
using GuardVsMannequin.Core;
using GuardVsMannequin.Map;
using GuardVsMannequin.Objectives;
using GuardVsMannequin.Props;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GuardVsMannequin.Editor
{
    /// <summary>
    /// 열린 맵 씬이 게임에서 동작하는 데 필요한 것을 갖췄는지 검사한다. (맵 제작 도우미의 "맵 검사")
    /// 오류는 게임이 동작하지 않는 문제, 경고는 동작은 하지만 확인이 필요한 문제다.
    /// </summary>
    public static class MapValidator
    {
        /// <summary>검사 결과 한 줄.</summary>
        public readonly struct Issue
        {
            public readonly bool isError;
            public readonly string message;
            public readonly Object context;

            public Issue(bool isError, string message, Object context = null)
            {
                this.isError = isError;
                this.message = message;
                this.context = context;
            }
        }

        // 최대 인원 기준 필요한 지점 수 (가드 최대 5명, 마네킹 최대 9명, 전원 장치 최대 12개)
        private const int MaxGuards = 5;
        private const int MaxMannequins = GameConstants.MaxPlayers - 1;
        private const int MaxGenerators = 12;

        /// <summary>
        /// 열린 씬을 검사한다.
        /// </summary>
        public static List<Issue> Validate()
        {
            var issues = new List<Issue>();
            Scene scene = SceneManager.GetActiveScene();

            MapInfo[] mapInfos = Object.FindObjectsByType<MapInfo>(FindObjectsInactive.Include);
            if (mapInfos.Length != 1)
            {
                issues.Add(new Issue(true, $"MapInfo가 {mapInfos.Length}개입니다. Map 오브젝트에 하나만 있어야 합니다."));
                return issues;
            }
            MapInfo mapInfo = mapInfos[0];
            if (mapInfo.CeilingHeight >= GameConstants.GunRange)
                issues.Add(new Issue(true, $"천장 높이({mapInfo.CeilingHeight})가 가드 사거리({GameConstants.GunRange})보다 낮아야 합니다.", mapInfo));

            NavMeshSurface surface = mapInfo.GetComponent<NavMeshSurface>();
            if (surface == null)
                issues.Add(new Issue(true, "Map에 NavMeshSurface가 없습니다. 가짜 마네킹이 움직이지 못합니다.", mapInfo));
            else if (surface.navMeshData == null)
                issues.Add(new Issue(false, "NavMesh를 미리 굽지 않았습니다. 빌드에서는 에셋 모델 메시를 실행 중에 구울 수 없어 가짜 마네킹 길이 불완전해집니다. '현재 맵 최적화'를 실행하세요.", mapInfo));
            if (mapInfo.transform.Find(MapAuthoringTools.EnvironmentName) == null)
                issues.Add(new Issue(false, "Map/Environment가 없습니다. 맵 최적화와 경계 맞추기는 이 오브젝트 아래 모델을 대상으로 합니다.", mapInfo));

            CheckSpawns(issues);
            CheckGuardSpawns(issues);
            CheckLights(issues);
            CheckProps(issues, mapInfo);

            if (scene.GetRootGameObjects().All(r => r.name != MapAuthoringTools.BoundaryName))
                issues.Add(new Issue(false, "투명 경계(Boundary (Invisible))가 없습니다. 벽을 타고 맵 밖으로 나갈 수 있습니다. '경계 맞추기'를 실행하세요."));

            if (!EditorBuildSettings.scenes.Any(s => s.path == scene.path && s.enabled) || MapCatalog.FindByScenePath(scene.path) == null)
                issues.Add(new Issue(false, "맵 목록에 등록되지 않았습니다. 직접 만든 맵은 Scenes/Custom 폴더에 두고 '맵 목록 갱신'을 실행하세요."));

            if (Object.FindObjectsByType<GameStateMachine>(FindObjectsInactive.Include).Length == 0)
                issues.Add(new Issue(true, "GameSystems(게임 시스템)가 없습니다. '새 맵 만들기'로 만든 씬을 사용하세요."));

            return issues;
        }

        /// <summary>
        /// 가드·마네킹 스폰 지점과 전원 장치 후보 수를 확인한다.
        /// </summary>
        private static void CheckSpawns(List<Issue> issues)
        {
            SpawnPoint[] spawns = Object.FindObjectsByType<SpawnPoint>(FindObjectsInactive.Include);
            int guards = spawns.Count(s => s.Role == PlayerRole.Guard);
            int mannequins = spawns.Count(s => s.Role == PlayerRole.Mannequin);
            if (guards == 0)
                issues.Add(new Issue(true, "가드 스폰 지점이 없습니다."));
            else if (guards < MaxGuards)
                issues.Add(new Issue(false, $"가드 스폰 지점이 {guards}개입니다. 최대 인원({MaxGuards}명)보다 적으면 같은 지점에 겹쳐 나옵니다."));

            if (mannequins == 0)
                issues.Add(new Issue(true, "마네킹 스폰 지점이 없습니다."));
            else if (mannequins < MaxMannequins)
                issues.Add(new Issue(false, $"마네킹 스폰 지점이 {mannequins}개입니다. 최대 인원({MaxMannequins}명)보다 적으면 같은 지점에 겹쳐 나옵니다."));

            int generators = Object.FindObjectsByType<GeneratorSpawnPoint>(FindObjectsInactive.Include).Length;
            if (generators == 0)
                issues.Add(new Issue(true, "전원 장치 후보가 없습니다."));
            else if (generators < MaxGenerators)
                issues.Add(new Issue(false, $"전원 장치 후보가 {generators}개입니다. 룸 설정 최댓값({MaxGenerators}개)보다 적으면 그 수까지만 나옵니다."));
        }

        /// <summary>
        /// 가드 스폰 후보를 확인한다. 후보가 없어도 NavMesh 위 무작위 위치를 쓰지만, 마네킹 스폰 지점과 가까운 후보는 쓰이지 않는다.
        /// 예전 가드 대기실이 남아 있으면 오류로 알린다. (Phase 7 적용 메뉴로 지움)
        /// </summary>
        private static void CheckGuardSpawns(List<Issue> issues)
        {
            GameObject waitingRoom = GameObject.Find("WaitingRoom");
            if (waitingRoom != null)
                issues.Add(new Issue(true, "예전 가드 대기실이 남아 있습니다. 'Phase 7 > 게임플레이 개편 적용'을 실행하세요.", waitingRoom));

            SpawnPoint[] spawns = Object.FindObjectsByType<SpawnPoint>(FindObjectsInactive.Include);
            Vector3[] mannequins = spawns.Where(s => s.Role == PlayerRole.Mannequin).Select(s => s.transform.position).ToArray();
            SpawnPoint[] guards = spawns.Where(s => s.Role == PlayerRole.Guard).ToArray();
            if (guards.Length == 0)
            {
                issues.Add(new Issue(false, "가드 스폰 후보가 없습니다. 바닥(NavMesh) 무작위 위치만 씁니다."));
                return;
            }

            float minDistance = GameTuning.GuardSpawnMinDistance;
            foreach (SpawnPoint guard in guards)
            {
                if (mannequins.Any(m => Vector3.Distance(m, guard.transform.position) < minDistance))
                    issues.Add(new Issue(false, $"가드 스폰 후보가 마네킹 스폰 지점과 {minDistance}m보다 가깝습니다. 이 후보는 거의 쓰이지 않습니다.", guard));
            }
        }

        /// <summary>
        /// 사냥 시간에 꺼질 조명이 있는지 확인한다.
        /// </summary>
        private static void CheckLights(List<Issue> issues)
        {
            if (Object.FindObjectsByType<HuntingLightingController>(FindObjectsInactive.Include).Length == 0)
                issues.Add(new Issue(true, "HuntingLightingController가 없습니다. 사냥 시간에 조명이 꺼지지 않습니다."));

            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include);
            int unregistered = lights.Count(l => l.GetComponentInParent<MapLight>() == null);
            if (lights.Length == 0)
                issues.Add(new Issue(false, "맵에 조명이 없습니다."));
            else if (unregistered > 0)
                issues.Add(new Issue(false, $"MapLight가 아닌 조명이 {unregistered}개 있습니다. 사냥 시간에 꺼지지 않습니다. '모든 조명 등록'을 실행하세요."));

            int baked = lights.Count(l => l.lightmapBakeType == LightmapBakeType.Baked);
            if (baked > 0)
                issues.Add(new Issue(false, $"구운(Baked) 조명이 {baked}개 있습니다. 라이트맵을 굽지 않으면 빛이 없고, 구우면 사냥 시간에도 밝게 남습니다."));
        }

        /// <summary>
        /// 변신 가능 사물이 있고, 모두 맵의 사물 목록에 있는 번호인지 확인한다.
        /// </summary>
        private static void CheckProps(List<Issue> issues, MapInfo mapInfo)
        {
            TransformableProp[] props = Object.FindObjectsByType<TransformableProp>(FindObjectsInactive.Include);
            if (props.Length == 0)
            {
                issues.Add(new Issue(false, "변신 가능 사물이 없습니다. 오브젝트를 선택하고 '변신 사물로 등록'을 실행하세요."));
                return;
            }

            PropDatabase database = mapInfo.PropDatabase;
            if (database == null)
            {
                issues.Add(new Issue(true, "MapInfo에 사물 목록(Prop Database)이 없습니다. 변신 사물을 등록하면 자동으로 만들어집니다.", mapInfo));
                return;
            }

            foreach (TransformableProp prop in props.Where(p => !database.IsValid(p.PropId)))
                issues.Add(new Issue(true, $"'{prop.name}'의 사물 번호({prop.PropId})가 이 맵의 사물 목록에 없습니다. 다시 등록하세요.", prop));

            foreach (TransformableProp prop in props.Where(p => p.GetComponent<Collider>() == null && p.GetComponentInChildren<Collider>() == null))
                issues.Add(new Issue(true, $"'{prop.name}'에 충돌체가 없어 조준할 수 없습니다.", prop));
        }
    }
}
