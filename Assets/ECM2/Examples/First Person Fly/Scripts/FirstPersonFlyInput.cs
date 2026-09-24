using ECM2.Examples.FirstPerson;
using UnityEngine;

namespace ECM2.Examples.FirstPersonFly
{
    /// <summary>
    /// Regular First Person Character Input. Shows how to handle movement while flying.
    /// In this case, we allow to fly towards our view direction, allowing to freely move through the air. 
    /// </summary>
    
    public class FirstPersonFlyInput : FirstPersonInput
    {
        protected override void HandleInput()
        {
            // Call base method implementation 
            
            base.HandleInput();

            if (character.IsFlying())
            {
                // Movement when Flying
                
                Vector2 movementInput = GetMovementInput();
                Vector3 movementDirection = Vector3.zero;
                
                // Strafe
                
                movementDirection += character.GetRightVector() * movementInput.x;
                
                // Forward, along camera view direction (if any) or along character's forward if camera not found 

                Vector3 forward = character.camera 
                    ? character.cameraTransform.forward 
                    : character.GetForwardVector();
                
                movementDirection += forward * movementInput.y;
                
                // Vertical movement
                
                if (character.jumpInputPressed)
                    movementDirection += Vector3.up;
                
                character.SetMovementDirection(movementDirection);
            }
        }
    }
}
