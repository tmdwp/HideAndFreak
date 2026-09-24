using UnityEngine;
using UnityEngine.InputSystem;

namespace ECM2.Examples.TwinStickMovement
{
    /// <summary>
    /// This example shows how to implement a basic twin-stick movement.
    /// This implements a typical Mouse and Keyboard twin-stick shooter control.
    /// </summary>
    
    public class TwinStickInput : CharacterInput
    {
        private TwinStickCharacter _twinStickCharacter;

        protected override void Awake()
        {
            base.Awake();
            
            _twinStickCharacter = character as TwinStickCharacter;
        }

        protected override void HandleInput()
        {
            // Call base method implementation
            
            base.HandleInput();
            
            // Calc aim direction
            
            Vector3 aimDirection = Vector3.zero;

            if (Mouse.current.leftButton.isPressed)
            {
                // Convert mouse screen position to world position

                Ray ray = character.camera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hitResult, Mathf.Infinity))
                {
                    // Compute aim direction vector (character direction -> mouse world position)

                    Vector3 toHitPoint2D = (hitResult.point - character.GetPosition()).onlyXZ();
                    aimDirection = toHitPoint2D.normalized;
                }
            }
            
            // Set Character's aim direction

            _twinStickCharacter.SetAimDirection(aimDirection);
        }
    }
}
