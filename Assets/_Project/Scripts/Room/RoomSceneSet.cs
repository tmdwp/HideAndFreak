using GuardVsMannequin.Mods;
using UnityEngine;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

namespace GuardVsMannequin.Room
{
    /// <summary>
    /// 룸 씬, 게임 시스템 씬, 맵 목록(<see cref="IMapCatalog"/>)의 맵 씬 이름을 다룬다.
    /// - 맵 번호로 불러올 맵 씬을 고른다. (내장 맵이 빌드에 없으면 첫 번째 맵으로 대신)
    /// - 이 기기가 목표 씬으로 전환 중인지 판단한다. (로딩 화면 표시용)
    /// 판 중에는 맵 씬과 게임 시스템 씬이 함께 열린다.
    /// </summary>
    public sealed class RoomSceneSet
    {
        private readonly string roomSceneName;
        private readonly string systemsSceneName;
        private readonly IMapCatalog catalog;

        /// <summary>
        /// 룸 씬 이름, 게임 시스템 씬 이름, 맵 목록으로 만든다.
        /// </summary>
        public RoomSceneSet(string roomSceneName, string systemsSceneName, IMapCatalog catalog)
        {
            this.roomSceneName = roomSceneName;
            this.systemsSceneName = systemsSceneName;
            this.catalog = catalog;
        }

        /// <summary>룸 씬 이름.</summary>
        public string RoomSceneName => roomSceneName;

        /// <summary>
        /// 맵 번호에 해당하는 맵 씬 이름. 범위 밖이면 첫 번째 맵, 내장 맵인데 빌드 설정에 없으면 첫 번째 맵으로 대신한다.
        /// (모드 맵은 번들을 불러온 뒤에야 씬을 찾을 수 있으므로 여기서 확인하지 않는다)
        /// </summary>
        public string ResolveGameScene(int mapId)
        {
            var maps = catalog.Maps;
            MapEntry map = maps[Mathf.Clamp(mapId, 0, maps.Count - 1)];
            if (!map.IsBuiltIn || Application.CanStreamedLevelBeLoaded(map.SceneName))
                return map.SceneName;

            Debug.LogWarning($"[RoomState] 맵 씬 '{map.SceneName}'이 빌드 설정에 없어 '{maps[0].SceneName}'을 사용합니다. 'GameSettingTools/맵 생성'을 실행하세요.");
            return maps[0].SceneName;
        }

        /// <summary>
        /// 이 기기가 아직 목표 씬(게임 중이면 맵+시스템, 아니면 룸)에 도착하지 못했는지 여부.
        /// 전환 중에는 이전·다음 씬이 함께 열려 있을 수 있으므로, 목표 씬만 남아야 도착으로 본다.
        /// </summary>
        public bool IsTransitionPending(bool isInGame)
        {
            bool roomLoaded = UnitySceneManager.GetSceneByName(roomSceneName).isLoaded;
            bool mapLoaded = IsAnyMapSceneLoaded();
            bool systemsLoaded = UnitySceneManager.GetSceneByName(systemsSceneName).isLoaded;
            return isInGame ? (!(mapLoaded && systemsLoaded) || roomLoaded) : (mapLoaded || systemsLoaded || !roomLoaded);
        }

        /// <summary>
        /// 맵 목록의 씬 중 하나라도 불러와져 있는지 확인한다.
        /// </summary>
        private bool IsAnyMapSceneLoaded()
        {
            foreach (MapEntry map in catalog.Maps)
            {
                if (UnitySceneManager.GetSceneByName(map.SceneName).isLoaded)
                    return true;
            }
            return false;
        }
    }
}
