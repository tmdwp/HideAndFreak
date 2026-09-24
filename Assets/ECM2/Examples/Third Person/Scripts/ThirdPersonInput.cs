using UnityEngine;
using UnityEngine.InputSystem;

namespace ECM2.Examples.ThirdPerson
{
    /// <summary>
    /// Third person character input.
    /// Extends the default CharacterInput component adding support for typical third person controls.
    /// </summary>
    
    public class ThirdPersonInput : CharacterInput
    {
        [Space(15.0f)]
        public bool invertLook = true;

        [Tooltip("Look Sensitivity")]
        public Vector2 lookSensitivity = new Vector2(0.05f, 0.05f);

        [Tooltip("Zoom Sensitivity")]
        public float zoomSensitivity = 1.0f;

        [Space(15.0f)]
        [Tooltip("How far in degrees can you move the camera down.")]
        public float minPitch = -80.0f;

        [Tooltip("How far in degrees can you move the camera up.")]
        public float maxPitch = 80.0f;
        
        /// <summary>
        /// Cached ThirdPersonCharacter.
        /// </summary>

        public ThirdPersonCharacter thirdPersonCharacter { get; private set; }

        /// <summary>
        /// Movement InputAction.
        /// </summary>

        public InputAction lookInputAction { get; set; }
        
        /// <summary>
        /// Zoom InputAction.
        /// </summary>

        public InputAction zoomInputAction { get; set; }

        /// <summary>
        /// Polls look InputAction (if any).
        /// Return its current value or zero if no valid InputAction found.
        /// </summary>
        
        public Vector2 GetLookInput()
        {
            return lookInputAction?.ReadValue<Vector2>() ?? Vector2.zero;
        }
        
        /// <summary>
        /// Polls zoom InputAction (if any).
        /// Return its current value or zero if no valid InputAction found.
        /// </summary>

        public Vector2 GetZoomInput()
        {
            return zoomInputAction?.ReadValue<Vector2>() ?? Vector2.zero;
        }
        
        /// <summary>
        /// Initialize player InputActions (if any).
        /// E.g. Subscribe to input action events and enable input actions here.
        /// </summary>

        protected override void InitPlayerInput()
        {
            base.InitPlayerInput();
            
            // Look input action (no handler, this is polled, e.g. GetLookInput())

            lookInputAction = inputActionsAsset.FindAction("Look");
            lookInputAction?.Enable();
            
            // Zoom input action (no handler, this is polled, e.g. GetLookInput())
            
            zoomInputAction = inputActionsAsset.FindAction("Zoom");
            zoomInputAction?.Enable();
        }
        
        /// <summary>
        /// Unsubscribe from input action events and disable input actions.
        /// </summary>

        protected override void DeinitPlayerInput()
        {
            base.DeinitPlayerInput();
            
            // Unsubscribe from input action events and disable input actions

            if (lookInputAction != null)
            {
                lookInputAction.Disable();
                lookInputAction = null;
            }

            if (zoomInputAction != null)
            {
                zoomInputAction.Disable();
                zoomInputAction = null;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            
            thirdPersonCharacter = character as ThirdPersonCharacter;
        }

        protected virtual void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
        }

        protected override void HandleInput()
        {
            // Move
            
            Vector2 movementInput = GetMovementInput();
            
            Vector3 movementDirection = Vector3.zero;
            movementDirection += Vector3.forward * movementInput.y;
            movementDirection += Vector3.right * movementInput.x;

            movementDirection = movementDirection.relativeTo(thirdPersonCharacter.cameraTransform, thirdPersonCharacter.GetUpVector());
            
            thirdPersonCharacter.SetMovementDirection(movementDirection);
            
            // Look
            
            Vector2 lookInput = GetLookInput() * lookSensitivity;

            thirdPersonCharacter.AddControlYawInput(lookInput.x);
            thirdPersonCharacter.AddControlPitchInput(invertLook ? -lookInput.y : lookInput.y, minPitch, maxPitch);
            
            // Zoom
            
            Vector2 zoomInput = GetZoomInput() * zoomSensitivity;
            thirdPersonCharacter.AddControlZoomInput(zoomInput.y);
        }
    }
}
