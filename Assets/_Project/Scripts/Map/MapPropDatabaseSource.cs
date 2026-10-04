using GuardVsMannequin.Props;

namespace GuardVsMannequin.Map
{
    /// <summary>
    /// 현재 맵(<see cref="MapInfo"/>)이 지정한 사물 목록을 쓰고, 지정하지 않았으면 기본 목록을 쓴다.
    /// </summary>
    public sealed class MapPropDatabaseSource : IPropDatabaseSource
    {
        private readonly PropDatabase fallback;

        /// <param name="fallback">맵이 목록을 지정하지 않았을 때 쓸 기본 목록. (프리팹 값)</param>
        public MapPropDatabaseSource(PropDatabase fallback)
        {
            this.fallback = fallback;
        }

        /// <inheritdoc />
        public PropDatabase Database
        {
            get
            {
                MapInfo map = MapInfo.Current;
                return map != null && map.PropDatabase != null ? map.PropDatabase : fallback;
            }
        }
    }
}
