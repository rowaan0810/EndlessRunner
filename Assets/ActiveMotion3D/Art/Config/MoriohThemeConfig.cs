using UnityEngine;

namespace ActiveMotion3D.EndlessRunner.Environment
{
    /// <summary>
    /// Inspector-editable catalog used to author the Morioh scene. Gameplay
    /// dimensions remain on the existing TrackManager, PlayerController, and
    /// ObstacleSpawner components.
    /// </summary>
    [CreateAssetMenu(fileName = "MoriohThemeConfig", menuName = "Active Motion 3D/Morioh Theme")]
    public sealed class MoriohThemeConfig : ScriptableObject
    {
        [Header("Twenty-unit street modules")]
        public GameObject[] streetModules;

        [Header("Spawner visual slots")]
        public GameObject lowBarrierVisual;
        public GameObject highBarrierVisual;
        public GameObject fullBarrierVisual;
        public GameObject coinVisual;

        [Header("Optional activity artwork")]
        public GameObject punchTargetVisual;
        public MoriohActivityArt activityArt;

        [Header("Cloud sky and continuous distance")]
        public Material skyboxMaterial;
        public GameObject horizonPrefab;
        public Vector3 horizonOffset;
        public Vector3 horizonScale = Vector3.one;
        public bool horizonFollowsCameraXZ = true;

        [Header("Built-in pipeline palette")]
        public Material[] palette;

        [Header("Scene atmosphere")]
        public Color cameraBackground = new Color(0.43f, 0.43f, 0.65f);
        public bool fogEnabled = true;
        public Color fogColor = new Color(0.45f, 0.39f, 0.57f);
        [Range(0f, 0.05f)] public float fogDensity = 0.008f;
        public Color ambientSky = new Color(0.64f, 0.61f, 0.76f);
        public Color ambientEquator = new Color(0.44f, 0.39f, 0.55f);
        public Color ambientGround = new Color(0.22f, 0.19f, 0.32f);
        public Color sunColor = new Color(1f, 0.83f, 0.68f);
        [Range(0f, 3f)] public float sunIntensity = 1.25f;
    }
}
