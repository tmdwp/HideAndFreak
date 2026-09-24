using ECM2.Examples;
using UnityEngine.InputSystem;

namespace ECM2.Walkthrough.Ex43
{
    /// <summary>
    /// Extends CharacterInput adding support to handle Dash mechanic.
    /// </summary>
    
    public class DashingCharacterInput : CharacterInput
    {
        private DashingCharacter _dashingCharacter;
        
        /// <summary>
        /// Dash InputAction.
        /// </summary>

        public InputAction dashInputAction { get; set; }
        
        /// <summary>
        /// Sprint InputAction handler.
        /// </summary>
        
        public virtual void OnDash(InputAction.CallbackContext context)
        {
            if (context.started)
                _dashingCharacter.Dash();
            else if (context.canceled)
                _dashingCharacter.StopDashing();
        }

        protected override void InitPlayerInput()
        {
            base.InitPlayerInput();
            
            // Setup Sprint input action handlers

            dashInputAction = inputActionsAsset.FindAction("Interact");
            if (dashInputAction != null)
            {
                dashInputAction.started += OnDash;
                dashInputAction.canceled += OnDash;

                dashInputAction.Enable();
            }
        }

        protected override void DeinitPlayerInput()
        {
            base.DeinitPlayerInput();
            
            if (dashInputAction != null)
            {
                dashInputAction.started -= OnDash;
                dashInputAction.canceled -= OnDash;

                dashInputAction.Disable();
                dashInputAction = null;
            }
        }

        protected override void Awake()
        {
            // Call base method implementation (a MUST)
            
            base.Awake();
            
            // Cache DashingCharacter
            
            _dashingCharacter = character as DashingCharacter;
        }
    }
}
