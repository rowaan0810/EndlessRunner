using UnityEngine;
using ActiveMotion3D.EndlessRunner.Core;
using ActiveMotion3D.EndlessRunner.Input;
using ActiveMotion3D.EndlessRunner.Input.PoseDetection;

namespace ActiveMotion3D.EndlessRunner.Collectibles
{
    public enum PunchTargetSide { Left, Right }

    /// <summary>
    /// A forgiving high-side bonus target. It turns yellow shortly before it
    /// reaches the player and accepts any hand shape; arm position is what matters.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class PunchTarget : MonoBehaviour
    {
        [Header("Timing")]
        [SerializeField] private float hitPlaneZ;
        [SerializeField] private float missZ = -4f;
        [SerializeField, Min(1f)] private float maximumHitDistance = 10f;
        [SerializeField, Range(0.25f, 2f)] private float minimumReactionSeconds = 0.85f;

        [Header("Visual")]
        [SerializeField] private float rotationSpeed = 90f;
        [SerializeField] private Color idleColor = new Color(0.92f, 0.95f, 1f);
        [SerializeField] private Color activeColor = new Color(1f, 0.82f, 0.05f);

        public PunchTargetSide Side { get; private set; }
        public bool IsHighlighted { get; private set; }
        public float CurrentHighlightSeconds { get; private set; }

        private Renderer targetRenderer;
        private Material runtimeMaterial;

        private void Awake()
        {
            targetRenderer = GetComponentInChildren<Renderer>();
            if (targetRenderer != null)
            {
                runtimeMaterial = targetRenderer.material;
                ApplyColor(idleColor, 0.15f);
            }
        }

        private void OnEnable()
        {
            IsHighlighted = false;
            transform.localScale = Vector3.one;
            ApplyColor(idleColor, 0.15f);
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;

            float worldSpeed = GameSpeed.Instance != null ? Mathf.Max(0.01f, GameSpeed.Instance.Current) : 10f;
            Vector3 position = transform.position;
            float distanceBeforeMove = position.z - hitPlaneZ;

            // Preserve the requested close activation distance, but cap only this
            // target's speed inside the yellow zone. This guarantees enough real
            // time for webcam capture + MediaPipe inference at high game speeds.
            float movementSpeed = worldSpeed;
            if (distanceBeforeMove <= maximumHitDistance && distanceBeforeMove >= missZ)
            {
                float assistedSpeed = maximumHitDistance / Mathf.Max(0.1f, minimumReactionSeconds);
                movementSpeed = Mathf.Min(worldSpeed, assistedSpeed);
            }

            position.z -= movementSpeed * Time.deltaTime;
            transform.position = position;
            transform.Rotate(new Vector3(25f, rotationSpeed, 12f) * Time.deltaTime, Space.Self);

            float distanceToHit = position.z - hitPlaneZ;
            CurrentHighlightSeconds = maximumHitDistance / Mathf.Max(0.01f, movementSpeed);
            SetHighlighted(distanceToHit <= maximumHitDistance &&
                           position.z >= missZ);

            // Only the one-frame transition from arm-down to arm-up can hit.
            // Holding an arm up therefore cannot collect later targets.
            if (IsHighlighted && PunchArmInput.TryConsumeRaise(Side))
            {
                Collect();
                return;
            }
            if (position.z < missZ) gameObject.SetActive(false);
        }

        public void Setup(Vector3 position, PunchTargetSide side, float hitDistance, float reactionSeconds)
        {
            Side = side;
            maximumHitDistance = Mathf.Max(1f, hitDistance);
            minimumReactionSeconds = Mathf.Max(0.25f, reactionSeconds);
            transform.position = position;
            transform.rotation = Quaternion.identity;
            gameObject.SetActive(true);
        }

        private void Collect()
        {
            if (GameManager.Instance != null) GameManager.Instance.AddCoin(1);
            PunchHitEffect.Spawn(transform.position, activeColor);
            gameObject.SetActive(false);
        }

        private void SetHighlighted(bool highlighted)
        {
            if (IsHighlighted == highlighted) return;
            IsHighlighted = highlighted;
            ApplyColor(highlighted ? activeColor : idleColor, highlighted ? 4f : 0.15f);
            transform.localScale = highlighted ? Vector3.one * 1.15f : Vector3.one;
        }

        private void ApplyColor(Color color, float emissionStrength)
        {
            if (runtimeMaterial == null) return;
            if (runtimeMaterial.HasProperty("_BaseColor")) runtimeMaterial.SetColor("_BaseColor", color);
            if (runtimeMaterial.HasProperty("_Color")) runtimeMaterial.SetColor("_Color", color);
            if (runtimeMaterial.HasProperty("_EmissionColor"))
            {
                runtimeMaterial.SetColor("_EmissionColor", color * emissionStrength);
                runtimeMaterial.EnableKeyword("_EMISSION");
            }
        }

        private void OnDestroy()
        {
            if (runtimeMaterial != null) Destroy(runtimeMaterial);
        }
    }

    /// <summary>
    /// Tracks each arm independently and exposes only a new raised-state transition.
    /// Any interruption of the raised state rearms the next punch; a confidently
    /// tracked arm-down pose is not required.
    /// </summary>
    internal static class PunchArmInput
    {
        private const float MinimumVisibility = 0.08f;

        private static int lastUpdatedFrame = -1;
        private static bool leftArmed;
        private static bool rightArmed;
        private static bool leftRaiseEvent;
        private static bool rightRaiseEvent;
        private static bool invertVerticalDirection = true;

        public static void SetInvertVerticalDirection(bool invert)
        {
            invertVerticalDirection = invert;
        }

        public static void UpdateState()
        {
            if (lastUpdatedFrame == Time.frameCount) return;
            lastUpdatedFrame = Time.frameCount;

            leftRaiseEvent = false;
            rightRaiseEvent = false;

            bool leftRaised = ReadArmRaised(PunchTargetSide.Left);
            if (!leftRaised)
            {
                // It is enough that the raised state has stopped. We do not
                // require a confidently tracked arm-down pose to rearm.
                leftArmed = true;
            }
            else if (leftArmed)
            {
                leftRaiseEvent = true;
                leftArmed = false;
            }

            bool rightRaised = ReadArmRaised(PunchTargetSide.Right);
            if (!rightRaised)
            {
                rightArmed = true;
            }
            else if (rightArmed)
            {
                rightRaiseEvent = true;
                rightArmed = false;
            }
        }

        public static bool TryConsumeRaise(PunchTargetSide side)
        {
            UpdateState();
            if (side == PunchTargetSide.Left)
            {
                if (!leftRaiseEvent) return false;
                leftRaiseEvent = false;
                return true;
            }

            if (!rightRaiseEvent) return false;
            rightRaiseEvent = false;
            return true;
        }

        private static bool ReadArmRaised(PunchTargetSide side)
        {
            // Q/E use the same edge detection, so holding either key also cannot
            // repeatedly break targets.
            if (InputManager.Instance != null && InputManager.Instance.CurrentMode == InputMode.Keyboard)
                return UnityEngine.Input.GetKey(side == PunchTargetSide.Left ? KeyCode.Q : KeyCode.E);

            MediaPipeManager manager = MediaPipeManager.Instance;
            PoseLandmark[] landmarks = manager != null ? manager.CurrentLandmarks : null;
            if (landmarks == null || landmarks.Length < 33 || !manager.PersonDetected) return false;

            // The webcam pipeline is mirrored relative to the user's perceived
            // left/right. Swap MediaPipe labels so the user's left hand hits the
            // left box and the user's right hand hits the right box.
            bool useMediaPipeLeft = side == PunchTargetSide.Right;
            int shoulderIndex = useMediaPipeLeft ? LM.L_SHOULDER : LM.R_SHOULDER;
            int wristIndex = useMediaPipeLeft ? LM.L_WRIST : LM.R_WRIST;
            PoseLandmark shoulder = landmarks[shoulderIndex];
            PoseLandmark wrist = landmarks[wristIndex];
            if (shoulder.visibility < MinimumVisibility) return false;

            // Shoulder height is the exact boundary. This webcam feed uses the
            // vertically inverted direction already required by PoseInput.
            bool poseWristRaised = wrist.visibility >= MinimumVisibility &&
                                   IsAboveShoulder(wrist.y, shoulder.y);
            if (poseWristRaised) return true;

            // Hand Landmarker can remain stable when Pose Landmarker briefly loses
            // a wrist. Assign each detected hand to whichever pose wrist it is
            // closest to, so two visible hands remain independent and cannot both
            // drive the same side.
            HandGestureManager handManager = HandGestureManager.Instance;
            if (handManager == null || handManager.CurrentHands == null) return false;

            int otherWristIndex = useMediaPipeLeft ? LM.R_WRIST : LM.L_WRIST;
            PoseLandmark otherWrist = landmarks[otherWristIndex];
            float closestDistance = float.MaxValue;
            float matchedHandY = 1f;
            bool foundMatchingHand = false;

            foreach (HandGestureResult hand in handManager.CurrentHands)
            {
                float targetDx = hand.handCenterX - wrist.x;
                float targetDy = hand.handCenterY - wrist.y;
                float targetDistance = targetDx * targetDx + targetDy * targetDy;
                float otherDx = hand.handCenterX - otherWrist.x;
                float otherDy = hand.handCenterY - otherWrist.y;
                float otherDistance = otherDx * otherDx + otherDy * otherDy;

                // Require a unique nearest arm. Equal distances are ambiguous and
                // must not let one detected hand trigger both left and right targets.
                if (targetDistance >= otherDistance || targetDistance >= closestDistance) continue;
                closestDistance = targetDistance;
                matchedHandY = hand.handCenterY;
                foundMatchingHand = true;
            }

            return foundMatchingHand && IsAboveShoulder(matchedHandY, shoulder.y);
        }

        private static bool IsAboveShoulder(float handY, float shoulderY)
        {
            return invertVerticalDirection ? handY > shoulderY : handY < shoulderY;
        }
    }

    /// <summary>Short particle burst plus a synthesized impact sound.</summary>
    internal static class PunchHitEffect
    {

        public static void Spawn(Vector3 position, Color color)
        {
            GameObject effect = new GameObject("PunchHitEffect");
            effect.transform.position = position;
            ParticleSystem particles = effect.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.duration = 0.35f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
            main.startColor = new ParticleSystem.MinMaxGradient(color, Color.white);
            main.maxParticles = 32;
            main.stopAction = ParticleSystemStopAction.Destroy;
            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 24) });
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.25f;

            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit");
            if (shader != null)
            {
                Material material = new Material(shader);
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
                if (material.HasProperty("_Color")) material.SetColor("_Color", color);
                renderer.material = material;
            }

            // Central 2D source keeps left/right targets equally audible and lets
            // the clip finish independently of the short-lived particle object.
            if (ActiveMotion3D.EndlessRunner.Core.SfxPlayer.Instance != null) ActiveMotion3D.EndlessRunner.Core.SfxPlayer.Instance.PlayPunch();
            particles.Play();
        }
    }
}
