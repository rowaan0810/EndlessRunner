// PoseController.cs — Direct C# port of SurfCam's pose-logic.mjs.
// Pure math: rolling percentiles, torso-normalized signals, hysteresis.
// No Unity dependencies — takes float arrays, returns intents.

using System;
using System.Collections.Generic;

namespace EndlessRunner.Input.PoseDetection
{
    /// <summary>
    /// Landmark indices for MediaPipe's 33-point pose model.
    /// </summary>
    public static class LM
    {
        public const int NOSE = 0;
        public const int L_SHOULDER = 11;
        public const int R_SHOULDER = 12;
        public const int L_HIP = 23;
        public const int R_HIP = 24;
        public const int L_KNEE = 25;
        public const int R_KNEE = 26;
        public const int L_ANKLE = 27;
        public const int R_ANKLE = 28;
    }

    /// <summary>
    /// A single landmark with x, y, z, visibility.
    /// Coordinates are normalized [0,1], y grows DOWN.
    /// </summary>
    public struct PoseLandmark
    {
        public float x, y, z;
        public float visibility;
    }

    /// <summary>
    /// Tuning constants. Matches SurfCam's DEFAULTS.
    /// </summary>
    public class PoseSettings
    {
        public bool mirror = true;
        public float laneMargin = 0.03f;
        public float laneCenterHalf = 1f / 6f;
        public float laneSmoothing = 0.6f;
        public float jumpLift = 0.22f;
        public float jumpHipLift = 0.12f;
        public float jumpNoAnkleHipLift = 0.24f;
        public float jumpVelocity = 1.2f;
        public float jumpHipFloor = -0.05f;
        public float jumpScale = 0.25f;
        public float duckScale = 0.5f;
        public float duckHoldMs = 150f;
        public float velocityWindowMs = 66f;
        public float rearmFrac = 0.4f;
        public float duckDrop = 0.45f;
        public float maxActionJumpMs = 1500f;
        public float maxActionDuckMs = 4000f;
        public float ankleWindowMs = 1500f;
        public float anklePercentile = 0.75f;
        public float standWindowMs = 5000f;
        public float standPercentile = 0.25f;
        public float torsoWindowMs = 2000f;
        public float cooldownJumpMs = 600f;
        public float cooldownDuckMs = 700f;
        public float warmupMs = 1000f;
        public float minVisibility = 0.5f;
    }

    /// <summary>
    /// Output from PoseController.Update().
    /// </summary>
    public struct PoseResult
    {
        public bool ready;
        public int lane;
        public bool laneChanged;
        public bool jumpEvent;
        public bool duckEvent;
        public bool tracking;

        // Debug metrics
        public float hipLift, noseLift, feetLift, jumpSignal, hipVel, noseDrop;
        public float jumpThresh, duckThresh;
        public bool anklesOk;
        public float smoothedX;
    }

    /// <summary>
    /// Time-windowed percentile tracker. Exact port of SurfCam's RollingPercentile.
    /// </summary>
    public class RollingPercentile
    {
        private readonly float windowMs;
        private readonly float q;
        private readonly List<(float t, float v)> samples = new List<(float, float)>();

        public RollingPercentile(float windowMs, float q = 0.5f)
        {
            this.windowMs = windowMs;
            this.q = q;
        }

        public void Push(float t, float v)
        {
            samples.Add((t, v));
            float cutoff = t - windowMs;
            while (samples.Count > 0 && samples[0].t < cutoff)
                samples.RemoveAt(0);
        }

        public float SpanMs => samples.Count > 0
            ? samples[samples.Count - 1].t - samples[0].t
            : 0f;

        public float Value()
        {
            if (samples.Count == 0) return float.NaN;
            var sorted = new List<float>(samples.Count);
            for (int i = 0; i < samples.Count; i++) sorted.Add(samples[i].v);
            sorted.Sort();
            float pos = (sorted.Count - 1) * q;
            int lo = (int)Math.Floor(pos);
            int hi = (int)Math.Ceiling(pos);
            return lo == hi ? sorted[lo] : sorted[lo] + (sorted[hi] - sorted[lo]) * (pos - lo);
        }

        public void Clear() => samples.Clear();
    }

    /// <summary>
    /// Core pose controller. Line-by-line port of SurfCam's PoseController class.
    /// Takes 33 normalized landmarks + timestamp, outputs lane/jump/duck intents.
    /// </summary>
    public class PoseController
    {
        public PoseSettings Settings { get; private set; }

        // State
        private int lane = 1;
        private float? firstT;
        private RollingPercentile torsoBase;
        private RollingPercentile ankleLBase, ankleRBase;
        private RollingPercentile hipStand, noseStand;
        private List<(float t, float y)> hipHistory;
        private float? laneX;
        private float? duckAboveSince;
        private bool armedJump, armedDuck;
        private float lastFiredJump, lastFiredDuck;

        public int CurrentLane => lane;

        public PoseController(PoseSettings settings = null)
        {
            Settings = settings ?? new PoseSettings();
            Reset();
        }

        public void Reset()
        {
            var o = Settings;
            lane = 1;
            firstT = null;
            torsoBase = new RollingPercentile(o.torsoWindowMs, 0.5f);
            ankleLBase = new RollingPercentile(o.ankleWindowMs, o.anklePercentile);
            ankleRBase = new RollingPercentile(o.ankleWindowMs, o.anklePercentile);
            hipStand = new RollingPercentile(o.standWindowMs, o.standPercentile);
            noseStand = new RollingPercentile(o.standWindowMs, o.standPercentile);
            hipHistory = new List<(float, float)>();
            laneX = null;
            duckAboveSince = null;
            armedJump = true;
            armedDuck = true;
            lastFiredJump = float.NegativeInfinity;
            lastFiredDuck = float.NegativeInfinity;
        }

        /// <summary>
        /// Visibility-weighted centre of the torso polygon.
        /// </summary>
        public static (float x, float y) TorsoCenter(PoseLandmark[] landmarks)
        {
            float sx = 0, sy = 0, sw = 0;
            int[] indices = { LM.L_SHOULDER, LM.R_SHOULDER, LM.L_HIP, LM.R_HIP };
            foreach (int i in indices)
            {
                var p = landmarks[i];
                float w = Math.Max(0.05f, p.visibility);
                sx += p.x * w;
                sy += p.y * w;
                sw += w;
            }
            return (sx / sw, sy / sw);
        }

        /// <summary>
        /// 3-band lane detection with hysteresis.
        /// </summary>
        public static int LaneFromX(float x, int currentLane, float margin, float centerHalf = 1f / 6f)
        {
            float b1 = 0.5f - centerHalf, b2 = 0.5f + centerHalf;
            if (currentLane == 0) return x > b1 + margin ? (x > b2 + margin ? 2 : 1) : 0;
            if (currentLane == 2) return x < b2 - margin ? (x < b1 - margin ? 0 : 1) : 2;
            if (x < b1 - margin) return 0;
            if (x > b2 + margin) return 2;
            return 1;
        }

        private float HipVelocity(float t, float hipY, float torso)
        {
            var o = Settings;
            hipHistory.Add((t, hipY));
            while (hipHistory.Count > 2 && hipHistory[1].t <= t - o.velocityWindowMs)
                hipHistory.RemoveAt(0);
            var old = hipHistory[0];
            float dt = (t - old.t) / 1000f;
            if (dt <= 0) return 0;
            return (old.y - hipY) / torso / dt; // positive = moving up
        }

        private static bool Visible(PoseLandmark lm, float min)
        {
            return lm.visibility >= min;
        }

        /// <summary>
        /// Process one frame of landmarks. Returns game intents.
        /// </summary>
        /// <param name="landmarks">33 MediaPipe pose landmarks (normalized, y grows DOWN)</param>
        /// <param name="t">Timestamp in milliseconds</param>
        public PoseResult Update(PoseLandmark[] landmarks, float t)
        {
            var o = Settings;

            if (landmarks == null || landmarks.Length < 33)
            {
                return new PoseResult { ready = false, lane = lane, tracking = false };
            }

            if (firstT == null) firstT = t;

            var ls = landmarks[LM.L_SHOULDER];
            var rs = landmarks[LM.R_SHOULDER];
            var lh = landmarks[LM.L_HIP];
            var rh = landmarks[LM.R_HIP];
            var la = landmarks[LM.L_ANKLE];
            var ra = landmarks[LM.R_ANKLE];
            var nose = landmarks[LM.NOSE];

            bool anklesOk = Visible(la, o.minVisibility) && Visible(ra, o.minVisibility);
            bool hipsOk = Visible(lh, o.minVisibility) && Visible(rh, o.minVisibility);
            
            // If hips aren't visible (user sitting close to webcam), 
            // MediaPipe guesses their location which causes massive jitter.
            // Fallback to a fixed torso scale based on shoulder width if hips are hidden.
            float shoulderY = (ls.y + rs.y) / 2f;
            float hipY = hipsOk ? (lh.y + rh.y) / 2f : shoulderY + Math.Abs(ls.x - rs.x) * 1.5f; 
            
            float torsoNow = Math.Max(1e-3f, hipY - shoulderY);

            torsoBase.Push(t, torsoNow);
            float torso = torsoBase.Value();
            if (float.IsNaN(torso)) torso = torsoNow;

            if (anklesOk)
            {
                ankleLBase.Push(t, la.y);
                ankleRBase.Push(t, ra.y);
            }
            hipStand.Push(t, hipY);
            noseStand.Push(t, nose.y);
            float hipVel = hipsOk ? HipVelocity(t, hipY, torso) : HipVelocity(t, nose.y, torso);

            // Lanes: centre of torso polygon, mirrored, lightly smoothed
            var tc = TorsoCenter(landmarks);
            float rawX = o.mirror ? 1f - tc.x : tc.x;
            if (laneX == null) laneX = rawX;
            else laneX = o.laneSmoothing * rawX + (1f - o.laneSmoothing) * laneX.Value;
            float xm = laneX.Value;
            int newLane = LaneFromX(xm, lane, o.laneMargin, o.laneCenterHalf);
            bool laneChanged = newLane != lane;
            lane = newLane;

            // Vertical signals in torso units (positive = moved UP)
            float hipStandVal = hipStand.Value();
            float noseStandVal = noseStand.Value();
            float hipLift = (hipStandVal - hipY) / torso;
            float noseLift = (noseStandVal - nose.y) / torso;
            float liftL = anklesOk ? (ankleLBase.Value() - la.y) / torso : float.NaN;
            float liftR = anklesOk ? (ankleRBase.Value() - ra.y) / torso : float.NaN;
            float feetLift = anklesOk ? Math.Min(liftL, liftR) : float.NaN;

            bool ready = (t - firstT.Value) >= o.warmupMs && hipStand.SpanMs >= o.warmupMs * 0.8f;

            // Timeouts
            if (!armedJump && t - lastFiredJump > o.maxActionJumpMs) armedJump = true;
            if (!armedDuck && t - lastFiredDuck > o.maxActionDuckMs) armedDuck = true;

            // Jump
            float k = o.jumpScale;
            float kd = o.duckScale;
            float jumpSignal, jumpThresh;

            if (anklesOk)
            {
                jumpSignal = hipLift > o.jumpHipFloor ? feetLift : Math.Min(feetLift, 0);
                jumpThresh = o.jumpLift * k;
            }
            else
            {
                jumpSignal = Math.Min(hipLift, noseLift);
                jumpThresh = o.jumpNoAnkleHipLift * k;
            }

            float duckThresh = o.duckDrop * kd;
            bool fastEnough = hipVel > o.jumpVelocity;
            bool jumpEvent = false;

            if (armedJump)
            {
                if (ready && jumpSignal > jumpThresh && fastEnough && t - lastFiredJump > o.cooldownJumpMs)
                {
                    jumpEvent = true;
                    lastFiredJump = t;
                    armedJump = false;
                }
            }
            else if (jumpSignal < jumpThresh * o.rearmFrac)
            {
                armedJump = true;
            }

            // Duck
            float noseDrop = -noseLift;
            if (noseDrop > duckThresh)
            {
                if (duckAboveSince == null) duckAboveSince = t;
            }
            else
            {
                duckAboveSince = null;
            }
            bool duckHeld = duckAboveSince != null && t - duckAboveSince.Value >= o.duckHoldMs;
            bool duckNow = noseDrop > o.duckDrop || (noseDrop > duckThresh && (duckHeld || kd >= 1f));
            bool duckEvent = false;

            if (armedDuck)
            {
                if (ready && duckNow && t - lastFiredDuck > o.cooldownDuckMs)
                {
                    duckEvent = true;
                    lastFiredDuck = t;
                    armedDuck = false;
                }
            }
            else if (noseDrop < duckThresh * o.rearmFrac)
            {
                armedDuck = true;
            }

            return new PoseResult
            {
                ready = ready,
                lane = lane,
                laneChanged = laneChanged,
                jumpEvent = jumpEvent,
                duckEvent = duckEvent,
                tracking = true,
                anklesOk = anklesOk,
                hipLift = hipLift,
                noseLift = noseLift,
                feetLift = feetLift,
                jumpSignal = jumpSignal,
                hipVel = hipVel,
                noseDrop = noseDrop,
                jumpThresh = jumpThresh,
                duckThresh = duckThresh,
                smoothedX = xm,
            };
        }
    }
}
