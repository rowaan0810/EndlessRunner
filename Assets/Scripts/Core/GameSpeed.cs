// GameSpeed.cs — Global speed control for the endless runner.
// All moving world objects (track, obstacles, coins) read from GameSpeed.Current.

using UnityEngine;

namespace EndlessRunner.Core
{
    /// <summary>
    /// Controls the global game speed. Speed increases over time during gameplay.
    /// Track segments, obstacles, and collectibles all move at this speed.
    /// </summary>
    public class GameSpeed : MonoBehaviour
    {
        public static GameSpeed Instance { get; private set; }

        [Header("Speed Settings")]
        [SerializeField] private float baseSpeed = 10f;
        [SerializeField] private float acceleration = 0.3f;
        [SerializeField] private float maxSpeed = 30f;

        /// <summary>
        /// The current world scroll speed. Increases over time during play.
        /// </summary>
        public float Current { get; private set; }

        /// <summary>
        /// Base speed before acceleration. Can be modified for Easy Mode.
        /// </summary>
        public float BaseSpeed
        {
            get => baseSpeed;
            set => baseSpeed = value;
        }

        /// <summary>
        /// How fast the speed ramps up (units per second squared).
        /// </summary>
        public float Acceleration
        {
            get => acceleration;
            set => acceleration = value;
        }

        /// <summary>
        /// Maximum speed cap.
        /// </summary>
        public float MaxSpeed
        {
            get => maxSpeed;
            set => maxSpeed = value;
        }

        /// <summary>
        /// Normalized speed (0 to 1) relative to max. Useful for UI and effects.
        /// </summary>
        public float NormalizedSpeed => Mathf.InverseLerp(baseSpeed, maxSpeed, Current);

        private float elapsedTime;
        private bool isRunning;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (!isRunning) return;

            // In webcam mode, use very slow acceleration (0.1/s vs 0.3/s in keyboard)
            var im = EndlessRunner.Input.InputManager.Instance;
            if (im != null && im.CurrentMode == Input.InputMode.Webcam)
            {
                elapsedTime += Time.deltaTime;
                Current = Mathf.Min(baseSpeed + (elapsedTime * 0.1f), maxSpeed);
                return;
            }

            elapsedTime += Time.deltaTime;
            Current = Mathf.Min(baseSpeed + (elapsedTime * acceleration), maxSpeed);
        }

        /// <summary>
        /// Start speed progression. Called when gameplay begins.
        /// </summary>
        public void StartRunning()
        {
            isRunning = true;
            elapsedTime = 0f;
            Current = baseSpeed;
        }

        /// <summary>
        /// Stop speed progression. Called on game over.
        /// </summary>
        public void StopRunning()
        {
            isRunning = false;
        }

        /// <summary>
        /// Reset to initial state. Called on game restart.
        /// </summary>
        public void ResetSpeed()
        {
            isRunning = false;
            elapsedTime = 0f;
            Current = baseSpeed;
        }
    }
}
