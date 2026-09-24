using ECM2;
using FishNet.Object;
using UnityEngine;
using UnityEngine.Rendering;

namespace GuardVsMannequin.Player
{
    /// <summary>
    /// 아바타가 로컬 소유자인지에 따라 카메라, 입력, 물리 시뮬레이션을 켜고 끈다.
    /// 이동은 소유자 권한(NetworkTransform)으로 동기화하고, 원격 아바타는 시뮬레이션하지 않는다.
    /// </summary>
    [RequireComponent(typeof(PlayerIdentity))]
    public class PlayerOwnershipSetup : NetworkBehaviour
    {
        [SerializeField] private Character character;
        [SerializeField] private Camera playerCamera;

        [Tooltip("로컬 소유자에게만 활성화할 컴포넌트. (입력, 이동 드라이버 등)")]
        [SerializeField] private Behaviour[] ownerOnlyBehaviours;

        [Tooltip("소유자 본인 화면에서는 그림자만 남길 렌더러. (1인칭 몸체 등)")]
        [SerializeField] private Renderer[] hiddenForOwner;

        private PlayerIdentity identity;

        /// <summary>
        /// 같은 오브젝트의 PlayerIdentity를 캐시한다.
        /// </summary>
        private void Awake()
        {
            identity = GetComponent<PlayerIdentity>();
        }

        /// <summary>
        /// 클라이언트에서 스폰되면 소유 여부에 맞게 구성한다.
        /// </summary>
        public override void OnStartClient()
        {
            base.OnStartClient();

            if (IsOwner)
                SetupAsOwner();
            else
                SetupAsRemote();
        }

        /// <summary>
        /// 로컬 소유자 아바타가 제거되면 알린다.
        /// </summary>
        public override void OnStopClient()
        {
            base.OnStopClient();

            if (IsOwner)
                LocalPlayerEvents.RaiseDespawned(identity);
        }

        /// <summary>
        /// 카메라와 입력을 켜고 로컬 플레이어로 등록한다.
        /// </summary>
        private void SetupAsOwner()
        {
            playerCamera.gameObject.SetActive(true);
            character.camera = playerCamera;
            SetBehavioursEnabled(true);

            foreach (Renderer target in hiddenForOwner)
                target.shadowCastingMode = ShadowCastingMode.ShadowsOnly;

            LocalPlayerEvents.RaiseSpawned(identity);
        }

        /// <summary>
        /// 원격 아바타는 카메라와 입력을 끄고, NetworkTransform이 위치를 적용하도록 시뮬레이션을 멈춘다.
        /// </summary>
        private void SetupAsRemote()
        {
            playerCamera.gameObject.SetActive(false);
            SetBehavioursEnabled(false);

            character.enabled = false;
            character.characterMovement.interpolation = RigidbodyInterpolation.None;
        }

        /// <summary>
        /// 소유자 전용 컴포넌트를 일괄로 켜고 끈다.
        /// </summary>
        private void SetBehavioursEnabled(bool enabledState)
        {
            foreach (Behaviour behaviour in ownerOnlyBehaviours)
                behaviour.enabled = enabledState;
        }
    }
}
