using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

namespace GuardVsMannequin.Core
{
    /// <summary>승리한 진영.</summary>
    public enum MatchWinner : byte
    {
        None = 0,
        Guard = 1,
        Mannequin = 2
    }

    /// <summary>승패가 결정된 이유.</summary>
    public enum WinReason : byte
    {
        None = 0,

        /// <summary>마네킹 전멸. (가드 승리)</summary>
        AllMannequinsEliminated = 1,

        /// <summary>제한 시간 동안 목표 미달성. (가드 승리)</summary>
        ObjectiveFailed = 2,

        /// <summary>사냥 시간 종료까지 가드 생존. (가드 승리)</summary>
        GuardsSurvived = 3,

        /// <summary>사냥 시간 중 가드 전멸. (마네킹 승리)</summary>
        AllGuardsEliminated = 4
    }

    /// <summary>
    /// 이번 판의 승패 결과를 동기화한다.
    /// </summary>
    public class MatchResult : NetworkBehaviour
    {
        [SerializeField] private GameStateMachine stateMachine;

        private readonly SyncVar<MatchWinner> winner = new(MatchWinner.None);
        private readonly SyncVar<WinReason> reason = new(WinReason.None);

        /// <summary>
        /// 서버에서 상태 변경을 구독한다.
        /// </summary>
        public override void OnStartServer()
        {
            base.OnStartServer();
            stateMachine.ServerStateChanged += OnServerStateChanged;
        }

        /// <summary>
        /// 상태 변경 구독을 해제한다.
        /// </summary>
        public override void OnStopServer()
        {
            base.OnStopServer();
            stateMachine.ServerStateChanged -= OnServerStateChanged;
        }

        /// <summary>
        /// 새 판(준비 단계)이 시작되면 이전 결과를 지운다.
        /// </summary>
        private void OnServerStateChanged(GameState prev, GameState next)
        {
            if (next == GameState.Preparation)
                Clear();
        }

        /// <summary>승리한 진영. 결정 전에는 None.</summary>
        public MatchWinner Winner => winner.Value;

        /// <summary>승패 사유.</summary>
        public WinReason Reason => reason.Value;

        /// <summary>
        /// 결과를 기록한다. (서버 전용)
        /// </summary>
        [Server]
        public void SetResult(MatchWinner newWinner, WinReason newReason)
        {
            winner.Value = newWinner;
            reason.Value = newReason;
        }

        /// <summary>
        /// 새 판을 위해 결과를 지운다. (서버 전용)
        /// </summary>
        [Server]
        public void Clear()
        {
            SetResult(MatchWinner.None, WinReason.None);
        }
    }
}
