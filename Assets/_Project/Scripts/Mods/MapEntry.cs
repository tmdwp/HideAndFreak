using GuardVsMannequin.Localization;

namespace GuardVsMannequin.Mods
{
    /// <summary>
    /// 등록소(<see cref="ContentRegistry"/>)에 오른 맵 하나. 맵 정보와 어디서 왔는지(내장 / 모드)를 함께 가진다.
    /// </summary>
    public sealed class MapEntry
    {
        /// <summary>맵 정보(map.json).</summary>
        public MapManifest Manifest { get; }

        /// <summary>이 맵을 제공한 콘텐츠 출처 ID. (내장: "builtin", 모드: 모드 ID)</summary>
        public string SourceId { get; }

        /// <summary>맵 씬이 들어 있는 번들 파일 경로. 내장 맵이면 null.</summary>
        public string BundlePath { get; }

        /// <summary>전역 고유 ID.</summary>
        public string Id => Manifest.id;

        /// <summary>불러올 씬 이름.</summary>
        public string SceneName => Manifest.scene;

        /// <summary>게임에 들어 있는 맵인지 여부.</summary>
        public bool IsBuiltIn => BundlePath == null;

        /// <param name="manifest">맵 정보.</param>
        /// <param name="sourceId">출처 ID.</param>
        /// <param name="bundlePath">번들 경로. 내장 맵이면 null.</param>
        public MapEntry(MapManifest manifest, string sourceId, string bundlePath)
        {
            Manifest = manifest;
            SourceId = sourceId;
            BundlePath = bundlePath;
        }

        /// <summary>
        /// 현재 언어의 표시 이름. 번역 key가 없으면 적힌 이름, 그것도 없으면 씬 이름.
        /// </summary>
        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(Manifest.nameKey) && Loc.HasKey(Manifest.nameKey))
                    return Loc.Get(Manifest.nameKey);
                return string.IsNullOrEmpty(Manifest.name) ? Manifest.scene : Manifest.name;
            }
        }
    }
}
