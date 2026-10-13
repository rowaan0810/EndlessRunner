// InputManager.cs — Manages switching between input modes.
// Holds references to all IGameInput implementations and exposes the active one.

using UnityEngine;
using ActiveMotion3D.EndlessRunner.Input.PoseDetection;

namespace ActiveMotion3D.EndlessRunner.Input
{
    public enum InputMode
    {
        Keyboard,
        Webcam
    }

    /// <summary>
    /// Keeps the player's menu selection while scenes are replaced.
    /// </summary>
    public static class InputModeSelection
    {
        public static InputMode Current { get; set; } = InputMode.Webcam;
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
        [SerializeField] private InputMode currentMode = InputMode.Webcam;

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

            // The Gameplay scene must use the mode chosen in MainMenu instead of
            // overwriting it with the scene's serialized default.
            currentMode = InputModeSelection.Current;
            SetMode(currentMode);

            if (CurrentInput == null)
            {
                Debug.LogWarning("InputManager: No input source found! Adding KeyboardInput.");
                keyboardInput = gameObject.AddComponent<KeyboardInput>();
                SetMode(InputMode.Keyboard);
            }

            Debug.Log($"InputManager ready. Mode: {currentMode}, Input: {CurrentInput?.GetType().Name ?? "NULL"}");
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Switch input mode at runtime. Called from settings UI.
        /// </summary>
        public void SetMode(InputMode mode)
        {
            currentMode = mode;
            InputModeSelection.Current = mode;

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
                        poseInput.RecenterHorizontal();
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
            // Original startup order used by the project before the scene changes.
            if (HandGestureManager.Instance != null && !HandGestureManager.Instance.IsRunning)
                HandGestureManager.Instance.StartDetection();

            if (MediaPipeManager.Instance != null && !MediaPipeManager.Instance.IsRunning)
                MediaPipeManager.Instance.StartDetection();
        }

        private void StopPoseDetectionIfRunning()
        {
            if (HandGestureManager.Instance != null)
                HandGestureManager.Instance.StopDetection();

            if (MediaPipeManager.Instance != null)
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
