using System.Collections.Generic;

namespace GuardVsMannequin.Mods
{
    /// <summary>
    /// 켜진 모드 하나가 등록소에 제공하는 맵. 맵 씬은 모드의 씬 번들 안에 있다.
    /// </summary>
    public sealed class ModContentSource : IContentSource
    {
        private readonly ModInfo mod;

        /// <param name="mod">켜져 있고 오류가 없는 모드.</param>
        public ModContentSource(ModInfo mod)
        {
            this.mod = mod;
        }

        /// <inheritdoc />
        public string SourceId => mod.Id;

        /// <inheritdoc />
        public IReadOnlyList<MapEntry> LoadMaps()
        {
            string sceneBundle = mod.GetFilePath(mod.Manifest.sceneBundle);
            var entries = new List<MapEntry>(mod.Manifest.maps.Count);
            foreach (MapManifest map in mod.Manifest.maps)
                entries.Add(new MapEntry(map, mod.Id, sceneBundle));
            return entries;
        }
    }
}
