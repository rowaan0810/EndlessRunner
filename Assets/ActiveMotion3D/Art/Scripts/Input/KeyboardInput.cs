// KeyboardInput.cs — Keyboard implementation of IGameInput.
// Arrow keys or WASD for lane switching, jumping, and ducking.

using UnityEngine;

namespace ActiveMotion3D.EndlessRunner.Input
{
    /// <summary>
    /// Reads keyboard input and translates it into game intents.
    /// Left/Right (or A/D) switch lanes, Up (or W/Space) jumps, Down (or S) ducks.
    /// </summary>
    public class KeyboardInput : MonoBehaviour, IGameInput
    {
        [Header("Keyboard Bindings")]
        [SerializeField] private KeyCode[] leftKeys = { KeyCode.LeftArrow, KeyCode.A };
        [SerializeField] private KeyCode[] rightKeys = { KeyCode.RightArrow, KeyCode.D };
        [SerializeField] private KeyCode[] jumpKeys = { KeyCode.UpArrow, KeyCode.W, KeyCode.Space };
        [SerializeField] private KeyCode[] duckKeys = { KeyCode.DownArrow, KeyCode.S };

        public int DesiredLane { get; private set; } = 1; // Start in center lane
        public bool UsesContinuousHorizontal => false;
        public float DesiredHorizontalPosition => 0f;

        public bool JumpRequested { get; private set; }
        public bool DuckRequested { get; private set; }

        public void ConsumeJump() => JumpRequested = false;
        public void ConsumeDuck() => DuckRequested = false;

        private void Update()
        {
            // Lane switching — only on key down (not held)
            if (AnyKeyDown(leftKeys))
            {
                DesiredLane = Mathf.Max(0, DesiredLane - 1);
            }
            if (AnyKeyDown(rightKeys))
            {
                DesiredLane = Mathf.Min(2, DesiredLane + 1);
            }

            // Jump — set flag, consumed by PlayerController
            if (AnyKeyDown(jumpKeys))
            {
                JumpRequested = true;
            }

            // Duck — set flag, consumed by PlayerController
            if (AnyKeyDown(duckKeys))
            {
                DuckRequested = true;
            }
        }

        private static bool AnyKeyDown(KeyCode[] keys)
        {
            if (keys == null) return false;
            foreach (KeyCode key in keys)
                if (UnityEngine.Input.GetKeyDown(key)) return true;
            return false;
        }

        /// <summary>
        /// Resets to center lane. Called on game restart.
        /// </summary>
        public void ResetLane()
        {
            DesiredLane = 1;
            JumpRequested = false;
            DuckRequested = false;
        }
    }
}
