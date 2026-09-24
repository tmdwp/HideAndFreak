using UnityEngine;

namespace ECM2.Examples.TwinStickMovement
{
    /// <summary>
    /// This example shows how to extend a Character (through inheritance), adding a custom rotation mode;
    /// in this case, implements a typical Mouse and Keyboard twin-stick shooter control.
    /// </summary>
    
    public class TwinStickCharacter : Character
    {
        private Vector3 _aimDirection;
        
        /// <summary>
        /// Returns the current aim direction vector.
        /// </summary>

        public virtual Vector3 GetAimDirection()
        {
            return _aimDirection;
        }
        
        /// <summary>
        /// Sets the desired aim direction vector (in world space).
        /// </summary>

        public virtual void SetAimDirection(Vector3 worldDirection)
        {
            _aimDirection = worldDirection;
        }
        
        /// <summary>
        /// Use a custom rotation mode to rotate towards aim direction (if shooting)
        /// or towards movement direction if not.
        /// </summary>
        /// <param name="deltaTime">The simulation delta time</param>

        protected override void CustomRotationMode(float deltaTime)
        {
            // Call base method implementation
            
            base.CustomRotationMode(deltaTime);
            
            // Update character rotation

            Vector3 targetDirection = 
                _aimDirection.isZero() ? GetMovementDirection() : GetAimDirection();

            RotateTowards(targetDirection, deltaTime);
        }
    }
}
