namespace GuardVsMannequin.Player.Emotes
{
    /// <summary>
    /// 감정 표현을 화면에 재생한다.
    /// 지금은 절차적 움직임(<see cref="ProceduralEmoteView"/>)이고, 애니메이션 에셋 도입 시 Animator 구현으로 교체한다.
    /// </summary>
    public interface IEmoteView
    {
        /// <summary>
        /// 감정 표현을 재생한다.
        /// </summary>
        /// <param name="emoteId">1부터 시작하는 감정 표현 번호.</param>
        void Play(int emoteId);
    }
}
