using System.Collections.Generic;
using FishNet;
using GuardVsMannequin.Core;
using GuardVsMannequin.Player;
using UnityEngine;

namespace GuardVsMannequin.Map
{
    /// <summary>
    /// 대기실이 맵과 떨어져 있는 맵에서, 탐색 단계가 시작되면 서버가 가드를 출발 지점(<see cref="GuardReleasePoint"/>)으로 옮긴다.
    /// 출발 지점이 없는 맵(대기실 문이 맵에 붙어 있는 맵)에서는 아무 일도 하지 않는다.
    /// 이동은 <see cref="MovementValidator.ServerTeleport"/>로 처리해 이동 검증에 걸리지 않는다.
    /// </summary>
    public class GuardReleaseTeleporter : MonoBehaviour
    {
        private readonly List<GuardReleasePoint> releasePoints = new();
        private GameState previousState = GameState.Waiting;

        /// <summary>
        /// 씬의 출발 지점을 모은다.
        /// </summary>
        private void Awake()
        {
            releasePoints.AddRange(FindObjectsByType<GuardReleasePoint>());
        }

        /// <summary>
        /// 서버에서 게임 상태가 탐색 단계로 바뀌는 순간 가드를 옮긴다.
        /// </summary>
        private void Update()
        {
            GameState state = GameServices.CurrentStateOrWaiting;
            if (state == previousState)
                return;

            bool released = previousState == GameState.Preparation && state == GameState.Playing;
            previousState = state;

            if (released && InstanceFinder.IsServerStarted && releasePoints.Count > 0)
                ReleaseGuards();
        }

        /// <summary>
        /// 살아 있는 가드를 출발 지점에 차례로 배정한다. (지점보다 가드가 많으면 돌아가며 쓴다)
        /// </summary>
        private void ReleaseGuards()
        {
            int index = 0;
            foreach (MovementValidator validator in FindObjectsByType<MovementValidator>())
            {
                PlayerIdentity identity = validator.GetComponent<PlayerIdentity>();
                if (identity == null || identity.Role != PlayerRole.Guard)
                    continue;

                Transform point = releasePoints[index % releasePoints.Count].transform;
                validator.ServerTeleport(point.position, point.rotation);
                index++;
            }
        }
    }
}
