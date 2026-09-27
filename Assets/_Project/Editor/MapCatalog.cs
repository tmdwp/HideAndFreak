using System.Linq;

namespace GuardVsMannequin.Editor
{
    /// <summary>
    /// 게임에서 고를 수 있는 맵 목록. 순서가 곧 맵 ID(룸 설정의 맵 번호)이다.
    /// 0번은 테스트 맵, 1번부터는 외부 에셋의 데모 씬으로 만든 맵이다(<see cref="MapSceneBuilder"/>).
    /// 맵을 추가하면 strings.csv의 map.&lt;번호&gt;와 game_settings.csv의 Map 최댓값도 함께 늘린다.
    /// </summary>
    public static class MapCatalog
    {
        /// <summary>
        /// 맵 하나의 생성 정보.
        /// </summary>
        public sealed class Entry
        {
            /// <summary>게임 씬 이름. (빌드 설정·RoomState에 등록되는 이름)</summary>
            public string sceneName;

            /// <summary>표시 이름. (MapInfo, 로그용)</summary>
            public string displayName;

            /// <summary>환경을 가져올 원본 씬. 비어 있으면 생성 대상이 아니다. (테스트 맵)</summary>
            public string sourceScenePath;

            /// <summary>이 맵에서 쓸 마네킹 외형. (MapInfo에 들어가며, 생성 후 인스펙터에서 바꿀 수 있다)</summary>
            public string mannequinSkinPath;

            /// <summary>실내 맵이면 true. 천장이 있는 바닥만 플레이 영역으로 본다.</summary>
            public bool indoor;

            /// <summary>
            /// 환경 크기 배율. 0이면 자동(바닥이 좁은 맵만 키움), 0보다 크면 그 배율로 고정.
            /// 맵이 작거나 크다고 느껴지면 이 값을 바꾸고 '맵 생성'을 다시 실행한다.
            /// </summary>
            public float scale;

            /// <summary>생성된 씬의 경로.</summary>
            public string ScenePath => $"{TestEnvironmentSetupTool.SceneFolder}/{sceneName}.unity";
        }

        /// <summary>맵 목록. (인덱스 = 맵 ID)</summary>
        public static readonly Entry[] Maps =
        {
            new Entry { sceneName = "Game_Test", displayName = "Test" },
            new Entry
            {
                sceneName = "Map_Asylum", displayName = "Abandoned Asylum",
                sourceScenePath = "Assets/Abandoned_Asylum/Show.unity",
                mannequinSkinPath = CharacterAssetBuilder.GhostSkinPath, indoor = true
            },
            new Entry
            {
                sceneName = "Map_Industrial", displayName = "Industrial",
                sourceScenePath = "Assets/RPG_FPS_game_assets_industrial/Map_v1.unity",
                mannequinSkinPath = CharacterAssetBuilder.DummySkinPath, indoor = false
            },
            new Entry
            {
                sceneName = "Map_Hospital", displayName = "Horror Hospital",
                sourceScenePath = "Assets/Horror Hospital Pack/DemoScene.unity",
                mannequinSkinPath = CharacterAssetBuilder.DummySkinPath, indoor = true
            },
            new Entry
            {
                sceneName = "Map_AlchemistHouse", displayName = "Alchemist House",
                sourceScenePath = "Assets/BK_AlchemistHouse/Scenes/Scene.unity",
                mannequinSkinPath = CharacterAssetBuilder.SlimeSkinPath, indoor = true
            },
        };

        /// <summary>맵 ID 순서의 게임 씬 이름. (RoomState에 등록)</summary>
        public static string[] SceneNames => Maps.Select(map => map.sceneName).ToArray();
    }
}
