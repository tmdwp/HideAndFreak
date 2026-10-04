using System.Collections.Generic;
using GuardVsMannequin.Core;
using UnityEngine;

namespace GuardVsMannequin.Map
{
    /// <summary>
    /// 씬의 스폰 지점을 역할별로 모아 두고, 겹치지 않게 순서대로 나눠 준다.
    /// </summary>
    public class SpawnPointRegistry : MonoBehaviour
    {
        private readonly Dictionary<PlayerRole, List<SpawnPoint>> pointsByRole = new();
        private readonly Dictionary<PlayerRole, int> nextIndexByRole = new();

        /// <summary>
        /// 하위 계층을 포함해 씬의 모든 스폰 지점을 수집한다.
        /// </summary>
        private void Awake()
        {
            foreach (SpawnPoint point in FindObjectsByType<SpawnPoint>())
            {
                if (!pointsByRole.TryGetValue(point.Role, out List<SpawnPoint> list))
                {
                    list = new List<SpawnPoint>();
                    pointsByRole[point.Role] = list;
                }
                list.Add(point);
            }

            foreach (List<SpawnPoint> list in pointsByRole.Values)
                Shuffle(list);
        }

        /// <summary>
        /// 새 판을 시작할 때 배정 순서를 초기화하고 스폰 지점을 다시 섞는다.
        /// </summary>
        public void ResetAssignments()
        {
            nextIndexByRole.Clear();
            foreach (List<SpawnPoint> list in pointsByRole.Values)
                Shuffle(list);
        }

        /// <summary>
        /// 역할에 맞는 다음 스폰 지점을 반환한다. 지점이 부족하면 처음부터 다시 사용한다.
        /// </summary>
        /// <param name="role">스폰할 역할.</param>
        /// <param name="point">배정된 스폰 지점.</param>
        /// <returns>해당 역할의 스폰 지점이 존재하는지 여부.</returns>
        public bool TryGetNext(PlayerRole role, out SpawnPoint point)
        {
            point = null;
            if (!pointsByRole.TryGetValue(role, out List<SpawnPoint> list) || list.Count == 0)
                return false;

            nextIndexByRole.TryGetValue(role, out int index);
            point = list[index % list.Count];
            nextIndexByRole[role] = index + 1;
            return true;
        }

        /// <summary>
        /// 역할의 스폰 지점 전체. (배정 순서와 무관하게 읽기만 함, 없으면 빈 목록)
        /// </summary>
        public IReadOnlyList<SpawnPoint> GetPoints(PlayerRole role)
        {
            return pointsByRole.TryGetValue(role, out List<SpawnPoint> list) ? list : System.Array.Empty<SpawnPoint>();
        }

        /// <summary>
        /// 목록의 순서를 무작위로 섞는다.
        /// </summary>
        private static void Shuffle(List<SpawnPoint> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
