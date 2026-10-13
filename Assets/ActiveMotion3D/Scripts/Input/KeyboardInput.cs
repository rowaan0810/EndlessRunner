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
            if (UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow) || UnityEngine.Input.GetKeyDown(KeyCode.A))
            {
                DesiredLane = Mathf.Max(0, DesiredLane - 1);
            }
            if (UnityEngine.Input.GetKeyDown(KeyCode.RightArrow) || UnityEngine.Input.GetKeyDown(KeyCode.D))
            {
                DesiredLane = Mathf.Min(2, DesiredLane + 1);
            }

            // Jump — set flag, consumed by PlayerController
            if (UnityEngine.Input.GetKeyDown(KeyCode.UpArrow) ||
                UnityEngine.Input.GetKeyDown(KeyCode.W) ||
                UnityEngine.Input.GetKeyDown(KeyCode.Space))
            {
                JumpRequested = true;
            }

            // Duck — set flag, consumed by PlayerController
            if (UnityEngine.Input.GetKeyDown(KeyCode.DownArrow) || UnityEngine.Input.GetKeyDown(KeyCode.S))
            {
                DuckRequested = true;
            }
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
