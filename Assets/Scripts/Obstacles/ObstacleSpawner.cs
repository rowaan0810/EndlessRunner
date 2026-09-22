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
        [SerializeField] private float barrierFullHeight = 2.5f;  // Full lane block

        [Header("Pool Settings")]
        [SerializeField] private int obstaclePoolSize = 20;
        [SerializeField] private int coinPoolSize = 40;

        // Object pools
        private List<Obstacle> obstaclePool = new List<Obstacle>();
        private List<Coin> coinPool = new List<Coin>();

        private float nextSpawnTime;
        private float nextCoinTime;

        // Cached shader for creating materials
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
            Obstacle obs = GetPooledObstacle();
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

            // Set visual scale
            Transform visual = obs.transform.GetChild(0);
            if (visual != null)
            {
                visual.localScale = new Vector3(width, height, 0.5f);
                visual.localPosition = Vector3.zero;

                // Set color based on type
                Renderer rend = visual.GetComponent<Renderer>();
                if (rend != null)
                {
                    Color color = type switch
                    {
                        ObstacleType.BarrierLow => new Color(0.9f, 0.3f, 0.2f),   // Red — jump
                        ObstacleType.BarrierHigh => new Color(1.0f, 0.6f, 0.1f),   // Orange — duck
                        ObstacleType.BarrierFull => new Color(0.8f, 0.2f, 0.3f),   // Dark red — dodge
                        _ => Color.red
                    };
                    SetMaterialColor(rend, color);
                }
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
            // Create obstacle pool
            for (int i = 0; i < obstaclePoolSize; i++)
            {
                GameObject obj = CreateObstaclePrefab();
                obj.SetActive(false);
                obj.transform.SetParent(transform);
                obstaclePool.Add(obj.GetComponent<Obstacle>());
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

        private Obstacle GetPooledObstacle()
        {
            foreach (var obs in obstaclePool)
            {
                if (!obs.gameObject.activeInHierarchy)
                    return obs;
            }
            // Pool exhausted — create new
            GameObject obj = CreateObstaclePrefab();
            obj.SetActive(false);
            obj.transform.SetParent(transform);
            Obstacle newObs = obj.GetComponent<Obstacle>();
            obstaclePool.Add(newObs);
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

        #region Prefab Creation (Placeholder visuals)

        private GameObject CreateObstaclePrefab()
        {
            GameObject obj = new GameObject("Obstacle");
            Obstacle obs = obj.AddComponent<Obstacle>();

            // Visual child — a cube
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "Visual";
            visual.transform.SetParent(obj.transform);
            visual.transform.localPosition = Vector3.zero;

            // The visual's collider becomes the trigger
            Collider visualCollider = visual.GetComponent<Collider>();
            if (visualCollider != null) Destroy(visualCollider);

            // Add trigger collider to the parent
            BoxCollider trigger = obj.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(1f, 1f, 0.5f);

            // Set layer to Obstacle (if it exists) or Default
            obj.layer = LayerMask.NameToLayer("Obstacle") >= 0 ? LayerMask.NameToLayer("Obstacle") : 0;

            // Add Rigidbody for trigger detection (kinematic)
            Rigidbody rb = obj.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            return obj;
        }

        private GameObject CreateCoinPrefab()
        {
            GameObject obj = new GameObject("Coin");
            Coin coin = obj.AddComponent<Coin>();

            // Visual — a flattened cylinder (disc)
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = "Visual";
            visual.transform.SetParent(obj.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(0.5f, 0.05f, 0.5f);
            visual.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

            // Remove visual's collider
            Collider visualCollider = visual.GetComponent<Collider>();
            if (visualCollider != null) Destroy(visualCollider);

            // Set color to gold
            Renderer rend = visual.GetComponent<Renderer>();
            if (rend != null)
            {
                SetMaterialColor(rend, new Color(1f, 0.85f, 0.1f));
            }

            // Trigger collider on the coin
            SphereCollider trigger = obj.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.5f;

            // Rigidbody for triggers
            Rigidbody rb = obj.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            return obj;
        }

        private Shader GetShader()
        {
            if (cachedShader != null) return cachedShader;

            string[] candidates = {
                "Universal Render Pipeline/Lit",
                "Universal Render Pipeline/Simple Lit",
                "Universal Render Pipeline/Unlit",
                "Standard",
                "Unlit/Color"
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

        private void SetMaterialColor(Renderer rend, Color color)
        {
            Material mat = new Material(GetShader());
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            mat.color = color;
            rend.material = mat;
        }

        #endregion

        /// <summary>
        /// Reset spawner state. Called on game restart.
        /// </summary>
        public void ResetSpawner()
        {
            // Deactivate all pooled objects
            foreach (var obs in obstaclePool)
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
