// PoseController.cs — Gesture detection for the endless runner.
// 
// GESTURES:
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

        public float smoothedX;
        public float shoulderMidY;
    }

    public class PoseController
    {
        // ── Lane tuning ──
        private const float LANE_CENTER_HALF = 1f / 6f;
        private const float LANE_MARGIN = 0.03f;
        private const float LANE_SMOOTHING = 0.6f;

        private const float WARMUP_MS = 800f;

        public bool Mirror { get; set; } = true;

        // State
        private int lane = 1;
        private float? firstT;
        private float? smoothedX;
        private int frameCount;

        public int CurrentLane => lane;

        public void Reset()
        {
            lane = 1;
            firstT = null;
            smoothedX = null;
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
            float shoulderMidY = (lm[LM.L_SHOULDER].y + lm[LM.R_SHOULDER].y) * 0.5f;
            float shoulderMidX = (lm[LM.L_SHOULDER].x + lm[LM.R_SHOULDER].x) * 0.5f;

            r.shoulderMidY = shoulderMidY;

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
