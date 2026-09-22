// EasyModeInput.cs — Implements IGameInput with reduced movement requirements.
// Designed for elderly users or people with limited mobility.
// Uses only upper body: head tilt for lanes, small nod up for jump, small nod down for duck.

using UnityEngine;
using EndlessRunner.Input.PoseDetection;

namespace EndlessRunner.Input
{
    /// <summary>
    /// Easy Mode input for seniors / limited mobility. Requires only minimal movement:
    /// - Head tilt left/right → switch lanes
    /// - Small upward nod → jump
    /// - Small downward nod → duck
    ///
    /// Uses the same PoseController but with much more sensitive thresholds
    /// and wider lane bands (so small leans register).
    /// </summary>
    public class EasyModeInput : MonoBehaviour, IGameInput
    {
        private PoseController poseController;
        private PoseResult lastResult;

        // IGameInput state
        private int desiredLane = 1;
        private bool jumpRequested;
        private bool duckRequested;

        public int DesiredLane => desiredLane;
        public bool JumpRequested => jumpRequested;
        public bool DuckRequested => duckRequested;

        public void ConsumeJump() => jumpRequested = false;
        public void ConsumeDuck() => duckRequested = false;

        public bool IsAvailable => MediaPipeManager.Instance != null && MediaPipeManager.Instance.IsRunning;

        private void Awake()
        {
            // Much more sensitive settings for easy mode
            poseController = new PoseController(new PoseSettings
            {
                mirror = true,

                // Wider lane bands — even a small lean triggers a lane change
                laneCenterHalf = 0.12f,         // Narrower center band (was 1/6 = 0.166)
                laneMargin = 0.015f,            // Less hysteresis (was 0.03)
                laneSmoothing = 0.5f,           // Smoother to avoid jitter

                // Jump: very sensitive — a small upward head movement is enough
                jumpScale = 0.12f,              // Half of normal (was 0.25)
                jumpVelocity = 0.6f,            // Much lower velocity gate (was 1.2)
                jumpHipFloor = -0.15f,          // More forgiving (was -0.05)
                jumpNoAnkleHipLift = 0.12f,     // Half of normal (was 0.24)

                // Duck: very sensitive — a small head dip triggers it
                duckScale = 0.5f,               // Was 0.25 (needs to hold for slightly more depth)
                duckDrop = 0.40f,               // Was 0.20 (requires much deeper instant nod)
                duckHoldMs = 200f,              // Was 80ms — hold longer to prove it's a nod, not a wind-up

                // Cooldowns remain reasonable to prevent accidental repeats
                cooldownJumpMs = 800f,
                cooldownDuckMs = 900f,

                // Faster warm-up
                warmupMs = 600f,

                minVisibility = 0.3f,
            });
        }

        private float lastProcessedTimestamp = -1f;

        private void Update()
        {
            if (MediaPipeManager.Instance == null || !MediaPipeManager.Instance.IsRunning)
                return;

            float timestamp = MediaPipeManager.Instance.CurrentTimestamp;
            if (timestamp <= lastProcessedTimestamp)
                return; // Only process when a new MediaPipe frame arrives
            
            lastProcessedTimestamp = timestamp;
            var landmarks = MediaPipeManager.Instance.CurrentLandmarks;

            lastResult = poseController.Update(landmarks, timestamp);

            if (lastResult.jumpEvent) jumpRequested = true;
            if (lastResult.duckEvent) duckRequested = true;
            desiredLane = lastResult.lane;
        }

        /// <summary>Access latest result for debug HUD.</summary>
        public PoseResult GetLastResult() => lastResult;

        /// <summary>Access the underlying PoseController for settings adjustments.</summary>
        public PoseController Controller => poseController;
    }
}
