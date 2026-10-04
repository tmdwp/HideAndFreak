namespace GuardVsMannequin.Core
{
    /// <summary>
    /// 언제 움직일 수 있는지 결정한다. 소유자 입력(<c>PlayerMovementDriver</c>)과 서버 검증(<c>MovementValidator</c>)이 같은 규칙을 쓴다.
    /// </summary>
    public static class MovementRules
    {
        /// <summary>
        /// 이동할 수 있는 상태인지 확인한다. 판 시작 준비(카운트다운) 중에는 모두 멈춰 있는다.
        /// </summary>
        public static bool CanMove(GameState state)
        {
            return state != GameState.Preparation;
        }
    }
}
