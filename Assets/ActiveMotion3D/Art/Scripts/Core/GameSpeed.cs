// GameSpeed.cs — Global speed control for the endless runner.
// All moving world objects (track, obstacles, coins) read from GameSpeed.Current.

using System;
using UnityEngine;

namespace ActiveMotion3D.EndlessRunner.Core
{
    /// <summary>
    /// Controls the global game speed using automatic progression or a fixed pace.
    /// Track segments, obstacles, and collectibles all move at this speed.
    /// </summary>
    public class GameSpeed : MonoBehaviour
    {
        public static GameSpeed Instance { get; private set; }

        private const string FixedSpeedPref = "MoriohFixedRunSpeed";
        internal const int FixedSpeedStep = 5;
        internal const int HighestFixedSpeed = 50;
        internal static event Action<int> FixedSpeedChanged;

        internal static int SelectedFixedSpeed
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(FixedSpeedPref), 0, HighestFixedSpeed);
            set
            {
                if (value == SelectedFixedSpeed) return;
                PlayerPrefs.SetInt(FixedSpeedPref, value);
                if (Instance != null && Instance.allowFixedRunSpeed)
                    Instance.ApplyFixedSpeed(value);
                FixedSpeedChanged?.Invoke(value);
            }
        }

        [Header("Speed Settings")]
        [SerializeField] private float baseSpeed = 10f;
        [SerializeField] private float acceleration = 0.3f;
        [SerializeField, Min(0f)] private float webcamAcceleration = 0.1f;
        [SerializeField] private float maxSpeed = 30f;
        [SerializeField] private bool allowFixedRunSpeed;

        /// <summary>
        /// The current world scroll speed in units per second.
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
        private int fixedSpeed;
        private bool isRunning;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (allowFixedRunSpeed) fixedSpeed = SelectedFixedSpeed;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (!isRunning || fixedSpeed > 0) return;

            elapsedTime += Time.deltaTime;
            Current = CalculateSpeed();
        }

        private float CalculateSpeed()
        {
            if (fixedSpeed > 0) return fixedSpeed;

            var im = EndlessRunner.Input.InputManager.Instance;
            float rate = im != null && im.CurrentMode == Input.InputMode.Webcam
                ? webcamAcceleration
                : acceleration;
            return Mathf.Min(baseSpeed + elapsedTime * rate, maxSpeed);
        }

        private void ApplyFixedSpeed(int speed)
        {
            fixedSpeed = speed;
            if (isRunning) Current = CalculateSpeed();
        }

        /// <summary>
        /// Start speed progression. Called when gameplay begins.
        /// </summary>
        public void StartRunning()
        {
            isRunning = true;
            elapsedTime = 0f;
            Current = CalculateSpeed();
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
            Current = CalculateSpeed();
        }
    }
}
