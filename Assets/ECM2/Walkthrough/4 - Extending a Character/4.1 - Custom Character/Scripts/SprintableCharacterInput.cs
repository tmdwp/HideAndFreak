using ECM2.Examples;
using UnityEngine.InputSystem;

namespace ECM2.Walkthrough.Ex41
{
    /// <summary>
    /// Extends CharacterInput adding support for Sprint InputAction.
    /// </summary>
    
    public class SprintableCharacterInput : CharacterInput
    {
        private SprintableCharacter _sprintableCharacter;
        
        /// <summary>
        /// Sprint InputAction.
        /// </summary>

        public InputAction sprintInputAction { get; set; }
        
        /// <summary>
        /// Sprint InputAction handler.
        /// </summary>
        
        public virtual void OnSprint(InputAction.CallbackContext context)
        {
            if (context.started)
                _sprintableCharacter.Sprint();
            else if (context.canceled)
                _sprintableCharacter.StopSprinting();
        }

        protected override void InitPlayerInput()
        {
            base.InitPlayerInput();
            
            // Setup Sprint input action handlers

            sprintInputAction = inputActionsAsset.FindAction("Sprint");
            if (sprintInputAction != null)
            {
                sprintInputAction.started += OnSprint;
                sprintInputAction.canceled += OnSprint;

                sprintInputAction.Enable();
            }
        }

        protected override void DeinitPlayerInput()
        {
            base.DeinitPlayerInput();
            
            if (sprintInputAction != null)
            {
                sprintInputAction.started -= OnSprint;
                sprintInputAction.canceled -= OnSprint;

                sprintInputAction.Disable();
                sprintInputAction = null;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            
            _sprintableCharacter = character as SprintableCharacter;
        }
    }
}
