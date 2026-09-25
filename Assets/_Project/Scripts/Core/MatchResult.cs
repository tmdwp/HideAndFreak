using System.Collections.Generic;
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

    /// <summary>참가자 한 명의 판 결과.</summary>
    public struct ParticipantResult
    {
        public string displayName;
        public PlayerRole role;
        public bool survived;
    }

    /// <summary>
    /// 이번 판의 승패 결과와 참가자별 결과를 동기화한다.
    /// </summary>
    public class MatchResult : NetworkBehaviour
    {
        [SerializeField] private GameStateMachine stateMachine;

        private readonly SyncVar<MatchWinner> winner = new(MatchWinner.None);
        private readonly SyncVar<WinReason> reason = new(WinReason.None);

        /// <summary>판이 끝났을 때 공개되는 참가자 결과. (진행 중에는 비어 있어 사망 정보가 새지 않음)</summary>
        private readonly SyncList<ParticipantResult> publishedParticipants = new();

        /// <summary>서버에서만 관리하는 이번 판 참가자와 아바타.</summary>
        private readonly List<(string name, PlayerRole role, NetworkObject avatar)> serverParticipants = new();

        /// <summary>공개된 참가자 결과. 판이 끝나기 전에는 비어 있다.</summary>
        public IReadOnlyList<ParticipantResult> Participants => publishedParticipants;

        /// <summary>
        /// 새 판을 위해 참가자 목록을 비운다. (서버 전용, 스폰 직전에 호출)
        /// </summary>
        [Server]
        public void ResetParticipants()
        {
            serverParticipants.Clear();
            publishedParticipants.Clear();
        }

        /// <summary>
        /// 이번 판 참가자를 등록한다. 결과 공개 시점에 아바타가 남아 있으면 생존으로 본다. (서버 전용)
        /// </summary>
        [Server]
        public void RegisterParticipant(string displayName, PlayerRole role, NetworkObject avatar)
        {
            serverParticipants.Add((displayName, role, avatar));
        }

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

            if (newWinner != MatchWinner.None)
                PublishParticipants();
        }

        /// <summary>
        /// 새 판을 위해 승패와 공개된 결과를 지운다. (서버 전용, 참가자 등록은 유지)
        /// </summary>
        [Server]
        public void Clear()
        {
            winner.Value = MatchWinner.None;
            reason.Value = WinReason.None;
            publishedParticipants.Clear();
        }

        /// <summary>
        /// 참가자별 생존 여부를 확정해 모든 클라이언트에 공개한다.
        /// 사망하거나 접속을 끊은 참가자는 아바타가 제거되어 탈락으로 표시된다.
        /// </summary>
        private void PublishParticipants()
        {
            publishedParticipants.Clear();
            foreach ((string name, PlayerRole role, NetworkObject avatar) in serverParticipants)
            {
                publishedParticipants.Add(new ParticipantResult
                {
                    displayName = name,
                    role = role,
                    survived = avatar != null && avatar.IsSpawned
                });
            }
        }
    }
}
