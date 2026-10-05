// PoseController.cs — Gesture detection for the endless runner.
// 
// GESTURES:
//   Jump  = Raise RIGHT hand clearly above your HEAD
//   Duck  = Raise LEFT hand clearly above your HEAD
//   Super = Raise BOTH hands clearly above your HEAD
//   Lanes = Lean torso left/right
//
// MediaPipe coordinate system:
//   X: 0 = left edge of frame, 1 = right edge
//   Y: 0 = TOP of frame, 1 = BOTTOM of frame  (Y grows DOWN!)
//   Landmark labels are from the PERSON's perspective (not camera's)

using System;
using UnityEngine;

namespace EndlessRunner.Input.PoseDetection
{
    public static class LM
    {
        public const int NOSE = 0;
        public const int L_SHOULDER = 11;
        public const int R_SHOULDER = 12;
        public const int L_WRIST = 15;
        public const int R_WRIST = 16;
        public const int L_HIP = 23;
        public const int R_HIP = 24;
    }

    public struct PoseLandmark
    {
        public float x, y, z;
        public float visibility;
    }

    public struct PoseResult
    {
        public bool ready;
        public int lane;
        public bool laneChanged;
        public bool jumpEvent;
        public bool duckEvent;
        public bool superDashEvent;
        public bool tracking;

        // Live state for HUD overlay
        public float smoothedX;
        public bool rightHandUp;
        public bool leftHandUp;
        public float noseY, noseX;
        public float rWristY, lWristY;
        public float shoulderMidY;
    }

    public class PoseController
    {
        // ── Lane tuning ──
        private const float LANE_CENTER_HALF = 1f / 6f;
        private const float LANE_MARGIN = 0.03f;
        private const float LANE_SMOOTHING = 0.6f;

        // ── Gesture tuning ──
        private const float WARMUP_MS = 800f;
        private const float COOLDOWN_JUMP_MS = 800f;
        private const float COOLDOWN_DUCK_MS = 800f;
        private const float COOLDOWN_SUPER_MS = 30000f;
        private const int HOLD_FRAMES = 3;

        public bool Mirror { get; set; } = true;

        // State
        private int lane = 1;
        private float? firstT;
        private float? smoothedX;
        private float lastJumpT = float.NegativeInfinity;
        private float lastDuckT = float.NegativeInfinity;
        private float lastSuperT = float.NegativeInfinity;
        private int rHold, lHold;
        private bool rWasUp, lWasUp;
        private int frameCount;

        public int CurrentLane => lane;

        public void Reset()
        {
            lane = 1;
            firstT = null;
            smoothedX = null;
            lastJumpT = float.NegativeInfinity;
            lastDuckT = float.NegativeInfinity;
            lastSuperT = float.NegativeInfinity;
            rHold = lHold = 0;
            rWasUp = lWasUp = false;
            frameCount = 0;
        }

        public PoseResult Update(PoseLandmark[] lm, float t)
        {
            var r = new PoseResult { lane = lane, tracking = false };
            if (lm == null || lm.Length < 33) return r;
            r.tracking = true;

            if (firstT == null) firstT = t;
            bool ready = (t - firstT.Value) >= WARMUP_MS;
            r.ready = ready;
            frameCount++;

            // ════════════════════════════════════════
            // 1. READ KEY LANDMARKS
            // ════════════════════════════════════════
            float noseY = lm[LM.NOSE].y;
            float noseX = lm[LM.NOSE].x;

            // Person's right wrist and left wrist (MediaPipe labels from person's perspective)
            float personRWristY = lm[LM.R_WRIST].y;
            float personLWristY = lm[LM.L_WRIST].y;
            float personRWristVis = lm[LM.R_WRIST].visibility;
            float personLWristVis = lm[LM.L_WRIST].visibility;

            float shoulderMidY = (lm[LM.L_SHOULDER].y + lm[LM.R_SHOULDER].y) * 0.5f;
            float shoulderMidX = (lm[LM.L_SHOULDER].x + lm[LM.R_SHOULDER].x) * 0.5f;

            r.noseY = noseY;
            r.noseX = noseX;
            r.shoulderMidY = shoulderMidY;
            // Store the PERSON's right/left wrist Y for HUD display
            r.rWristY = personRWristY;
            r.lWristY = personLWristY;

            // ════════════════════════════════════════
            // 2. LANE DETECTION
            // ════════════════════════════════════════
            // For lanes: mirror the X so leaning left in real life = lane 0
            float rawX = Mirror ? 1f - shoulderMidX : shoulderMidX;
            if (smoothedX == null) smoothedX = rawX;
            else smoothedX = LANE_SMOOTHING * rawX + (1f - LANE_SMOOTHING) * smoothedX.Value;

            float xm = smoothedX.Value;
            r.smoothedX = xm;
            int newLane = LaneFromX(xm, lane);
            r.laneChanged = newLane != lane;
            lane = newLane;
            r.lane = lane;

            // ════════════════════════════════════════
            // 3. ARM-RAISE DETECTION
            // ════════════════════════════════════════
            // "Hand above head" = wrist Y is LESS than nose Y (because Y grows DOWN)
            // We use nose as the reference, NOT shoulders — this requires a deliberate 
            // arm raise above your head, not just hands at chest level.
            //
            // NO mirror swap needed here — MediaPipe labels wrists from the person's 
            // perspective, so R_WRIST is always the person's right hand regardless of camera.

            bool personRightHandUp = personRWristVis >= 0.3f && personRWristY < noseY;
            bool personLeftHandUp = personLWristVis >= 0.3f && personLWristY < noseY;

            // Hold counter — require HOLD_FRAMES consecutive frames of "up"
            rHold = personRightHandUp ? rHold + 1 : 0;
            lHold = personLeftHandUp ? lHold + 1 : 0;

            bool rUp = rHold >= HOLD_FRAMES;
            bool lUp = lHold >= HOLD_FRAMES;
            r.rightHandUp = rUp;
            r.leftHandUp = lUp;

            // Debug log every 60 frames so we can see what's happening
            if (frameCount % 60 == 0)
            {
                Debug.Log($"[PoseCtrl] noseY={noseY:F3} shoulderY={shoulderMidY:F3} " +
                          $"R_wristY={personRWristY:F3}(vis={personRWristVis:F2}) " +
                          $"L_wristY={personLWristY:F3}(vis={personLWristVis:F2}) " +
                          $"rUp={personRightHandUp} lUp={personLeftHandUp} " +
                          $"rHold={rHold} lHold={lHold}");
            }

            if (!ready) { rWasUp = rUp; lWasUp = lUp; return r; }

            // ════════════════════════════════════════
            // 4. FIRE EVENTS (rising edge + cooldown)
            // ════════════════════════════════════════

            // Super Dash: both hands confirmed up, rising edge
            if (rUp && lUp && !(rWasUp && lWasUp) && t - lastSuperT > COOLDOWN_SUPER_MS)
            {
                r.superDashEvent = true;
                lastSuperT = t;
                Debug.Log("[PoseCtrl] >>> SUPER DASH FIRED");
            }

            // Jump: person's right hand up, rising edge
            if (!r.superDashEvent && rUp && !rWasUp && t - lastJumpT > COOLDOWN_JUMP_MS)
            {
                r.jumpEvent = true;
                lastJumpT = t;
                Debug.Log("[PoseCtrl] >>> JUMP FIRED");
            }

            // Duck: person's left hand up, rising edge
            if (!r.superDashEvent && lUp && !lWasUp && t - lastDuckT > COOLDOWN_DUCK_MS)
            {
                r.duckEvent = true;
                lastDuckT = t;
                Debug.Log("[PoseCtrl] >>> DUCK FIRED");
            }

            rWasUp = rUp;
            lWasUp = lUp;

            return r;
        }

        private static int LaneFromX(float x, int current)
        {
            float b1 = 0.5f - LANE_CENTER_HALF, b2 = 0.5f + LANE_CENTER_HALF;
            if (current == 0) return x > b1 + LANE_MARGIN ? (x > b2 + LANE_MARGIN ? 2 : 1) : 0;
            if (current == 2) return x < b2 - LANE_MARGIN ? (x < b1 - LANE_MARGIN ? 0 : 1) : 2;
            if (x < b1 - LANE_MARGIN) return 0;
            if (x > b2 + LANE_MARGIN) return 2;
            return 1;
        }
    }
}
