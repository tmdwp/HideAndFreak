using ECM2;
using GuardVsMannequin.Core;
using UnityEngine;

namespace GuardVsMannequin.Player.Guard
{
    /// <summary>
    /// 1인칭 가드 캐릭터.
    /// 좌우 시점은 몸체를 회전시키고, 상하 시점은 카메라 피벗만 회전시킨다.
    /// </summary>
    public class GuardCharacter : Character, ILookControllable
    {
        [Space(15f)]
        [Tooltip("상하 회전을 적용할 카메라 피벗(눈 높이).")]
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private float minPitch = -80f;
        [SerializeField] private float maxPitch = 80f;

        private float cameraPitch;
        private readonly PendingLookYaw pendingYaw = new();

        /// <summary>
        /// 회전은 시점 입력으로만 처리한다. (이동 속도는 MoveSpeedController가 게임 설정으로 적용)
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            SetRotationMode(RotationMode.None);
        }

        /// <summary>
        /// 좌우 입력은 몸체 Yaw, 상하 입력은 카메라 Pitch에 반영한다.
        /// </summary>
        public void AddLookInput(Vector2 lookDelta)
        {
            if (lookDelta.x != 0f)
            {
                AddYawInput(lookDelta.x);
                pendingYaw.Add(lookDelta.x);
            }

            if (lookDelta.y != 0f)
                cameraPitch = MathLib.ClampAngle(cameraPitch - lookDelta.y, minPitch, maxPitch);
        }

        /// <summary>
        /// 모아 둔 좌우 입력이 물리 단계에서 몸에 반영되면 카메라용 대기 입력도 비운다.
        /// </summary>
        protected override void ConsumeRotationInput()
        {
            base.ConsumeRotationInput();
            pendingYaw.Consume();
        }

        /// <summary>
        /// 카메라 피벗에 현재 Pitch를 적용한다.
        /// 시점을 직접 조작하는 소유자는 좌우도 매 프레임 최신 입력으로 돌린다. (몸은 물리 주기로 따라온다)
        /// </summary>
        private void LateUpdate()
        {
            if (cameraPivot == null)
                return;

            if (pendingYaw.HasInput)
                cameraPivot.rotation = Quaternion.Euler(cameraPitch, pendingYaw.ViewYaw(this), 0f);
            else
                cameraPivot.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }

        /// <summary>
        /// 컴포넌트 추가 시 1인칭에 맞게 회전 모드를 초기화한다.
        /// </summary>
        protected override void Reset()
        {
            base.Reset();
            SetRotationMode(RotationMode.None);
        }
    }
}
