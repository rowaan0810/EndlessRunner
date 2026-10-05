// InputManager.cs — Manages switching between input modes.
// Holds references to all IGameInput implementations and exposes the active one.

using UnityEngine;
using EndlessRunner.Input.PoseDetection;

namespace EndlessRunner.Input
{
    public enum InputMode
    {
        Keyboard,
        Webcam
    }

    /// <summary>
    /// Central input manager. Holds all input implementations and allows
    /// runtime switching between them. PlayerController reads from CurrentInput.
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        [Header("Input Implementations")]
        [SerializeField] private KeyboardInput keyboardInput;
        [SerializeField] private PoseInput poseInput;

        [Header("Settings")]
        [SerializeField] private InputMode currentMode = InputMode.Keyboard;

        /// <summary>
        /// The currently active input source. All game systems read from this.
        /// </summary>
        public IGameInput CurrentInput { get; private set; }

        /// <summary>
        /// The current input mode enum value.
        /// </summary>
        public InputMode CurrentMode => currentMode;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Auto-find inputs if not assigned
            if (keyboardInput == null)
                keyboardInput = GetComponent<KeyboardInput>();
            if (keyboardInput == null)
                keyboardInput = FindAnyObjectByType<KeyboardInput>();

            if (poseInput == null)
                poseInput = GetComponent<PoseInput>();
            if (poseInput == null)
                poseInput = FindAnyObjectByType<PoseInput>();

            SetMode(currentMode);

            if (CurrentInput == null)
            {
                Debug.LogWarning("InputManager: No input source found! Adding KeyboardInput.");
                keyboardInput = gameObject.AddComponent<KeyboardInput>();
                SetMode(InputMode.Keyboard);
            }

            Debug.Log($"InputManager ready. Mode: {currentMode}, Input: {CurrentInput?.GetType().Name ?? "NULL"}");
        }

        /// <summary>
        /// Switch input mode at runtime. Called from settings UI.
        /// </summary>
        public void SetMode(InputMode mode)
        {
            currentMode = mode;

            switch (mode)
            {
                case InputMode.Keyboard:
                    CurrentInput = keyboardInput;
                    // Stop webcam if switching away from pose modes
                    StopPoseDetectionIfRunning();
                    break;

                case InputMode.Webcam:
                    if (poseInput != null)
                    {
                        CurrentInput = poseInput;
                        StartPoseDetection();
                    }
                    else
                    {
                        Debug.LogWarning("Webcam input not available, falling back to Keyboard");
                        CurrentInput = keyboardInput;
                        currentMode = InputMode.Keyboard;
                    }
                    break;
            }

            Debug.Log($"InputManager: Switched to {currentMode} ({CurrentInput?.GetType().Name})");
        }

        private void StartPoseDetection()
        {
            // Start hand gesture detection (for jump/duck)
            if (HandGestureManager.Instance != null && !HandGestureManager.Instance.IsRunning)
                HandGestureManager.Instance.StartDetection();

            // Start pose detection (for body lean lane switching)
            if (MediaPipeManager.Instance != null && !MediaPipeManager.Instance.IsRunning)
                MediaPipeManager.Instance.StartDetection();
        }

        private void StopPoseDetectionIfRunning()
        {
            if (HandGestureManager.Instance != null && HandGestureManager.Instance.IsRunning)
                HandGestureManager.Instance.StopDetection();

            if (MediaPipeManager.Instance != null && MediaPipeManager.Instance.IsRunning)
                MediaPipeManager.Instance.StopDetection();
        }

        /// <summary>
        /// Returns the display name for the current input mode.
        /// </summary>
        public string GetModeName()
        {
            return currentMode switch
            {
                InputMode.Keyboard => "Keyboard",
                InputMode.Webcam => "Webcam",
                _ => "Unknown"
            };
        }
    }
}
