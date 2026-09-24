using UnityEngine.InputSystem;

namespace ECM2.Examples.Ladders
{
    /// <summary>
    /// Extends default Character Input to handle LadderClimbInput Input.
    /// </summary>
    
    public class LadderClimbInput : CharacterInput
    {
        private LadderClimbAbility _ladderClimbAbility;
        
        /// <summary>
        /// Interact InputAction.
        /// </summary>

        public InputAction interactInputAction { get; set; }
        
        /// <summary>
        /// Jump InputAction handler.
        /// </summary>
        
        public virtual void OnInteract(InputAction.CallbackContext context)
        {
            if (context.started)
                _ladderClimbAbility.Climb();
            else if (context.canceled)
                _ladderClimbAbility.StopClimbing();
        }

        protected override void InitPlayerInput()
        {
            // Call base method implementation
            
            base.InitPlayerInput();
            
            // Setup Interact input action handlers

            interactInputAction = inputActionsAsset.FindAction("Interact");
            if (interactInputAction != null)
            {
                interactInputAction.started += OnInteract;
                interactInputAction.canceled += OnInteract;

                interactInputAction.Enable();
            }
        }

        protected override void DeinitPlayerInput()
        {
            base.DeinitPlayerInput();
            
            // Unsubscribe from input action events and disable input actions
            
            if (interactInputAction != null)
            {
                interactInputAction.started -= OnInteract;
                interactInputAction.canceled -= OnInteract;

                interactInputAction.Disable();
                interactInputAction = null;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            
            // Cache Ladder Climb Ability
            
            _ladderClimbAbility = GetComponent<LadderClimbAbility>();
        }
    }
}