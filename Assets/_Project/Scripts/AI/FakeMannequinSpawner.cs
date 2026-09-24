using System.Collections.Generic;
using FishNet.Object;
using GuardVsMannequin.Core;
using GuardVsMannequin.Map;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace GuardVsMannequin.AI
{
    /// <summary>
    /// 가짜 마네킹의 수명 주기를 관리한다.
    /// - 준비 단계: 무작위 수만큼 생성 (최초 1회 NavMesh를 런타임에 굽는다)
    /// - 탐색 단계: 일정 간격마다 하나씩 제거
    /// - 남은 시간 20초 또는 사냥 시간 진입: 모두 제거 (실제 마네킹의 변신은 유지)
    /// </summary>
    public class FakeMannequinSpawner : NetworkBehaviour
    {
        [SerializeField] private GameStateMachine stateMachine;
        [SerializeField] private NetworkObject fakeMannequinPrefab;

        [Tooltip("가짜 마네킹이 돌아다닐 NavMesh. 준비 단계 첫 진입 시 서버에서 굽는다.")]
        [SerializeField] private NavMeshSurface navMeshSurface;

        [Tooltip("스폰 지점 주변으로 흩어 놓을 반경.")]
        [SerializeField] private float scatterRadius = 4f;

        private readonly List<Vector3> spawnAnchors = new();
        private readonly List<FakeMannequin> fakes = new();
        private bool navMeshBuilt;
        private float removalInterval;
        private float nextRemovalTime;

        /// <summary>
        /// 마네킹 스폰 지점을 기준점으로 수집하고 상태 변경을 구독한다.
        /// </summary>
        public override void OnStartServer()
        {
            base.OnStartServer();
            spawnAnchors.Clear();
            foreach (SpawnPoint point in FindObjectsByType<SpawnPoint>())
            {
                if (point.Role == PlayerRole.Mannequin)
                    spawnAnchors.Add(point.transform.position);
            }
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
        /// 단계에 따라 생성, 감소 일정 계산, 전체 제거를 한다.
        /// </summary>
        private void OnServerStateChanged(GameState prev, GameState next)
        {
            switch (next)
            {
                case GameState.Preparation:
                    SpawnFakes();
                    break;
                case GameState.Playing:
                    ScheduleRemovals();
                    break;
                default:
                    // 사냥 시간, 종료, 대기
                    DespawnAll();
                    break;
            }
        }

        /// <summary>
        /// 탐색 단계에서 주기적 감소와 남은 시간 20초 일괄 제거를 처리한다.
        /// </summary>
        private void Update()
        {
            if (!this.IsServerReady() || stateMachine.CurrentState != GameState.Playing || fakes.Count == 0)
                return;

            if (stateMachine.RemainingTime <= GameConstants.FakeMannequinPurgeTime)
            {
                DespawnAll();
                return;
            }

            if (Time.time >= nextRemovalTime)
            {
                Despawn(fakes[Random.Range(0, fakes.Count)]);
                nextRemovalTime = Time.time + removalInterval;
            }
        }

        /// <summary>
        /// NavMesh를 준비하고 무작위 수의 가짜 마네킹을 생성한다.
        /// </summary>
        private void SpawnFakes()
        {
            DespawnAll();
            EnsureNavMesh();

            if (spawnAnchors.Count == 0)
            {
                Debug.LogWarning("[FakeMannequinSpawner] 마네킹 스폰 지점이 없어 가짜 마네킹을 생성하지 않습니다.");
                return;
            }

            int count = Random.Range(GameConstants.FakeMannequinMinCount, GameConstants.FakeMannequinMaxCount + 1);
            for (int i = 0; i < count; i++)
            {
                if (TryFindSpawnPosition(out Vector3 position))
                    SpawnOne(position);
            }
        }

        /// <summary>
        /// 준비 단계 첫 진입 시 한 번만 NavMesh를 굽는다. (문이 닫힌 상태라 대기실은 분리됨)
        /// </summary>
        private void EnsureNavMesh()
        {
            if (navMeshBuilt || navMeshSurface == null)
                return;

            navMeshSurface.BuildNavMesh();
            navMeshBuilt = true;
        }

        /// <summary>
        /// 무작위 스폰 지점 주변에서 NavMesh 위의 위치를 찾는다.
        /// </summary>
        private bool TryFindSpawnPosition(out Vector3 position)
        {
            for (int attempt = 0; attempt < 5; attempt++)
            {
                Vector3 anchor = spawnAnchors[Random.Range(0, spawnAnchors.Count)];
                Vector2 offset = Random.insideUnitCircle * scatterRadius;
                if (NavMesh.SamplePosition(anchor + new Vector3(offset.x, 0f, offset.y), out NavMeshHit hit, 2f, NavMesh.AllAreas))
                {
                    position = hit.position;
                    return true;
                }
            }

            position = default;
            return false;
        }

        /// <summary>
        /// 가짜 마네킹 하나를 생성하고 피격 이벤트를 구독한다.
        /// </summary>
        private void SpawnOne(Vector3 position)
        {
            Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            NetworkObject instance = Instantiate(fakeMannequinPrefab, position, rotation);
            ServerManager.Spawn(instance);

            FakeMannequin fake = instance.GetComponent<FakeMannequin>();
            fake.ServerKilled += Despawn;
            fakes.Add(fake);
        }

        /// <summary>
        /// 남은 수가 사라지는 시점(남은 시간 20초)까지 고르게 줄어들도록 감소 간격을 정한다.
        /// </summary>
        private void ScheduleRemovals()
        {
            float activeTime = stateMachine.Settings.playTime - GameConstants.FakeMannequinPurgeTime;
            removalInterval = fakes.Count > 0 ? Mathf.Max(1f, activeTime / (fakes.Count + 1)) : float.MaxValue;
            nextRemovalTime = Time.time + removalInterval;
        }

        /// <summary>
        /// 가짜 마네킹 하나를 제거한다.
        /// </summary>
        private void Despawn(FakeMannequin fake)
        {
            fake.ServerKilled -= Despawn;
            fakes.Remove(fake);

            if (fake != null && fake.IsSpawned)
                ServerManager.Despawn(fake.NetworkObject);
        }

        /// <summary>
        /// 모든 가짜 마네킹을 제거한다.
        /// </summary>
        private void DespawnAll()
        {
            for (int i = fakes.Count - 1; i >= 0; i--)
                Despawn(fakes[i]);
        }
    }
}
