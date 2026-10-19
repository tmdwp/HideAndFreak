namespace GuardVsMannequin.Core
{
    /// <summary>
    /// 쿨타임이 있는 도구(총, 수류탄 등).
    /// HUD는 이 인터페이스만 보고 쿨타임을 표시한다.
    /// </summary>
    public interface ITool
    {
        /// <summary>HUD에 표시할 이름.</summary>
        string DisplayName { get; }

        /// <summary>지금 사용할 수 있는지 여부.</summary>
        bool IsReady { get; }

        /// <summary>남은 쿨타임(초).</summary>
        float CooldownRemaining { get; }
    }
}
