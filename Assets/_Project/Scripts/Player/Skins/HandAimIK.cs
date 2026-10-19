using UnityEngine;

namespace GuardVsMannequin.Player
{
    /// <summary>
    /// 휴머노이드 외형의 오른손을 조준 방향 앞으로 뻗고, 머리가 조준 방향을 보게 한다.
    /// (총을 든 가드용. 애니메이터 레이어의 IK Pass가 켜져 있어야 동작한다)
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class HandAimIK : MonoBehaviour
    {
        private const float LookDistance = 10f;

        private Animator animator;

        /// <summary>조준 방향 기준. (가드의 카메라 피벗) 없으면 IK를 쓰지 않는다.</summary>
        public Transform AimSource { get; set; }

        /// <summary>조준 기준 좌표의 오른손 위치. (GuardWeaponView가 인스펙터 값으로 매 프레임 지정)</summary>
        public Vector3 HandOffset { get; set; } = new Vector3(0.18f, -0.15f, 0.45f);

        /// <summary>손 목표를 직접 정하는 쪽. (양손 총) 없거나 정하지 않으면 <see cref="HandOffset"/>의 오른손만 쓴다.</summary>
        public IHandIKGoalSource GoalSource { get; set; }

        /// <summary>
        /// 애니메이터를 캐시한다.
        /// </summary>
        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        /// <summary>
        /// 손 위치와 시선을 조준 방향에 맞춘다. 양손 총이면 두 손을 총의 손잡이 지점에 붙이고 팔꿈치 방향을 정한다.
        /// </summary>
        private void OnAnimatorIK(int layerIndex)
        {
            if (AimSource == null)
                return;

            if (GoalSource == null || !GoalSource.TryGetHandGoals(out HandIKGoals goals))
                goals = new HandIKGoals { RightHand = AimSource.TransformPoint(HandOffset) };

            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1f);
            animator.SetIKPosition(AvatarIKGoal.RightHand, goals.RightHand);
            ApplyOptional(AvatarIKGoal.LeftHand, goals.LeftHand);
            ApplyOptionalHint(AvatarIKHint.RightElbow, goals.RightElbowHint);
            ApplyOptionalHint(AvatarIKHint.LeftElbow, goals.LeftElbowHint);

            animator.SetLookAtWeight(1f, 0.3f, 0.8f);
            animator.SetLookAtPosition(AimSource.position + AimSource.forward * LookDistance);
        }

        /// <summary>
        /// 값이 있으면 손 IK를 켜고 위치를 정하고, 없으면 끈다. (애니메이션 그대로)
        /// </summary>
        private void ApplyOptional(AvatarIKGoal goal, Vector3? position)
        {
            animator.SetIKPositionWeight(goal, position.HasValue ? 1f : 0f);
            if (position.HasValue)
                animator.SetIKPosition(goal, position.Value);
        }

        /// <summary>
        /// 값이 있으면 팔꿈치 힌트를 켜고 위치를 정하고, 없으면 끈다.
        /// </summary>
        private void ApplyOptionalHint(AvatarIKHint hint, Vector3? position)
        {
            animator.SetIKHintPositionWeight(hint, position.HasValue ? 1f : 0f);
            if (position.HasValue)
                animator.SetIKHintPosition(hint, position.Value);
        }
    }
}
