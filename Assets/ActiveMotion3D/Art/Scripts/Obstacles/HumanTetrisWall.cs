// HumanTetrisWall.cs — A full-width upper-body pose gate for the three-lane runner.

using System.Collections.Generic;
using UnityEngine;
using ActiveMotion3D.EndlessRunner.Core;
using ActiveMotion3D.EndlessRunner.Input;
using ActiveMotion3D.EndlessRunner.Input.PoseDetection;
using ActiveMotion3D.EndlessRunner.Player;
using ActiveMotion3D.EndlessRunner.Environment;

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

        [Header("Visual")]
        [Tooltip("Scale of the Human Tetris pose image inside the gate.")]
        [SerializeField, Range(0.8f, 2.5f)] private float silhouetteScale = 1.8f;
        [Tooltip("Vertical placement of the short pose instruction on the gate.")]
        [SerializeField, Range(0.5f, 6.4f)] private float instructionLabelHeight = 6f;
        [SerializeField] private MoriohActivityArt activityArt;

        public void ConfigureArt(MoriohActivityArt art) => activityArt = art;

        private readonly List<Renderer> silhouetteRenderers = new List<Renderer>();
        private HumanTetrisPose requiredPose;
        private int requiredLane;
        private float lastSuccessfulMatchTime;
        private bool trackingReliable;
        private bool poseReady;
        private Material waitingMaterial;
        private Material readyMaterial;
        private TextMesh instructionText;
        private TextMesh statusText;
        private bool lastLeftRaised;
        private bool lastRightRaised;
        private bool lastLeftHorizontal;
        private bool lastRightHorizontal;
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
            lastSuccessfulMatchTime = float.NegativeInfinity;
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
                GameManager.Instance.Feedback.PoseClears++;
                if (ActiveMotion3D.EndlessRunner.Core.SfxPlayer.Instance != null) ActiveMotion3D.EndlessRunner.Core.SfxPlayer.Instance.PlaySuccess();
                Debug.Log($"Human Tetris passed: pose {requiredPose}");
                gameObject.SetActive(false);
                return;
            }

            PlayerCollision collision = player.GetComponent<PlayerCollision>();
            if (collision != null && collision.IsShielded)
            {
                GameManager.Instance.Feedback.ShieldHits++;
                collision.IsShielded = false;
                gameObject.SetActive(false);
                return;
            }

            Debug.Log($"Human Tetris failed: pose {requiredPose}, ready={poseReady}, " +
                      $"left[horizontal={lastLeftHorizontal}, raised={lastLeftRaised}], " +
                      $"right[horizontal={lastRightHorizontal}, raised={lastRightRaised}], upY={lastUpwardYSign}");
            bool keyboard = InputManager.Instance != null && InputManager.Instance.CurrentMode == InputMode.Keyboard;
            string pose = requiredPose switch
            {
                HumanTetrisPose.BothArmsV => "both arms in a V",
                HumanTetrisPose.LeftArmHorizontal => "screen-left arm horizontal",
                HumanTetrisPose.RightArmHorizontal => "screen-right arm horizontal",
                HumanTetrisPose.BothArmsHorizontal => "both arms horizontal",
                HumanTetrisPose.LeftArmRaised => "screen-left arm raised",
                _ => "screen-right arm raised"
            };
            GameManager.Instance.Feedback.EndAtPose(pose, keyboard, ((int)requiredPose + 1).ToString(),
                trackingReliable, Time.time - lastSuccessfulMatchTime, recognitionGraceSeconds);
            GameManager.Instance.TriggerGameOver();
        }

        private bool IsRequiredPoseHeld()
        {
            trackingReliable = false;
            lastLeftRaised = false;
            lastRightRaised = false;
            lastLeftHorizontal = false;
            lastRightHorizontal = false;

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
            if (manager.SharedInputTexture != null)
                imageAspectCorrection = manager.SharedInputTexture.height / (float)manager.SharedInputTexture.width;
            float upwardYSign = GetUpwardYSign(lm);
            var anatomicalLeft = (Shoulder: LeftShoulder, Elbow: LeftElbow, Wrist: LeftWrist);
            var anatomicalRight = (Shoulder: RightShoulder, Elbow: RightElbow, Wrist: RightWrist);
            bool leftAppearsOnScreenLeft = LM.LeftShoulderAppearsOnScreenLeft(lm);
            var left = ReadArm(leftAppearsOnScreenLeft ? anatomicalLeft : anatomicalRight, 1f);
            var right = ReadArm(leftAppearsOnScreenLeft ? anatomicalRight : anatomicalLeft, -1f);

            lastLeftRaised = left.Raised;
            lastRightRaised = right.Raised;
            lastLeftHorizontal = left.Horizontal;
            lastRightHorizontal = right.Horizontal;
            lastUpwardYSign = upwardYSign;
            trackingReliable = requiredPose switch
            {
                HumanTetrisPose.LeftArmHorizontal or HumanTetrisPose.LeftArmRaised => left.Tracked,
                HumanTetrisPose.RightArmHorizontal or HumanTetrisPose.RightArmRaised => right.Tracked,
                _ => left.Tracked && right.Tracked
            };

            // A cropped resting arm is common in a face-height webcam. Reject a
            // tracked second arm in a raised or horizontal pose.
            bool leftInactive = !left.Raised && !left.Horizontal;
            bool rightInactive = !right.Raised && !right.Horizontal;
            return requiredPose switch
            {
                HumanTetrisPose.BothArmsV => left.Raised && right.Raised && !left.Horizontal && !right.Horizontal,
                HumanTetrisPose.BothArmsHorizontal => left.Horizontal && right.Horizontal,
                HumanTetrisPose.LeftArmHorizontal => left.Horizontal && rightInactive,
                HumanTetrisPose.RightArmHorizontal => right.Horizontal && leftInactive,
                HumanTetrisPose.LeftArmRaised => left.Raised && !left.Horizontal && rightInactive,
                HumanTetrisPose.RightArmRaised => right.Raised && !right.Horizontal && leftInactive,
                _ => false
            };

            (bool Raised, bool Horizontal, bool Tracked) ReadArm((int Shoulder, int Elbow, int Wrist) arm,
                float outwardDirection)
            {
                bool elbowVisible = lm[arm.Elbow].visibility >= minimumElbowVisibility;
                bool wristVisible = lm[arm.Wrist].visibility >= minimumWristVisibility;
                bool allowInferred = requiredPose == HumanTetrisPose.BothArmsV;
                bool raised = IsArmRaised(lm, arm.Shoulder, arm.Elbow, arm.Wrist, shoulderWidth,
                    imageAspectCorrection, upwardYSign, raisedMinimumDegrees,
                    elbowVisible || allowInferred, wristVisible || allowInferred);
                bool horizontal = IsArmHorizontal(lm, arm.Shoulder, arm.Elbow, arm.Wrist,
                    shoulderWidth, outwardDirection, elbowVisible, wristVisible,
                    imageAspectCorrection, horizontalToleranceDegrees);
                return (raised, horizontal, elbowVisible || wristVisible);
            }
        }

        private static bool IsArmRaised(PoseLandmark[] lm, int shoulder, int elbow, int wrist,
            float scale, float imageAspectCorrection, float upwardYSign, float minimumDegrees,
            bool useElbow, bool useWrist)
        {
            float elbowUp = (lm[elbow].y - lm[shoulder].y) * upwardYSign * imageAspectCorrection;
            float elbowSide = Mathf.Abs(lm[elbow].x - lm[shoulder].x);
            float elbowAngle = Mathf.Atan2(Mathf.Max(0f, elbowUp), Mathf.Max(elbowSide, 0.0001f)) * Mathf.Rad2Deg;

            float wristUp = (lm[wrist].y - lm[shoulder].y) * upwardYSign * imageAspectCorrection;
            float wristSide = Mathf.Abs(lm[wrist].x - lm[shoulder].x);
            float wristAngle = Mathf.Atan2(Mathf.Max(0f, wristUp), Mathf.Max(wristSide, 0.0001f)) * Mathf.Rad2Deg;

            bool elbowShowsRaisedArm = useElbow && elbowUp > scale * 0.025f && elbowAngle >= minimumDegrees;
            bool wristEstimateShowsRaisedArm = useWrist && wristUp > scale * 0.10f && wristAngle >= minimumDegrees;
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

            // MediaPipe reads the upright camera frame with a top-left origin.
            return -1f;
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
            waitingMaterial = activityArt != null && activityArt.waitingSilhouette != null
                ? activityArt.waitingSilhouette : CreateMaterial(new Color(1f, 0.08f, 0.7f), 4f);
            readyMaterial = activityArt != null && activityArt.readySilhouette != null
                ? activityArt.readySilhouette : CreateMaterial(new Color(0.1f, 1f, 0.35f), 5f);
            Material holeMaterial = activityArt != null && activityArt.openingBacking != null
                ? activityArt.openingBacking : CreateMaterial(new Color(0.005f, 0.008f, 0.018f), 0f);
            if (activityArt != null && activityArt.gateDecoration != null)
                Instantiate(activityArt.gateDecoration, transform, false);
            else
            {
                Material wallMaterial = CreateMaterial(new Color(0.035f, 0.055f, 0.11f), 0.2f);
                Material frameMaterial = CreateMaterial(new Color(0f, 0.75f, 1f), 3f);

                CreateCube("Wall", transform, new Vector3(0f, 3.3f, 0f), new Vector3(10.6f, 6.6f, 0.45f), wallMaterial);
                CreateCube("TopFrame", transform, new Vector3(0f, 6.65f, -0.02f), new Vector3(10.85f, 0.16f, 0.58f), frameMaterial);
                CreateCube("BottomFrame", transform, new Vector3(0f, 0.08f, -0.02f), new Vector3(10.85f, 0.16f, 0.58f), frameMaterial);
                CreateCube("LeftFrame", transform, new Vector3(-5.35f, 3.35f, -0.02f), new Vector3(0.16f, 6.7f, 0.58f), frameMaterial);
                CreateCube("RightFrame", transform, new Vector3(5.35f, 3.35f, -0.02f), new Vector3(0.16f, 6.7f, 0.58f), frameMaterial);
            }

            GameObject opening = CreateCube("TargetOpening", transform, new Vector3(0f, 3.15f, -0.25f),
                activityArt != null ? new Vector3(activityArt.backingSize.x, activityArt.backingSize.y, .05f)
                    : new Vector3(4.5f, 6.15f, 0.05f), holeMaterial);

            GameObject figure = new GameObject("PoseSilhouette");
            figure.transform.SetParent(transform, false);
            figure.transform.localPosition = new Vector3(0f, -2.1f, -0.34f);
            figure.transform.localRotation = Quaternion.identity;
            figure.transform.localScale = Vector3.one * silhouetteScale;

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
            label.transform.localPosition = new Vector3(0f, instructionLabelHeight, -0.3f);
            label.transform.localRotation = Quaternion.identity;
            instructionText = label.GetComponent<TextMesh>();
            instructionText.anchor = TextAnchor.MiddleCenter;
            instructionText.alignment = TextAlignment.Center;
            instructionText.characterSize = 0.2f;
            instructionText.fontSize = 48;
            instructionText.color = new Color(0.2f, 0.95f, 1f);
            if (activityArt != null)
            {
                StyleLabel(instructionText, activityArt.instructionCharacterSize);
                GameObject status = new GameObject("PoseStatus", typeof(TextMesh));
                status.transform.SetParent(transform, false);
                status.transform.localPosition = activityArt.statusPosition;
                statusText = status.GetComponent<TextMesh>();
                statusText.anchor = TextAnchor.MiddleCenter;
                statusText.alignment = TextAlignment.Center;
                statusText.fontSize = 48;
                StyleLabel(statusText, activityArt.statusCharacterSize);
            }

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
            figure.localScale = Vector3.one * silhouetteScale;
            if (instructionText != null)
                instructionText.transform.localPosition = new Vector3(laneX, instructionLabelHeight, -0.3f);

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
            instructionText.text = PoseLabel(requiredPose);
            if (activityArt != null && activityArt.poseLabels != null &&
                (int)requiredPose < activityArt.poseLabels.Length)
                instructionText.text = activityArt.poseLabels[(int)requiredPose];
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
                instructionText.color = activityArt != null
                    ? ready ? activityArt.readyColor : activityArt.instructionColor
                    : ready ? new Color(0.1f, 1f, 0.35f) : new Color(0.2f, 0.95f, 1f);
            if (statusText != null && activityArt != null)
            {
                statusText.text = ready ? activityArt.readyLabel : activityArt.waitingLabel;
                statusText.color = ready ? activityArt.readyColor : activityArt.instructionColor;
            }
        }

        private void StyleLabel(TextMesh text, float size)
        {
            text.characterSize = size;
            text.color = activityArt.instructionColor;
            if (activityArt.instructionFont == null) return;
            text.font = activityArt.instructionFont;
            text.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
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
