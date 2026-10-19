using GuardVsMannequin.Core;
using UnityEngine;

namespace GuardVsMannequin.Player
{
    /// <summary>
    /// 달리기 키(기본 Shift)를 누른 채 움직이면 스태미나를 쓰며 빨리 달린다.
    /// 로컬 소유자 아바타에서만 활성화된다(<see cref="PlayerOwnershipSetup"/>의 소유자 전용 목록).
    /// 꺼져 있으면(원격·서버의 다른 플레이어 아바타) 배율은 항상 1이다.
    /// 서버는 <see cref="MovementValidator"/>가 같은 규칙(<see cref="SprintRules"/>)으로 달리기 거리를 검증한다.
    /// </summary>
    public class SprintAbility : MonoBehaviour, IStaminaSource, IMoveSpeedModifier
    {
        private const float MinMoveInput = 0.01f;

        private readonly StaminaModel stamina = new();
        private IPlayerInput playerInput;
        private bool isSprinting;

        /// <inheritdoc />
        public float Stamina => stamina.Current;

        /// <inheritdoc />
        public bool IsExhausted => stamina.IsExhausted;

        /// <inheritdoc />
        public float SpeedMultiplier => isSprinting && isActiveAndEnabled ? SprintRules.SpeedMultiplier : 1f;

        /// <summary>
        /// 같은 오브젝트의 입력 구현을 찾는다.
        /// </summary>
        private void Awake()
        {
            playerInput = GetComponent<IPlayerInput>();
        }

        /// <summary>
        /// 꺼지면 달리기를 멈춘다.
        /// </summary>
        private void OnDisable()
        {
            isSprinting = false;
        }

        /// <summary>
        /// 달리기 입력과 이동 입력으로 스태미나를 진행한다.
        /// </summary>
        private void Update()
        {
            bool wantsSprint = playerInput != null && playerInput.IsSprintHeld && playerInput.Move.sqrMagnitude > MinMoveInput;
            isSprinting = stamina.Tick(wantsSprint, Time.deltaTime);
        }
    }
}
