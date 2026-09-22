// PoseInput.cs — Implements IGameInput using webcam pose detection.
// Reads from MediaPipeManager → PoseController → game intents.

using UnityEngine;
using EndlessRunner.Input.PoseDetection;

namespace EndlessRunner.Input
{
    /// <summary>
    /// Webcam pose-based input. Full body tracking: lean to switch lanes,
    /// jump to jump, crouch to duck. Uses the SurfCam PoseController algorithm.
    /// </summary>
    public class PoseInput : MonoBehaviour, IGameInput
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
            poseController = new PoseController(new PoseSettings
            {
                mirror = true,

                // Require more deliberate movement to trigger jump/duck
                jumpScale = 0.6f,               // Was 0.25 — need bigger jump now
                jumpVelocity = 2.0f,            // Was 1.2 — must be moving up fast
                jumpHipFloor = 0.0f,            // Was -0.05 — hips must actually be rising

                duckScale = 0.6f,               // Must drop by 60% of duckDrop to start holding
                duckDrop = 0.8f,                // Extremely deep duck required to trigger instantly
                duckHoldMs = 250f,              // Hold slightly longer (250ms) to confirm it's a duck, not a jump wind-up

                // Longer warmup so baselines settle before detecting
                warmupMs = 2000f,               // Was 1000 — 2 seconds to calibrate

                // Longer cooldowns prevent rapid false re-triggers
                cooldownJumpMs = 800f,          // Was 600
                cooldownDuckMs = 900f,          // Was 700
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

        /// <summary>Access latest result for debug HUD display.</summary>
        public PoseResult GetLastResult() => lastResult;

        /// <summary>Access the underlying PoseController for settings adjustments.</summary>
        public PoseController Controller => poseController;
    }
}
