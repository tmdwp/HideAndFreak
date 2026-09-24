using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GuardVsMannequin.Player
{
    /// <summary>
    /// Input System 액션을 <see cref="IPlayerInput"/>으로 변환한다.
    /// 로컬 소유자 아바타에서만 활성화된다.
    /// </summary>
    public class PlayerInputReader : MonoBehaviour, IPlayerInput
    {
        private const string PlayerMapName = "Player";
        private const int EmoteCount = 6;

        [SerializeField] private InputActionAsset inputActions;

        private InputActionAsset runtimeActions;
        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction primaryAction;
        private InputAction secondaryAction;
        private InputAction flashlightAction;
        private InputAction rotateAction;
        private InputAction spectatePreviousAction;
        private InputAction spectateNextAction;
        private InputAction toggleCursorAction;
        private readonly InputAction[] emoteActions = new InputAction[EmoteCount];

        public Vector2 Move => moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
        public Vector2 Look => IsCursorLocked ? lookAction?.ReadValue<Vector2>() ?? Vector2.zero : Vector2.zero;
        public float Rotate => IsCursorLocked ? rotateAction?.ReadValue<float>() ?? 0f : 0f;
        public bool IsPrimaryHeld => IsCursorLocked && primaryAction != null && primaryAction.IsPressed();

        public event Action PrimaryPressed;
        public event Action PrimaryReleased;
        public event Action SecondaryPressed;
        public event Action<int> EmotePressed;
        public event Action FlashlightPressed;
        public event Action<int> SpectateCycled;

        /// <summary>커서가 잠겨 있을 때만 게임 입력을 처리한다. (ESC로 잠금 해제 시 HUD 조작 가능)</summary>
        private static bool IsCursorLocked => Cursor.lockState == CursorLockMode.Locked;

        /// <summary>
        /// 에셋을 복제해 액션을 찾는다. 복제본을 쓰면 다른 아바타와 상태가 섞이지 않는다.
        /// </summary>
        private void Awake()
        {
            runtimeActions = Instantiate(inputActions);
            InputActionMap map = runtimeActions.FindActionMap(PlayerMapName, true);

            moveAction = map.FindAction("Move", true);
            lookAction = map.FindAction("Look", true);
            primaryAction = map.FindAction("Primary", true);
            secondaryAction = map.FindAction("Secondary", true);
            flashlightAction = map.FindAction("Flashlight", true);
            rotateAction = map.FindAction("Rotate", true);
            spectatePreviousAction = map.FindAction("SpectatePrevious", true);
            spectateNextAction = map.FindAction("SpectateNext", true);
            toggleCursorAction = map.FindAction("ToggleCursor", true);
            for (int i = 0; i < EmoteCount; i++)
                emoteActions[i] = map.FindAction($"Emote{i + 1}", true);
        }

        /// <summary>
        /// 액션을 활성화하고 버튼 이벤트를 구독한다.
        /// </summary>
        private void OnEnable()
        {
            primaryAction.performed += OnPrimaryPerformed;
            primaryAction.canceled += OnPrimaryCanceled;
            secondaryAction.performed += OnSecondaryPerformed;
            flashlightAction.performed += OnFlashlightPerformed;
            spectatePreviousAction.performed += OnSpectatePreviousPerformed;
            spectateNextAction.performed += OnSpectateNextPerformed;
            toggleCursorAction.performed += OnToggleCursorPerformed;
            foreach (InputAction emote in emoteActions)
                emote.performed += OnEmotePerformed;

            runtimeActions.Enable();
            SetCursorLocked(true);
        }

        /// <summary>
        /// 액션을 비활성화하고 이벤트 구독을 해제한다.
        /// </summary>
        private void OnDisable()
        {
            runtimeActions.Disable();

            primaryAction.performed -= OnPrimaryPerformed;
            primaryAction.canceled -= OnPrimaryCanceled;
            secondaryAction.performed -= OnSecondaryPerformed;
            flashlightAction.performed -= OnFlashlightPerformed;
            spectatePreviousAction.performed -= OnSpectatePreviousPerformed;
            spectateNextAction.performed -= OnSpectateNextPerformed;
            toggleCursorAction.performed -= OnToggleCursorPerformed;
            foreach (InputAction emote in emoteActions)
                emote.performed -= OnEmotePerformed;

            SetCursorLocked(false);
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
        /// 커서 잠금 상태를 설정한다.
        /// </summary>
        private static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        /// <summary>좌클릭 누름을 전달한다.</summary>
        private void OnPrimaryPerformed(InputAction.CallbackContext context)
        {
            if (IsCursorLocked)
                PrimaryPressed?.Invoke();
        }

        /// <summary>좌클릭 뗌을 전달한다. 잠금 상태와 관계없이 전달해 상호작용이 끊기지 않게 한다.</summary>
        private void OnPrimaryCanceled(InputAction.CallbackContext context)
        {
            PrimaryReleased?.Invoke();
        }

        /// <summary>
        /// 우클릭을 전달한다.
        /// </summary>
        private void OnSecondaryPerformed(InputAction.CallbackContext context)
        {
            if (IsCursorLocked)
                SecondaryPressed?.Invoke();
        }

        /// <summary>손전등 키를 전달한다.</summary>
        private void OnFlashlightPerformed(InputAction.CallbackContext context)
        {
            if (IsCursorLocked)
                FlashlightPressed?.Invoke();
        }

        /// <summary>이전 관전 대상 키를 전달한다.</summary>
        private void OnSpectatePreviousPerformed(InputAction.CallbackContext context)
        {
            SpectateCycled?.Invoke(-1);
        }

        /// <summary>다음 관전 대상 키를 전달한다.</summary>
        private void OnSpectateNextPerformed(InputAction.CallbackContext context)
        {
            SpectateCycled?.Invoke(1);
        }

        /// <summary>
        /// 눌린 감정 표현 액션의 번호(1~6)를 찾아 전달한다.
        /// </summary>
        private void OnEmotePerformed(InputAction.CallbackContext context)
        {
            if (!IsCursorLocked)
                return;

            int index = Array.IndexOf(emoteActions, context.action);
            if (index >= 0)
                EmotePressed?.Invoke(index + 1);
        }

        /// <summary>
        /// ESC로 커서 잠금을 전환한다. (Phase 6에서 설정 메뉴 열기로 대체)
        /// </summary>
        private void OnToggleCursorPerformed(InputAction.CallbackContext context)
        {
            SetCursorLocked(!IsCursorLocked);
        }
    }
}
