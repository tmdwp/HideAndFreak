namespace GuardVsMannequin.Core
{
    /// <summary>
    /// 한 판의 진행 단계.
    /// Waiting → Preparation → Playing → (Hunting) → Ended → Waiting 순서로 순환한다.
    /// </summary>
    public enum GameState : byte
    {
        /// <summary>접속 대기. 아직 아바타가 스폰되지 않은 상태.</summary>
        Waiting = 0,

        /// <summary>판 시작 준비(카운트다운). 모두 스폰된 채 움직일 수 없다. (GameTuning.MatchStartDelay)</summary>
        Preparation = 1,

        /// <summary>제한 시간 동안의 탐색 단계.</summary>
        Playing = 2,

        /// <summary>사냥 시간. 조명이 꺼지고 마네킹이 가드를 공격할 수 있다.</summary>
        Hunting = 3,

        /// <summary>승패가 결정되어 결과를 보여주는 단계.</summary>
        Ended = 4
    }
}
