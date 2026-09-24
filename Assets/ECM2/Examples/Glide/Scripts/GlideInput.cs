using UnityEngine.InputSystem;

namespace ECM2.Examples.Glide
{
    /// <summary>
    /// Extends default Character Input to handle GlideAbility Input.
    /// </summary>
    
    public class GlideInput : CharacterInput
    {
        private GlideAbility _glideAbility;

        /// <summary>
        /// Extend OnJump handler to add GlideAbility input support.
        /// </summary>
        
        public override void OnJump(InputAction.CallbackContext context)
        {
            // Call base method implementation (handle jump)
            
            base.OnJump(context);

            if (context.started)
                _glideAbility.Glide();
            else if (context.canceled)
                _glideAbility.StopGliding();
        }

        protected override void Awake()
        {
            base.Awake();
            
            // Cache Glide Ability
            
            _glideAbility = GetComponent<GlideAbility>();
        }
    }
}