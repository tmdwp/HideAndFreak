namespace GuardVsMannequin.Player
{
    /// <summary>
    /// HUD 등이 읽는 스태미나 상태. 달리기 구현에 직접 의존하지 않도록 분리한다(DIP).
    /// </summary>
    public interface IStaminaSource
    {
        /// <summary>남은 스태미나(0~1).</summary>
        float Stamina { get; }

        /// <summary>스태미나를 다 써서 다시 달릴 수 없는 상태인지 여부.</summary>
        bool IsExhausted { get; }
    }
}
