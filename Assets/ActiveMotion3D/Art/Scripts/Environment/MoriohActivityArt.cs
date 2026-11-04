using UnityEngine;

namespace ActiveMotion3D.EndlessRunner.Environment
{
    /// <summary>Artwork only. Recognition, hit windows, scoring and collision stay on existing systems.</summary>
    [CreateAssetMenu(menuName = "Active Motion 3D/Morioh Activity Art")]
    public sealed class MoriohActivityArt : ScriptableObject
    {
        [Header("Pose gate")]
        public GameObject gateDecoration;
        public Material waitingSilhouette;
        public Material readySilhouette;
        public Material openingBacking;
        public Vector2 backingSize = new Vector2(6.2f, 6.15f);
        public Font instructionFont;
        public Color instructionColor = new Color(1f, .9f, .69f);
        public Color readyColor = new Color(.68f, 1f, .73f);
        public string waitingLabel = "MATCH THE POSE";
        public string readyLabel = "READY";
        public Vector3 statusPosition = new Vector3(0f, .55f, -.4f);
        [Min(.01f)] public float instructionCharacterSize = .14f;
        [Min(.01f)] public float statusCharacterSize = .12f;
        [Tooltip("Order: V, left horizontal, right horizontal, T, left raised, right raised.")]
        public string[] poseLabels = { "V POSE", "LEFT ARM OUT", "RIGHT ARM OUT", "T POSE", "LEFT ARM UP", "RIGHT ARM UP" };

        [Header("High side target")]
        public Color targetIdle = new Color(.76f, .64f, .86f);
        public Color targetActive = new Color(1f, .82f, .05f);
        [Range(0f, 3f)] public float targetEmission = .5f;
        public string leftTargetLabel = "LEFT";
        public string rightTargetLabel = "RIGHT";
        public string activeTargetLabel = "HIT";
        public GameObject punchHitEffect;
        public GameObject coinPickupEffect;
    }
}
