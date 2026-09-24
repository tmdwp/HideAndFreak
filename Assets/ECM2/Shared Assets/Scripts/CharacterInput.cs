using UnityEngine;
using UnityEngine.InputSystem;

namespace ECM2.Examples
{
    /// <summary>
    /// Character Input.
    /// Shows how control a Character using the Input System.
    /// </summary>
    
    public class CharacterInput : MonoBehaviour
    {
        [Space(15f)]
        [Tooltip("Collection of input action maps and control schemes available for user controls.")]
        [SerializeField]
        private InputActionAsset _inputActionsAsset;
        
        [Tooltip("The character to be controlled. If left unassigned, this will attempt to locate a Character component within this GameObject.")]
        [SerializeField]
        private Character _character;

        /// <summary>
        /// Our controlled character.
        /// </summary>

        public Character character => _character;

        /// <summary>
        /// InputActions assets.
        /// </summary>

        public InputActionAsset inputActionsAsset
        {
            get => _inputActionsAsset;
            set
            {
                DeinitPlayerInput();
                
                _inputActionsAsset = value;
                InitPlayerInput();
            } 
        }
        
        /// <summary>
        /// Movement InputAction.
        /// </summary>

        public InputAction movementInputAction { get; set; }
        
        /// <summary>
        /// Jump InputAction.
        /// </summary>

        public InputAction jumpInputAction { get; set; }

        /// <summary>
        /// Crouch InputAction.
        /// </summary>

        public InputAction crouchInputAction { get; set; }

        /// <summary>
        /// Polls movement InputAction (if any).
        /// Return its current value or zero if no valid InputAction found.
        /// </summary>
        
        public virtual Vector2 GetMovementInput()
        {
            return movementInputAction?.ReadValue<Vector2>() ?? Vector2.zero;
        }
        
        /// <summary>
        /// Jump InputAction handler.
        /// </summary>
        
        public virtual void OnJump(InputAction.CallbackContext context)
        {
            if (context.started)
                _character.Jump();
            else if (context.canceled)
                _character.StopJumping();
        }
        
        /// <summary>
        /// Crouch InputAction handler.
        /// </summary>
        
        public virtual void OnCrouch(InputAction.CallbackContext context)
        {
            if (context.started)
                _character.Crouch();
            else if (context.canceled)
                _character.UnCrouch();
        }

        /// <summary>
        /// Initialize player InputActions (if any).
        /// E.g. Subscribe to input action events and enable input actions here.
        /// </summary>

        protected virtual void InitPlayerInput()
        {
            // Attempts to cache Character InputActions (if any)

            if (inputActionsAsset == null)
                return;
            
            // Movement input action (no handler, this is polled, e.g. GetMovementInput())

            movementInputAction = inputActionsAsset.FindAction("Move");
            movementInputAction?.Enable();
            
            // Setup Jump input action handlers

            jumpInputAction = inputActionsAsset.FindAction("Jump");
            if (jumpInputAction != null)
            {
                jumpInputAction.started += OnJump;
                jumpInputAction.canceled += OnJump;

                jumpInputAction.Enable();
            }
            
            // Setup Crouch input action handlers

            crouchInputAction = inputActionsAsset.FindAction("Crouch");
            if (crouchInputAction != null)
            {
                crouchInputAction.started += OnCrouch;
                crouchInputAction.canceled += OnCrouch;

                crouchInputAction.Enable();
            }
        }

        /// <summary>
        /// Unsubscribe from input action events and disable input actions.
        /// </summary>

        protected virtual void DeinitPlayerInput()
        {
            // Unsubscribe from input action events and disable input actions

            if (movementInputAction != null)
            {
                movementInputAction.Disable();
                movementInputAction = null;
            }
            
            if (jumpInputAction != null)
            {
                jumpInputAction.started -= OnJump;
                jumpInputAction.canceled -= OnJump;

                jumpInputAction.Disable();
                jumpInputAction = null;
            }

            if (crouchInputAction != null)
            {
                crouchInputAction.started -= OnCrouch;
                crouchInputAction.canceled -= OnCrouch;

                crouchInputAction.Disable();
                crouchInputAction = null;
            }
        }
        
        protected virtual void HandleInput()
        {
            // Should this character handle input ?

            if (inputActionsAsset == null)
                return;
            
            // Poll movement InputAction

            Vector2 movementInput = GetMovementInput();
            
            Vector3 movementDirection =  Vector3.zero;

            movementDirection += Vector3.right * movementInput.x;
            movementDirection += Vector3.forward * movementInput.y;
            
            // If character has a camera assigned...
            
            if (_character.camera)
            {
                // Make movement direction relative to its camera view direction
                
                movementDirection = movementDirection.relativeTo(_character.cameraTransform, _character.GetUpVector());
            }
            
            // Set character's movement direction vector

            _character.SetMovementDirection(movementDirection);
        }

        protected virtual void Awake()
        {
            // If no character assigned, try to get Character from this GameObject
            
            if (_character == null)
            {
                _character = GetComponent<Character>();
            }
        }

        protected virtual void OnEnable()
        {
            InitPlayerInput();
        }

        protected virtual void OnDisable()
        {
            DeinitPlayerInput();
        }

        protected virtual void Update()
        {
            HandleInput();
        }
    }
}
