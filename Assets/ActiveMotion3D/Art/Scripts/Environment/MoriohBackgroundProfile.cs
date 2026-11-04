using System;
using UnityEngine;

namespace ActiveMotion3D.EndlessRunner.Environment
{
    [Serializable]
    public sealed class MoriohCloudPlacement
    {
        public bool visible = true;
        [Min(0)] public int spriteIndex;
        public float azimuthDegrees;
        [Range(-5f, 60f)] public float elevationDegrees = 10f;
        [Range(1f, 40f)] public float widthDegrees = 12f;
    }

    [Serializable]
    public sealed class MoriohCloudBand
    {
        public string name;
        [Min(100f)] public float radius = 400f;
        public float driftDegreesPerSecond = .05f;
        public Color tint = Color.white;
        public MoriohCloudPlacement[] clouds;
    }

    [CreateAssetMenu(menuName = "Active Motion 3D/Morioh Hybrid Background")]
    public sealed class MoriohBackgroundProfile : ScriptableObject
    {
        [Header("Original painted artwork")]
        public Texture2D paintedSky;
        public Texture2D cloudAtlas;
        public Texture2D paintedHills;
        [Min(1)] public int atlasColumns = 4;
        [Min(1)] public int atlasRows = 3;
        [Range(0f, .2f)] public float atlasInset = .015f;

        [Header("Cloud motion and composition")]
        public bool animateWhilePaused = true;
        public MoriohCloudBand[] cloudBands;

        [Header("Painted hills")]
        [Min(100f)] public float farHillRadius = 350f;
        public float farHillBottom = -30f;
        public float farHillTop = 140f;
        public Color farHillTint = new Color(.96f, .95f, 1f);
        [Min(100f)] public float nearHillRadius = 305f;
        public float nearHillBottom = -40f;
        public float nearHillTop = 105f;
        public Color nearHillTint = new Color(.88f, .88f, .96f);

        [Header("Town silhouette")]
        public Vector3 townScale = Vector3.one;
        public float townVerticalOffset;

        [Header("Atmosphere applied by the install command")]
        public Color horizonColor = new Color(.66f, .59f, .71f);
        [Range(0f, .03f)] public float fogDensity = .008f;

        [Header("Generated visual assets")]
        public Material skyMaterial;
        public Material cloudMaterial;
        public Material farHillMaterial;
        public Material nearHillMaterial;
        public GameObject backgroundPrefab;
    }
}
