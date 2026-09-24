using System.Collections.Generic;
using FishNet.Object;
using GuardVsMannequin.Core;
using UnityEngine;

namespace GuardVsMannequin.Objectives
{
    /// <summary>
    /// 준비 단계에 후보 위치 중 무작위로 전원 장치를 생성하고, 대기 상태로 돌아가면 제거한다.
    /// </summary>
    public class GeneratorSpawner : NetworkBehaviour
    {
        [SerializeField] private GameStateMachine stateMachine;
        [SerializeField] private ObjectiveTracker objectiveTracker;
        [SerializeField] private NetworkObject generatorPrefab;

        private readonly List<GeneratorSpawnPoint> spawnPoints = new();
        private readonly List<NetworkObject> spawnedGenerators = new();

        /// <summary>
        /// 후보 위치를 수집하고 상태 변경 이벤트를 구독한다.
        /// </summary>
        public override void OnStartServer()
        {
            base.OnStartServer();
            spawnPoints.Clear();
            spawnPoints.AddRange(FindObjectsByType<GeneratorSpawnPoint>());
            stateMachine.ServerStateChanged += OnServerStateChanged;
        }

        /// <summary>
        /// 상태 변경 이벤트 구독을 해제한다.
        /// </summary>
        public override void OnStopServer()
        {
            base.OnStopServer();
            stateMachine.ServerStateChanged -= OnServerStateChanged;
        }

        /// <summary>
        /// 준비 단계 진입 시 생성, 대기 상태 복귀 시 제거한다.
        /// </summary>
        private void OnServerStateChanged(GameState prev, GameState next)
        {
            if (next == GameState.Preparation)
                SpawnGenerators(stateMachine.Settings.generatorCount);
            else if (next == GameState.Waiting)
                DespawnAll();
        }

        /// <summary>
        /// 후보 위치를 섞어 앞에서부터 count개에 전원 장치를 생성한다.
        /// </summary>
        private void SpawnGenerators(int count)
        {
            DespawnAll();
            objectiveTracker.ResetCount();

            if (count > spawnPoints.Count)
            {
                Debug.LogWarning($"[GeneratorSpawner] 후보 위치({spawnPoints.Count})가 설정된 전원 장치 수({count})보다 적습니다.");
                count = spawnPoints.Count;
            }

            Shuffle(spawnPoints);
            for (int i = 0; i < count; i++)
            {
                Transform point = spawnPoints[i].transform;
                NetworkObject instance = Instantiate(generatorPrefab, point.position, point.rotation);
                ServerManager.Spawn(instance);

                objectiveTracker.Track(instance.GetComponent<Generator>());
                spawnedGenerators.Add(instance);
            }
        }

        /// <summary>
        /// 이번 판에 생성한 전원 장치를 모두 제거한다.
        /// </summary>
        private void DespawnAll()
        {
            foreach (NetworkObject generator in spawnedGenerators)
            {
                if (generator != null && generator.IsSpawned)
                    ServerManager.Despawn(generator);
            }
            spawnedGenerators.Clear();
        }

        /// <summary>
        /// 목록의 순서를 무작위로 섞는다.
        /// </summary>
        private static void Shuffle(List<GeneratorSpawnPoint> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
