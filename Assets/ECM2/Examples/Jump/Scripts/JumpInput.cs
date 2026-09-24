using UnityEngine.InputSystem;

namespace ECM2.Examples.Jump
{
    /// <summary>
    /// Extends default Character Input to handle JumpAbility Input.
    /// </summary>
    
    public class JumpInput : CharacterInput
    {
        // The jump ability
        
        private JumpAbility _jumpAbility;
        
        /// <summary>
        /// Extend OnJump handler to manage JumpAbility input.
        /// </summary>

        public override void OnJump(InputAction.CallbackContext context)
        {
            if (context.started)
                _jumpAbility.Jump();
            else if (context.canceled)
                _jumpAbility.StopJumping();
        }

        protected override void Awake()
        {
            base.Awake();
            
            // Cache JumpAbility
            
            _jumpAbility = GetComponent<JumpAbility>();
        }
    }
}