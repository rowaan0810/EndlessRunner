// HandGestureManager.cs — Detects hand gestures using MediaPipe Hand Landmarker.
// Detects OPEN PALM (fingers extended) and FIST (fingers curled) for game input.
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
        PointingUp
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
    /// Manages webcam + MediaPipe Hand Landmarker.
    /// Detects open palm vs fist gestures for game input.
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
        public float CurrentTimestamp { get; private set; }

        public bool IsRunning { get; private set; }
        public bool HandDetected => CurrentHands.Count > 0;
        public WebCamTexture WebcamTexture { get; private set; }
        public string StatusMessage { get; private set; } = "Not started";

        // Raw landmark data for HUD drawing
        public List<NormalizedLandmarks> RawLandmarks { get; private set; }
        public List<Classifications> RawHandedness { get; private set; }

        // MediaPipe internals
        private GestureRecognizer gestureRecognizer;
        private Texture2D inputTexture;
        private GestureRecognizerResult latestResult;
        private bool isInitialized;
        private bool isStarting;
        private bool ownsWebcam;
        private long lastTimestampMs = -1;
        private float nextInferenceTime;
        private uint lastSharedFrameId;

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
            CurrentHands.Clear();
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

            if (inputTexture != null)
            {
                Destroy(inputTexture);
                inputTexture = null;
            }
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

            inputTexture = new Texture2D(WebcamTexture.width, WebcamTexture.height, TextureFormat.RGBA32, false);
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
            if (!ownsWebcam && poseManager != null && poseManager.SharedInputTexture != null)
            {
                if (poseManager.FrameId == lastSharedFrameId) return;
                lastSharedFrameId = poseManager.FrameId;
                sourceTexture = poseManager.SharedInputTexture;
            }
            else
            {
                if (inputTexture == null) return;
                inputTexture.SetPixels32(WebcamTexture.GetPixels32());
                inputTexture.Apply();
                sourceTexture = inputTexture;
            }

            long timestampMs = (long)(Time.realtimeSinceStartup * 1000);
            if (timestampMs <= lastTimestampMs) timestampMs = lastTimestampMs + 1;
            lastTimestampMs = timestampMs;

            var imageProcessingOptions = new Mediapipe.Tasks.Vision.Core.ImageProcessingOptions(rotationDegrees: 0);

            using (var image = new Mediapipe.Image(sourceTexture))
            {
                try
                {
                    if (gestureRecognizer.TryRecognizeForVideo(image, timestampMs, imageProcessingOptions, ref latestResult))
                    {
                        ProcessResult(timestampMs);
                    }
                    else
                    {
                        CurrentHands.Clear();
                        RawLandmarks = null;
                        RawHandedness = null;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"HandGestureManager: Detection error: {e.Message}");
                }
            }
        }

        private void ProcessResult(long timestampMs)
        {
            CurrentHands.Clear();
            RawLandmarks = latestResult.handLandmarks;
            RawHandedness = latestResult.handedness;

            if (latestResult.handLandmarks == null || latestResult.handLandmarks.Count == 0)
                return;

            CurrentTimestamp = timestampMs;

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

                HandGesture gesture = HandGesture.None;
                
                // Map native categories to our enum
                string catLower = gestureCategory.ToLower();
                if (catLower.Contains("pointing_up"))
                    gesture = HandGesture.PointingUp;
                else if (catLower.Contains("thumb_up") || catLower.Contains("thumb_down"))
                    gesture = HandGesture.ThumbsUp;
                else if (catLower.Contains("open_palm") || catLower.Contains("paper") || catLower.Contains("five"))
                    gesture = HandGesture.OpenPalm;
                else if (catLower.Contains("closed_fist") || catLower.Contains("rock"))
                    gesture = HandGesture.Fist;

                // MediaPipe's ML model often misclassifies "Index Pointing Up" as "Thumb_Down" or "None".
                // We run a fallback heuristic to override it if the index finger is clearly extended while others are not.
                float wristX = landmarks[0].x, wristY = landmarks[0].y;
                int[] tips = { 8, 12, 16, 20 }; // Index, Middle, Ring, Pinky
                int[] pips = { 6, 10, 14, 18 };
                int extendedFingers = 0;
                bool isIndexExtended = false;

                for (int j = 0; j < 4; j++)
                {
                    float tipDist = Dist(landmarks[tips[j]].x, landmarks[tips[j]].y, wristX, wristY);
                    float pipDist = Dist(landmarks[pips[j]].x, landmarks[pips[j]].y, wristX, wristY);
                    bool extended = tipDist > pipDist * 1.12f;
                    if (extended) extendedFingers++;
                    if (j == 0 && extended) isIndexExtended = true; // Index finger
                }

                float indexDx = landmarks[8].x - wristX;
                float indexDy = landmarks[8].y - wristY;
                bool indexMostlyVertical = Mathf.Abs(indexDy) > Mathf.Abs(indexDx) * 1.2f;

                // Reject unstable native PointingUp classifications before mapping.
                if (gesture == HandGesture.PointingUp &&
                    (!isIndexExtended || extendedFingers > 2 || !indexMostlyVertical))
                {
                    gesture = HandGesture.None;
                }

                if ((gesture == HandGesture.None || gesture == HandGesture.ThumbsUp) &&
                    isIndexExtended && extendedFingers == 1 && indexMostlyVertical)
                {
                    gesture = HandGesture.PointingUp;
                }
                else if (extendedFingers >= 3)
                {
                    // Geometry wins over the classifier here. The stock gesture model
                    // can label an open palm as Thumb_Up depending on camera angle.
                    gesture = HandGesture.OpenPalm;
                }
                else if (gesture == HandGesture.None && extendedFingers == 0)
                {
                    gesture = HandGesture.Fist;
                }

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

        private static float Dist(float x1, float y1, float x2, float y2)
        {
            float dx = x1 - x2, dy = y1 - y2;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        private void OnDestroy()
        {
            StopDetection();
            if (Instance == this) Instance = null;
        }
    }
}
