using ECM2.Examples;
using UnityEngine.InputSystem;

namespace ECM2.Walkthrough.Ex42
{
    /// <summary>
    /// Extends CharacterInput adding support to handle the Sprint Ability.
    /// </summary>
    
    public class SprintAbilityInput : CharacterInput
    {
        // The Sprint Ability
        
        private SprintAbility _sprintAbility;
        
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
                _sprintAbility.Sprint();
            else if (context.canceled)
                _sprintAbility.StopSprinting();
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
            // Call base method implementation (a MUST)
            
            base.Awake();
            
            // Cache character sprint ability component

            _sprintAbility = GetComponent<SprintAbility>();
        }
    }
}

