using FishNet.Object;
using UnityEngine;
using UnityEngine.Rendering;

namespace GuardVsMannequin.Player.Guard
{
    /// <summary>
    /// 가드의 총 외형을 관리한다.
    /// - 소유자: 카메라 앞의 1인칭 총을 보여준다. (손에 든 3인칭 총은 그림자만)
    /// - 다른 플레이어: 외형의 오른손에 3인칭 총을 들려 조준 방향으로 향하게 한다.
    /// 사격 연출은 <see cref="Muzzle"/>(보는 사람 기준 총구)에서 시작한다.
    /// 총 모델 프리팹은 "캐릭터·무기 에셋 생성" 도구가 총구가 +Z를 향하고 Muzzle 자식을 갖도록 만든다.
    /// </summary>
    public class GuardWeaponView : NetworkBehaviour
    {
        private const string MuzzleName = "Muzzle";

        [Tooltip("카메라 피벗 아래의 1인칭 총. 소유자에게만 보인다.")]
        [SerializeField] private Transform firstPersonGun;

        [Tooltip("다른 플레이어에게 보일 3인칭 총 프리팹. (Muzzle 자식 필요)")]
        [SerializeField] private GameObject thirdPersonGunPrefab;

        [Tooltip("조준 방향 기준. (카메라 피벗, 원격 아바타는 동기화된 상하 시선이 적용된다)")]
        [SerializeField] private Transform aimPivot;

        [Tooltip("손 뼈 기준 3인칭 총 위치 보정(조준 방향 좌표).")]
        [SerializeField] private Vector3 handOffset = new Vector3(0f, 0.02f, 0.06f);

        private CharacterSkinView skinView;
        private Transform firstPersonMuzzle;
        private Transform thirdPersonGun;
        private Transform thirdPersonMuzzle;
        private Transform rightHand;

        /// <summary>보는 사람 기준 총구. (소유자는 1인칭 총, 다른 플레이어는 3인칭 총)</summary>
        public Transform Muzzle => IsOwner || thirdPersonMuzzle == null ? firstPersonMuzzle : thirdPersonMuzzle;

        /// <summary>
        /// 3인칭 총을 만들고, 외형의 조준 IK와 손 뼈를 연결한다.
        /// </summary>
        private void Awake()
        {
            skinView = GetComponent<CharacterSkinView>();
            if (firstPersonGun != null)
                firstPersonMuzzle = FindMuzzle(firstPersonGun);

            if (thirdPersonGunPrefab != null)
            {
                // 뼈의 크기·회전에 영향받지 않도록 아바타 루트에 두고 매 프레임 손 위치로 옮긴다.
                thirdPersonGun = Instantiate(thirdPersonGunPrefab, transform, false).transform;
                thirdPersonGun.name = "ThirdPersonGun";
                thirdPersonMuzzle = FindMuzzle(thirdPersonGun);
            }

            if (skinView != null)
            {
                skinView.SetAimSource(aimPivot);
                Animator animator = skinView.Animator;
                if (animator != null && animator.isHuman)
                    rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            }
        }

        /// <summary>
        /// 총 모델의 Muzzle 자식을 찾는다. 없으면 총 자체를 총구로 쓴다.
        /// </summary>
        private static Transform FindMuzzle(Transform gun)
        {
            Transform found = gun.Find(MuzzleName);
            return found != null ? found : gun;
        }

        /// <summary>
        /// 소유자에게는 1인칭 총만, 다른 플레이어에게는 3인칭 총만 보이게 한다.
        /// </summary>
        public override void OnStartClient()
        {
            base.OnStartClient();
            if (firstPersonGun != null)
                firstPersonGun.gameObject.SetActive(IsOwner);

            if (IsOwner && thirdPersonGun != null)
            {
                foreach (Renderer gunRenderer in thirdPersonGun.GetComponentsInChildren<Renderer>(true))
                    gunRenderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            }
        }

        /// <summary>
        /// 애니메이션(IK 포함)이 끝난 뒤 3인칭 총을 손 위치로 옮기고 조준 방향으로 향하게 한다.
        /// </summary>
        private void LateUpdate()
        {
            if (thirdPersonGun == null || aimPivot == null)
                return;

            Quaternion aimRotation = aimPivot.rotation;
            Vector3 position = rightHand != null
                ? rightHand.position + aimRotation * handOffset
                : aimPivot.position + aimRotation * new Vector3(0.2f, -0.3f, 0.4f);
            thirdPersonGun.SetPositionAndRotation(position, aimRotation);
        }
    }
}
