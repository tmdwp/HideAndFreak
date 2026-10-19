using System.Collections.Generic;

namespace GuardVsMannequin.Mods
{
    /// <summary>
    /// 맵(나중에는 외형 등)을 등록소에 제공하는 출처. 내장 콘텐츠(<see cref="BuiltInContentSource"/>)와 모드가 같은 방식으로 등록된다.
    /// </summary>
    public interface IContentSource
    {
        /// <summary>출처 ID. (내장: "builtin", 모드: 모드 ID)</summary>
        string SourceId { get; }

        /// <summary>이 출처가 제공하는 맵 목록. (출처 안의 순서대로)</summary>
        IReadOnlyList<MapEntry> LoadMaps();
    }
}
