using ECM2;
using GuardVsMannequin.Core;
using UnityEngine;

namespace GuardVsMannequin.Player
{
    /// <summary>
    /// 이번 판 게임 설정(룸에서 호스트가 정한 값)에 맞춰 캐릭터 이동 속도를 적용한다.
    /// - 가드: guardMoveSpeed
    /// - 마네킹: mannequinMoveSpeed, 사냥 시간에는 × <see cref="GameConstants.MannequinHuntSpeedMultiplier"/>
    /// 벽타기(Flying 모드)도 걷기와 같은 속도를 쓴다.
    /// 이동은 소유자 권한이지만 설정은 모든 피어에 동기화되므로 어디서 실행해도 같은 값이 된다.
    /// </summary>
    [RequireComponent(typeof(Character))]
    public class MoveSpeedController : MonoBehaviour
    {
        [Tooltip("속도 설정을 고를 역할. (Guard 또는 Mannequin)")]
        [SerializeField] private PlayerRole role = PlayerRole.Guard;

        private Character character;
        private float appliedSpeed = -1f;

        /// <summary>
        /// 캐릭터를 캐시한다.
        /// </summary>
        private void Awake()
        {
            character = GetComponent<Character>();
        }

        /// <summary>
        /// 목표 속도가 바뀌었을 때만 캐릭터에 적용한다. (설정 동기화, 사냥 시간 진입)
        /// </summary>
        private void Update()
        {
            IGameStateProvider state = GameServices.State;
            if (state == null)
                return;

            float speed = GetTargetSpeed(state.Settings, state.CurrentState);
            // 설정이 아직 동기화되지 않았으면(0) 프리팹 값을 유지한다.
            if (speed <= 0f || Mathf.Approximately(speed, appliedSpeed))
                return;

            appliedSpeed = speed;
            character.maxWalkSpeed = speed;
            character.maxFlySpeed = speed;
        }

        /// <summary>
        /// 역할과 게임 상태에 맞는 이동 속도를 계산한다.
        /// </summary>
        private float GetTargetSpeed(GameSettings settings, GameState currentState)
        {
            if (role == PlayerRole.Mannequin)
            {
                float multiplier = currentState == GameState.Hunting ? GameConstants.MannequinHuntSpeedMultiplier : 1f;
                return settings.mannequinMoveSpeed * multiplier;
            }
            return settings.guardMoveSpeed;
        }
    }
}
