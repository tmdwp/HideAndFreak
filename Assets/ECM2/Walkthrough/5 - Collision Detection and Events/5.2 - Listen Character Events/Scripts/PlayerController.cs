using UnityEngine;

namespace ECM2.Walkthrough.Ex52
{
    /// <summary>
    /// This example shows how to listen to Character events when extending a Character through composition.
    /// </summary>
    
    public class PlayerController : MonoBehaviour
    {
        // Our controlled Character
        
        [SerializeField]
        private Character _character;
        
        protected void OnCollided(ref CollisionResult collisionResult)
        {
            Debug.Log($"Collided with {collisionResult.collider.name}");
        }

        protected void OnFoundGround(ref FindGroundResult foundGround)
        {
            Debug.Log($"Found {foundGround.collider.name} ground");
        }

        protected void OnLanded(Vector3 landingVelocity)
        {
            Debug.Log($"Landed with {landingVelocity:F4} landing velocity.");
        }

        protected void OnCrouched()
        {
            Debug.Log("Crouched");
        }

        protected void OnUnCrouched()
        {
            Debug.Log("UnCrouched");
        }

        protected void OnJumped()
        {
            Debug.Log("Jumped!");
            
            // Enable apex notification event
            
            _character.notifyJumpApex = true;
        }

        protected void OnReachedJumpApex()
        {
            Debug.Log($"Apex reached {_character.GetVelocity():F4}");
        }

        private void Awake()
        {
            // If Character is not assigned, look into this GameObject

            if (_character == null)
                _character = GetComponent<Character>();
        }

        private void OnEnable()
        {
            // Subscribe to Character events
            
            _character.Collided += OnCollided;
            _character.FoundGround += OnFoundGround;
            _character.Landed += OnLanded;
            _character.Crouched += OnCrouched;
            _character.UnCrouched += OnUnCrouched;
            _character.Jumped += OnJumped;
            _character.ReachedJumpApex += OnReachedJumpApex;
        }

        private void OnDisable()
        {
            // Un-subscribe from Character events
            
            _character.Collided -= OnCollided;
            _character.FoundGround -= OnFoundGround;
            _character.Landed -= OnLanded;
            _character.Crouched -= OnCrouched;
            _character.UnCrouched -= OnUnCrouched;
            _character.Jumped -= OnJumped;
            _character.ReachedJumpApex -= OnReachedJumpApex;
        }
    }
}