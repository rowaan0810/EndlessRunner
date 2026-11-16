using System;
using System.Collections.Generic;
using ActiveMotion3D.EndlessRunner.Player;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ActiveMotion3D.EndlessRunner.UI
{
    internal sealed class StandCoachPortrait : IDisposable
    {
        private const int Layer = 31;
        private static readonly Vector3 Stage = new Vector3(0, -160, 0);
        private readonly Dictionary<Material, Material> stageMaterials = new Dictionary<Material, Material>();
        private readonly RawImage image;
        private GameObject scene;
        private RenderTexture picture;
        private Camera camera;
        private StandCoachPose pose;
        private float nextFrame;
        private bool redraw = true;

        private StandCoachPortrait(RawImage image) => this.image = image;

        public static StandCoachPortrait Create(RunResultsView view, CharacterSelector selector, bool killerQueen)
        {
            GameObject asset = killerQueen ? view.killerQueenModel : view.d4cModel;
            view.portrait.enabled = asset != null;
            if (asset == null) return null;

            var portrait = new StandCoachPortrait(view.portrait);
            if (portrait.Build(view, asset, selector != null ? selector.CurrentModel : null, killerQueen))
                return portrait;
            portrait.Dispose();
            view.portrait.enabled = false;
            return null;
        }

        private bool Build(RunResultsView view, GameObject asset, GameObject source, bool killerQueen)
        {
            scene = new GameObject("Stand Coach portrait stage");
            GameObject model = Object.Instantiate(asset, Vector3.zero, Quaternion.identity, scene.transform);
            var animator = model.GetComponent<Animator>();
            if (animator == null || !animator.isHuman) return false;
            animator.runtimeAnimatorController = null;
            animator.applyRootMotion = false;
            StandCoachPose.PrepareRig(animator, killerQueen);
            model.transform.position = Stage;
            pose = new StandCoachPose(animator, killerQueen);

            var sourceRenderers = new Dictionary<string, Renderer>();
            if (source != null)
                foreach (Renderer renderer in source.GetComponentsInChildren<Renderer>(true))
                    sourceRenderers[RelativePath(renderer.transform, source.transform)] = renderer;

            Bounds bounds = default;
            bool hasMesh = false;
            var mesh = new Mesh();
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                renderer.gameObject.layer = Layer;
                if (sourceRenderers.TryGetValue(RelativePath(renderer.transform, model.transform), out Renderer original))
                {
                    renderer.enabled = original.enabled && original.gameObject.activeInHierarchy;
                    renderer.sharedMaterials = original.sharedMaterials;
                }
                if (killerQueen && renderer.name.EndsWith(" teeth", StringComparison.Ordinal))
                    renderer.enabled = false;
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                ApplyLighting(renderer, view.portraitShader);
                Bounds part = renderer.bounds;
                if (renderer is SkinnedMeshRenderer skin)
                {
                    skin.updateWhenOffscreen = true;
                    skin.BakeMesh(mesh);
                    part = WorldBounds(mesh.bounds, skin.transform);
                }
                if (hasMesh) bounds.Encapsulate(part);
                else bounds = part;
                hasMesh = true;
            }
            Object.Destroy(mesh);
            if (!hasMesh) return false;

            var cameraObject = new GameObject("Stand portrait camera", typeof(Camera));
            cameraObject.transform.SetParent(scene.transform, false);
            camera = cameraObject.GetComponent<Camera>();
            camera.cullingMask = 1 << Layer;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = view.stageColor;
            camera.orthographic = true;
            camera.farClipPlane = 30f;
            camera.allowHDR = false;
            Vector2 size = image.rectTransform.rect.size;
            float aspect = size.y > 0f ? Mathf.Max(0.5f, size.x / size.y) : 1f;
            camera.orthographicSize = Mathf.Max(bounds.extents.y * 1.13f, bounds.extents.x * 1.24f / aspect, 0.2f);
            Vector3 focus = bounds.center + Vector3.up * (0.025f * bounds.size.y);
            camera.transform.position = focus + new Vector3(0.10f, 0.035f, 2.2f) * bounds.size.y;
            camera.transform.LookAt(focus);
            picture = new RenderTexture(1024, Mathf.RoundToInt(1024f / aspect), 24, RenderTextureFormat.ARGB32);
            picture.antiAliasing = 2;
            camera.targetTexture = picture;
            image.texture = picture;
            return true;
        }

        public void Animate(bool speaking, float sentenceProgress)
        {
            redraw |= pose.Update(speaking, sentenceProgress);
            camera.enabled = redraw && Time.unscaledTime >= nextFrame;
            if (camera.enabled)
            {
                redraw = false;
                nextFrame = Time.unscaledTime + 1f / 30f;
            }
        }

        private void ApplyLighting(Renderer renderer, Shader shader)
        {
            if (shader == null) return;
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                Material original = materials[i];
                if (original == null) continue;
                if (!stageMaterials.TryGetValue(original, out Material material))
                {
                    material = new Material(shader)
                    {
                        mainTexture = original.mainTexture,
                        mainTextureOffset = original.mainTextureOffset,
                        mainTextureScale = original.mainTextureScale,
                        color = original.HasProperty("_Color") ? original.color : Color.white
                    };
                    stageMaterials.Add(original, material);
                }
                materials[i] = material;
            }
            renderer.sharedMaterials = materials;
        }

        private static string RelativePath(Transform node, Transform root)
        {
            if (node == root) return "";
            return RelativePath(node.parent, root) + "/" + node.name;
        }

        private static Bounds WorldBounds(Bounds local, Transform node)
        {
            // BakeMesh returns the positions with the skin's effective scale.
            // Applying TransformPoint would multiply D4C's 100x FBX scale again.
            var bounds = new Bounds(node.position + node.rotation * local.center, Vector3.zero);
            Vector3 e = local.extents;
            for (int corner = 0; corner < 8; corner++)
                bounds.Encapsulate(node.position + node.rotation * (local.center + new Vector3(
                    (corner & 1) == 0 ? -e.x : e.x, (corner & 2) == 0 ? -e.y : e.y, (corner & 4) == 0 ? -e.z : e.z)));
            return bounds;
        }

        public void Dispose()
        {
            if (image != null) image.texture = null;
            if (scene != null)
            {
                scene.SetActive(false);
                Object.Destroy(scene);
            }
            if (picture != null)
            {
                picture.Release();
                Object.Destroy(picture);
            }
            foreach (Material material in stageMaterials.Values) Object.Destroy(material);
        }
    }
}
