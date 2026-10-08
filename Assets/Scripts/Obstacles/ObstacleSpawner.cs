// ObstacleSpawner.cs — Spawns obstacles and coins ahead of the player.
// Object pools all spawned objects for performance.

using System.Collections.Generic;
using UnityEngine;
using EndlessRunner.Core;
using EndlessRunner.Collectibles;

namespace EndlessRunner.Obstacles
{
    /// <summary>
    /// Spawns obstacles and coins at randomized intervals ahead of the player.
    /// Uses object pooling. Never blocks all 3 lanes simultaneously.
    /// </summary>
    public class ObstacleSpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private float spawnDistance = 80f;       // How far ahead to spawn
        [SerializeField] private float minInterval = 1.2f;       // Min seconds between obstacles
        [SerializeField] private float maxInterval = 3.0f;       // Max seconds between obstacles
        [SerializeField] private float coinChance = 0.6f;        // Chance to spawn coins between obstacles

        [Header("Lane Settings")]
        [SerializeField] private float laneWidth = 2.5f;         // Must match PlayerController

        [Header("Obstacle Dimensions")]
        [SerializeField] private float barrierLowHeight = 0.6f;  // Jump over this
        [SerializeField] private float barrierHighY = 1.5f;      // Duck under this
        [SerializeField] private float barrierHighHeight = 1.0f;
        [SerializeField] private float barrierFullHeight = 4.0f;  // Massive wall to prevent jumping

        [Header("Pool Settings")]
        [SerializeField] private int obstaclePoolSize = 20;
        [SerializeField] private int coinPoolSize = 40;

        // Object pools — one per obstacle type for distinct visuals
        private List<Obstacle> poolBarrierLow = new List<Obstacle>();
        private List<Obstacle> poolBarrierHigh = new List<Obstacle>();
        private List<Obstacle> poolBarrierFull = new List<Obstacle>();
        private List<Coin> coinPool = new List<Coin>();

        private float nextSpawnTime;
        private float nextCoinTime;

        // Cached shaders for creating materials
        private Shader cachedShader;

        private void Start()
        {
            CreatePools();
            ScheduleNextSpawn();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameRestart.AddListener(ResetSpawner);
            }
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
                return;

            // Spawn obstacles at intervals
            if (Time.time >= nextSpawnTime)
            {
                SpawnObstacleGroup();
                ScheduleNextSpawn();
            }

            // Spawn coins periodically
            if (Time.time >= nextCoinTime)
            {
                if (Random.value < coinChance)
                {
                    SpawnCoinLine();
                }
                nextCoinTime = Time.time + Random.Range(0.8f, 2.0f);
            }
        }

        private void ScheduleNextSpawn()
        {
            float speed = GameSpeed.Instance != null ? GameSpeed.Instance.Current : 10f;
            // Reduce interval as speed increases (more obstacles at higher speed)
            float speedFactor = Mathf.InverseLerp(10f, 30f, speed);
            float interval = Mathf.Lerp(maxInterval, minInterval, speedFactor);
            nextSpawnTime = Time.time + interval;
        }

        /// <summary>
        /// Spawn one or two obstacles. Never blocks all 3 lanes.
        /// </summary>
        private void SpawnObstacleGroup()
        {
            float roll = Random.value;

            if (roll < 0.35f)
            {
                // Single full-lane barrier — player must dodge sideways
                int lane = Random.Range(0, 3);
                SpawnObstacle(ObstacleType.BarrierFull, lane);
            }
            else if (roll < 0.55f)
            {
                // Two full-lane barriers — leaves one lane open
                int openLane = Random.Range(0, 3);
                for (int i = 0; i < 3; i++)
                {
                    if (i != openLane) SpawnObstacle(ObstacleType.BarrierFull, i);
                }
            }
            else if (roll < 0.75f)
            {
                // Low barrier across all lanes — must jump
                SpawnObstacle(ObstacleType.BarrierLow, -1); // -1 = all lanes
            }
            else
            {
                // High barrier across all lanes — must duck
                SpawnObstacle(ObstacleType.BarrierHigh, -1); // -1 = all lanes
            }
        }

        private void SpawnObstacle(ObstacleType type, int lane)
        {
            Obstacle obs = GetPooledObstacle(type);
            if (obs == null) return;

            float xPos;
            float width;

            if (lane == -1)
            {
                // Spans all lanes
                xPos = 0f;
                width = laneWidth * 3f + 0.5f;
            }
            else
            {
                // Single lane
                xPos = (lane - 1) * laneWidth;
                width = laneWidth * 0.8f;
            }

            float yPos, height;

            switch (type)
            {
                case ObstacleType.BarrierLow:
                    yPos = barrierLowHeight / 2f;
                    height = barrierLowHeight;
                    break;
                case ObstacleType.BarrierHigh:
                    yPos = barrierHighY + barrierHighHeight / 2f;
                    height = barrierHighHeight;
                    break;
                case ObstacleType.BarrierFull:
                default:
                    yPos = barrierFullHeight / 2f;
                    height = barrierFullHeight;
                    break;
            }

            Vector3 position = new Vector3(xPos, yPos, spawnDistance);
            obs.Setup(position, lane, type);

            // Scale the visual container to match lane width
            Transform visual = obs.transform.Find("Visual");
            if (visual != null)
            {
                // Scale X to fit the lane(s), keep Y/Z at 1 (each builder sets its own proportions)
                visual.localScale = new Vector3(width, 1f, 1f);
                visual.localPosition = Vector3.zero;
            }

            // Update the trigger collider to match
            BoxCollider trigger = obs.GetComponent<BoxCollider>();
            if (trigger != null)
            {
                trigger.size = new Vector3(width, height, 0.5f);
                trigger.center = Vector3.zero;
            }

        }

        /// <summary>
        /// Spawn a line of coins along a random lane.
        /// </summary>
        private void SpawnCoinLine()
        {
            int lane = Random.Range(0, 3);
            float xPos = (lane - 1) * laneWidth;
            int count = Random.Range(3, 7);
            float spacing = 2.5f;

            for (int i = 0; i < count; i++)
            {
                Coin coin = GetPooledCoin();
                if (coin == null) break;

                Vector3 pos = new Vector3(xPos, 1.0f, spawnDistance + (i * spacing));
                coin.Setup(pos);
            }
        }

        #region Object Pooling

        private void CreatePools()
        {
            // Create obstacle pools — one per type
            int perType = obstaclePoolSize / 3;
            for (int i = 0; i < perType + 2; i++) // A few extra for Full since it's spawned in pairs
            {
                poolBarrierLow.Add(CreatePooledObstacle(ObstacleType.BarrierLow));
                poolBarrierHigh.Add(CreatePooledObstacle(ObstacleType.BarrierHigh));
                poolBarrierFull.Add(CreatePooledObstacle(ObstacleType.BarrierFull));
            }

            // Create coin pool
            for (int i = 0; i < coinPoolSize; i++)
            {
                GameObject obj = CreateCoinPrefab();
                obj.SetActive(false);
                obj.transform.SetParent(transform);
                coinPool.Add(obj.GetComponent<Coin>());
            }
        }

        private Obstacle CreatePooledObstacle(ObstacleType type)
        {
            GameObject obj = CreateObstaclePrefab(type);
            obj.SetActive(false);
            obj.transform.SetParent(transform);
            return obj.GetComponent<Obstacle>();
        }

        private List<Obstacle> GetPool(ObstacleType type)
        {
            return type switch
            {
                ObstacleType.BarrierLow => poolBarrierLow,
                ObstacleType.BarrierHigh => poolBarrierHigh,
                _ => poolBarrierFull
            };
        }

        private Obstacle GetPooledObstacle(ObstacleType type)
        {
            var pool = GetPool(type);
            foreach (var obs in pool)
            {
                if (!obs.gameObject.activeInHierarchy)
                    return obs;
            }
            // Pool exhausted — create new
            Obstacle newObs = CreatePooledObstacle(type);
            pool.Add(newObs);
            return newObs;
        }

        private Coin GetPooledCoin()
        {
            foreach (var coin in coinPool)
            {
                if (!coin.gameObject.activeInHierarchy)
                    return coin;
            }
            // Pool exhausted — create new
            GameObject obj = CreateCoinPrefab();
            obj.SetActive(false);
            obj.transform.SetParent(transform);
            Coin newCoin = obj.GetComponent<Coin>();
            coinPool.Add(newCoin);
            return newCoin;
        }

        #endregion

        #region Cyberpunk Obstacle Builders

        /// <summary>
        /// Create an obstacle prefab with type-specific cyberpunk visuals.
        /// </summary>
        private GameObject CreateObstaclePrefab(ObstacleType type)
        {
            GameObject obj = new GameObject($"Obstacle_{type}");
            obj.AddComponent<Obstacle>();

            // Visual container — will be scaled by SpawnObstacle to fit lanes
            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(obj.transform);
            visual.transform.localPosition = Vector3.zero;

            switch (type)
            {
                case ObstacleType.BarrierLow:
                    BuildBarrierLow(visual);
                    break;
                case ObstacleType.BarrierHigh:
                    BuildBarrierHigh(visual);
                    break;
                case ObstacleType.BarrierFull:
                    BuildBarrierFull(visual);
                    break;
            }

            // Add trigger collider to the parent (sized later by SpawnObstacle)
            BoxCollider trigger = obj.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(1f, 1f, 0.5f);

            // Set layer
            obj.layer = LayerMask.NameToLayer("Obstacle") >= 0 ? LayerMask.NameToLayer("Obstacle") : 0;

            // Kinematic rigidbody for trigger detection
            Rigidbody rb = obj.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            return obj;
        }

        /// <summary>
        /// BarrierLow: Glowing cyan laser tripwire — two small posts with a bright beam between them.
        /// Player must JUMP over this.
        /// </summary>
        private void BuildBarrierLow(GameObject parent)
        {
            Color cyan = new Color(0f, 0.9f, 1f);
            Color darkMetal = new Color(0.15f, 0.15f, 0.2f);

            // Left post
            GameObject postL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            postL.name = "PostL";
            postL.transform.SetParent(parent.transform);
            postL.transform.localPosition = new Vector3(-0.45f, 0f, 0f);
            postL.transform.localScale = new Vector3(0.08f, 0.6f, 0.08f);
            DestroyCollider(postL);
            SetMaterialColor(postL.GetComponent<Renderer>(), darkMetal);

            // Right post
            GameObject postR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            postR.name = "PostR";
            postR.transform.SetParent(parent.transform);
            postR.transform.localPosition = new Vector3(0.45f, 0f, 0f);
            postR.transform.localScale = new Vector3(0.08f, 0.6f, 0.08f);
            DestroyCollider(postR);
            SetMaterialColor(postR.GetComponent<Renderer>(), darkMetal);

            // Laser beam (the main barrier)
            GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beam.name = "LaserBeam";
            beam.transform.SetParent(parent.transform);
            beam.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            beam.transform.localScale = new Vector3(0.9f, 0.12f, 0.15f);
            DestroyCollider(beam);
            SetEmissiveMaterial(beam.GetComponent<Renderer>(), cyan, 3f);

            // Second thin beam below for extra detail
            GameObject beam2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beam2.name = "LaserBeam2";
            beam2.transform.SetParent(parent.transform);
            beam2.transform.localPosition = new Vector3(0f, -0.05f, 0f);
            beam2.transform.localScale = new Vector3(0.9f, 0.05f, 0.15f);
            DestroyCollider(beam2);
            SetEmissiveMaterial(beam2.GetComponent<Renderer>(), cyan * 0.6f, 2f);

            // Small indicator lights on posts
            AddIndicatorLight(parent, new Vector3(-0.45f, 0.35f, 0.05f), cyan);
            AddIndicatorLight(parent, new Vector3(0.45f, 0.35f, 0.05f), cyan);
        }

        /// <summary>
        /// BarrierHigh: Bright overhead canopy with dangling warning strips.
        /// Visually distinct from BarrierLow — big, orange, impossible to miss.
        /// Player must DUCK under this.
        /// </summary>
        private void BuildBarrierHigh(GameObject parent)
        {
            Color orange = new Color(1f, 0.5f, 0f);
            Color brightYellow = new Color(1f, 0.9f, 0f);
            Color darkMetal = new Color(0.15f, 0.15f, 0.2f);
            Color dangerRed = new Color(1f, 0.2f, 0.1f);

            // Left pillar — thicker than BarrierLow posts
            GameObject pillarL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillarL.name = "PillarL";
            pillarL.transform.SetParent(parent.transform);
            pillarL.transform.localPosition = new Vector3(-0.45f, -0.1f, 0f);
            pillarL.transform.localScale = new Vector3(0.1f, 1.2f, 0.1f);
            DestroyCollider(pillarL);
            SetEmissiveMaterial(pillarL.GetComponent<Renderer>(), brightYellow, 1.5f);

            // Right pillar
            GameObject pillarR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillarR.name = "PillarR";
            pillarR.transform.SetParent(parent.transform);
            pillarR.transform.localPosition = new Vector3(0.45f, -0.1f, 0f);
            pillarR.transform.localScale = new Vector3(0.1f, 1.2f, 0.1f);
            DestroyCollider(pillarR);
            SetEmissiveMaterial(pillarR.GetComponent<Renderer>(), brightYellow, 1.5f);

            // Wide overhead canopy — the main visual cue
            GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Cube);
            canopy.name = "Canopy";
            canopy.transform.SetParent(parent.transform);
            canopy.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            canopy.transform.localScale = new Vector3(0.95f, 0.25f, 0.5f);
            DestroyCollider(canopy);
            SetEmissiveMaterial(canopy.GetComponent<Renderer>(), orange, 3f);

            // Dangling strips (chevron-like) — 3 hanging elements below canopy
            for (int i = -1; i <= 1; i++)
            {
                GameObject strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
                strip.name = $"DangleStrip_{i}";
                strip.transform.SetParent(parent.transform);
                strip.transform.localPosition = new Vector3(i * 0.25f, 0.15f, 0f);
                strip.transform.localScale = new Vector3(0.06f, 0.3f, 0.35f);
                DestroyCollider(strip);
                SetEmissiveMaterial(strip.GetComponent<Renderer>(), dangerRed, 2f);
            }

            // Bottom warning bar — bright yellow hazard line
            GameObject bottomBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bottomBar.name = "BottomBar";
            bottomBar.transform.SetParent(parent.transform);
            bottomBar.transform.localPosition = new Vector3(0f, -0.02f, 0f);
            bottomBar.transform.localScale = new Vector3(0.95f, 0.08f, 0.4f);
            DestroyCollider(bottomBar);
            SetEmissiveMaterial(bottomBar.GetComponent<Renderer>(), brightYellow, 4f);

            // Indicator lights — orange glow on top corners
            AddIndicatorLight(parent, new Vector3(-0.45f, 0.55f, 0.06f), orange);
            AddIndicatorLight(parent, new Vector3(0.45f, 0.55f, 0.06f), orange);
            AddIndicatorLight(parent, new Vector3(0f, 0.55f, 0.06f), brightYellow);
        }

        /// <summary>
        /// BarrierFull: Cyberpunk barricade — chunky body with warning stripes.
        /// Blocks one lane. Player must DODGE sideways.
        /// </summary>
        private void BuildBarrierFull(GameObject parent)
        {
            Color red = new Color(1f, 0.15f, 0.15f);
            Color darkMetal = new Color(0.12f, 0.12f, 0.18f);
            Color warningYellow = new Color(1f, 0.7f, 0f);

            // Main body - Made massive and thick to indicate it cannot be jumped
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(parent.transform);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(0.85f, 3.8f, 0.6f);
            DestroyCollider(body);
            SetMaterialColor(body.GetComponent<Renderer>(), darkMetal);

            // Top warning bar (red glow)
            GameObject topBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            topBar.name = "TopBar";
            topBar.transform.SetParent(parent.transform);
            topBar.transform.localPosition = new Vector3(0f, 1.8f, 0f);
            topBar.transform.localScale = new Vector3(0.9f, 0.2f, 0.7f);
            DestroyCollider(topBar);
            SetEmissiveMaterial(topBar.GetComponent<Renderer>(), red, 2.5f);

            // Middle warning stripe (yellow)
            GameObject midStripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            midStripe.name = "MidStripe";
            midStripe.transform.SetParent(parent.transform);
            midStripe.transform.localPosition = new Vector3(0f, 0.2f, 0.31f);
            midStripe.transform.localScale = new Vector3(0.86f, 0.3f, 0.02f);
            DestroyCollider(midStripe);
            SetEmissiveMaterial(midStripe.GetComponent<Renderer>(), warningYellow, 1.5f);

            // Bottom warning stripe (yellow)
            GameObject botStripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            botStripe.name = "BotStripe";
            botStripe.transform.SetParent(parent.transform);
            botStripe.transform.localPosition = new Vector3(0f, -0.6f, 0.31f);
            botStripe.transform.localScale = new Vector3(0.86f, 0.3f, 0.02f);
            DestroyCollider(botStripe);
            SetEmissiveMaterial(botStripe.GetComponent<Renderer>(), warningYellow, 1.5f);

            // Bottom base
            GameObject baseBlock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseBlock.name = "Base";
            baseBlock.transform.SetParent(parent.transform);
            baseBlock.transform.localPosition = new Vector3(0f, -1.8f, 0f);
            baseBlock.transform.localScale = new Vector3(0.95f, 0.3f, 0.8f);
            DestroyCollider(baseBlock);
            SetMaterialColor(baseBlock.GetComponent<Renderer>(), darkMetal * 0.8f);

            // Side indicator lights
            AddIndicatorLight(parent, new Vector3(-0.44f, 1.8f, 0.36f), red);
            AddIndicatorLight(parent, new Vector3(0.44f, 1.8f, 0.36f), red);
        }

        #endregion

        #region Coin Builder

        private GameObject CreateCoinPrefab()
        {
            GameObject obj = new GameObject("Coin");
            Coin coin = obj.AddComponent<Coin>();

            Color gold = new Color(1f, 0.85f, 0.15f);
            Color innerGold = new Color(1f, 0.7f, 0f);

            // Outer rim — a flattened cylinder
            GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rim.name = "Rim";
            rim.transform.SetParent(obj.transform);
            rim.transform.localPosition = Vector3.zero;
            rim.transform.localScale = new Vector3(0.6f, 0.04f, 0.6f);
            rim.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            DestroyCollider(rim);
            SetEmissiveMaterial(rim.GetComponent<Renderer>(), gold, 2f);

            // Inner face — slightly smaller, different shade
            GameObject face = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            face.name = "Face";
            face.transform.SetParent(obj.transform);
            face.transform.localPosition = new Vector3(0.01f, 0f, 0f);
            face.transform.localScale = new Vector3(0.42f, 0.05f, 0.42f);
            face.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            DestroyCollider(face);
            SetEmissiveMaterial(face.GetComponent<Renderer>(), innerGold, 3f);

            // Center gem — a tiny bright sphere
            GameObject gem = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            gem.name = "CenterGem";
            gem.transform.SetParent(obj.transform);
            gem.transform.localPosition = new Vector3(0.03f, 0f, 0f);
            gem.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);
            DestroyCollider(gem);
            SetEmissiveMaterial(gem.GetComponent<Renderer>(), new Color(1f, 1f, 0.5f), 5f);

            // Trigger collider
            SphereCollider trigger = obj.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.4f;

            // Rigidbody for triggers
            Rigidbody rb = obj.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            return obj;
        }

        #endregion

        #region Material Helpers

        /// <summary>
        /// Remove the auto-generated collider from a primitive.
        /// </summary>
        private void DestroyCollider(GameObject primitive)
        {
            Collider col = primitive.GetComponent<Collider>();
            if (col != null) Destroy(col);
        }

        /// <summary>
        /// Add a tiny glowing indicator light sphere.
        /// </summary>
        private void AddIndicatorLight(GameObject parent, Vector3 localPos, Color color)
        {
            GameObject light = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            light.name = "Light";
            light.transform.SetParent(parent.transform);
            light.transform.localPosition = localPos;
            light.transform.localScale = new Vector3(0.06f, 0.06f, 0.06f);
            DestroyCollider(light);
            SetEmissiveMaterial(light.GetComponent<Renderer>(), color, 5f);
        }

        private Shader GetShader()
        {
            if (cachedShader != null) return cachedShader;

            string[] candidates = {
                "Universal Render Pipeline/Lit",
                "Universal Render Pipeline/Simple Lit",
                "Standard",
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

            // Grab from existing renderer
            Renderer existing = FindAnyObjectByType<Renderer>();
            if (existing != null && existing.sharedMaterial != null)
                cachedShader = existing.sharedMaterial.shader;

            return cachedShader;
        }

        /// <summary>
        /// Create a solid-color material (non-emissive).
        /// </summary>
        private void SetMaterialColor(Renderer rend, Color color)
        {
            Material mat = new Material(GetShader());
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            mat.color = color;
            rend.material = mat;
        }

        /// <summary>
        /// Create a material with emissive glow for the cyberpunk neon look.
        /// </summary>
        private void SetEmissiveMaterial(Renderer rend, Color color, float intensity)
        {
            Material mat = new Material(GetShader());

            // Base color
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            mat.color = color;

            // Emissive glow
            Color emissive = color * intensity;
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.SetColor("_EmissionColor", emissive);
                mat.EnableKeyword("_EMISSION");
            }
            if (mat.HasProperty("_EmissiveColor"))
            {
                mat.SetColor("_EmissiveColor", emissive);
            }

            // For URP Lit shader — set surface type and metallic/smoothness
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.7f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.85f);

            rend.material = mat;
        }

        #endregion

        /// <summary>
        /// Reset spawner state. Called on game restart.
        /// </summary>
        public void ResetSpawner()
        {
            // Deactivate all pooled objects
            foreach (var obs in poolBarrierLow)
            {
                if (obs != null) obs.gameObject.SetActive(false);
            }
            foreach (var obs in poolBarrierHigh)
            {
                if (obs != null) obs.gameObject.SetActive(false);
            }
            foreach (var obs in poolBarrierFull)
            {
                if (obs != null) obs.gameObject.SetActive(false);
            }
            foreach (var coin in coinPool)
            {
                if (coin != null) coin.gameObject.SetActive(false);
            }

            nextSpawnTime = Time.time + 2f; // Brief grace period on restart
            nextCoinTime = Time.time + 1f;
        }
    }
}
