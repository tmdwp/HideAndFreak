using ECM2.Examples.ThirdPerson;
using UnityEngine;

namespace ECM2.Examples.PlanetWalk
{
    /// <summary>
    /// This example extends a Character (through inheritance) adjusting its gravity and orientation
    /// to follow a planet curvature similar to the Mario Galaxy game.
    /// </summary>
    
    public class PlayerCharacter : ThirdPersonCharacter
    {
        [Space(15f)]
        public Transform planetTransform;
        
        // Current camera forward, perpendicular to target's up vector.
        
        private Vector3 _cameraForward = Vector3.forward;
        
        public override void AddControlYawInput(float value)
        {
            // Rotate our forward along follow target's up axis
        
            Vector3 targetUp = followTarget.transform.up;
            _cameraForward = Quaternion.Euler(targetUp * value) * _cameraForward;
        }
        
        protected override void UpdateCameraRotation()
        {
            // Make sure camera forward vector is perpendicular to Character's current up vector
            
            Vector3 targetUp = followTarget.transform.up;
            Vector3.OrthoNormalize(ref targetUp, ref _cameraForward);
            
            // Computes final Camera rotation from yaw and pitch
            
            cameraTransform.rotation =
                Quaternion.LookRotation(_cameraForward, targetUp) * Quaternion.Euler(_cameraPitch, 0.0f, 0.0f);
        }
        
        protected override void UpdateRotation(float deltaTime)
        {
            // Call base method (i.e: rotate towards movement direction)

            base.UpdateRotation(deltaTime);
            
            // Adjust gravity direction (ie: a vector pointing from character position to planet's center)
            
            Vector3 toPlanet = planetTransform.position - GetPosition();
            SetGravityVector(toPlanet.normalized * GetGravityMagnitude());
            
            // Adjust Character's rotation following the new world-up (defined by gravity direction)
            
            Vector3 worldUp = GetGravityDirection() * -1.0f;
            Quaternion newRotation = Quaternion.FromToRotation(GetUpVector(), worldUp) * GetRotation();
            
            SetRotation(newRotation);
        }
    }
}
