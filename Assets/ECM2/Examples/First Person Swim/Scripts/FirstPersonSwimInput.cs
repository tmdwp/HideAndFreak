using ECM2.Examples.FirstPerson;
using UnityEngine;

namespace ECM2.Examples.FirstPersonSwim
{
    /// <summary>
    /// This example shows how to handle movement while swimming.
    /// Here, we allow to swim towards our view direction, allowing to freely move through the water.
    /// 
    /// Swimming is automatically enabled / disabled when Water Physics Volume is used,
    /// otherwise it must be enabled / disabled as needed.
    /// </summary>
    
    public class FirstPersonSwimInput : FirstPersonInput
    {
        protected override void HandleInput()
        {
            // Call base method implementation 
            
            base.HandleInput();

            if (character.IsSwimming())
            {
                // Handle movement when swimming
                
                // Strafe

                Vector2 movementInput = GetMovementInput();
                Vector3 movementDirection = Vector3.zero;
                movementDirection += character.GetRightVector() * movementInput.x;
                
                // Forward, along camera view direction (if any) or along character's forward if camera not found 
                
                Vector3 forward =
                    character.camera ? character.cameraTransform.forward : character.GetForwardVector();
                
                movementDirection += forward * movementInput.y;
                
                // Vertical movement
                
                if (character.jumpInputPressed)
                {
                    // Use immersion depth to check if we are at top of water line,
                    // if yes, jump of water
                
                    float depth = character.CalcImmersionDepth();
                    if (depth > 0.65f)
                        movementDirection += character.GetUpVector();
                    else
                    {
                        // Jump out of water
                
                        character.SetMovementMode(Character.MovementMode.Falling);
                        character.LaunchCharacter(character.GetUpVector() * 9.0f, true);
                    }
                }
                
                character.SetMovementDirection(movementDirection);
            }
        }
    }
}
