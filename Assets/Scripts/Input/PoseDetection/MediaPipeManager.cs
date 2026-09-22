// MediaPipeManager.cs — Initializes webcam + MediaPipe Pose Landmarker.
// Runs pose detection per frame and outputs PoseLandmark[] for PoseController.
// Self-contained: handles bootstrapping, model loading, and webcam capture.

using System;
using System.Collections;
using UnityEngine;
using Mediapipe;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using Mediapipe.Unity;
using Mediapipe.Unity.Sample;

namespace EndlessRunner.Input.PoseDetection
{
    /// <summary>
    /// Manages webcam capture and MediaPipe pose landmark detection.
    /// Outputs PoseLandmark[] each frame for consumption by PoseInput/EasyModeInput.
    /// Self-bootstraps MediaPipe (no separate Bootstrap component needed).
    /// </summary>
    public class MediaPipeManager : MonoBehaviour
    {
        public static MediaPipeManager Instance { get; private set; }

        [Header("Detection Settings")]
        [SerializeField] private float minDetectionConfidence = 0.5f;
        [SerializeField] private float minTrackingConfidence = 0.5f;
        [SerializeField] private float minPresenceConfidence = 0.5f;

        [Header("Webcam Settings")]
        [SerializeField] private int preferredWidth = 640;
        [SerializeField] private int preferredHeight = 480;
        [SerializeField] private int preferredFps = 30;

        /// <summary>Latest landmarks as our PoseLandmark struct array (33 points).</summary>
        public PoseLandmark[] CurrentLandmarks { get; private set; }

        /// <summary>Timestamp of the latest detection in milliseconds.</summary>
        public float CurrentTimestamp { get; private set; }

        /// <summary>Whether the webcam is active and detection is running.</summary>
        public bool IsRunning { get; private set; }

        /// <summary>Whether a person was detected in the current frame.</summary>
        public bool PersonDetected { get; private set; }

        /// <summary>The raw webcam texture (for WebcamHUD display).</summary>
        public WebCamTexture WebcamTexture { get; private set; }

        /// <summary>Status message for UI display.</summary>
        public string StatusMessage { get; private set; } = "Not started";

        // MediaPipe internals
        private PoseLandmarker poseLandmarker;
        private Texture2D inputTexture;
        private PoseLandmarkerResult latestResult;
        private bool isInitialized;
        private bool isGlogInitialized;
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

        /// <summary>
        /// Start the webcam and pose detection pipeline.
        /// </summary>
        public void StartDetection()
        {
            if (IsRunning) return;
            StatusMessage = "Starting...";
            StartCoroutine(InitializePipeline());
        }

        /// <summary>
        /// Stop detection and release resources.
        /// </summary>
        public void StopDetection()
        {
            IsRunning = false;
            PersonDetected = false;
            StatusMessage = "Stopped";

            if (WebcamTexture != null && WebcamTexture.isPlaying)
            {
                WebcamTexture.Stop();
            }

            if (poseLandmarker != null)
            {
                ((IDisposable)poseLandmarker).Dispose();
                poseLandmarker = null;
            }

            if (inputTexture != null)
            {
                Destroy(inputTexture);
                inputTexture = null;
            }
        }

        private IEnumerator InitializePipeline()
        {
            Debug.Log("MediaPipeManager: Initializing pose detection...");
            StatusMessage = "Initializing MediaPipe...";

            // Step 1: Bootstrap MediaPipe (if not already done)
            if (!isInitialized)
            {
                yield return BootstrapMediaPipe();
            }

            // Step 2: Prepare the model
            StatusMessage = "Loading pose model...";
            var modelPath = "pose_landmarker_lite.bytes";
            yield return AssetLoader.PrepareAssetAsync(modelPath);
            Debug.Log("MediaPipeManager: Model loaded.");

            // Step 3: Create the pose landmarker
            StatusMessage = "Creating pose landmarker...";
            var baseOptions = new Mediapipe.Tasks.Core.BaseOptions(
                Mediapipe.Tasks.Core.BaseOptions.Delegate.CPU,
                modelAssetPath: modelPath);

            var options = new PoseLandmarkerOptions(
                baseOptions,
                runningMode: Mediapipe.Tasks.Vision.Core.RunningMode.VIDEO,
                numPoses: 1,
                minPoseDetectionConfidence: minDetectionConfidence,
                minPosePresenceConfidence: minPresenceConfidence,
                minTrackingConfidence: minTrackingConfidence
            );

            poseLandmarker = PoseLandmarker.CreateFromOptions(options);
            latestResult = PoseLandmarkerResult.Alloc(1);

            // Step 4: Start webcam
            StatusMessage = "Starting webcam...";
            WebCamDevice[] devices = WebCamTexture.devices;
            if (devices.Length == 0)
            {
                StatusMessage = "ERROR: No webcam found!";
                Debug.LogError("MediaPipeManager: No webcam found!");
                yield break;
            }

            Debug.Log($"MediaPipeManager: Using webcam: {devices[0].name}");
            WebcamTexture = new WebCamTexture(devices[0].name, preferredWidth, preferredHeight, preferredFps);
            WebcamTexture.Play();

            // Wait for webcam to be ready
            int maxWait = 150;
            while (!WebcamTexture.didUpdateThisFrame && maxWait > 0)
            {
                maxWait--;
                yield return null;
            }

            if (WebcamTexture.width <= 16)
            {
                StatusMessage = "ERROR: Webcam failed to start!";
                Debug.LogError("MediaPipeManager: Webcam failed to start!");
                yield break;
            }

            inputTexture = new Texture2D(WebcamTexture.width, WebcamTexture.height, TextureFormat.RGBA32, false);
            IsRunning = true;
            StatusMessage = "Running";

            Debug.Log($"MediaPipeManager: Ready! Webcam: {WebcamTexture.width}x{WebcamTexture.height}");
        }

        /// <summary>
        /// Minimal MediaPipe bootstrap — initializes Glog, Protobuf, and the AssetLoader.
        /// </summary>
        private IEnumerator BootstrapMediaPipe()
        {
            Protobuf.SetLogHandler(Protobuf.DefaultLogHandler);

            if (!isGlogInitialized)
            {
                Glog.Initialize("MediaPipeUnityPlugin");
                isGlogInitialized = true;
            }

#if UNITY_EDITOR
            AssetLoader.Provide(new LocalResourceManager());
#else
            AssetLoader.Provide(new StreamingAssetsResourceManager());
#endif

            isInitialized = true;
            Debug.Log("MediaPipeManager: Bootstrap complete.");
            yield return null;
        }

        private void Update()
        {
            if (!IsRunning || poseLandmarker == null || WebcamTexture == null || !WebcamTexture.didUpdateThisFrame)
                return;

            // Copy webcam to Texture2D
            inputTexture.SetPixels32(WebcamTexture.GetPixels32());
            inputTexture.Apply();

            // Build MediaPipe Image directly from Texture2D
            var image = new Mediapipe.Image(inputTexture);

            // Ensure monotonically increasing timestamps
            long timestampMs = (long)(Time.realtimeSinceStartup * 1000);
            if (timestampMs <= lastTimestampMs) timestampMs = lastTimestampMs + 1;
            lastTimestampMs = timestampMs;

            var imageProcessingOptions = new Mediapipe.Tasks.Vision.Core.ImageProcessingOptions(rotationDegrees: 0);

            try
            {
                if (poseLandmarker.TryDetectForVideo(image, timestampMs, imageProcessingOptions, ref latestResult))
                {
                    ProcessResult(timestampMs);
                }
                else
                {
                    PersonDetected = false;
                    CurrentLandmarks = null;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"MediaPipeManager: Detection error: {e.Message}");
                PersonDetected = false;
            }
        }

        private void ProcessResult(long timestampMs)
        {
            if (latestResult.poseLandmarks == null || latestResult.poseLandmarks.Count == 0)
            {
                PersonDetected = false;
                CurrentLandmarks = null;
                return;
            }

            var mpLandmarks = latestResult.poseLandmarks[0].landmarks;
            if (mpLandmarks == null || mpLandmarks.Count < 33)
            {
                PersonDetected = false;
                CurrentLandmarks = null;
                return;
            }

            var output = new PoseLandmark[33];
            for (int i = 0; i < 33; i++)
            {
                var lm = mpLandmarks[i];
                output[i] = new PoseLandmark
                {
                    x = lm.x,
                    y = lm.y,
                    z = lm.z,
                    visibility = lm.visibility ?? 0f
                };
            }

            CurrentLandmarks = output;
            CurrentTimestamp = timestampMs;
            PersonDetected = true;
        }

        private void OnDestroy()
        {
            StopDetection();

            if (isGlogInitialized)
            {
                Glog.Shutdown();
                isGlogInitialized = false;
            }
            Protobuf.ResetLogHandler();
        }
    }
}
