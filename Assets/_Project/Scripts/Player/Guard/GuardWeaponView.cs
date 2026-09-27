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
    ///
    /// 위치 조정 (값은 매 프레임 적용되므로 Play 중 바꾸며 맞출 수 있다)
    /// - 1인칭 총: 카메라 기준이라 외형과 무관 → 이 컴포넌트의 값. Play 중 맞춘 뒤 Copy Component → 프리팹에 Paste Component Values.
    /// - 3인칭 총·조준 IK 손 위치: 외형마다 다름 → 외형 에셋(Skin_*.asset)의 Weapon Grip. 에셋이라 Play 중 바꾼 값이 그대로 저장된다.
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

        [Header("1인칭 총 위치 (카메라 기준)")]
        [SerializeField] private Vector3 firstPersonPosition = new Vector3(0.18f, -0.2f, 0.3f);
        [SerializeField] private Vector3 firstPersonRotation = Vector3.zero;
        [SerializeField] private float firstPersonScale = 1f;

        [Header("3인칭 총 위치 (외형이 없을 때만 사용)")]
        [Tooltip("손 위치는 외형마다 다르므로 보통 외형 에셋(Skin_*.asset)의 Weapon Grip을 쓴다. 외형이 없을 때만 이 값을 쓴다.")]
        [SerializeField] private WeaponGrip fallbackGrip = new WeaponGrip();

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
            if (firstPersonGun != null)
                firstPersonMuzzle = FindMuzzle(firstPersonGun);

            if (thirdPersonGunPrefab != null)
            {
                // 뼈의 크기·회전에 영향받지 않도록 아바타 루트에 두고 매 프레임 손 위치로 옮긴다.
                thirdPersonGun = Instantiate(thirdPersonGunPrefab, transform, false).transform;
                thirdPersonGun.name = "ThirdPersonGun";
                thirdPersonMuzzle = FindMuzzle(thirdPersonGun);
            }

            skinView = GetComponent<CharacterSkinView>();
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
        /// 애니메이션(IK 포함)이 끝난 뒤 총 위치를 적용한다. 인스펙터 값을 매 프레임 반영한다.
        /// </summary>
        private void LateUpdate()
        {
            WeaponGrip grip = CurrentGrip;
            if (skinView != null)
                skinView.SetAimHandOffset(grip.ikHandPosition);

            if (firstPersonGun != null)
            {
                firstPersonGun.localPosition = firstPersonPosition;
                firstPersonGun.localRotation = Quaternion.Euler(firstPersonRotation);
                firstPersonGun.localScale = Vector3.one * firstPersonScale;
            }

            if (thirdPersonGun == null || aimPivot == null)
                return;

            thirdPersonGun.localScale = Vector3.one * grip.gunScale;
            if (rightHand == null)
            {
                // 휴머노이드가 아니면 카메라 피벗 오른쪽 아래에 둔다.
                thirdPersonGun.SetPositionAndRotation(aimPivot.TransformPoint(new Vector3(0.2f, -0.3f, 0.4f)), aimPivot.rotation);
                return;
            }

            Quaternion baseRotation = grip.followHandRotation ? rightHand.rotation : aimPivot.rotation;
            thirdPersonGun.SetPositionAndRotation(rightHand.position + baseRotation * grip.handPositionOffset,
                baseRotation * Quaternion.Euler(grip.handRotationOffset));
        }

        /// <summary>
        /// 적용된 외형의 손 위치 설정. 외형이 없으면 이 컴포넌트의 예비 설정.
        /// (매 프레임 읽으므로 Play 중 외형 에셋 값을 바꾸면 바로 반영된다)
        /// </summary>
        private WeaponGrip CurrentGrip
        {
            get
            {
                CharacterSkin skin = skinView != null ? skinView.ActiveSkin : null;
                return skin != null && skin.weaponGrip != null ? skin.weaponGrip : fallbackGrip;
            }
        }

        /// <summary>
        /// 선택했을 때 씬 뷰에 총구 위치를 표시한다. (위치 조정용)
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            if (firstPersonMuzzle != null)
                Gizmos.DrawWireSphere(firstPersonMuzzle.position, 0.02f);
            if (thirdPersonMuzzle != null)
                Gizmos.DrawWireSphere(thirdPersonMuzzle.position, 0.03f);
        }
    }
}
