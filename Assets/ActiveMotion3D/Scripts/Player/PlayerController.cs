// PlayerController.cs — Core player movement controller.
// Handles 3-lane switching, jumping, and ducking.
// Input is read exclusively from IGameInput (fully decoupled from input source).

using UnityEngine;
using ActiveMotion3D.EndlessRunner.Input;
using ActiveMotion3D.EndlessRunner.Core;

namespace ActiveMotion3D.EndlessRunner.Player
{
    /// <summary>
    /// Controls the player character's movement: lane switching, jumping, and ducking.
    /// Reads from IGameInput so it works identically with keyboard, pose, or easy mode input.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [Header("Lane Settings")]
        [SerializeField] private float laneWidth = 2.5f;         // Distance between lanes
        [SerializeField] private float laneSwitchSpeed = 24f;
        [SerializeField] private float laneSwitchSmoothTime = 0.045f;

        [Header("Jump Settings")]
        [SerializeField] private float jumpForce = 6.5f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float groundY = 0f;              // Was 0.5f. Set to 0 so feet touch the ground (fixes collision skip)

        [Header("Duck Settings")]
        [SerializeField] private float duckDuration = 0.9f;
        [SerializeField] private float duckColliderHeight = 0.5f; // Collider height while ducking
        [SerializeField] private float duckColliderCenterY = 0.25f; // Collider center while ducking

        [Header("References")]
        [SerializeField] private CapsuleCollider playerCollider;

        // State
        private int currentLane = 1;         // 0=left, 1=center, 2=right
        private float targetX;               // Target X position for current lane
        private float horizontalVelocity;
        private float verticalVelocity;      // Current vertical velocity (jump/fall)
        private bool isGrounded = true;
        private bool isDucking;
        private float duckTimer;

        // Collider defaults (saved on start, restored after duck)
        private float defaultColliderHeight;
        private float defaultColliderCenterY;

        /// <summary>Current lane index (0=left, 1=center, 2=right).</summary>
        public int CurrentLane => currentLane;

        /// <summary>Whether the player is currently on the ground.</summary>
        public bool IsGrounded => isGrounded;

        /// <summary>Whether the player is currently ducking/rolling.</summary>
        public bool IsDucking => isDucking;

        /// <summary>Whether the player is mid-jump.</summary>
        public bool IsJumping => !isGrounded && verticalVelocity > 0;

        /// <summary>Whether the player is falling after a jump.</summary>
        public bool IsFalling => !isGrounded && verticalVelocity <= 0;

        private void Start()
        {
            // Add toe-clipping forgiveness: Shrink the bottom of the collider slightly
            if (playerCollider != null)
            {
                float forgiveness = 0.4f; // Raise the bottom edge by 0.4 units
                playerCollider.height -= forgiveness;
                playerCollider.center = new Vector3(playerCollider.center.x, playerCollider.center.y + (forgiveness * 0.5f), playerCollider.center.z);

                // Save default collider dimensions for restoring after a duck
                defaultColliderHeight = playerCollider.height;
                defaultColliderCenterY = playerCollider.center.y;
            }

            // Initialize position
            currentLane = 1;
            targetX = 0f;
            transform.position = new Vector3(0f, groundY, 0f);
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
                return;

            IGameInput input = InputManager.Instance?.CurrentInput;
            if (input == null) return;

            HandleHorizontalMovement(input);
            HandleJump(input);
            HandleDuck(input);
            ApplyMovement();
        }

        private void HandleHorizontalMovement(IGameInput input)
        {
            if (input.UsesContinuousHorizontal)
            {
                targetX = input.DesiredHorizontalPosition;

                // Preserve an approximate lane for scoring/debug systems that still
                // display it, while movement itself remains fully continuous.
                if (targetX < -laneWidth * 0.5f) currentLane = 0;
                else if (targetX > laneWidth * 0.5f) currentLane = 2;
                else currentLane = 1;
                return;
            }

            // Read desired lane from input and update target
            if (input.DesiredLane != currentLane)
            {
                currentLane = input.DesiredLane;
                targetX = (currentLane - 1) * laneWidth; // Lane 0=-2.5, 1=0, 2=2.5
            }
        }

        private void HandleJump(IGameInput input)
        {
            if (input.JumpRequested)
            {
                if (isGrounded && !isDucking)
                {
                    verticalVelocity = jumpForce;
                    isGrounded = false;
                }
                
                // ALWAYS consume the jump, regardless of whether we actually jumped or not.
                // This prevents buffered jumps from triggering instantly when the player lands or stops ducking.
                input.ConsumeJump();
            }
        }

        private void HandleDuck(IGameInput input)
        {
            if (input.DuckRequested)
            {
                if (isGrounded && !isDucking)
                {
                    StartDuck();
                }
                else if (!isGrounded)
                {
                    // Duck while in air = fast fall (slam down)
                    verticalVelocity = gravity * 0.5f;
                }
                input.ConsumeDuck();
            }

            // Duck timer
            if (isDucking)
            {
                duckTimer -= Time.deltaTime;
                if (duckTimer <= 0f)
                {
                    EndDuck();
                }
            }
        }

        private void StartDuck()
        {
            isDucking = true;
            duckTimer = duckDuration;

            // Shrink collider
            if (playerCollider != null)
            {
                playerCollider.height = duckColliderHeight;
                playerCollider.center = new Vector3(0f, duckColliderCenterY, 0f);
            }

            // The roll animation handles the visual. Scaling the whole character made
            // transitions look jerky and also distorted child models.
        }

        private void EndDuck()
        {
            isDucking = false;
            duckTimer = 0f;

            // Restore collider
            if (playerCollider != null)
            {
                playerCollider.height = defaultColliderHeight;
                playerCollider.center = new Vector3(0f, defaultColliderCenterY, 0f);
            }

        }

        private void ApplyMovement()
        {
            Vector3 pos = transform.position;

            // Horizontal: smooth slide to target lane
            pos.x = Mathf.SmoothDamp(pos.x, targetX, ref horizontalVelocity,
                laneSwitchSmoothTime, laneSwitchSpeed, Time.deltaTime);

            // Vertical: gravity and grounding
            if (!isGrounded)
            {
                verticalVelocity += gravity * Time.deltaTime;
                pos.y += verticalVelocity * Time.deltaTime;

                // Check for landing
                if (pos.y <= groundY)
                {
                    pos.y = groundY;
                    verticalVelocity = 0f;
                    isGrounded = true;
                }
            }

            transform.position = pos;
        }

        /// <summary>
        /// Reset player to initial state. Called on game restart.
        /// </summary>
        public void ResetPlayer(bool recenterPose = true)
        {
            if (recenterPose && InputManager.Instance?.CurrentInput is PoseInput poseInput)
                poseInput.RecenterHorizontal();

            currentLane = 1;
            targetX = 0f;
            verticalVelocity = 0f;
            horizontalVelocity = 0f;
            isGrounded = true;

            if (isDucking) EndDuck();

            transform.position = new Vector3(0f, groundY, 0f);
            transform.localScale = Vector3.one;

            // Rebind every authored character Animator so a restart cannot retain
            // the previous run's jump, fall, or duck animation state.
            var playerAnimators = GetComponentsInChildren<PlayerAnimator>(true);
            foreach (var playerAnimator in playerAnimators)
                playerAnimator.ResetAnimationState();

            // Also reset keyboard input lane
            var keyboardInput = InputManager.Instance?.GetComponent<KeyboardInput>();
            if (keyboardInput != null) keyboardInput.ResetLane();
        }
    }
}
