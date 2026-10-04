namespace GuardVsMannequin.Player
{
    /// <summary>
    /// 수류탄 등으로 걸린 일시 효과 상태. HUD·외곽선 표시가 구현(<see cref="StatusEffects"/>)에 의존하지 않도록 분리한다(DIP).
    /// </summary>
    public interface IStatusEffects
    {
        /// <summary>위치가 상대 진영에게 외곽선으로 드러난 상태인지 여부.</summary>
        bool IsRevealed { get; }

        /// <summary>감속 중인지 여부.</summary>
        bool IsSlowed { get; }
    }
}
