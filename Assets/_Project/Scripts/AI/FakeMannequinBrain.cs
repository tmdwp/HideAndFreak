using FishNet.Object;
using GuardVsMannequin.Core;
using GuardVsMannequin.Player.Emotes;
using UnityEngine;
using UnityEngine.AI;

namespace GuardVsMannequin.AI
{
    /// <summary>
    /// 가짜 마네킹의 서버 AI.
    /// "무작위 방향으로 이동 / 감정 표현 / 잠시 멈춤"을 무작위로 반복한다.
    /// 이동 속도와 감정 표현은 실제 마네킹과 같다.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent), typeof(EmoteController))]
    public class FakeMannequinBrain : NetworkBehaviour
    {
        /// <summary>현재 행동.</summary>
        private enum Action
        {
            Idle,
            Walk,
            Emote
        }

        [SerializeField] private float wanderRadius = 7f;
        [SerializeField] private float maxWalkTime = 6f;
        [SerializeField] private Vector2 idleTimeRange = new Vector2(1f, 3f);

        [Tooltip("행동 선택 가중치. (이동, 감정 표현, 멈춤)")]
        [SerializeField] private Vector3 actionWeights = new Vector3(0.5f, 0.25f, 0.25f);

        private NavMeshAgent agent;
        private EmoteController emoteController;
        private Action currentAction;
        private float actionEndTime;

        /// <summary>
        /// 구성 요소를 캐시한다. 에이전트는 서버에서만 켠다.
        /// </summary>
        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            emoteController = GetComponent<EmoteController>();
            agent.enabled = false;
        }

        /// <summary>
        /// 서버에서 에이전트를 켜고 실제 마네킹과 같은 이동 속도를 적용한다.
        /// </summary>
        public override void OnStartServer()
        {
            base.OnStartServer();
            agent.enabled = true;
            agent.speed = GameConstants.MannequinMoveSpeed;
            agent.Warp(transform.position);
            ChooseNextAction();
        }

        /// <summary>
        /// 서버에서 에이전트를 끈다.
        /// </summary>
        public override void OnStopServer()
        {
            base.OnStopServer();
            agent.enabled = false;
        }

        /// <summary>
        /// 서버에서 현재 행동이 끝났는지 확인하고 다음 행동을 고른다.
        /// </summary>
        private void Update()
        {
            if (!this.IsServerReady() || !agent.isOnNavMesh)
                return;

            if (IsCurrentActionFinished())
                ChooseNextAction();
        }

        /// <summary>
        /// 현재 행동의 종료 여부를 판단한다. 이동은 도착하거나 시간이 지나면 끝난다.
        /// </summary>
        private bool IsCurrentActionFinished()
        {
            if (Time.time >= actionEndTime)
                return true;

            return currentAction == Action.Walk && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f;
        }

        /// <summary>
        /// 가중치에 따라 다음 행동을 고르고 시작한다.
        /// </summary>
        private void ChooseNextAction()
        {
            float total = actionWeights.x + actionWeights.y + actionWeights.z;
            float roll = Random.value * total;

            if (roll < actionWeights.x && TryStartWalk())
                return;
            if (roll < actionWeights.x + actionWeights.y && TryStartEmote())
                return;

            StartIdle();
        }

        /// <summary>
        /// 주변의 무작위 NavMesh 지점으로 이동을 시작한다.
        /// </summary>
        private bool TryStartWalk()
        {
            // 수평으로만 고른다. (위아래로 고르면 천장 위 등 도달할 수 없는 면이 선택될 수 있음)
            Vector2 offset = Random.insideUnitCircle * wanderRadius;
            Vector3 candidate = transform.position + new Vector3(offset.x, 0f, offset.y);
            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                return false;

            agent.isStopped = false;
            agent.SetDestination(hit.position);
            currentAction = Action.Walk;
            actionEndTime = Time.time + maxWalkTime;
            return true;
        }

        /// <summary>
        /// 제자리에서 무작위 감정 표현을 재생한다.
        /// </summary>
        private bool TryStartEmote()
        {
            if (!emoteController.ServerPlay(Random.Range(1, GameConstants.EmoteCount + 1)))
                return false;

            agent.isStopped = true;
            currentAction = Action.Emote;
            actionEndTime = Time.time + GameConstants.EmoteDuration + 0.2f;
            return true;
        }

        /// <summary>
        /// 잠시 제자리에 멈춘다.
        /// </summary>
        private void StartIdle()
        {
            agent.isStopped = true;
            currentAction = Action.Idle;
            actionEndTime = Time.time + Random.Range(idleTimeRange.x, idleTimeRange.y);
        }
    }
}
