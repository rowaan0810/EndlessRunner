// TrackManager.cs — Manages the infinite scrolling track.
// Object-pools track segments and recycles them as they pass behind the camera.

using System.Collections.Generic;
using UnityEngine;
using EndlessRunner.Core;

namespace EndlessRunner.Track
{
    /// <summary>
    /// Spawns and recycles track segments to create an infinite scrolling road.
    /// The player stays at Z=0, and the world moves toward them.
    /// </summary>
    public class TrackManager : MonoBehaviour
    {
        [Header("Track Settings")]
        [SerializeField] private GameObject trackSegmentPrefab;
        [SerializeField] private int poolSize = 8;
        [SerializeField] private float segmentLength = 20f;

        [Header("Lane Visual Settings")]
        [SerializeField] private float laneWidth = 2.5f;

        [Header("Cyberpunk Environment Props")]
        public List<GameObject> sideProps = new List<GameObject>();
        public List<GameObject> lampProps = new List<GameObject>();

        private List<TrackSegment> segments = new List<TrackSegment>();
        private float nextSpawnZ;
        private float cameraPosZ;

        // Cached shader for material creation
        private static Shader cachedShader;

        private void Start()
        {
            cameraPosZ = Camera.main != null ? Camera.main.transform.position.z : 0f;
            InitializeTrack();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameRestart.AddListener(ResetTrack);
            }
        }

        private void InitializeTrack()
        {
            nextSpawnZ = -segmentLength;

            for (int i = 0; i < poolSize; i++)
            {
                SpawnSegment();
            }
        }

        private void SpawnSegment()
        {
            GameObject segObj;

            if (trackSegmentPrefab != null)
            {
                segObj = Instantiate(trackSegmentPrefab, transform);
            }
            else
            {
                segObj = CreatePlaceholderSegment();
            }

            segObj.transform.position = new Vector3(0f, 0f, nextSpawnZ);
            nextSpawnZ += segmentLength;

            TrackSegment segment = segObj.GetComponent<TrackSegment>();
            if (segment == null)
            {
                segment = segObj.AddComponent<TrackSegment>();
            }
            segments.Add(segment);
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
                return;

            for (int i = 0; i < segments.Count; i++)
            {
                if (segments[i].IsBehindCamera(cameraPosZ))
                {
                    RecycleSegment(segments[i]);
                }
            }
        }

        private void RecycleSegment(TrackSegment segment)
        {
            // Find the furthest segment currently in the world
            float maxZ = float.MinValue;
            foreach (var seg in segments)
            {
                if (seg != segment && seg.transform.position.z > maxZ)
                {
                    maxZ = seg.transform.position.z;
                }
            }

            // Snap this segment perfectly to the end of the furthest one
            segment.transform.position = new Vector3(0f, 0f, maxZ + segmentLength);
        }

        /// <summary>
        /// Find a shader that works at runtime in Unity 6 URP.
        /// </summary>
        private static Shader GetShader()
        {
            if (cachedShader != null) return cachedShader;

            string[] candidates = {
                "Universal Render Pipeline/Lit",
                "Universal Render Pipeline/Simple Lit",
                "Universal Render Pipeline/Unlit",
                "Standard",
                "Unlit/Color",
                "Sprites/Default"
            };

            foreach (string name in candidates)
            {
                Shader s = Shader.Find(name);
                if (s != null && s.name != "Hidden/InternalErrorShader")
                {
                    cachedShader = s;
                    return s;
                }
            }

            // Grab shader from any existing renderer
            Renderer existing = FindAnyObjectByType<Renderer>();
            if (existing != null && existing.sharedMaterial != null)
            {
                cachedShader = existing.sharedMaterial.shader;
                return cachedShader;
            }

            cachedShader = Shader.Find("Unlit/Color");
            return cachedShader;
        }

        private static Material MakeMaterial(Color color)
        {
            Material mat = new Material(GetShader());
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);
            mat.color = color;
            return mat;
        }

        private GameObject CreatePlaceholderSegment()
        {
            GameObject segObj = new GameObject("TrackSegment");

            // Main road surface
            GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
            road.transform.SetParent(segObj.transform);
            road.transform.localPosition = new Vector3(0f, -0.05f, segmentLength / 2f);
            road.transform.localScale = new Vector3(laneWidth * 3f + 1f, 0.1f, segmentLength);

            Renderer roadRenderer = road.GetComponent<Renderer>();
            if (roadRenderer != null)
            {
                // Asphalt color
                roadRenderer.material = MakeMaterial(new Color(0.1f, 0.12f, 0.15f));
            }

            // Wide ground plane to fill the gaps to the buildings
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.SetParent(segObj.transform);
            ground.transform.localPosition = new Vector3(0f, -0.1f, segmentLength / 2f); // Slightly below the road
            ground.transform.localScale = new Vector3(100f, 0.1f, segmentLength); // 100 units wide

            Renderer groundRenderer = ground.GetComponent<Renderer>();
            if (groundRenderer != null)
            {
                // Dark pavement color
                groundRenderer.material = MakeMaterial(new Color(0.05f, 0.05f, 0.06f));
            }

            // Remove colliders (not needed for physics)
            Destroy(road.GetComponent<Collider>());
            Destroy(ground.GetComponent<Collider>());

            // Lane divider lines
            CreateLaneLine(segObj.transform, -laneWidth / 2f);
            CreateLaneLine(segObj.transform, laneWidth / 2f);

            // Edge lines
            CreateLaneLine(segObj.transform, -laneWidth * 1.5f, true);
            CreateLaneLine(segObj.transform, laneWidth * 1.5f, true);

            // Spawn Cyberpunk Scenery (Push buildings out by 25 units so they don't block the road)
            SpawnScenery(segObj.transform, -25f, 90f);  // Left side
            SpawnScenery(segObj.transform, 25f, -90f);  // Right side

            return segObj;
        }

        private void SpawnScenery(Transform parent, float xOffset, float yRotation)
        {
            // The road edge is around 4.5f
            float lampX = xOffset > 0 ? 5f : -5f;

            // Spawn Lamp
            if (lampProps.Count > 0)
            {
                GameObject lampPrefab = lampProps[Random.Range(0, lampProps.Count)];
                if (lampPrefab != null)
                {
                    GameObject lamp = Instantiate(lampPrefab, parent);
                    lamp.transform.localPosition = new Vector3(lampX, 0f, segmentLength / 2f);
                    lamp.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
                }
            }

            // Spawn Building
            if (sideProps.Count > 0)
            {
                GameObject buildingPrefab = sideProps[Random.Range(0, sideProps.Count)];
                if (buildingPrefab != null)
                {
                    GameObject building = Instantiate(buildingPrefab, parent);
                    // Place building further out and randomly offset along Z so they don't look perfectly aligned
                    float zOffset = Random.Range(segmentLength * 0.2f, segmentLength * 0.8f);
                    building.transform.localPosition = new Vector3(xOffset, 0f, zOffset);
                    building.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
                    // Buildings might need scaling depending on the asset pack, but we will leave default for now.
                }
            }
        }

        private void CreateLaneLine(Transform parent, float xPos, bool isEdge = false)
        {
            GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.transform.SetParent(parent);
            line.transform.localPosition = new Vector3(xPos, 0.01f, segmentLength / 2f);
            line.transform.localScale = new Vector3(isEdge ? 0.15f : 0.08f, 0.02f, segmentLength);

            Renderer lineRenderer = line.GetComponent<Renderer>();
            if (lineRenderer != null)
            {
                // Cyberpunk Neon colors (Cyan for inner, Magenta for edge)
                Color lineColor = isEdge ? new Color(1f, 0.2f, 0.8f) : new Color(0.2f, 1f, 1f);
                lineRenderer.material = MakeMaterial(lineColor);
            }

            Collider lineCollider = line.GetComponent<Collider>();
            if (lineCollider != null) Destroy(lineCollider);
        }

        public void ResetTrack()
        {
            foreach (var seg in segments)
            {
                if (seg != null) Destroy(seg.gameObject);
            }
            segments.Clear();

            nextSpawnZ = -segmentLength;
            for (int i = 0; i < poolSize; i++)
            {
                SpawnSegment();
            }
        }
    }
}
