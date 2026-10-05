// PoseInput.cs — Implements IGameInput using webcam HAND + BODY detection.
//
// GESTURES (Hand Landmarker):
//   Thumbs Up 👍 → JUMP
//   Fist ✊ → DUCK
//
// LANES (Pose Landmarker):
//   Lean body left/right → Switch lanes

using UnityEngine;
using EndlessRunner.Input.PoseDetection;

namespace EndlessRunner.Input
{
    public class PoseInput : MonoBehaviour, IGameInput
    {
        // IGameInput state
        private int desiredLane = 1;
        private bool jumpRequested;
        private bool duckRequested;

        public int DesiredLane => desiredLane;
        public bool JumpRequested => jumpRequested;
        public bool DuckRequested => duckRequested;

        public void ConsumeJump() => jumpRequested = false;
        public void ConsumeDuck() => duckRequested = false;

        public bool IsAvailable => HandGestureManager.Instance != null && HandGestureManager.Instance.IsRunning;

        // Gesture state for rising-edge detection
        private HandGesture lastGesture = HandGesture.None;
        private float lastJumpTime = -999f;
        private float lastDuckTime = -999f;
        private const float COOLDOWN = 0.4f;

        // Lane detection from body pose
        private float? smoothedX;
        private const float LANE_SMOOTHING = 0.5f;

        // For HUD display
        public HandGesture CurrentGesture { get; private set; } = HandGesture.None;
        public int FingersExtended { get; private set; }
        public float HandX { get; private set; } = 0.5f;

        private void Update()
        {
            // ── Hand gesture detection (jump/duck) ──
            var hgm = HandGestureManager.Instance;
            if (hgm != null && hgm.IsRunning && hgm.CurrentHands.Count > 0)
            {
                var hand = hgm.CurrentHands[0];
                CurrentGesture = hand.gesture;
                FingersExtended = hand.fingersExtended;
                HandX = hand.handCenterX;

                float now = Time.time;

                // Thumbs Up = JUMP (auto-repeats every COOLDOWN while held)
                if (hand.gesture == HandGesture.ThumbsUp && now - lastJumpTime > COOLDOWN)
                {
                    jumpRequested = true;
                    lastJumpTime = now;
                    Debug.Log($"[PoseInput] JUMP! (Native Gesture: {hand.rawGestureCategory})");
                }

                // Open Palm = DUCK (auto-repeats every COOLDOWN while held)
                if (hand.gesture == HandGesture.OpenPalm && now - lastDuckTime > COOLDOWN)
                {
                    duckRequested = true;
                    lastDuckTime = now;
                    Debug.Log($"[PoseInput] DUCK! (Native Gesture: {hand.rawGestureCategory})");
                }

                lastGesture = hand.gesture;
            }
            else
            {
                CurrentGesture = HandGesture.None;
                FingersExtended = 0;
                lastGesture = HandGesture.None;
            }

            // ── Lane detection from body pose (lean left/right) ──
            var mpm = MediaPipeManager.Instance;
            if (mpm != null && mpm.IsRunning && mpm.CurrentLandmarks != null)
            {
                var lm = mpm.CurrentLandmarks;
                if (lm.Length >= 33)
                {
                    float shoulderMidX = (lm[11].x + lm[12].x) * 0.5f;
                    // Mirror: camera left = user's right
                    float rawX = 1f - shoulderMidX;

                    if (smoothedX == null) smoothedX = rawX;
                    else smoothedX = LANE_SMOOTHING * rawX + (1f - LANE_SMOOTHING) * smoothedX.Value;

                    float x = smoothedX.Value;
                    // Narrower thresholds for easier 3-lane switching
                    if (x < 0.42f) desiredLane = 0;
                    else if (x > 0.58f) desiredLane = 2;
                    else desiredLane = 1;
                }
            }
        }
    }
}
