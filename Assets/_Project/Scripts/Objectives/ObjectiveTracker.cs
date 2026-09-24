using FishNet.Object;
using FishNet.Object.Synchronizing;
using GuardVsMannequin.Core;

namespace GuardVsMannequin.Objectives
{
    /// <summary>
    /// 이번 판에서 파괴된 전원 장치 수를 세고 동기화한다.
    /// 목표 횟수는 게임 설정(objectiveCount)을 따른다.
    /// </summary>
    public class ObjectiveTracker : NetworkBehaviour
    {
        private readonly SyncVar<int> destroyedCount = new();

        /// <summary>현재 달성 횟수.</summary>
        public int DestroyedCount => destroyedCount.Value;

        /// <summary>목표 수행 횟수.</summary>
        public int RequiredCount => GameServices.State?.Settings.objectiveCount ?? 0;

        /// <summary>목표가 1회 이상이고 모두 달성했는지 여부.</summary>
        public bool IsComplete => RequiredCount > 0 && DestroyedCount >= RequiredCount;

        /// <summary>
        /// 새 판을 위해 달성 횟수를 초기화한다.
        /// </summary>
        [Server]
        public void ResetCount()
        {
            destroyedCount.Value = 0;
        }

        /// <summary>
        /// 전원 장치의 파괴 이벤트를 구독한다.
        /// </summary>
        [Server]
        public void Track(Generator generator)
        {
            generator.ServerDestroyed += OnGeneratorDestroyed;
        }

        /// <summary>
        /// 파괴 시 달성 횟수를 올리고 구독을 해제한다.
        /// </summary>
        private void OnGeneratorDestroyed(Generator generator)
        {
            generator.ServerDestroyed -= OnGeneratorDestroyed;
            destroyedCount.Value++;
        }
    }
}
