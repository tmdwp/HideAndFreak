using UnityEngine;

namespace GuardVsMannequin.Player
{
    /// <summary>
    /// 조준 IK의 손·팔꿈치 목표 위치(월드 좌표). 값이 없는 항목은 IK를 쓰지 않는다.
    /// </summary>
    public struct HandIKGoals
    {
        public Vector3 RightHand;
        public Vector3? LeftHand;

        /// <summary>오른팔꿈치가 향할 위치. (팔이 굽는 방향)</summary>
        public Vector3? RightElbowHint;

        /// <summary>왼팔꿈치가 향할 위치. (팔이 굽는 방향)</summary>
        public Vector3? LeftElbowHint;
    }

    /// <summary>
    /// 조준 IK(<see cref="HandAimIK"/>)에 손 목표 위치를 알려 주는 쪽. (총을 양손으로 잡는 GuardWeaponView)
    /// IK 직전에 불리므로, 여기서 총을 놓고 그 총의 손잡이 위치를 돌려주면 손과 총이 같은 프레임에 맞는다.
    /// </summary>
    public interface IHandIKGoalSource
    {
        /// <summary>
        /// 손 목표를 구한다. 정하지 않으면 false. (그러면 IK는 기본 오른손 위치를 쓴다)
        /// </summary>
        bool TryGetHandGoals(out HandIKGoals goals);
    }
}
