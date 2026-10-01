using UnityEngine;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

namespace GuardVsMannequin.Room
{
    /// <summary>
    /// 룸 씬과 맵 ID별 게임 씬 이름을 다룬다.
    /// - 맵 ID로 불러올 게임 씬을 고른다. (빌드에 없으면 첫 번째 맵으로 대신)
    /// - 이 기기가 목표 씬으로 전환 중인지 판단한다. (로딩 화면 표시용)
    /// </summary>
    public sealed class RoomSceneSet
    {
        private readonly string roomSceneName;
        private readonly string[] gameSceneNames;

        /// <summary>
        /// 룸 씬 이름과 맵 ID 순서의 게임 씬 이름으로 만든다.
        /// </summary>
        public RoomSceneSet(string roomSceneName, string[] gameSceneNames)
        {
            this.roomSceneName = roomSceneName;
            this.gameSceneNames = gameSceneNames ?? new string[0];
        }

        /// <summary>룸 씬 이름.</summary>
        public string RoomSceneName => roomSceneName;

        /// <summary>
        /// 맵 ID에 해당하는 게임 씬 이름. 아직 만들지 않았거나 빌드 설정에 없는 맵이면 첫 번째 맵(테스트 맵)으로 대신한다.
        /// </summary>
        public string ResolveGameScene(int mapId)
        {
            int index = Mathf.Clamp(mapId, 0, gameSceneNames.Length - 1);
            string sceneName = gameSceneNames[index];
            if (Application.CanStreamedLevelBeLoaded(sceneName))
                return sceneName;

            Debug.LogWarning($"[RoomState] 맵 씬 '{sceneName}'이 빌드 설정에 없어 '{gameSceneNames[0]}'을 사용합니다. 'GuardVsMannequin/맵 생성'을 실행하세요.");
            return gameSceneNames[0];
        }

        /// <summary>
        /// 이 기기가 아직 목표 씬(게임 중이면 맵, 아니면 룸)에 도착하지 못했는지 여부.
        /// 전환 중에는 두 씬이 함께 열려 있을 수 있으므로, 목표 씬만 남아야 도착으로 본다.
        /// </summary>
        public bool IsTransitionPending(bool isInGame)
        {
            bool roomLoaded = UnitySceneManager.GetSceneByName(roomSceneName).isLoaded;
            bool gameLoaded = IsAnyGameSceneLoaded();
            return isInGame ? (!gameLoaded || roomLoaded) : (gameLoaded || !roomLoaded);
        }

        /// <summary>
        /// 게임 씬 목록 중 하나라도 불러와져 있는지 확인한다.
        /// </summary>
        private bool IsAnyGameSceneLoaded()
        {
            foreach (string sceneName in gameSceneNames)
            {
                if (UnitySceneManager.GetSceneByName(sceneName).isLoaded)
                    return true;
            }
            return false;
        }
    }
}
