// HumanTetrisWall.cs — A full-width upper-body pose gate for the three-lane runner.

using System.Collections.Generic;
using UnityEngine;
using ActiveMotion3D.EndlessRunner.Core;
using ActiveMotion3D.EndlessRunner.Input;
using ActiveMotion3D.EndlessRunner.Input.PoseDetection;
using ActiveMotion3D.EndlessRunner.Player;

namespace ActiveMotion3D.EndlessRunner.Obstacles
{
    public enum HumanTetrisPose
    {
        BothArmsV,
        LeftArmHorizontal,
        RightArmHorizontal,
        BothArmsHorizontal,
        LeftArmRaised,
        RightArmRaised
    }

    /// <summary>
    /// A wall spanning all lanes. The player must occupy the highlighted lane and
    /// hold the displayed upper-body pose shortly before reaching the wall.
    /// </summary>
    public class HumanTetrisWall : MonoBehaviour
    {
        private const int LeftShoulder = 11;
        private const int RightShoulder = 12;
        private const int LeftElbow = 13;
        private const int RightElbow = 14;
        private const int LeftWrist = 15;
        private const int RightWrist = 16;
        private const int Nose = 0;
        private const int LeftHip = 23;
        private const int RightHip = 24;

        [Header("Recognition")]
        [Tooltip("Keep a recent valid result briefly so one dropped webcam frame at the wall does not cause failure.")]
        [SerializeField, Range(0.1f, 2f)] private float recognitionGraceSeconds = 0.85f;
        [Tooltip("Minimum confidence for the two shoulders. Hands may leave a narrow webcam frame.")]
        [SerializeField, Range(0f, 1f)] private float minimumVisibility = 0.15f;
        [Tooltip("Elbows remain the fallback joints when a wrist or hand is outside the camera frame.")]
        [SerializeField, Range(0f, 1f)] private float minimumElbowVisibility = 0.02f;
        [Tooltip("Below this confidence, wrist position is treated only as a hint and elbow direction becomes authoritative.")]
        [SerializeField, Range(0f, 1f)] private float minimumWristVisibility = 0.12f;
        [Tooltip("Allowed angle above or below a level shoulder-to-elbow line.")]
        [SerializeField, Range(10f, 45f)] private float horizontalToleranceDegrees = 30f;
        [Tooltip("Minimum upward angle for a raised arm. Coordinates may remain usable even when an off-screen elbow has low visibility.")]
        [SerializeField, Range(30f, 70f)] private float raisedMinimumDegrees = 40f;
        [SerializeField] private float despawnZ = -15f;

        private readonly List<Renderer> silhouetteRenderers = new List<Renderer>();
        private HumanTetrisPose requiredPose;
        private int requiredLane;
        private float lastSuccessfulMatchTime;
        private bool poseReady;
        private Material waitingMaterial;
        private Material readyMaterial;
        private TextMesh instructionText;
        private bool lastLeftRaised;
        private bool lastRightRaised;
        private bool lastLeftHorizontal;
        private bool lastRightHorizontal;
        private bool lastLeftDown;
        private bool lastRightDown;
        private float lastUpwardYSign = 1f;

        public HumanTetrisPose RequiredPose => requiredPose;
        public int RequiredLane => requiredLane;
        public bool PoseReady => poseReady;

        public void Setup(Vector3 position, int lane, HumanTetrisPose pose)
        {
            if (transform.childCount == 0)
                BuildVisuals();

            transform.position = position;
            requiredLane = Mathf.Clamp(lane, 0, 2);
            requiredPose = pose;
            lastSuccessfulMatchTime = -999f;
            poseReady = false;
            ConfigureSilhouette();
            SetSilhouetteReady(false);
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
                return;

            float speed = GameSpeed.Instance != null ? GameSpeed.Instance.Current : 10f;
            transform.Translate(Vector3.back * speed * Time.deltaTime, Space.World);

            // Evaluate from the moment the wall appears, not only when it is close.
            // Lane, horizontal movement, jumping and ducking are intentionally ignored;
            // this challenge checks only the requested upper-body pose.
            if (transform.position.z >= -1f)
            {
                bool poseMatches = IsRequiredPoseHeld();
                if (poseMatches)
                    lastSuccessfulMatchTime = Time.time;

                bool recentlyMatched = Time.time - lastSuccessfulMatchTime <= recognitionGraceSeconds;
                SetSilhouetteReady(poseMatches || recentlyMatched);
            }

            if (transform.position.z < despawnZ)
                gameObject.SetActive(false);
        }

        private void OnTriggerEnter(Collider other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null || GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
                return;

            bool poseMatchesNow = IsRequiredPoseHeld();
            bool recentlyMatched = Time.time - lastSuccessfulMatchTime <= recognitionGraceSeconds;
            bool passed = poseMatchesNow || recentlyMatched;
            if (passed)
            {
                Debug.Log($"Human Tetris passed: pose {requiredPose}");
                gameObject.SetActive(false);
                return;
            }

            PlayerCollision collision = player.GetComponent<PlayerCollision>();
            if (collision != null && collision.IsShielded)
            {
                collision.IsShielded = false;
                gameObject.SetActive(false);
                return;
            }

            Debug.Log($"Human Tetris failed: pose {requiredPose}, ready={poseReady}, " +
                      $"left[horizontal={lastLeftHorizontal}, raised={lastLeftRaised}], " +
                      $"right[horizontal={lastRightHorizontal}, raised={lastRightRaised}], " +
                      $"down[left={lastLeftDown}, right={lastRightDown}], upY={lastUpwardYSign}");
            GameManager.Instance.TriggerGameOver();
        }

        private bool IsRequiredPoseHeld()
        {
            lastLeftRaised = false;
            lastRightRaised = false;
            lastLeftHorizontal = false;
            lastRightHorizontal = false;
            lastLeftDown = false;
            lastRightDown = false;

            // Keyboard fallback keeps wall/lane testing possible without starting a webcam.
            if (InputManager.Instance != null && InputManager.Instance.CurrentMode == InputMode.Keyboard)
                return IsKeyboardTestPoseHeld();

            MediaPipeManager manager = MediaPipeManager.Instance;
            PoseLandmark[] lm = manager != null ? manager.CurrentLandmarks : null;
            if (manager == null || !manager.PersonDetected || lm == null || lm.Length < 33)
                return false;

            // A narrow webcam commonly crops hands and part of the forearms. Shoulders
            // are mandatory; each arm then uses whatever elbow/wrist evidence exists.
            if (lm[LeftShoulder].visibility < minimumVisibility ||
                lm[RightShoulder].visibility < minimumVisibility)
                return false;

            float shoulderWidth = Mathf.Max(Mathf.Abs(lm[LeftShoulder].x - lm[RightShoulder].x), 0.08f);
            float imageAspectCorrection = 0.75f;
            if (manager.WebcamTexture != null && manager.WebcamTexture.width > 0)
                imageAspectCorrection = manager.WebcamTexture.height / (float)manager.WebcamTexture.width;
            float upwardYSign = GetUpwardYSign(lm);
            bool leftElbowVisible = lm[LeftElbow].visibility >= minimumElbowVisibility;
            bool rightElbowVisible = lm[RightElbow].visibility >= minimumElbowVisibility;
            bool leftWristVisible = lm[LeftWrist].visibility >= minimumWristVisibility;
            bool rightWristVisible = lm[RightWrist].visibility >= minimumWristVisibility;
            bool leftRaised = IsArmRaised(lm, LeftShoulder, LeftElbow, LeftWrist,
                shoulderWidth, imageAspectCorrection, upwardYSign, raisedMinimumDegrees);
            bool rightRaised = IsArmRaised(lm, RightShoulder, RightElbow, RightWrist,
                shoulderWidth, imageAspectCorrection, upwardYSign, raisedMinimumDegrees);
            float leftOutwardDirection = Mathf.Sign(lm[LeftShoulder].x - lm[RightShoulder].x);
            if (Mathf.Approximately(leftOutwardDirection, 0f)) leftOutwardDirection = 1f;
            bool leftHorizontal = IsArmHorizontal(lm, LeftShoulder, LeftElbow, LeftWrist,
                shoulderWidth, leftOutwardDirection, leftElbowVisible, leftWristVisible,
                imageAspectCorrection, horizontalToleranceDegrees);
            bool rightHorizontal = IsArmHorizontal(lm, RightShoulder, RightElbow, RightWrist,
                shoulderWidth, -leftOutwardDirection, rightElbowVisible, rightWristVisible,
                imageAspectCorrection, horizontalToleranceDegrees);
            bool leftDown = IsArmDown(lm, LeftShoulder, LeftElbow, LeftWrist, shoulderWidth,
                imageAspectCorrection, upwardYSign, 0.08f, minimumWristVisibility);
            bool rightDown = IsArmDown(lm, RightShoulder, RightElbow, RightWrist, shoulderWidth,
                imageAspectCorrection, upwardYSign, 0.08f, minimumWristVisibility);

            lastLeftRaised = leftRaised;
            lastRightRaised = rightRaised;
            lastLeftHorizontal = leftHorizontal;
            lastRightHorizontal = rightHorizontal;
            lastLeftDown = leftDown;
            lastRightDown = rightDown;
            lastUpwardYSign = upwardYSign;

            return MatchesRequiredPose(requiredPose, leftRaised, rightRaised,
                leftHorizontal, rightHorizontal, leftDown, rightDown);
        }

        private static bool MatchesRequiredPose(HumanTetrisPose pose,
            bool mediaPipeLeftRaised, bool mediaPipeRightRaised,
            bool mediaPipeLeftHorizontal, bool mediaPipeRightHorizontal,
            bool mediaPipeLeftDown, bool mediaPipeRightDown)
        {
            // Match the screen-side silhouette, just like PunchArmInput: in this
            // webcam pipeline MediaPipe's right arm is the user's perceived left.
            // LeftArm* is drawn at negative X, so the user follows screen-left.
            bool leftRaised = mediaPipeRightRaised;
            bool rightRaised = mediaPipeLeftRaised;
            bool leftHorizontal = mediaPipeRightHorizontal;
            bool rightHorizontal = mediaPipeLeftHorizontal;
            bool leftDown = mediaPipeRightDown;
            bool rightDown = mediaPipeLeftDown;

            return pose switch
            {
                // Pose categories are intentionally exclusive. A T pose must never
                // pass as a V pose, and a two-arm action must never pass a one-arm gate.
                HumanTetrisPose.BothArmsV => leftRaised && rightRaised &&
                                             !leftHorizontal && !rightHorizontal,
                // Only the displayed side may match. The opposite arm must be
                // positively detected as lowered; missing data is not "down".
                HumanTetrisPose.LeftArmHorizontal => leftHorizontal && !rightHorizontal && rightDown,
                HumanTetrisPose.RightArmHorizontal => rightHorizontal && !leftHorizontal && leftDown,
                HumanTetrisPose.BothArmsHorizontal => leftHorizontal && rightHorizontal,
                HumanTetrisPose.LeftArmRaised => leftRaised && !leftHorizontal &&
                    !rightRaised && !rightHorizontal && rightDown,
                HumanTetrisPose.RightArmRaised => rightRaised && !rightHorizontal &&
                    !leftRaised && !leftHorizontal && leftDown,
                _ => false
            };
        }

        private static bool IsArmDown(PoseLandmark[] lm, int shoulder, int elbow, int wrist,
            float scale, float imageAspectCorrection, float upwardYSign, float minimumElbowVisibility,
            float minimumVisibleWrist)
        {
            float elbowDown = (lm[shoulder].y - lm[elbow].y) * upwardYSign * imageAspectCorrection;
            float elbowSide = Mathf.Abs(lm[elbow].x - lm[shoulder].x);
            float elbowAngle = Mathf.Atan2(Mathf.Max(0f, elbowDown), Mathf.Max(elbowSide, 0.0001f)) * Mathf.Rad2Deg;
            bool elbowShowsDown = lm[elbow].visibility >= minimumElbowVisibility &&
                                  elbowDown > scale * 0.04f && elbowAngle >= 25f;

            float wristDown = (lm[shoulder].y - lm[wrist].y) * upwardYSign * imageAspectCorrection;
            float wristSide = Mathf.Abs(lm[wrist].x - lm[shoulder].x);
            float wristAngle = Mathf.Atan2(Mathf.Max(0f, wristDown), Mathf.Max(wristSide, 0.0001f)) * Mathf.Rad2Deg;
            bool wristShowsDown = lm[wrist].visibility >= minimumVisibleWrist &&
                                  wristDown > scale * 0.12f && wristAngle >= 25f;
            return elbowShowsDown || wristShowsDown;
        }

        private static bool IsArmRaised(PoseLandmark[] lm, int shoulder, int elbow, int wrist,
            float scale, float imageAspectCorrection, float upwardYSign, float minimumDegrees)
        {
            // The Unity webcam-to-MediaPipe path can expose either top-to-bottom or
            // bottom-to-top normalized Y coordinates. Use the body-derived direction
            // instead of assuming one convention. Low-visibility off-screen joints can
            // still provide useful inferred coordinates.
            float elbowUp = (lm[elbow].y - lm[shoulder].y) * upwardYSign * imageAspectCorrection;
            float elbowSide = Mathf.Abs(lm[elbow].x - lm[shoulder].x);
            float elbowAngle = Mathf.Atan2(Mathf.Max(0f, elbowUp), Mathf.Max(elbowSide, 0.0001f)) * Mathf.Rad2Deg;

            float wristUp = (lm[wrist].y - lm[shoulder].y) * upwardYSign * imageAspectCorrection;
            float wristSide = Mathf.Abs(lm[wrist].x - lm[shoulder].x);
            float wristAngle = Mathf.Atan2(Mathf.Max(0f, wristUp), Mathf.Max(wristSide, 0.0001f)) * Mathf.Rad2Deg;

            bool elbowShowsRaisedArm = elbowUp > scale * 0.025f && elbowAngle >= minimumDegrees;
            bool wristEstimateShowsRaisedArm = wristUp > scale * 0.10f && wristAngle >= minimumDegrees;
            return elbowShowsRaisedArm || wristEstimateShowsRaisedArm;
        }

        private static float GetUpwardYSign(PoseLandmark[] lm)
        {
            float shoulderY = (lm[LeftShoulder].y + lm[RightShoulder].y) * 0.5f;

            // The nose is normally above the shoulders and is usually the most reliable
            // upper-body reference in a narrow webcam image.
            float noseDirection = lm[Nose].y - shoulderY;
            if (lm[Nose].visibility >= 0.10f && Mathf.Abs(noseDirection) >= 0.015f)
                return Mathf.Sign(noseDirection);

            // Fall back to the hips when the face is cropped or temporarily uncertain.
            float hipY = (lm[LeftHip].y + lm[RightHip].y) * 0.5f;
            float hipDirection = shoulderY - hipY;
            if (Mathf.Abs(hipDirection) >= 0.015f)
                return Mathf.Sign(hipDirection);

            // This project's current webcam pipeline normally reports bottom-to-top Y.
            return 1f;
        }

        private static bool IsArmHorizontal(PoseLandmark[] lm, int shoulder, int elbow, int wrist,
            float scale, float outwardDirection, bool elbowVisible, bool wristVisible,
            float imageAspectCorrection, float toleranceDegrees)
        {
            float wristExtension = (lm[wrist].x - lm[shoulder].x) * outwardDirection;
            float elbowExtension = (lm[elbow].x - lm[shoulder].x) * outwardDirection;
            float elbowVertical = Mathf.Abs(lm[elbow].y - lm[shoulder].y) * imageAspectCorrection;
            float wristVertical = Mathf.Abs(lm[wrist].y - lm[shoulder].y) * imageAspectCorrection;
            float elbowAngle = Mathf.Atan2(elbowVertical, Mathf.Max(elbowExtension, 0.0001f)) * Mathf.Rad2Deg;
            float wristAngle = Mathf.Atan2(wristVertical, Mathf.Max(wristExtension, 0.0001f)) * Mathf.Rad2Deg;

            // The shoulder-to-elbow segment by itself is sufficient. This is the
            // important fallback when the complete forearm and hand leave the frame.
            bool elbowIsHorizontal = elbowVisible && elbowExtension > scale * 0.15f &&
                                     elbowAngle <= toleranceDegrees;
            bool fullArmIsHorizontal = wristVisible && wristExtension > scale * 0.50f &&
                                       wristAngle <= toleranceDegrees + 5f;
            return elbowIsHorizontal || fullArmIsHorizontal;
        }

        private bool IsKeyboardTestPoseHeld()
        {
            return requiredPose switch
            {
                HumanTetrisPose.BothArmsV => UnityEngine.Input.GetKey(KeyCode.Alpha1) || UnityEngine.Input.GetKey(KeyCode.Keypad1),
                HumanTetrisPose.LeftArmHorizontal => UnityEngine.Input.GetKey(KeyCode.Alpha2) || UnityEngine.Input.GetKey(KeyCode.Keypad2),
                HumanTetrisPose.RightArmHorizontal => UnityEngine.Input.GetKey(KeyCode.Alpha3) || UnityEngine.Input.GetKey(KeyCode.Keypad3),
                HumanTetrisPose.BothArmsHorizontal => UnityEngine.Input.GetKey(KeyCode.Alpha4) || UnityEngine.Input.GetKey(KeyCode.Keypad4),
                HumanTetrisPose.LeftArmRaised => UnityEngine.Input.GetKey(KeyCode.Alpha5) || UnityEngine.Input.GetKey(KeyCode.Keypad5),
                HumanTetrisPose.RightArmRaised => UnityEngine.Input.GetKey(KeyCode.Alpha6) || UnityEngine.Input.GetKey(KeyCode.Keypad6),
                _ => false
            };
        }

        private void BuildVisuals()
        {
            waitingMaterial = CreateMaterial(new Color(1f, 0.08f, 0.7f), 4f);
            readyMaterial = CreateMaterial(new Color(0.1f, 1f, 0.35f), 5f);
            Material wallMaterial = CreateMaterial(new Color(0.035f, 0.055f, 0.11f), 0.2f);
            Material frameMaterial = CreateMaterial(new Color(0f, 0.75f, 1f), 3f);
            Material holeMaterial = CreateMaterial(new Color(0.005f, 0.008f, 0.018f), 0f);

            CreateCube("Wall", transform, new Vector3(0f, 3.3f, 0f), new Vector3(10.6f, 6.6f, 0.45f), wallMaterial);
            CreateCube("TopFrame", transform, new Vector3(0f, 6.65f, -0.02f), new Vector3(10.85f, 0.16f, 0.58f), frameMaterial);
            CreateCube("BottomFrame", transform, new Vector3(0f, 0.08f, -0.02f), new Vector3(10.85f, 0.16f, 0.58f), frameMaterial);
            CreateCube("LeftFrame", transform, new Vector3(-5.35f, 3.35f, -0.02f), new Vector3(0.16f, 6.7f, 0.58f), frameMaterial);
            CreateCube("RightFrame", transform, new Vector3(5.35f, 3.35f, -0.02f), new Vector3(0.16f, 6.7f, 0.58f), frameMaterial);

            GameObject opening = CreateCube("TargetOpening", transform, new Vector3(0f, 3.15f, -0.25f),
                new Vector3(4.5f, 6.15f, 0.05f), holeMaterial);

            GameObject figure = new GameObject("PoseSilhouette");
            figure.transform.SetParent(transform, false);
            figure.transform.localPosition = new Vector3(0f, -2.1f, -0.34f);
            figure.transform.localRotation = Quaternion.identity;
            figure.transform.localScale = Vector3.one * 1.4f;

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(figure.transform, false);
            head.transform.localScale = Vector3.one * 0.48f;
            RemoveCollider(head);
            silhouetteRenderers.Add(head.GetComponent<Renderer>());

            AddLimb(figure.transform, "Torso", Vector3.zero, Vector3.up, 1.5f, 0.25f);
            AddLimb(figure.transform, "Shoulders", Vector3.zero, Vector3.right, 0.65f, 0.28f);
            AddLimb(figure.transform, "LeftUpperArm", Vector3.zero, Vector3.left, 0.8f, 0.2f);
            AddLimb(figure.transform, "LeftForearm", Vector3.zero, Vector3.left, 0.8f, 0.18f);
            AddLimb(figure.transform, "RightUpperArm", Vector3.zero, Vector3.right, 0.8f, 0.2f);
            AddLimb(figure.transform, "RightForearm", Vector3.zero, Vector3.right, 0.8f, 0.18f);

            // Two dark eyes make the silhouette unmistakably front-facing. They stay
            // symmetrical for all four poses; no profile/side-facing variant is used.
            GameObject leftEye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leftEye.name = "LeftEye";
            leftEye.transform.SetParent(figure.transform, false);
            leftEye.transform.localPosition = new Vector3(-0.095f, 3.7f, -0.25f);
            leftEye.transform.localScale = Vector3.one * 0.055f;
            RemoveCollider(leftEye);
            leftEye.GetComponent<Renderer>().sharedMaterial = holeMaterial;

            GameObject rightEye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rightEye.name = "RightEye";
            rightEye.transform.SetParent(figure.transform, false);
            rightEye.transform.localPosition = new Vector3(0.095f, 3.7f, -0.25f);
            rightEye.transform.localScale = Vector3.one * 0.055f;
            RemoveCollider(rightEye);
            rightEye.GetComponent<Renderer>().sharedMaterial = holeMaterial;

            GameObject label = new GameObject("Instruction", typeof(TextMesh));
            label.transform.SetParent(transform, false);
            label.transform.localPosition = new Vector3(0f, 7.05f, -0.3f);
            label.transform.localRotation = Quaternion.identity;
            instructionText = label.GetComponent<TextMesh>();
            instructionText.anchor = TextAnchor.MiddleCenter;
            instructionText.alignment = TextAlignment.Center;
            instructionText.characterSize = 0.18f;
            instructionText.fontSize = 48;
            instructionText.color = new Color(0.2f, 0.95f, 1f);

            BoxCollider trigger = gameObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 3.3f, 0f);
            trigger.size = new Vector3(10.7f, 6.6f, 0.7f);
            Rigidbody body = gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
        }

        private void ConfigureSilhouette()
        {
            // All silhouettes stay at the centre regardless of the setup lane value.
            const float laneX = 0f;
            Transform marker = transform.Find("TargetOpening");
            if (marker != null) marker.localPosition = new Vector3(laneX, 3.15f, -0.25f);
            Transform figure = transform.Find("PoseSilhouette");
            if (figure == null) return;
            figure.localPosition = new Vector3(laneX, -2.1f, -0.34f);

            Transform head = figure.Find("Head");
            if (head != null) head.localPosition = new Vector3(0f, 3.65f, 0f);
            SetLimb(figure.Find("Torso"), new Vector3(0f, 1.75f, 0f), new Vector3(0f, 3.3f, 0f), 0.58f);
            SetLimb(figure.Find("Shoulders"), new Vector3(-0.3f, 3.18f, 0f),
                new Vector3(0.3f, 3.18f, 0f), 0.3f);

            Vector3 shoulderL = new Vector3(-0.18f, 3.2f, 0f);
            Vector3 shoulderR = new Vector3(0.18f, 3.2f, 0f);
            Vector3 relaxedLeftElbow = new Vector3(-0.38f, 2.65f, 0f);
            Vector3 relaxedLeftWrist = new Vector3(-0.32f, 2.05f, 0f);
            Vector3 relaxedRightElbow = new Vector3(0.38f, 2.65f, 0f);
            Vector3 relaxedRightWrist = new Vector3(0.32f, 2.05f, 0f);

            bool leftV = requiredPose == HumanTetrisPose.BothArmsV ||
                         requiredPose == HumanTetrisPose.LeftArmRaised;
            bool rightV = requiredPose == HumanTetrisPose.BothArmsV ||
                          requiredPose == HumanTetrisPose.RightArmRaised;
            bool leftH = requiredPose == HumanTetrisPose.LeftArmHorizontal || requiredPose == HumanTetrisPose.BothArmsHorizontal;
            bool rightH = requiredPose == HumanTetrisPose.RightArmHorizontal || requiredPose == HumanTetrisPose.BothArmsHorizontal;

            Vector3 leftElbow = leftV ? new Vector3(-0.62f, 3.75f, 0f) : leftH ? new Vector3(-0.72f, 3.2f, 0f) : relaxedLeftElbow;
            Vector3 leftWrist = leftV ? new Vector3(-1.0f, 4.35f, 0f) : leftH ? new Vector3(-1.3f, 3.2f, 0f) : relaxedLeftWrist;
            Vector3 rightElbow = rightV ? new Vector3(0.62f, 3.75f, 0f) : rightH ? new Vector3(0.72f, 3.2f, 0f) : relaxedRightElbow;
            Vector3 rightWrist = rightV ? new Vector3(1.0f, 4.35f, 0f) : rightH ? new Vector3(1.3f, 3.2f, 0f) : relaxedRightWrist;

            SetLimb(figure.Find("LeftUpperArm"), shoulderL, leftElbow);
            SetLimb(figure.Find("LeftForearm"), leftElbow, leftWrist);
            SetLimb(figure.Find("RightUpperArm"), shoulderR, rightElbow);
            SetLimb(figure.Find("RightForearm"), rightElbow, rightWrist);
            instructionText.text = $"HUMAN TETRIS   {PoseLabel(requiredPose)}";
        }

        private void SetSilhouetteReady(bool ready)
        {
            Material targetMaterial = ready ? readyMaterial : waitingMaterial;
            if (poseReady == ready && silhouetteRenderers.Count > 0 &&
                silhouetteRenderers[0].sharedMaterial == targetMaterial)
                return;
            poseReady = ready;
            foreach (Renderer renderer in silhouetteRenderers)
                renderer.sharedMaterial = targetMaterial;
            if (instructionText != null)
                instructionText.color = ready ? new Color(0.1f, 1f, 0.35f) : new Color(0.2f, 0.95f, 1f);
        }

        private void AddLimb(Transform parent, string name, Vector3 start, Vector3 end, float ignoredLength, float width)
        {
            GameObject limb = GameObject.CreatePrimitive(PrimitiveType.Cube);
            limb.name = name;
            limb.transform.SetParent(parent, false);
            RemoveCollider(limb);
            silhouetteRenderers.Add(limb.GetComponent<Renderer>());
            SetLimb(limb.transform, start, end, width);
        }

        private static void SetLimb(Transform limb, Vector3 start, Vector3 end, float width = 0.18f)
        {
            if (limb == null) return;
            Vector3 delta = end - start;
            limb.localPosition = (start + end) * 0.5f;
            limb.localScale = new Vector3(width, delta.magnitude, width);
            limb.localRotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
        }

        private static GameObject CreateCube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = position;
            cube.transform.localScale = scale;
            RemoveCollider(cube);
            cube.GetComponent<Renderer>().sharedMaterial = material;
            return cube;
        }

        private static void RemoveCollider(GameObject target)
        {
            Collider collider = target.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
        }

        private static Material CreateMaterial(Color color, float emission)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new Material(shader);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            material.color = color;
            if (emission > 0f && material.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmissionColor", color * emission);
                material.EnableKeyword("_EMISSION");
            }
            return material;
        }

        private static string PoseLabel(HumanTetrisPose pose)
        {
            return pose switch
            {
                HumanTetrisPose.BothArmsV => "V POSE",
                HumanTetrisPose.LeftArmHorizontal => "LEFT ARM",
                HumanTetrisPose.RightArmHorizontal => "RIGHT ARM",
                HumanTetrisPose.BothArmsHorizontal => "T POSE",
                HumanTetrisPose.LeftArmRaised => "LEFT ARM UP",
                HumanTetrisPose.RightArmRaised => "RIGHT ARM UP",
                _ => pose.ToString()
            };
        }
    }
}
