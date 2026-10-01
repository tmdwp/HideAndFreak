using UnityEngine;

namespace GuardVsMannequin.Player
{
    /// <summary>
    /// 일반(Generic) 모델의 이동 애니메이션을 제자리 애니메이션으로 만든다.
    /// 클립이 뿌리 뼈(또는 그 부모)를 앞으로 옮기면, 애니메이션 적용 후(LateUpdate) 그 수평 이동만큼 뿌리 뼈를 되돌린다.
    /// 높이(통통 튀는 움직임)와 회전은 그대로 둔다. 애니메이터가 있는 오브젝트에 붙인다.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class RootBoneInPlaceLock : MonoBehaviour
    {
        private Transform rootBone;
        private Vector3 restLocalPosition;

        /// <summary>
        /// 스킨드 메시의 뿌리 뼈를 찾고, 기본 자세의 위치(애니메이터 기준 좌표)를 저장한다.
        /// </summary>
        private void Awake()
        {
            SkinnedMeshRenderer skinned = GetComponentInChildren<SkinnedMeshRenderer>(true);
            rootBone = skinned != null ? skinned.rootBone : null;
            if (rootBone == null || rootBone == transform)
            {
                // 뿌리 뼈가 없거나 애니메이터 자신이면 되돌릴 기준이 없으므로 끈다.
                enabled = false;
                return;
            }
            restLocalPosition = transform.InverseTransformPoint(rootBone.position);
        }

        /// <summary>
        /// 애니메이션이 적용된 뒤 뿌리 뼈의 수평 이동을 지운다.
        /// </summary>
        private void LateUpdate()
        {
            Vector3 current = transform.InverseTransformPoint(rootBone.position);
            Vector3 drift = current - restLocalPosition;
            drift.y = 0f;
            if (drift.sqrMagnitude > 0f)
                rootBone.position -= transform.TransformVector(drift);
        }
    }
}
