using System.Collections;
using ActiveMotion3D.EndlessRunner.Core;
using ActiveMotion3D.EndlessRunner.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ActiveMotion3D.EndlessRunner.Input.PoseDetection
{
    /// <summary>Scene-independent camera gestures for quitting and pausing.</summary>
    [DefaultExecutionOrder(-150)]
    public sealed class GlobalGestureController : MonoBehaviour
    {
        public static GlobalGestureController Instance { get; private set; }
        public static bool PauseGestureInProgress => Instance != null && Instance.pauseGestureInProgress;

        [Header("Scenes")]
        [SerializeField] private string pauseSceneName = "AM3D_Morioh_Full";

        [Header("Detection")]
        [SerializeField, Range(0f, 1f)] private float minimumLandmarkVisibility = 0.35f;
        [SerializeField, Range(0.01f, 0.3f)] private float minimumShoulderWidth = 0.08f;
        [SerializeField, Min(0.05f)] private float maximumFrameAgeSeconds = 0.35f;

        [Header("Crossed Arms To Exit")]
        [SerializeField, Min(0.1f)] private float exitHoldSeconds = 0.8f;
        [SerializeField, Range(0f, 0.5f)] private float crossSideMargin = 0.08f;
        [SerializeField, Range(0.1f, 3f)] private float crossMaximumOppositeShoulderDistance = 1.7f;
        [SerializeField, Range(0.1f, 3f)] private float crossMaximumVerticalOffset = 1.4f;

        [Header("Palms Together To Pause")]
        [SerializeField, Min(0.1f)] private float pauseHoldSeconds = 0.45f;
        [SerializeField, Range(0.1f, 1f)] private float palmsMaximumWristSeparation = 0.5f;
        [SerializeField, Range(0.1f, 2f)] private float palmsMaximumChestOffset = 0.8f;
        [SerializeField, Range(-1f, 1f)] private float palmsMinimumChestY = -0.25f;
        [SerializeField, Range(0f, 3f)] private float palmsMaximumChestY = 1.5f;
        [SerializeField, Range(0.1f, 2f)] private float palmsMaximumHandCenterSeparation = 0.65f;
        [SerializeField] private bool allowOneVisibleOpenPalm = true;

        private Coroutine detectionRoutine;
        private float lastPoseTimestamp = -1f;
        private float lastPoseFrameTime = -1f;
        private float crossSince = -1f;
        private float palmsSince = -1f;
        private bool pauseNeedsRelease;
        private bool pauseGestureInProgress;
        private bool exiting;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            GameObject prefab = Resources.Load<GameObject>("GlobalGestureController");
            if (prefab == null || prefab.GetComponent<GlobalGestureController>() == null)
            {
                Debug.LogError("GlobalGestureController prefab is missing from Resources.");
                return;
            }
            Instantiate(prefab);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += SceneLoaded;
        }

        private void Start() => BeginDetection();

        private void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            lastPoseTimestamp = -1f;
            crossSince = -1f;
            palmsSince = -1f;
            pauseNeedsRelease = false;
            pauseGestureInProgress = false;
            BeginDetection();
        }

        private void BeginDetection()
        {
            if (detectionRoutine != null) StopCoroutine(detectionRoutine);
            detectionRoutine = StartCoroutine(EnsureDetection());
        }

        private IEnumerator EnsureDetection()
        {
            // Give the destination scene's authored PoseDetector time to register.
            yield return null;
            MediaPipeManager pose = MediaPipeManager.Instance;
            HandGestureManager hands = HandGestureManager.Instance;
            if (pose == null || hands == null)
            {
                GameObject detector = new GameObject("GlobalGestureDetector");
                if (pose == null) pose = detector.AddComponent<MediaPipeManager>();
                if (hands == null) hands = detector.AddComponent<HandGestureManager>();
            }

            if (!pose.IsRunning) pose.StartDetection();
            while (!pose.IsRunning)
            {
                if (pose.StatusMessage.StartsWith("ERROR")) yield break;
                yield return null;
            }
            if (!hands.IsRunning) hands.StartDetection();
            detectionRoutine = null;
        }

        private void Update()
        {
            MediaPipeManager pose = MediaPipeManager.Instance;
            if (pose == null || !pose.IsRunning || !pose.PersonDetected ||
                pose.CurrentLandmarks == null || pose.CurrentLandmarks.Length < 33)
            {
                ResetCandidates();
                return;
            }

            float now = Time.unscaledTime;
            if (pose.CurrentTimestamp == lastPoseTimestamp)
            {
                if (lastPoseFrameTime >= 0f && now - lastPoseFrameTime > maximumFrameAgeSeconds)
                    ResetCandidates();
                return;
            }
            lastPoseTimestamp = pose.CurrentTimestamp;
            lastPoseFrameTime = now;
            PoseLandmark[] landmarks = pose.CurrentLandmarks;
            bool crossed = ArmsCrossed(landmarks);
            if (crossed)
            {
                if (crossSince < 0f) crossSince = now;
                if (!exiting && now - crossSince >= exitHoldSeconds) ExitGame();
            }
            else crossSince = -1f;

            bool palmsTogether = !crossed && PalmsTogether(landmarks, HandGestureManager.Instance, now);
            if (!palmsTogether)
            {
                palmsSince = -1f;
                pauseNeedsRelease = false;
                pauseGestureInProgress = false;
                return;
            }

            GameManager game = GameManager.Instance;
            bool canPause = SceneManager.GetActiveScene().name == pauseSceneName &&
                            game != null && game.State == GameState.Playing;
            pauseGestureInProgress = canPause;
            if (!canPause || pauseNeedsRelease) return;
            if (palmsSince < 0f) palmsSince = now;
            if (now - palmsSince < pauseHoldSeconds) return;

            PauseMenu pauseMenu = FindAnyObjectByType<PauseMenu>();
            if (pauseMenu == null)
            {
                Debug.LogWarning("Palms-together gesture found, but the scene has no PauseMenu.");
                pauseNeedsRelease = true;
                return;
            }
            pauseMenu.TogglePause();
            pauseNeedsRelease = true;
            pauseGestureInProgress = false;
        }

        private bool ArmsCrossed(PoseLandmark[] p)
        {
            PoseLandmark leftShoulder = p[11], rightShoulder = p[12];
            PoseLandmark leftWrist = p[15], rightWrist = p[16];
            if (!Visible(leftShoulder, rightShoulder, leftWrist, rightWrist)) return false;
            float width = Mathf.Abs(leftShoulder.x - rightShoulder.x);
            if (width < minimumShoulderWidth) return false;
            float middleX = (leftShoulder.x + rightShoulder.x) * 0.5f;
            float shoulderY = (leftShoulder.y + rightShoulder.y) * 0.5f;
            float leftToRight = Mathf.Sign(rightShoulder.x - leftShoulder.x);
            if ((leftWrist.x - middleX) * leftToRight < crossSideMargin * width ||
                (rightWrist.x - middleX) * leftToRight > -crossSideMargin * width)
                return false;
            if (Mathf.Abs(leftWrist.y - shoulderY) > crossMaximumVerticalOffset * width ||
                Mathf.Abs(rightWrist.y - shoulderY) > crossMaximumVerticalOffset * width)
                return false;
            return Vector2.Distance(new Vector2(leftWrist.x, leftWrist.y),
                       new Vector2(rightShoulder.x, rightShoulder.y)) <= crossMaximumOppositeShoulderDistance * width &&
                   Vector2.Distance(new Vector2(rightWrist.x, rightWrist.y),
                       new Vector2(leftShoulder.x, leftShoulder.y)) <= crossMaximumOppositeShoulderDistance * width;
        }

        private bool PalmsTogether(PoseLandmark[] p, HandGestureManager hands, float now)
        {
            if (hands == null || !hands.IsRunning || hands.CurrentHands.Count == 0 ||
                now - hands.CurrentTimestamp / 1000f > maximumFrameAgeSeconds)
                return false;
            PoseLandmark leftShoulder = p[11], rightShoulder = p[12];
            PoseLandmark leftWrist = p[15], rightWrist = p[16];
            if (!Visible(leftShoulder, rightShoulder, leftWrist, rightWrist)) return false;
            float width = Mathf.Abs(leftShoulder.x - rightShoulder.x);
            if (width < minimumShoulderWidth) return false;
            Vector2 wristMid = new Vector2((leftWrist.x + rightWrist.x) * 0.5f,
                                           (leftWrist.y + rightWrist.y) * 0.5f);
            float shoulderMidX = (leftShoulder.x + rightShoulder.x) * 0.5f;
            float shoulderMidY = (leftShoulder.y + rightShoulder.y) * 0.5f;
            if (Vector2.Distance(new Vector2(leftWrist.x, leftWrist.y),
                                 new Vector2(rightWrist.x, rightWrist.y)) > palmsMaximumWristSeparation * width ||
                Mathf.Abs(wristMid.x - shoulderMidX) > palmsMaximumChestOffset * width ||
                wristMid.y < shoulderMidY + palmsMinimumChestY * width ||
                wristMid.y > shoulderMidY + palmsMaximumChestY * width)
                return false;

            int palmCount = 0;
            for (int i = 0; i < hands.CurrentHands.Count; i++)
                if (hands.CurrentHands[i].gesture == HandGesture.OpenPalm)
                    palmCount++;
            if (palmCount < (allowOneVisibleOpenPalm ? 1 : 2)) return false;
            if (hands.CurrentHands.Count > 1)
            {
                if (palmCount < 2) return false;
                Vector2 first = new Vector2(hands.CurrentHands[0].handCenterX, hands.CurrentHands[0].handCenterY);
                Vector2 second = new Vector2(hands.CurrentHands[1].handCenterX, hands.CurrentHands[1].handCenterY);
                if (Vector2.Distance(first, second) > palmsMaximumHandCenterSeparation * width)
                    return false;
            }
            return true;
        }

        private bool Visible(params PoseLandmark[] landmarks)
        {
            foreach (PoseLandmark landmark in landmarks)
                if (landmark.visibility < minimumLandmarkVisibility) return false;
            return true;
        }

        private void ResetCandidates()
        {
            crossSince = -1f;
            palmsSince = -1f;
            pauseGestureInProgress = false;
        }

        private void ExitGame()
        {
            exiting = true;
            Time.timeScale = 1f;
            HandGestureManager.Instance?.StopDetection();
            MediaPipeManager.Instance?.StopDetection();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            SceneManager.sceneLoaded -= SceneLoaded;
            Instance = null;
        }
    }
}
