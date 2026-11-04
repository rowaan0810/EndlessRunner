using System;
using UnityEngine;

namespace ActiveMotion3D.EndlessRunner.Environment
{
    [Serializable]
    public sealed class MoriohCloudCard
    {
        public Transform transform;
        public MeshFilter meshFilter;
        public Renderer renderer;
    }

    [Serializable]
    public sealed class MoriohCloudRig
    {
        public MoriohCloudCard[] cards;
    }

    /// <summary>Visual presentation only. Clouds orbit continuously, outside the track pool.</summary>
    public sealed class MoriohBackgroundController : MonoBehaviour
    {
        public MoriohBackgroundProfile profile;
        public Mesh[] cloudSpriteMeshes;
        public MoriohCloudRig[] cloudRigs;
        public Transform farHills;
        public Transform nearHills;
        public Transform town;
        private double elapsed;
        private MaterialPropertyBlock properties;

        private void LateUpdate()
        {
            if (profile == null) return;
            elapsed += profile.animateWhilePaused ? Time.unscaledDeltaTime : Time.deltaTime;
            ApplyLayout();
        }

        // Called once by the asset builder too, so prefab previews have the saved layout.
        public void ApplyLayout()
        {
            if (profile == null) return;
            if (properties == null) properties = new MaterialPropertyBlock();
            if (town != null)
            {
                town.localPosition = Vector3.up * profile.townVerticalOffset;
                town.localScale = profile.townScale;
            }
            ApplyHills(farHills, profile.farHillRadius, profile.farHillBottom, profile.farHillTop, profile.farHillTint);
            ApplyHills(nearHills, profile.nearHillRadius, profile.nearHillBottom, profile.nearHillTop, profile.nearHillTint);
            if (profile.cloudAtlas == null || cloudRigs == null || profile.cloudBands == null) return;
            float cellAspect = (profile.cloudAtlas.width / (float)Mathf.Max(1, profile.atlasColumns)) /
                               (profile.cloudAtlas.height / (float)Mathf.Max(1, profile.atlasRows));
            for (int b = 0; b < cloudRigs.Length; b++)
            {
                MoriohCloudRig rig = cloudRigs[b];
                if (rig?.cards == null) continue;
                MoriohCloudBand band = b < profile.cloudBands.Length ? profile.cloudBands[b] : null;
                for (int i = 0; i < rig.cards.Length; i++)
                {
                    MoriohCloudCard card = rig.cards[i];
                    if (card?.transform == null) continue;
                    MoriohCloudPlacement cloud = band?.clouds != null && i < band.clouds.Length ? band.clouds[i] : null;
                    card.transform.gameObject.SetActive(cloud != null && cloud.visible);
                    if (cloud == null || !cloud.visible) continue;
                    float yaw = (float)((cloud.azimuthDegrees + elapsed * band.driftDegreesPerSecond) % 360) * Mathf.Deg2Rad;
                    float pitch = cloud.elevationDegrees * Mathf.Deg2Rad;
                    Vector3 direction = new Vector3(Mathf.Sin(yaw) * Mathf.Cos(pitch), Mathf.Sin(pitch), Mathf.Cos(yaw) * Mathf.Cos(pitch));
                    float radius = Mathf.Max(100f, band.radius);
                    float width = 2f * radius * Mathf.Tan(Mathf.Clamp(cloud.widthDegrees, 1f, 40f) * Mathf.Deg2Rad * .5f);
                    card.transform.localPosition = direction * radius;
                    card.transform.localRotation = Quaternion.LookRotation(direction, Vector3.up);
                    card.transform.localScale = new Vector3(width, width / cellAspect, 1f);
                    if (card.meshFilter != null && cloudSpriteMeshes != null && cloudSpriteMeshes.Length > 0)
                        card.meshFilter.sharedMesh = cloudSpriteMeshes[Mathf.Clamp(cloud.spriteIndex, 0, cloudSpriteMeshes.Length - 1)];
                    if (card.renderer != null)
                    {
                        properties.Clear();
                        properties.SetColor("_Tint", band.tint);
                        card.renderer.SetPropertyBlock(properties);
                    }
                }
            }
        }

        private void ApplyHills(Transform target, float radius, float bottom, float top, Color tint)
        {
            if (target == null) return;
            target.localPosition = Vector3.up * bottom;
            target.localScale = new Vector3(radius, Mathf.Max(.1f, top - bottom), radius);
            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer == null) return;
            properties.Clear();
            properties.SetColor("_Tint", tint);
            renderer.SetPropertyBlock(properties);
        }
    }
}
