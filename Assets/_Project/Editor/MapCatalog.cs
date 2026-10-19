using System.Linq;

namespace GuardVsMannequin.Editor
{
    /// <summary>
    /// 게임에서 고를 수 있는 맵 목록. 순서가 곧 맵 ID(룸 설정의 맵 번호)이다.
    /// 0~3번은 외부 에셋의 데모 씬으로 만든 맵(<see cref="MapSceneBuilder"/>),
    /// 그 뒤는 Scenes/Custom 폴더에 직접 만든 맵이다(<see cref="MapMakerWindow"/>).
    /// 테스트 씬 Game_Test는 게임에 넣지 않는다. (모드 SDK 시험용 원본, 모드 맵과 이름이 겹치지 않게)
    /// 맵이 늘거나 줄면 "맵 목록 갱신"(<see cref="MapRegistrySync"/>)이 빌드 설정·룸 맵 목록·맵 이름 문구·설정 범위를 맞춘다.
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

            /// <summary>직접 만든 맵(Scenes/Custom 폴더)이면 true.</summary>
            public bool customFolder;

            /// <summary>씬 경로.</summary>
            public string ScenePath => customFolder
                ? $"{CustomMapFolder}/{sceneName}.unity"
                : $"{TestEnvironmentSetupTool.SceneFolder}/{sceneName}.unity";
        }

        /// <summary>맵 목록. (인덱스 = 맵 ID)</summary>
        public static readonly Entry[] Maps =
        {
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

        /// <summary>직접 만든 맵 씬을 두는 폴더. 이 폴더의 씬은 자동으로 맵 목록 뒤에 붙는다. (파일 이름 순)</summary>
        public static string CustomMapFolder => $"{TestEnvironmentSetupTool.SceneFolder}/Custom";

        /// <summary>
        /// 전체 맵 목록 (인덱스 = 맵 ID). 자동 생성 맵(<see cref="Maps"/>) 뒤에 직접 만든 맵이 이어진다.
        /// 직접 만든 맵을 추가·삭제하면 그 뒤 맵들의 번호가 바뀌므로 "맵 목록 갱신"을 실행한다.
        /// </summary>
        public static Entry[] AllMaps
        {
            get
            {
                var result = Maps.ToList();
                if (!System.IO.Directory.Exists(CustomMapFolder))
                    return result.ToArray();

                foreach (string path in System.IO.Directory.GetFiles(CustomMapFolder, "*.unity").OrderBy(p => p))
                {
                    string sceneName = System.IO.Path.GetFileNameWithoutExtension(path);
                    result.Add(new Entry { sceneName = sceneName, displayName = sceneName, customFolder = true });
                }
                return result.ToArray();
            }
        }

        /// <summary>맵 ID 순서의 게임 씬 이름. (RoomState에 등록)</summary>
        public static string[] SceneNames => AllMaps.Select(map => map.sceneName).ToArray();

        /// <summary>
        /// 씬 경로로 맵 항목을 찾는다. 없으면 null.
        /// </summary>
        public static Entry FindByScenePath(string scenePath)
        {
            return AllMaps.FirstOrDefault(map => map.ScenePath == scenePath);
        }
    }
}
