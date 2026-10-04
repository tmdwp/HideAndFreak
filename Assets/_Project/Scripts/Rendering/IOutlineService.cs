using UnityEngine;

namespace GuardVsMannequin.Rendering
{
    /// <summary>외곽선 종류. 종류마다 색이 다르다. (색·두께는 OutlineRendererFeature 설정)</summary>
    public enum OutlineKind : byte
    {
        /// <summary>마네킹이 조준한 변신 사물. (본인 화면)</summary>
        Aim = 0,

        /// <summary>수류탄에 맞은 마네킹. (가드 화면, 벽 너머도 보임)</summary>
        Reveal = 1
    }

    /// <summary>
    /// 외곽선을 켜고 끄는 공용 기능. 게임 코드는 어떤 렌더러에 어떤 외곽선을 그릴지만 알리고, 그리는 방법은 모른다(DIP).
    /// 같은 요청자(owner)가 다시 Show하면 대상이 바뀐다. 요청자마다 따로 관리하므로 서로의 외곽선을 지우지 않는다.
    /// </summary>
    public interface IOutlineService
    {
        /// <summary>
        /// 요청자의 외곽선을 대상 렌더러에 표시한다. (이전 대상은 대체)
        /// </summary>
        void Show(Object owner, Renderer[] renderers, OutlineKind kind);

        /// <summary>
        /// 요청자의 외곽선을 지운다.
        /// </summary>
        void Hide(Object owner);
    }
}
