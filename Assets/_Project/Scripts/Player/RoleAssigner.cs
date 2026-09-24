using System;
using System.Collections.Generic;
using GuardVsMannequin.Core;

namespace GuardVsMannequin.Player
{
    /// <summary>
    /// 참가자에게 가드/마네킹 역할을 무작위로 배정한다.
    /// 네트워크에 의존하지 않는 순수 로직이라 단독으로 테스트할 수 있다.
    /// </summary>
    public class RoleAssigner
    {
        private readonly Random random;

        /// <summary>
        /// 난수 생성기를 주입받는다. null이면 새로 생성한다.
        /// </summary>
        public RoleAssigner(Random random = null)
        {
            this.random = random ?? new Random();
        }

        /// <summary>
        /// 클라이언트 ID 목록에 역할을 배정한다.
        /// 가드 수는 1 ~ 인원의 절반으로 보정되고, 나머지는 모두 마네킹이 된다.
        /// </summary>
        /// <param name="clientIds">참가자 클라이언트 ID 목록.</param>
        /// <param name="guardCount">희망 가드 수.</param>
        /// <returns>클라이언트 ID별 역할.</returns>
        public Dictionary<int, PlayerRole> Assign(IReadOnlyList<int> clientIds, int guardCount)
        {
            var result = new Dictionary<int, PlayerRole>(clientIds.Count);
            if (clientIds.Count == 0)
                return result;

            int maxGuardCount = GameSettingsValidator.GetMaxGuardCount(clientIds.Count);
            int clampedGuardCount = Math.Clamp(guardCount, 1, maxGuardCount);

            List<int> shuffled = new List<int>(clientIds);
            Shuffle(shuffled);

            for (int i = 0; i < shuffled.Count; i++)
                result[shuffled[i]] = i < clampedGuardCount ? PlayerRole.Guard : PlayerRole.Mannequin;

            return result;
        }

        /// <summary>
        /// Fisher-Yates 알고리즘으로 목록을 섞는다.
        /// </summary>
        private void Shuffle(List<int> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
