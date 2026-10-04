namespace GuardVsMannequin.Player
{
    /// <summary>
    /// 이동 속도에 곱할 배율을 제공한다. (달리기 등)
    /// <see cref="MoveSpeedController"/>가 같은 오브젝트에서 찾아 역할 속도에 곱한다.
    /// </summary>
    public interface IMoveSpeedModifier
    {
        /// <summary>현재 이동 속도 배율. 효과가 없으면 1.</summary>
        float SpeedMultiplier { get; }
    }
}
