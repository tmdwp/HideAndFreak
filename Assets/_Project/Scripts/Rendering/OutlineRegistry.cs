using System.Collections.Generic;
using UnityEngine;

namespace GuardVsMannequin.Rendering
{
    /// <summary>외곽선 하나: 그릴 렌더러들과 종류.</summary>
    public readonly struct OutlineEntry
    {
        public readonly Renderer[] Renderers;
        public readonly OutlineKind Kind;

        public OutlineEntry(Renderer[] renderers, OutlineKind kind)
        {
            Renderers = renderers;
            Kind = kind;
        }
    }

    /// <summary>
    /// 지금 표시할 외곽선 목록을 제공한다. (<see cref="OutlineRendererFeature"/>가 매 프레임 읽음)
    /// </summary>
    public interface IOutlineSource
    {
        /// <summary>표시 중인 외곽선들.</summary>
        IEnumerable<OutlineEntry> Entries { get; }
    }

    /// <summary>
    /// 외곽선 요청을 모아 두는 기본 구현. 게임 코드는 <see cref="IOutlineService"/>로, 렌더러 기능은 <see cref="IOutlineSource"/>로만 쓴다.
    /// 렌더러를 직접 그리므로(DrawRenderer) 오클루전 컬링으로 가려진 대상도 그릴 수 있고, 렌더러 설정은 바꾸지 않는다.
    /// 요청자가 사라지면(파괴) 다음 조회 때 자동으로 빠진다.
    /// </summary>
    public sealed class OutlineRegistry : IOutlineService, IOutlineSource
    {
        /// <summary>게임 전체에서 쓰는 하나의 목록. (씬·프리팹에서 렌더러 기능을 직렬화 참조할 수 없어 정적 접근을 쓴다)</summary>
        public static OutlineRegistry Instance { get; } = new();

        private readonly Dictionary<Object, OutlineEntry> entries = new();
        private readonly List<Object> deadOwners = new();

        /// <inheritdoc />
        public void Show(Object owner, Renderer[] renderers, OutlineKind kind)
        {
            if (owner == null)
                return;
            if (renderers == null || renderers.Length == 0)
            {
                entries.Remove(owner);
                return;
            }
            entries[owner] = new OutlineEntry(renderers, kind);
        }

        /// <inheritdoc />
        public void Hide(Object owner)
        {
            if (owner != null)
                entries.Remove(owner);
        }

        /// <inheritdoc />
        public IEnumerable<OutlineEntry> Entries
        {
            get
            {
                RemoveDestroyedOwners();
                return entries.Values;
            }
        }

        /// <summary>
        /// 파괴된 요청자의 외곽선을 지운다. (Hide를 부르지 못하고 사라진 경우)
        /// </summary>
        private void RemoveDestroyedOwners()
        {
            deadOwners.Clear();
            foreach (Object owner in entries.Keys)
            {
                if (owner == null)
                    deadOwners.Add(owner);
            }
            foreach (Object owner in deadOwners)
                entries.Remove(owner);
        }

        /// <summary>
        /// 플레이 모드에 들어갈 때 이전 플레이의 요청을 지운다. (도메인 재로드를 끈 경우)
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            Instance.entries.Clear();
        }
    }
}
