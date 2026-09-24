using UnityEngine;

namespace ECM2.Examples.SideScrolling
{
    /// <summary>
    /// This example shows how to implement a typical side-scrolling movement with side to side rotation snap.
    /// </summary>
    
    public class SideScrollingInput : CharacterInput
    {
        protected override void Awake()
        {
            // Call base method implementation
            
            base.Awake();
            
            // Disable Character rotation, well handle it here (snap move direction)
            
            character.SetRotationMode(Character.RotationMode.None);
        }

        protected override void HandleInput()
        {
            // Add horizontal movement (in world space)
            
            Vector2 movementInput = GetMovementInput();
            character.SetMovementDirection(Vector3.right * movementInput.x);
            
            // Snap side to side rotation
            
            if (movementInput.x != 0)
                character.SetYaw(movementInput.x * 90.0f);
        }
    }
}
