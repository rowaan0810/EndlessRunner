// HandGestureManager.cs — Classifies individual hands for gameplay input.
//
// Hand Landmarks (21 points per hand):
//   0: WRIST
//   1-4: THUMB (CMC, MCP, IP, TIP)
//   5-8: INDEX (MCP, PIP, DIP, TIP)  
//   9-12: MIDDLE (MCP, PIP, DIP, TIP)
//   13-16: RING (MCP, PIP, DIP, TIP)
//   17-20: PINKY (MCP, PIP, DIP, TIP)

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mediapipe;
using Mediapipe.Tasks.Vision.GestureRecognizer;
using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Unity;
using NLandmark = Mediapipe.Tasks.Components.Containers.NormalizedLandmark;
using Mediapipe.Unity.Sample;

namespace ActiveMotion3D.EndlessRunner.Input.PoseDetection
{
    public enum HandGesture
    {
        None,
        OpenPalm,
        ThumbsUp,
        Fist,
        PointingUp,
        PointingDown,
        Victory,
        BothPalms,
        ThumbsDown
    }

    public struct HandGestureResult
    {
        public bool detected;
        public HandGesture gesture;
        public string handedness; // "Left" or "Right"
        public float handCenterX; // 0-1
        public float handCenterY;
        public int fingersExtended; // count of extended fingers (0-5)
        public string rawGestureCategory;
    }

    /// <summary>
    /// Manages webcam capture and MediaPipe hand gesture recognition.
    /// </summary>
    [DefaultExecutionOrder(-190)]
    public class HandGestureManager : MonoBehaviour
    {
        public static HandGestureManager Instance { get; private set; }

        [Header("Detection Settings")]
        [SerializeField] private float minDetectionConfidence = 0.5f;
        [SerializeField] private float minTrackingConfidence = 0.5f;
        [SerializeField] private float minPresenceConfidence = 0.5f;

        [Header("Webcam Settings")]
        [SerializeField] private int preferredWidth = 640;
        [SerializeField] private int preferredHeight = 480;
        [SerializeField] private int preferredFps = 30;
        [SerializeField, Range(8, 30)] private int maxInferenceFps = 20;
        [SerializeField] private bool logGestureDebug;

        /// <summary>Latest gesture results (up to 2 hands).</summary>
        public List<HandGestureResult> CurrentHands { get; private set; } = new List<HandGestureResult>();

        /// <summary>Timestamp of latest detection in ms.</summary>
        public long CurrentTimestamp { get; private set; }

        public bool IsRunning { get; private set; }
        public bool HandDetected => CurrentHands.Count > 0;
        public WebCamTexture WebcamTexture { get; private set; }
        public Texture2D PreviewTexture { get; private set; }
        public string StatusMessage { get; private set; } = "Not started";

        // Raw landmark data for HUD drawing
        public List<NormalizedLandmarks> RawLandmarks { get; private set; }
        public List<Classifications> RawHandedness { get; private set; }

        // MediaPipe internals
        private GestureRecognizer gestureRecognizer;
        private readonly WebcamFrame webcamFrame = new WebcamFrame();
        private GestureRecognizerResult latestResult;
        private bool isInitialized;
        private bool isStarting;
        private bool ownsWebcam;
        private long lastTimestampMs = -1;
        private float nextInferenceTime;
        private uint lastSharedFrameId;
        private static readonly int[] FingerTips = { 8, 12, 16, 20 };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void StartDetection()
        {
            if (IsRunning || isStarting) return;
            isStarting = true;
            StatusMessage = "Starting hand detection...";
            StartCoroutine(InitializePipeline());
        }

        public void StopDetection()
        {
            StopAllCoroutines();
            isStarting = false;
            IsRunning = false;
            ClearResults();
            PreviewTexture = null;
            StatusMessage = "Stopped";

            if (ownsWebcam && WebcamTexture != null)
            {
                WebCamTexture textureToRelease = WebcamTexture;
                WebcamTexture = null;
                if (textureToRelease.isPlaying)
                    textureToRelease.Stop();
                Destroy(textureToRelease);
            }
            WebcamTexture = null;
            ownsWebcam = false;

            if (gestureRecognizer != null)
            {
                ((IDisposable)gestureRecognizer).Dispose();
                gestureRecognizer = null;
            }

            webcamFrame.Dispose();
        }

        private IEnumerator InitializePipeline()
        {
            Debug.Log("HandGestureManager: Initializing...");
            StatusMessage = "Initializing MediaPipe...";

            // Bootstrap MediaPipe
            if (!isInitialized)
            {
                yield return BootstrapMediaPipe();
            }

            // Load model
            StatusMessage = "Loading gesture model...";
            var modelPath = "gesture_recognizer.bytes";
            yield return AssetLoader.PrepareAssetAsync(modelPath);
            Debug.Log("HandGestureManager: Model loaded.");

            // Create gesture recognizer
            StatusMessage = "Creating gesture recognizer...";
            var baseOptions = new Mediapipe.Tasks.Core.BaseOptions(
                Mediapipe.Tasks.Core.BaseOptions.Delegate.CPU,
                modelAssetPath: modelPath);

            var options = new GestureRecognizerOptions(
                baseOptions,
                runningMode: Mediapipe.Tasks.Vision.Core.RunningMode.VIDEO,
                numHands: 2,
                minHandDetectionConfidence: minDetectionConfidence,
                minHandPresenceConfidence: minPresenceConfidence,
                minTrackingConfidence: minTrackingConfidence
            );

            gestureRecognizer = GestureRecognizer.CreateFromOptions(options);
            latestResult = GestureRecognizerResult.Alloc(2);

            // Start webcam using the original project behavior.
            WebCamDevice[] devices = WebCamTexture.devices;
            if (devices.Length == 0)
            {
                StatusMessage = "ERROR: No webcam found!";
                Debug.LogError("HandGestureManager: No webcam found!");
                isStarting = false;
                yield break;
            }

            StatusMessage = "Starting webcam...";
            var existingMPM = MediaPipeManager.Instance;
            if (existingMPM != null && existingMPM.WebcamTexture != null && existingMPM.WebcamTexture.isPlaying)
            {
                ownsWebcam = false;
                WebcamTexture = existingMPM.WebcamTexture;
                Debug.Log("HandGestureManager: Reusing existing webcam from MediaPipeManager.");
            }
            else
            {
                WebcamTexture = new WebCamTexture(devices[0].name, preferredWidth, preferredHeight, preferredFps);
                ownsWebcam = true;
                WebcamTexture.Play();

                int maxWait = 150;
                while (!WebcamTexture.didUpdateThisFrame && maxWait > 0)
                {
                    maxWait--;
                    yield return null;
                }

                if (WebcamTexture.width <= 16)
                {
                    StatusMessage = "ERROR: Webcam failed!";
                    Debug.LogError("HandGestureManager: Webcam failed to start!");
                    WebCamTexture failedTexture = WebcamTexture;
                    WebcamTexture = null;
                    if (failedTexture.isPlaying)
                        failedTexture.Stop();
                    Destroy(failedTexture);
                    ownsWebcam = false;
                    isStarting = false;
                    yield break;
                }
            }

            IsRunning = true;
            isStarting = false;
            StatusMessage = "Running";
            Debug.Log($"HandGestureManager: Ready! Webcam: {WebcamTexture.width}x{WebcamTexture.height}");
        }

        private IEnumerator BootstrapMediaPipe()
        {
            Protobuf.SetLogHandler(Protobuf.DefaultLogHandler);

            if (!MediaPipeManager.IsGlogInitialized)
            {
                Glog.Initialize("MediaPipeUnityPlugin");
                MediaPipeManager.IsGlogInitialized = true;
            }

            AssetLoader.Provide(new StreamingAssetsResourceManager("MediaPipe"));

            isInitialized = true;
            Debug.Log("HandGestureManager: Bootstrap complete.");
            yield return null;
        }

        private void Update()
        {
            if (!IsRunning || gestureRecognizer == null || WebcamTexture == null || !WebcamTexture.didUpdateThisFrame)
                return;

            float now = Time.realtimeSinceStartup;
            if (now < nextInferenceTime) return;
            nextInferenceTime = now + 1f / Mathf.Max(1, maxInferenceFps);

            Texture2D sourceTexture;
            var poseManager = MediaPipeManager.Instance;
            if (poseManager != null && poseManager.WebcamTexture == WebcamTexture &&
                poseManager.SharedInputTexture != null)
            {
                if (poseManager.FrameId == lastSharedFrameId) return;
                lastSharedFrameId = poseManager.FrameId;
                sourceTexture = poseManager.SharedInputTexture;
            }
            else
            {
                webcamFrame.Capture(WebcamTexture);
                sourceTexture = webcamFrame.Texture;
            }
            PreviewTexture = sourceTexture;

            long timestampMs = (long)(Time.realtimeSinceStartupAsDouble * 1000);
            if (timestampMs <= lastTimestampMs) timestampMs = lastTimestampMs + 1;
            lastTimestampMs = timestampMs;

            using (var image = new Mediapipe.Image(sourceTexture))
            {
                try
                {
                    if (gestureRecognizer.TryRecognizeForVideo(image, timestampMs, null, ref latestResult))
                    {
                        ProcessResult(timestampMs);
                    }
                    else
                    {
                        ClearResults();
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"HandGestureManager: Detection error: {e.Message}");
                    ClearResults();
                }
            }
        }

        private void ClearResults()
        {
            CurrentHands.Clear();
            RawLandmarks = null;
            RawHandedness = null;
        }

        private void ProcessResult(long timestampMs)
        {
            CurrentHands.Clear();
            RawLandmarks = latestResult.handLandmarks;
            RawHandedness = latestResult.handedness;
            CurrentTimestamp = timestampMs;

            if (latestResult.handLandmarks == null || latestResult.handLandmarks.Count == 0)
                return;

            float aspect = (float)PreviewTexture.width / PreviewTexture.height;

            for (int h = 0; h < latestResult.handLandmarks.Count; h++)
            {
                var landmarks = latestResult.handLandmarks[h].landmarks;
                if (landmarks == null || landmarks.Count < 21) continue;

                // Determine handedness
                string handLabel = "Unknown";
                if (latestResult.handedness != null && h < latestResult.handedness.Count)
                {
                    var cats = latestResult.handedness[h].categories;
                    if (cats != null && cats.Count > 0)
                        handLabel = cats[0].categoryName;
                }

                // Compute hand center
                float cx = 0, cy = 0;
                for (int i = 0; i < landmarks.Count; i++)
                {
                    cx += landmarks[i].x;
                    cy += landmarks[i].y;
                }
                cx /= landmarks.Count;
                cy /= landmarks.Count;

                // Use the same adjustable view boundary as body tracking. A range
                // above 1 keeps partially off-frame hands usable near the edges.
                var detectionView = MediaPipeManager.Instance;
                if (detectionView != null && !detectionView.IsInsideDetectionView(cx, cy))
                    continue;

                // Determine gesture natively from model
                string gestureCategory = "None";
                if (latestResult.gestures != null && h < latestResult.gestures.Count)
                {
                    var gCats = latestResult.gestures[h].categories;
                    if (gCats != null && gCats.Count > 0)
                    {
                        // The model returns the top gesture category
                        gestureCategory = string.IsNullOrEmpty(gCats[0].categoryName) ? gCats[0].displayName : gCats[0].categoryName;
                        if (string.IsNullOrEmpty(gestureCategory)) gestureCategory = "None";
                    }
                }

                HandGesture gesture = ClassifyHand(landmarks, aspect, gestureCategory, out int extendedFingers);

                // Debug log the raw category so we can see what the model is actually outputting
                if (logGestureDebug)
                    Debug.Log($"[HandGestureManager] Raw Gesture: {gestureCategory} -> Mapped: {gesture} (Ext Fingers: {extendedFingers})");

                CurrentHands.Add(new HandGestureResult
                {
                    detected = true,
                    gesture = gesture,
                    handedness = handLabel,
                    handCenterX = cx,
                    handCenterY = cy,
                    fingersExtended = extendedFingers,
                    rawGestureCategory = gestureCategory
                });
            }
        }

        internal static HandGesture ClassifyHand(List<NLandmark> landmarks, float aspect, string category,
            out int extendedFingers)
        {
            Vector2 wrist = ImagePoint(landmarks[0], aspect);
            bool indexExtended = false;
            bool middleExtended = false;
            extendedFingers = 0;
            foreach (int tip in FingerTips)
            {
                Vector2 tipOffset = ImagePoint(landmarks[tip], aspect) - wrist;
                Vector2 pipOffset = ImagePoint(landmarks[tip - 2], aspect) - wrist;
                if (tipOffset.sqrMagnitude <= pipOffset.sqrMagnitude * (1.12f * 1.12f)) continue;

                extendedFingers++;
                if (tip == 8) indexExtended = true;
                if (tip == 12) middleExtended = true;
            }

            if (indexExtended && extendedFingers == 1)
            {
                Vector2 direction = ImagePoint(landmarks[8], aspect) - ImagePoint(landmarks[5], aspect);
                // Top-left image origin: smaller Y means up.
                if (Mathf.Abs(direction.y) > Mathf.Abs(direction.x) * 1.2f)
                    return direction.y < 0f ? HandGesture.PointingUp : HandGesture.PointingDown;
            }

            if (indexExtended && middleExtended && extendedFingers == 2)
            {
                Vector2 tips = ImagePoint(landmarks[8], aspect) - ImagePoint(landmarks[12], aspect);
                Vector2 knuckles = ImagePoint(landmarks[5], aspect) - ImagePoint(landmarks[9], aspect);
                if (tips.sqrMagnitude > knuckles.sqrMagnitude * (1.15f * 1.15f) ||
                    string.Equals(category, "Victory", StringComparison.OrdinalIgnoreCase))
                    return HandGesture.Victory;
            }

            if (extendedFingers == 4 ||
                (extendedFingers == 3 && string.Equals(category, "Open_Palm", StringComparison.OrdinalIgnoreCase)))
                return HandGesture.OpenPalm;

            if (string.Equals(category, "Thumb_Down", StringComparison.OrdinalIgnoreCase))
                return HandGesture.ThumbsDown;

            if (string.Equals(category, "Thumb_Up", StringComparison.OrdinalIgnoreCase))
                return HandGesture.ThumbsUp;

            return extendedFingers == 0 ? HandGesture.Fist : HandGesture.None;
        }

        private static Vector2 ImagePoint(NLandmark landmark, float aspect)
        {
            return new Vector2(landmark.x * aspect, landmark.y);
        }

        private void OnDestroy()
        {
            StopDetection();
            if (Instance == this) Instance = null;
        }
    }
}
