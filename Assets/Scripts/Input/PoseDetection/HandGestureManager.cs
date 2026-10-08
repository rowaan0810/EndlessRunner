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

namespace EndlessRunner.Input.PoseDetection
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
        private long lastTimestampMs = -1;

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
            if (IsRunning) return;
            StatusMessage = "Starting hand detection...";
            StartCoroutine(InitializePipeline());
        }

        public void StopDetection()
        {
            IsRunning = false;
            CurrentHands.Clear();
            StatusMessage = "Stopped";

            if (WebcamTexture != null && WebcamTexture.isPlaying)
                WebcamTexture.Stop();

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

            // Start webcam
            StatusMessage = "Starting webcam...";
            WebCamDevice[] devices = WebCamTexture.devices;
            if (devices.Length == 0)
            {
                StatusMessage = "ERROR: No webcam found!";
                Debug.LogError("HandGestureManager: No webcam found!");
                yield break;
            }

            // Check if MediaPipeManager already has a webcam running — reuse it
            var existingMPM = MediaPipeManager.Instance;
            if (existingMPM != null && existingMPM.WebcamTexture != null && existingMPM.WebcamTexture.isPlaying)
            {
                WebcamTexture = existingMPM.WebcamTexture;
                Debug.Log("HandGestureManager: Reusing existing webcam from MediaPipeManager.");
            }
            else
            {
                WebcamTexture = new WebCamTexture(devices[0].name, preferredWidth, preferredHeight, preferredFps);
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
                    yield break;
                }
            }

            inputTexture = new Texture2D(WebcamTexture.width, WebcamTexture.height, TextureFormat.RGBA32, false);
            IsRunning = true;
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

#if UNITY_EDITOR
            AssetLoader.Provide(new LocalResourceManager());
#else
            AssetLoader.Provide(new StreamingAssetsResourceManager());
#endif

            isInitialized = true;
            Debug.Log("HandGestureManager: Bootstrap complete.");
            yield return null;
        }

        private void Update()
        {
            if (!IsRunning || gestureRecognizer == null || WebcamTexture == null || !WebcamTexture.didUpdateThisFrame)
                return;

            inputTexture.SetPixels32(WebcamTexture.GetPixels32());
            inputTexture.Apply();

            var image = new Mediapipe.Image(inputTexture);

            long timestampMs = (long)(Time.realtimeSinceStartup * 1000);
            if (timestampMs <= lastTimestampMs) timestampMs = lastTimestampMs + 1;
            lastTimestampMs = timestampMs;

            var imageProcessingOptions = new Mediapipe.Tasks.Vision.Core.ImageProcessingOptions(rotationDegrees: 0);

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
                    bool extended = tipDist > pipDist * 1.05f;
                    if (extended) extendedFingers++;
                    if (j == 0 && extended) isIndexExtended = true; // Index finger
                }

                if (isIndexExtended && extendedFingers == 1)
                {
                    gesture = HandGesture.PointingUp;
                }
                else if (gesture == HandGesture.None)
                {
                    if (extendedFingers >= 3)
                        gesture = HandGesture.OpenPalm;
                    else if (extendedFingers == 0)
                        gesture = HandGesture.Fist;
                }

                // Debug log the raw category so we can see what the model is actually outputting
                Debug.Log($"[HandGestureManager] Raw Gesture: {gestureCategory} -> Mapped: {gesture} (Ext Fingers: {extendedFingers})");

                CurrentHands.Add(new HandGestureResult
                {
                    detected = true,
                    gesture = gesture,
                    handedness = handLabel,
                    handCenterX = cx,
                    handCenterY = cy,
                    fingersExtended = 0, // No longer manually counted
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
        }
    }
}
