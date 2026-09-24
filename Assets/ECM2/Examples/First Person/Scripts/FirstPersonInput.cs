using UnityEngine;
using UnityEngine.InputSystem;

namespace ECM2.Examples.FirstPerson
{
    /// <summary>
    /// First person character input.
    /// Extends the default CharacterInput component adding support for typical first person controls.
    /// </summary>
    
    public class FirstPersonInput : CharacterInput
    {
        [Space(15.0f)]
        public bool invertLook = true;
        [Tooltip("Look sensitivity")]
        public Vector2 sensitivity = new Vector2(0.05f, 0.05f);
        
        [Space(15.0f)]
        [Tooltip("How far in degrees can you move the camera down.")]
        public float minPitch = -80.0f;
        [Tooltip("How far in degrees can you move the camera up.")]
        public float maxPitch = 80.0f;
        
        /// <summary>
        /// Cached FirstPersonCharacter.
        /// </summary>

        public FirstPersonCharacter firstPersonCharacter { get; private set; }

        /// <summary>
        /// Movement InputAction.
        /// </summary>

        public InputAction lookInputAction { get; set; }

        /// <summary>
        /// Polls look InputAction (if any).
        /// Return its current value or zero if no valid InputAction found.
        /// </summary>
        
        public Vector2 GetLookInput()
        {
            return lookInputAction?.ReadValue<Vector2>() ?? Vector2.zero;
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
        }

        protected override void Awake()
        {
            base.Awake();
            
            firstPersonCharacter = character as FirstPersonCharacter;
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
            
            movementDirection = 
                movementDirection.relativeTo(firstPersonCharacter.cameraTransform, firstPersonCharacter.GetUpVector());
            
            firstPersonCharacter.SetMovementDirection(movementDirection);
            
            // Look
            
            Vector2 lookInput = GetLookInput() * sensitivity;

            firstPersonCharacter.AddControlYawInput(lookInput.x);
            firstPersonCharacter.AddControlPitchInput(invertLook ? -lookInput.y : lookInput.y, minPitch, maxPitch);
        }
    }
}
