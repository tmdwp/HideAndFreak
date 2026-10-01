using FishNet.Connection;
using FishNet.Object;
using GuardVsMannequin.Core;
using UnityEngine;

namespace GuardVsMannequin.Rules
{
    /// <summary>
    /// 룸과 게임 판을 잇는다. (서버 전용)
    /// - 게임 씬이 열리면 모든 플레이어가 씬을 불러올 때까지 기다렸다가 판을 시작한다.
    /// - 판이 끝나 대기 상태로 돌아가면 모두 룸 씬으로 돌려보낸다.
    /// 룸 없이 실행한 경우에는 아무것도 하지 않는다.
    /// </summary>
    public class MatchSessionBridge : NetworkBehaviour
    {
        [SerializeField] private GameStateMachine stateMachine;

        [Tooltip("씬을 불러오지 못한 플레이어가 \"로딩 중\" 알림도 보내지 않으면(멈춤·끊김) 이 시간 뒤에 시작한다.")]
        [SerializeField] private float maxWaitForPlayers = 15f;

        [Tooltip("로딩 중 알림을 계속 보내는 느린 플레이어라도 이 시간이 지나면 기다리지 않고 시작한다.")]
        [SerializeField] private float maxLoadingWait = 120f;

        private float waitStartTime;
        private bool matchStarted;
        private bool returningToRoom;

        /// <summary>
        /// 대기 시작 시각을 기록하고 상태 변경을 구독한다.
        /// </summary>
        public override void OnStartServer()
        {
            base.OnStartServer();
            waitStartTime = Time.time;
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
        /// 모든 플레이어가 준비되면 판을 시작한다.
        /// </summary>
        private void Update()
        {
            if (!this.IsServerReady() || matchStarted || GameServices.Room == null)
                return;
            if (stateMachine.CurrentState != GameState.Waiting)
                return;

            bool allLoaded = AllPlayersLoadedScene(out bool anyStillLoading);
            float elapsed = Time.time - waitStartTime;
            // 느리게라도 로딩 중인 플레이어는 최대 maxLoadingWait까지 기다리고, 알림이 끊긴 플레이어는 maxWaitForPlayers 후 포기한다.
            bool timedOut = elapsed >= maxLoadingWait || (elapsed >= maxWaitForPlayers && !anyStillLoading);
            if (!allLoaded && !timedOut)
                return;
            if (!allLoaded)
                Debug.LogWarning($"[MatchSessionBridge] 일부 플레이어가 맵을 불러오지 못한 채 {elapsed:F0}초가 지나 판을 시작합니다.");

            if (stateMachine.TryStartMatch(out string failReason))
                matchStarted = true;
            else if (timedOut)
            {
                // 인원이 부족해 시작할 수 없으면 룸으로 돌아간다.
                Debug.LogWarning($"[MatchSessionBridge] 판을 시작하지 못해 룸으로 돌아갑니다: {failReason}");
                ReturnToRoom();
            }
        }

        /// <summary>
        /// 접속한 모든 클라이언트가 이 게임 씬을 불러왔는지 확인한다.
        /// 아직 못 불러온 클라이언트 중 "로딩 중" 알림을 보내고 있는 클라이언트가 있으면 anyStillLoading = true.
        /// </summary>
        private bool AllPlayersLoadedScene(out bool anyStillLoading)
        {
            anyStillLoading = false;
            bool allLoaded = true;
            foreach (NetworkConnection conn in ServerManager.Clients.Values)
            {
                if (conn.Scenes.Contains(gameObject.scene))
                    continue;

                allLoaded = false;
                if (GameServices.SceneFlow != null && GameServices.SceneFlow.IsClientLoading(conn.ClientId))
                    anyStillLoading = true;
            }
            return allLoaded && ServerManager.Clients.Count > 0;
        }

        /// <summary>
        /// 결과 표시가 끝나 대기 상태로 돌아가면 룸으로 이동한다.
        /// </summary>
        private void OnServerStateChanged(GameState prev, GameState next)
        {
            if (prev == GameState.Ended && next == GameState.Waiting)
                ReturnToRoom();
        }

        /// <summary>
        /// 한 번만 룸으로 돌아간다.
        /// </summary>
        private void ReturnToRoom()
        {
            if (returningToRoom)
                return;

            returningToRoom = true;
            GameServices.SceneFlow?.ReturnToRoom();
        }
    }
}
