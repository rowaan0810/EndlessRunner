// PlayerAnimator.cs — Drives animation states based on player movement.
// For Week 1 this uses placeholder visual changes; later it will drive a proper Animator.

using UnityEngine;

namespace EndlessRunner.Player
{
    /// <summary>
    /// Manages player animations by feeding PlayerController state to the Animator.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerController playerController;
        
        private Animator animator;

        // Animator hash parameters for performance
        private readonly int isGroundedHash = Animator.StringToHash("isGrounded");
        private readonly int isDuckingHash = Animator.StringToHash("isDucking");

        private void Awake()
        {
            animator = GetComponent<Animator>();
            if (playerController == null)
                playerController = GetComponentInParent<PlayerController>();
        }

        private void Update()
        {
            if (playerController == null || animator == null) return;

            // Feed the controller state to the animation state machine
            animator.SetBool(isGroundedHash, playerController.IsGrounded);
            animator.SetBool(isDuckingHash, playerController.IsDucking);
        }
    }
}
