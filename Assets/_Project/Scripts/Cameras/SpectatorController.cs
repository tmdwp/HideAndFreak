using GuardVsMannequin.Core;
using GuardVsMannequin.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GuardVsMannequin.Cameras
{
    /// <summary>
    /// 로컬 아바타가 없을 때(사망 또는 미참가) 생존 중인 다른 플레이어의 시야로 관전하게 한다.
    /// 좌우 화살표로 관전 대상을 바꾸고, 대상이 사망하면 자동으로 다음 대상으로 넘어간다.
    /// 씬 카메라(SceneCameraSwitcher가 아바타가 없을 때 켜는 카메라)를 움직인다.
    /// </summary>
    public class SpectatorController : MonoBehaviour
    {
        private const string PlayerMapName = "Player";

        [SerializeField] private InputActionAsset inputActions;

        private InputActionAsset runtimeActions;
        private InputAction previousAction;
        private InputAction nextAction;
        private Vector3 overviewPosition;
        private Quaternion overviewRotation;

        /// <summary>현재 관전 중인 대상. 관전 중이 아니면 null.</summary>
        public SpectatorViewPoint CurrentTarget { get; private set; }

        /// <summary>
        /// 관전 대상을 바꾸고, 이전/새 대상에게 관전 여부를 알린다. (대상 시야 효과 적용용)
        /// </summary>
        private void SetTarget(SpectatorViewPoint target)
        {
            if (CurrentTarget == target)
                return;

            if (CurrentTarget != null)
                CurrentTarget.SetSpectated(false);

            CurrentTarget = target;

            if (CurrentTarget != null)
                CurrentTarget.SetSpectated(true);
        }

        /// <summary>관전 중인지 여부.</summary>
        public bool IsSpectating => CurrentTarget != null;

        /// <summary>
        /// 대기 화면용 위치를 저장하고 관전 전환 입력을 준비한다.
        /// </summary>
        private void Awake()
        {
            overviewPosition = transform.position;
            overviewRotation = transform.rotation;

            runtimeActions = Instantiate(inputActions);
            InputActionMap map = runtimeActions.FindActionMap(PlayerMapName, true);
            previousAction = map.FindAction("SpectatePrevious", true);
            nextAction = map.FindAction("SpectateNext", true);
        }

        /// <summary>
        /// 관전 전환 입력을 켠다.
        /// </summary>
        private void OnEnable()
        {
            previousAction.Enable();
            nextAction.Enable();
        }

        /// <summary>
        /// 관전 전환 입력을 끈다.
        /// </summary>
        private void OnDisable()
        {
            previousAction.Disable();
            nextAction.Disable();
        }

        /// <summary>
        /// 복제한 입력 에셋을 해제한다.
        /// </summary>
        private void OnDestroy()
        {
            if (runtimeActions != null)
                Destroy(runtimeActions);
        }

        /// <summary>
        /// 관전 조건을 확인하고 대상을 고른다. 카메라 이동은 대상 위치가 갱신된 뒤인 LateUpdate에서 한다.
        /// </summary>
        private void Update()
        {
            if (!ShouldSpectate())
            {
                SetTarget(null);
                return;
            }

            int direction = previousAction.WasPressedThisFrame() ? -1 : nextAction.WasPressedThisFrame() ? 1 : 0;
            if (CurrentTarget == null || !IsAlive(CurrentTarget))
                SelectTarget(0, 0);
            else if (direction != 0)
                SelectTarget(IndexOf(CurrentTarget), direction);
        }

        /// <summary>
        /// 관전 중이면 대상의 시야로, 아니면 대기 화면 위치로 카메라를 옮긴다.
        /// </summary>
        private void LateUpdate()
        {
            if (CurrentTarget != null)
            {
                CurrentTarget.GetPose(out Vector3 position, out Quaternion rotation);
                transform.SetPositionAndRotation(position, rotation);
            }
            else
            {
                transform.SetPositionAndRotation(overviewPosition, overviewRotation);
            }
        }

        /// <summary>
        /// 판이 진행 중이고 내 아바타가 없으며, 볼 수 있는 생존자가 있을 때 관전한다.
        /// </summary>
        private static bool ShouldSpectate()
        {
            GameState state = GameServices.CurrentStateOrWaiting;
            return state != GameState.Waiting && LocalPlayerEvents.Current == null && ClientPlayerList.Players.Count > 0;
        }

        /// <summary>
        /// 현재 위치에서 방향만큼 이동한 생존자를 관전 대상으로 고른다.
        /// </summary>
        private void SelectTarget(int fromIndex, int direction)
        {
            int count = ClientPlayerList.Players.Count;
            for (int step = 0; step < count; step++)
            {
                int index = ((fromIndex + direction * (step + 1)) % count + count) % count;
                if (direction == 0)
                    index = (fromIndex + step) % count;

                PlayerIdentity candidate = ClientPlayerList.Players[index];
                if (candidate != null && candidate.TryGetComponent(out SpectatorViewPoint viewPoint))
                {
                    SetTarget(viewPoint);
                    return;
                }
            }
            SetTarget(null);
        }

        /// <summary>
        /// 관전 대상이 아직 생존자 목록에 있는지 확인한다.
        /// </summary>
        private static bool IsAlive(SpectatorViewPoint target)
        {
            return target != null && IndexOf(target) >= 0;
        }

        /// <summary>
        /// 생존자 목록에서 대상의 위치를 찾는다.
        /// </summary>
        private static int IndexOf(SpectatorViewPoint target)
        {
            for (int i = 0; i < ClientPlayerList.Players.Count; i++)
            {
                if (ClientPlayerList.Players[i] == target.Identity)
                    return i;
            }
            return -1;
        }
    }
}
