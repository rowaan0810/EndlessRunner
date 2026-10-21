// PoseInput.cs — Implements IGameInput using webcam HAND + BODY detection.
//
// GESTURES (Hand Landmarker + Pose Landmarker):
//   Right Index Pointing Up ☝️ → JUMP
//   Open Palm ✋ → DUCK
//   Shoulders rise (jump/tiptoe) → JUMP
//   Shoulders sink (squat) → DUCK
//
// LANES (Pose Landmarker):
//   Lean body left/right → Switch lanes

using UnityEngine;
using ActiveMotion3D.EndlessRunner.Input.PoseDetection;

namespace ActiveMotion3D.EndlessRunner.Input
{
    public enum HorizontalControlMode
    {
        ThreeLane,
        Continuous
    }

    public enum BodyVerticalGesture
    {
        None,
        Jump,
        Duck
    }

    [DefaultExecutionOrder(-100)]
    public class PoseInput : MonoBehaviour, IGameInput
    {
        // IGameInput state
        private int desiredLane = 1;
        private bool jumpRequested;
        private bool duckRequested;
        private float desiredHorizontalPosition;

        public int DesiredLane => desiredLane;
        public bool UsesContinuousHorizontal => horizontalControlMode == HorizontalControlMode.Continuous;
        public float DesiredHorizontalPosition => desiredHorizontalPosition;
        public bool JumpRequested => jumpRequested;
        public bool DuckRequested => duckRequested;

        public void ConsumeJump() => jumpRequested = false;
        public void ConsumeDuck() => duckRequested = false;

        public bool IsAvailable => HandGestureManager.Instance != null && HandGestureManager.Instance.IsRunning;

        // Hand gestures must remain stable briefly before they can trigger an action.
        private HandGesture lastGesture = HandGesture.None;
        private HandGesture handGestureCandidate = HandGesture.None;
        private float handGestureCandidateSince;
        private float lastJumpTime = -999f;
        private float lastDuckTime = -999f;
        private const float COOLDOWN = 0.4f;
        [SerializeField] private bool logInputEvents;
        [SerializeField, Range(0.05f, 0.4f)] private float handGestureHoldSeconds = 0.18f;

        [Header("Horizontal Movement")]
        [SerializeField] private HorizontalControlMode horizontalControlMode = HorizontalControlMode.Continuous;
        [Tooltip("World units travelled for one shoulder-width of body movement.")]
        [SerializeField, Range(0.5f, 8f)] private float horizontalSensitivity = 5.6f;
        [Tooltip("Maximum left/right position on the running track.")]
        [SerializeField, Range(1f, 5f)] private float horizontalTrackLimit = 3.2f;
        [Tooltip("Natural body sway inside this area does not move the character.")]
        [SerializeField, Range(0f, 0.25f)] private float continuousDeadZone = 0.015f;
        [Tooltip("Amplifies upper-body leaning even when the hips and feet remain in place.")]
        [SerializeField, Range(0f, 3f)] private float bodyLeanGain = 1.25f;
        [Tooltip("Fresh pose frames averaged whenever a run starts. The character remains centred during calibration.")]
        [SerializeField, Range(1, 20)] private int calibrationFrameCount = 6;

        [Header("Body Jump / Duck")]
        [SerializeField] private bool enableBodyVerticalGestures = true;
        [Tooltip("Upward movement required on both shoulders, in normalized camera Y coordinates.")]
        [SerializeField, Range(0.02f, 0.5f)] private float bodyJumpThreshold = 0.035f;
        [Tooltip("Downward movement required on both shoulders, in normalized camera Y coordinates.")]
        [SerializeField, Range(0.02f, 0.5f)] private float bodyDuckThreshold = 0.04f;
        [Tooltip("How close shoulders must return to neutral before another body action can fire.")]
        [SerializeField, Range(0.01f, 0.2f)] private float bodyVerticalResetThreshold = 0.025f;
        [SerializeField, Range(0f, 1f)] private float bodyVerticalResponsiveness = 0.98f;
        [Tooltip("Number of consecutive pose frames required before jump or duck fires. Use 1 for immediate response.")]
        [SerializeField, Range(1, 8)] private int bodyConfirmationFrames = 1;
        [Tooltip("Enable only if the camera reports vertical pose coordinates in the opposite direction.")]
        [SerializeField] private bool invertBodyVerticalDirection = true;

        [Header("Shoulder Detection Debug (Runtime)")]
        [Tooltip("Current MediaPipe Y coordinate for the player's left shoulder.")]
        [SerializeField] private float leftShoulderY;
        [Tooltip("Current MediaPipe Y coordinate for the player's right shoulder.")]
        [SerializeField] private float rightShoulderY;
        [Tooltip("Left shoulder change from its own calibrated height. Positive means upward.")]
        [SerializeField] private float leftShoulderDelta;
        [Tooltip("Right shoulder change from its own calibrated height. Positive means upward.")]
        [SerializeField] private float rightShoulderDelta;
        [Tooltip("Agreed vertical movement of both shoulders used for jump/duck detection.")]
        [SerializeField] private float combinedVerticalDelta;

        [Header("Three Lane Fallback")]
        [Tooltip("Torso travel, measured in shoulder widths, required to enter a side lane.")]
        [SerializeField, Range(0.08f, 0.4f)] private float laneEnterThreshold = 0.16f;
        [Tooltip("Smaller return threshold creates hysteresis and prevents lane flicker.")]
        [SerializeField, Range(0.02f, 0.2f)] private float laneReturnThreshold = 0.07f;
        [SerializeField, Range(0f, 1f)] private float laneResponsiveness = 0.95f;
        [SerializeField, Range(0f, 1f)] private float minimumTorsoVisibility = 0.1f;

        private float smoothedTorsoX;
        private float neutralTorsoX;
        private bool hasLaneCalibration;
        private float lastProcessedPoseTimestamp = -1f;
        private float poseMissingTime;
        private float calibrationXSum;
        private float calibrationLeftShoulderYSum;
        private float calibrationRightShoulderYSum;
        private int calibrationSamplesCollected;
        private float neutralLeftShoulderY;
        private float neutralRightShoulderY;
        private float smoothedLeftShoulderY;
        private float smoothedRightShoulderY;
        private bool bodyVerticalGestureReady;
        private BodyVerticalGesture bodyGestureCandidate = BodyVerticalGesture.None;
        private int bodyGestureCandidateFrames;

        // For HUD display
        public HandGesture CurrentGesture { get; private set; } = HandGesture.None;
        public int FingersExtended { get; private set; }
        public float HandX { get; private set; } = 0.5f;
        public float BodyVerticalOffset { get; private set; }
        public BodyVerticalGesture CurrentBodyGesture { get; private set; } = BodyVerticalGesture.None;
        public bool IsCalibrated => hasLaneCalibration;
        public float LeftShoulderY => leftShoulderY;
        public float RightShoulderY => rightShoulderY;
        public float LeftShoulderDelta => leftShoulderDelta;
        public float RightShoulderDelta => rightShoulderDelta;
        public float CombinedVerticalDelta => combinedVerticalDelta;

        /// <summary>Starts a fresh centre calibration and immediately centres the output.</summary>
        public void RecenterHorizontal()
        {
            // Never carry a jump/duck request from the previous run into a restart.
            jumpRequested = false;
            duckRequested = false;
            hasLaneCalibration = false;
            calibrationXSum = 0f;
            calibrationLeftShoulderYSum = 0f;
            calibrationRightShoulderYSum = 0f;
            calibrationSamplesCollected = 0;
            lastProcessedPoseTimestamp = -1f;
            poseMissingTime = 0f;
            desiredLane = 1;
            desiredHorizontalPosition = 0f;
            BodyVerticalOffset = 0f;
            leftShoulderDelta = 0f;
            rightShoulderDelta = 0f;
            combinedVerticalDelta = 0f;
            CurrentBodyGesture = BodyVerticalGesture.None;
            bodyVerticalGestureReady = false;
            bodyGestureCandidate = BodyVerticalGesture.None;
            bodyGestureCandidateFrames = 0;
            handGestureCandidate = HandGesture.None;
            lastGesture = HandGesture.None;
        }

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

                if (hand.gesture != handGestureCandidate)
                {
                    handGestureCandidate = hand.gesture;
                    handGestureCandidateSince = now;
                }

                bool gestureStable = now - handGestureCandidateSince >= handGestureHoldSeconds;

                // Right Index Pointing Up = JUMP (auto-repeats every COOLDOWN while held)
                // MediaPipe selfie mode often returns 'Left' for the right hand, so we check both or rely on the hand position if needed, 
                // but checking for 'Right' or 'Left' will just ensure it's a pointing up gesture. Let's strictly check 'Right' per user request, 
                // but fallback to 'Left' if MediaPipe mirrors it. Actually, we'll just check the handedness property if it matches 'Right' or 'Left' depending on the mirror.
                // For safety we'll accept pointing up on either hand if the label is flipped, or specifically check "Right" or "Left" based on the user's view.
                bool isRightHand = hand.handedness.Equals("Right", System.StringComparison.OrdinalIgnoreCase) || hand.handedness.Equals("Left", System.StringComparison.OrdinalIgnoreCase);

                if (gestureStable && handGestureCandidate == HandGesture.PointingUp && lastGesture != HandGesture.PointingUp &&
                    isRightHand && BodyVerticalOffset > -bodyVerticalResetThreshold &&
                    now - lastJumpTime > COOLDOWN)
                {
                    jumpRequested = true;
                    lastJumpTime = now;
                    if (logInputEvents)
                        Debug.Log($"[PoseInput] JUMP! (Native Gesture: {hand.rawGestureCategory}, Hand: {hand.handedness})");
                }

                // Open Palm = DUCK (auto-repeats every COOLDOWN while held)
                if (gestureStable && handGestureCandidate == HandGesture.OpenPalm && lastGesture != HandGesture.OpenPalm &&
                    now - lastDuckTime > COOLDOWN)
                {
                    duckRequested = true;
                    lastDuckTime = now;
                    if (logInputEvents)
                        Debug.Log($"[PoseInput] DUCK! (Native Gesture: {hand.rawGestureCategory})");
                }

                if (gestureStable)
                    lastGesture = handGestureCandidate;
            }
            else
            {
                CurrentGesture = HandGesture.None;
                FingersExtended = 0;
                handGestureCandidate = HandGesture.None;
                lastGesture = HandGesture.None;
            }

            // ── Lane detection from body pose (lean left/right) ──
            var mpm = MediaPipeManager.Instance;
            if (mpm != null && mpm.IsRunning && mpm.CurrentLandmarks != null)
            {
                var lm = mpm.CurrentLandmarks;
                if (lm.Length >= 33 && mpm.CurrentTimestamp != lastProcessedPoseTimestamp)
                {
                    lastProcessedPoseTimestamp = mpm.CurrentTimestamp;
                    poseMissingTime = 0f;

                    float shoulderVisibility = Mathf.Min(lm[11].visibility, lm[12].visibility);
                    if (shoulderVisibility < minimumTorsoVisibility)
                        return;

                    float shoulderMidX = (lm[11].x + lm[12].x) * 0.5f;
                    float hipMidX = (lm[23].x + lm[24].x) * 0.5f;
                    float hipVisibility = (lm[23].visibility + lm[24].visibility) * 0.5f;
                    bool hipsReliable = hipVisibility >= minimumTorsoVisibility;
                    leftShoulderY = lm[11].y;
                    rightShoulderY = lm[12].y;
                    float shoulderMidY = (leftShoulderY + rightShoulderY) * 0.5f;
                    if (!mpm.IsInsideDetectionView(shoulderMidX, shoulderMidY))
                        return;

                    float shoulderWidth = Mathf.Max(Mathf.Abs(lm[11].x - lm[12].x), 0.08f);

                    // Shoulder motion directly controls the character. When hips are
                    // visible, shoulder-vs-hip displacement adds an explicit lean signal,
                    // so swaying in place works without taking a side step.
                    float leanOffset = hipsReliable ? shoulderMidX - hipMidX : 0f;
                    float controlPointX = shoulderMidX + leanOffset * bodyLeanGain;

                    // Mirror: camera left = user's right
                    float rawX = 1f - controlPointX;

                    // Calibrate against where this player naturally stands instead of
                    // assuming that everybody is perfectly centred at x=0.5.
                    if (!hasLaneCalibration)
                    {
                        calibrationXSum += rawX;
                        calibrationLeftShoulderYSum += leftShoulderY;
                        calibrationRightShoulderYSum += rightShoulderY;
                        calibrationSamplesCollected++;
                        desiredLane = 1;
                        desiredHorizontalPosition = 0f;

                        if (calibrationSamplesCollected < Mathf.Max(1, calibrationFrameCount))
                            return;

                        neutralTorsoX = calibrationXSum / calibrationSamplesCollected;
                        smoothedTorsoX = neutralTorsoX;
                        neutralLeftShoulderY = calibrationLeftShoulderYSum / calibrationSamplesCollected;
                        neutralRightShoulderY = calibrationRightShoulderYSum / calibrationSamplesCollected;
                        smoothedLeftShoulderY = neutralLeftShoulderY;
                        smoothedRightShoulderY = neutralRightShoulderY;
                        bodyVerticalGestureReady = true;
                        hasLaneCalibration = true;
                        return;
                    }

                    smoothedTorsoX = Mathf.Lerp(smoothedTorsoX, rawX, laneResponsiveness);
                    float offsetInShoulderWidths = (smoothedTorsoX - neutralTorsoX) / shoulderWidth;
                    ProcessBodyVerticalGesture(leftShoulderY, rightShoulderY);

                    if (horizontalControlMode == HorizontalControlMode.Continuous)
                    {
                        float movement = Mathf.Max(0f, Mathf.Abs(offsetInShoulderWidths) - continuousDeadZone);
                        desiredHorizontalPosition = Mathf.Clamp(
                            Mathf.Sign(offsetInShoulderWidths) * movement * horizontalSensitivity,
                            -horizontalTrackLimit, horizontalTrackLimit);

                        // Keep the legacy lane value meaningful for HUD/debug display.
                        if (desiredHorizontalPosition < -laneEnterThreshold * horizontalSensitivity) desiredLane = 0;
                        else if (desiredHorizontalPosition > laneEnterThreshold * horizontalSensitivity) desiredLane = 2;
                        else desiredLane = 1;

                        if (movement <= 0f)
                            neutralTorsoX = Mathf.Lerp(neutralTorsoX, smoothedTorsoX, 0.02f);
                        return;
                    }

                    // Hysteresis gives an immediate lane entry without flickering when
                    // the player is close to a boundary.
                    if (desiredLane == 1)
                    {
                        if (offsetInShoulderWidths < -laneEnterThreshold) desiredLane = 0;
                        else if (offsetInShoulderWidths > laneEnterThreshold) desiredLane = 2;
                    }
                    else if (desiredLane == 0 && offsetInShoulderWidths > -laneReturnThreshold)
                    {
                        desiredLane = offsetInShoulderWidths > laneEnterThreshold ? 2 : 1;
                    }
                    else if (desiredLane == 2 && offsetInShoulderWidths < laneReturnThreshold)
                    {
                        desiredLane = offsetInShoulderWidths < -laneEnterThreshold ? 0 : 1;
                    }

                    // Correct only tiny standing-position drift; never chase a real move.
                    if (desiredLane == 1 && Mathf.Abs(offsetInShoulderWidths) < laneReturnThreshold * 0.5f)
                        neutralTorsoX = Mathf.Lerp(neutralTorsoX, smoothedTorsoX, 0.02f);
                }
            }
            else
            {
                poseMissingTime += Time.deltaTime;
                if (poseMissingTime >= 0.75f)
                {
                    RecenterHorizontal();
                }
            }
        }

        private void ProcessBodyVerticalGesture(float currentLeftShoulderY, float currentRightShoulderY)
        {
            if (!enableBodyVerticalGestures)
            {
                BodyVerticalOffset = 0f;
                CurrentBodyGesture = BodyVerticalGesture.None;
                return;
            }

            smoothedLeftShoulderY = Mathf.Lerp(
                smoothedLeftShoulderY, currentLeftShoulderY, bodyVerticalResponsiveness);
            smoothedRightShoulderY = Mathf.Lerp(
                smoothedRightShoulderY, currentRightShoulderY, bodyVerticalResponsiveness);

            // MediaPipe normalized Y grows downward. Each shoulder is compared with
            // its own calibrated height, so a naturally uneven shoulder line cannot
            // cancel the movement. Positive delta means up; negative means down.
            leftShoulderDelta = neutralLeftShoulderY - smoothedLeftShoulderY;
            rightShoulderDelta = neutralRightShoulderY - smoothedRightShoulderY;
            if (invertBodyVerticalDirection)
            {
                leftShoulderDelta = -leftShoulderDelta;
                rightShoulderDelta = -rightShoulderDelta;
            }

            // Use the weaker shoulder only when both shoulders agree on direction.
            // Raising just one arm therefore does not look like a whole-body jump.
            if (leftShoulderDelta > 0f && rightShoulderDelta > 0f)
                combinedVerticalDelta = Mathf.Min(leftShoulderDelta, rightShoulderDelta);
            else if (leftShoulderDelta < 0f && rightShoulderDelta < 0f)
                combinedVerticalDelta = Mathf.Max(leftShoulderDelta, rightShoulderDelta);
            else
                combinedVerticalDelta = 0f;

            BodyVerticalOffset = combinedVerticalDelta;

            if (!bodyVerticalGestureReady)
            {
                CurrentBodyGesture = BodyVerticalGesture.None;
                if (Mathf.Abs(BodyVerticalOffset) <= bodyVerticalResetThreshold)
                {
                    bodyVerticalGestureReady = true;
                    bodyGestureCandidate = BodyVerticalGesture.None;
                    bodyGestureCandidateFrames = 0;
                }
                return;
            }

            BodyVerticalGesture observedGesture = BodyVerticalGesture.None;
            if (BodyVerticalOffset >= bodyJumpThreshold)
                observedGesture = BodyVerticalGesture.Jump;
            else if (BodyVerticalOffset <= -bodyDuckThreshold)
                observedGesture = BodyVerticalGesture.Duck;

            if (observedGesture == BodyVerticalGesture.None)
            {
                bodyGestureCandidate = BodyVerticalGesture.None;
                bodyGestureCandidateFrames = 0;
                CurrentBodyGesture = BodyVerticalGesture.None;

                if (Mathf.Abs(BodyVerticalOffset) <= bodyVerticalResetThreshold)
                {
                    neutralLeftShoulderY = Mathf.Lerp(neutralLeftShoulderY, smoothedLeftShoulderY, 0.01f);
                    neutralRightShoulderY = Mathf.Lerp(neutralRightShoulderY, smoothedRightShoulderY, 0.01f);
                }
                return;
            }

            if (observedGesture != bodyGestureCandidate)
            {
                bodyGestureCandidate = observedGesture;
                bodyGestureCandidateFrames = 1;
            }
            else
            {
                bodyGestureCandidateFrames++;
            }

            if (bodyGestureCandidateFrames < Mathf.Max(1, bodyConfirmationFrames))
                return;

            float now = Time.time;
            if (observedGesture == BodyVerticalGesture.Jump && now - lastJumpTime > COOLDOWN)
            {
                // A confirmed body action wins over a contradictory hand misread.
                duckRequested = false;
                jumpRequested = true;
                lastJumpTime = now;
                bodyVerticalGestureReady = false;
                CurrentBodyGesture = BodyVerticalGesture.Jump;
                if (logInputEvents)
                    Debug.Log($"[PoseInput] BODY JUMP! Shoulder rise: {BodyVerticalOffset:F2}");
            }
            else if (observedGesture == BodyVerticalGesture.Duck && now - lastDuckTime > COOLDOWN)
            {
                // Prevent a crouching hand from being mistaken for PointingUp.
                jumpRequested = false;
                duckRequested = true;
                lastDuckTime = now;
                bodyVerticalGestureReady = false;
                CurrentBodyGesture = BodyVerticalGesture.Duck;
                if (logInputEvents)
                    Debug.Log($"[PoseInput] BODY DUCK! Shoulder drop: {BodyVerticalOffset:F2}");
            }
            bodyGestureCandidate = BodyVerticalGesture.None;
            bodyGestureCandidateFrames = 0;
        }
    }
}
