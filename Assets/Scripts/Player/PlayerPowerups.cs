// PlayerPowerups.cs — Character-specific power-up system.
// Each character has a unique ability activated by the double middle finger gesture.
//
// David  (index 0) → Shield:     Absorbs one obstacle hit
// Ty     (index 1) → Coin Magnet: Auto-collects coins from all lanes for 8s
// Big Vegas (index 2) → 2x Score: Doubles all points for 10s
// Tung Tung (index 3) → Tung Smash: Destroys all obstacles ahead for 6s

using UnityEngine;
using EndlessRunner.Core;
using EndlessRunner.Input;

namespace EndlessRunner.Player
{
    public enum PowerUpType
    {
        None,
        Shield,
        CoinMagnet,
        ScoreMultiplier,
        TungSmash
    }

    /// <summary>
    /// Manages character-specific power-ups. Listens for the power-up gesture
    /// and activates the ability mapped to the current character.
    /// </summary>
    public class PlayerPowerups : MonoBehaviour
    {
        public static PlayerPowerups Instance { get; private set; }

        [Header("Power-Up Durations")]
        [SerializeField] private float magnetDuration = 8f;
        [SerializeField] private float scoreDuration = 10f;
        [SerializeField] private float smashDuration = 6f;

        [Header("Magnet Settings")]
        [SerializeField] private float magnetRadius = 8f;
        [SerializeField] private float magnetPullSpeed = 15f;

        // Current active power-up state
        public PowerUpType ActivePowerUp { get; private set; } = PowerUpType.None;
        public float PowerUpTimeRemaining { get; private set; }
        public float PowerUpTotalDuration { get; private set; }

        // Shield is a one-shot, not timed
        public bool ShieldActive { get; private set; }

        // Score multiplier
        public int ScoreMultiplier => ActivePowerUp == PowerUpType.ScoreMultiplier ? 2 : 1;

        // References
        private CharacterSelector characterSelector;
        private PlayerCollision playerCollision;

        // Visual effects
        private GameObject shieldVisual;
        private GameObject magnetVisual;
        private GameObject scoreVisual;
        private GameObject smashVisual;

        // Cooldown to prevent spamming (one power-up per run or per long cooldown)
        private float lastActivationTime = -999f;
        private const float ACTIVATION_COOLDOWN = 15f;

        // Keyboard trigger for testing (P key)
        private bool keyboardPowerUpRequested;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            characterSelector = GetComponent<CharacterSelector>();
            playerCollision = GetComponent<PlayerCollision>();
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
                return;

            // Check for power-up activation (webcam gesture or keyboard)
            CheckForActivation();

            // Tick down timed power-ups
            if (ActivePowerUp != PowerUpType.None && ActivePowerUp != PowerUpType.Shield)
            {
                PowerUpTimeRemaining -= Time.deltaTime;
                if (PowerUpTimeRemaining <= 0f)
                {
                    DeactivatePowerUp();
                }
            }

            // Magnet pull logic
            if (ActivePowerUp == PowerUpType.CoinMagnet)
            {
                PullCoins();
            }

            // Smash logic — destroy obstacles in front
            if (ActivePowerUp == PowerUpType.TungSmash)
            {
                SmashObstacles();
            }
        }

        private void CheckForActivation()
        {
            // Keyboard shortcut for testing
            if (UnityEngine.Input.GetKeyDown(KeyCode.P))
            {
                keyboardPowerUpRequested = true;
            }

            // Check webcam gesture
            bool gestureRequested = false;
            var poseInput = InputManager.Instance?.GetComponent<PoseInput>();
            if (poseInput != null && poseInput.PowerUpRequested)
            {
                gestureRequested = true;
                poseInput.ConsumePowerUp();
            }

            if ((gestureRequested || keyboardPowerUpRequested) && Time.time - lastActivationTime > ACTIVATION_COOLDOWN)
            {
                keyboardPowerUpRequested = false;
                ActivatePowerUp();
            }
            else
            {
                keyboardPowerUpRequested = false;
            }
        }

        private void ActivatePowerUp()
        {
            // Determine which power-up based on current character
            int charIndex = characterSelector != null ? characterSelector.CurrentIndex : 0;
            string charName = characterSelector != null ? characterSelector.GetCurrentCharacterName() : "DAVID";

            PowerUpType type;
            float duration;

            switch (charIndex)
            {
                case 0: // David
                    type = PowerUpType.Shield;
                    duration = 0f; // Shield is one-shot, not timed
                    break;
                case 1: // Ty
                    type = PowerUpType.CoinMagnet;
                    duration = magnetDuration;
                    break;
                case 2: // Big Vegas
                    type = PowerUpType.ScoreMultiplier;
                    duration = scoreDuration;
                    break;
                case 3: // Tung Tung
                    type = PowerUpType.TungSmash;
                    duration = smashDuration;
                    break;
                default:
                    type = PowerUpType.Shield; // Fallback
                    duration = 0f;
                    break;
            }

            // Don't stack power-ups
            if (ActivePowerUp != PowerUpType.None)
            {
                DeactivatePowerUp();
            }

            ActivePowerUp = type;
            PowerUpTotalDuration = duration;
            PowerUpTimeRemaining = duration;
            lastActivationTime = Time.time;

            // Apply immediate effects
            if (type == PowerUpType.Shield)
            {
                ShieldActive = true;
                if (playerCollision != null)
                    playerCollision.IsShielded = true;
                CreateShieldVisual();
            }
            else if (type == PowerUpType.CoinMagnet)
            {
                CreateMagnetVisual();
            }
            else if (type == PowerUpType.ScoreMultiplier)
            {
                CreateScoreVisual();
            }
            else if (type == PowerUpType.TungSmash)
            {
                CreateSmashVisual();
            }

            Debug.Log($"[PowerUp] {charName} activated {type}! Duration: {duration}s");
        }

        private void DeactivatePowerUp()
        {
            Debug.Log($"[PowerUp] {ActivePowerUp} expired!");

            // Clean up visuals
            if (shieldVisual != null) Destroy(shieldVisual);
            if (magnetVisual != null) Destroy(magnetVisual);
            if (scoreVisual != null) Destroy(scoreVisual);
            if (smashVisual != null) Destroy(smashVisual);

            ActivePowerUp = PowerUpType.None;
            PowerUpTimeRemaining = 0f;
            PowerUpTotalDuration = 0f;
        }

        /// <summary>
        /// Called by PlayerCollision when the shield absorbs a hit.
        /// </summary>
        public void ConsumeShield()
        {
            ShieldActive = false;
            if (shieldVisual != null) Destroy(shieldVisual);
            ActivePowerUp = PowerUpType.None;
            Debug.Log("[PowerUp] Shield consumed!");
        }

        #region Magnet Logic

        private void PullCoins()
        {
            // Find all active coins and pull them toward the player
            var coins = FindObjectsByType<Collectibles.Coin>(FindObjectsSortMode.None);
            Vector3 playerPos = transform.position;

            foreach (var coin in coins)
            {
                if (!coin.gameObject.activeInHierarchy) continue;

                float dist = Vector3.Distance(coin.transform.position, playerPos);
                if (dist < magnetRadius)
                {
                    // Pull coin toward player
                    Vector3 dir = (playerPos - coin.transform.position).normalized;
                    coin.transform.position += dir * magnetPullSpeed * Time.deltaTime;
                }
            }
        }

        #endregion

        #region Smash Logic

        private void SmashObstacles()
        {
            // Find all active obstacles in front of the player and destroy them
            var obstacles = FindObjectsByType<Obstacles.Obstacle>(FindObjectsSortMode.None);
            Vector3 playerPos = transform.position;

            foreach (var obs in obstacles)
            {
                if (!obs.gameObject.activeInHierarchy) continue;

                float dist = obs.transform.position.z - playerPos.z;
                // Destroy obstacles within 15 units ahead
                if (dist > 0f && dist < 15f)
                {
                    obs.gameObject.SetActive(false);
                }
            }
        }

        #endregion

        #region Visual Effects

        private void CreateShieldVisual()
        {
            shieldVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shieldVisual.name = "ShieldBubble";
            shieldVisual.transform.SetParent(transform);
            shieldVisual.transform.localPosition = new Vector3(0f, 1f, 0f);
            shieldVisual.transform.localScale = new Vector3(2f, 2.5f, 2f);

            // Remove collider
            Collider col = shieldVisual.GetComponent<Collider>();
            if (col != null) Destroy(col);

            // Semi-transparent cyan shield
            Renderer rend = shieldVisual.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            Color shieldColor = new Color(0f, 0.8f, 1f, 0.25f);
            mat.color = shieldColor;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", shieldColor);
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f); // Transparent
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.SetColor("_EmissionColor", new Color(0f, 0.5f, 1f) * 2f);
                mat.EnableKeyword("_EMISSION");
            }
            rend.material = mat;
        }

        private void CreateMagnetVisual()
        {
            magnetVisual = new GameObject("MagnetAura");
            magnetVisual.transform.SetParent(transform);
            magnetVisual.transform.localPosition = new Vector3(0f, 1.2f, 0f);

            // Spinning ring effect
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "MagnetRing";
            ring.transform.SetParent(magnetVisual.transform);
            ring.transform.localPosition = Vector3.zero;
            ring.transform.localScale = new Vector3(2.5f, 0.05f, 2.5f);

            Collider col = ring.GetComponent<Collider>();
            if (col != null) Destroy(col);

            Renderer rend = ring.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            Color magnetColor = new Color(1f, 0.2f, 0.2f);
            mat.color = magnetColor;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", magnetColor);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.SetColor("_EmissionColor", magnetColor * 3f);
                mat.EnableKeyword("_EMISSION");
            }
            rend.material = mat;

            // Add rotation script
            magnetVisual.AddComponent<SpinEffect>();
        }

        private void CreateScoreVisual()
        {
            scoreVisual = new GameObject("ScoreAura");
            scoreVisual.transform.SetParent(transform);
            scoreVisual.transform.localPosition = new Vector3(0f, 2.5f, 0f);

            // Floating "2X" sphere above head
            GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.name = "ScoreOrb";
            orb.transform.SetParent(scoreVisual.transform);
            orb.transform.localPosition = Vector3.zero;
            orb.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

            Collider col = orb.GetComponent<Collider>();
            if (col != null) Destroy(col);

            Renderer rend = orb.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            Color goldColor = new Color(1f, 0.85f, 0.15f);
            mat.color = goldColor;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", goldColor);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.SetColor("_EmissionColor", goldColor * 4f);
                mat.EnableKeyword("_EMISSION");
            }
            rend.material = mat;

            scoreVisual.AddComponent<BobEffect>();
        }

        private void CreateSmashVisual()
        {
            smashVisual = new GameObject("SmashAura");
            smashVisual.transform.SetParent(transform);
            smashVisual.transform.localPosition = new Vector3(0f, 1f, 0f);

            // Pulsing red aura
            GameObject aura = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            aura.name = "SmashSphere";
            aura.transform.SetParent(smashVisual.transform);
            aura.transform.localPosition = Vector3.zero;
            aura.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);

            Collider col = aura.GetComponent<Collider>();
            if (col != null) Destroy(col);

            Renderer rend = aura.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            Color smashColor = new Color(1f, 0.3f, 0f, 0.3f);
            mat.color = smashColor;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", smashColor);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3000;
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.SetColor("_EmissionColor", new Color(1f, 0.3f, 0f) * 3f);
                mat.EnableKeyword("_EMISSION");
            }
            rend.material = mat;

            smashVisual.AddComponent<PulseEffect>();
        }

        #endregion

        /// <summary>
        /// Reset power-up state. Called on game restart.
        /// </summary>
        public void ResetPowerUps()
        {
            DeactivatePowerUp();
            ShieldActive = false;
            lastActivationTime = -999f;

            if (playerCollision != null)
                playerCollision.IsShielded = false;
        }
    }

    #region Visual Effect Helpers

    /// <summary>Spins the attached GameObject.</summary>
    public class SpinEffect : MonoBehaviour
    {
        private void Update()
        {
            transform.Rotate(Vector3.up, 120f * Time.deltaTime, Space.World);
        }
    }

    /// <summary>Bobs the attached GameObject up and down.</summary>
    public class BobEffect : MonoBehaviour
    {
        private Vector3 startPos;
        private void Start() => startPos = transform.localPosition;
        private void Update()
        {
            Vector3 pos = startPos;
            pos.y += Mathf.Sin(Time.time * 3f) * 0.15f;
            transform.localPosition = pos;
        }
    }

    /// <summary>Pulses the scale of the attached GameObject.</summary>
    public class PulseEffect : MonoBehaviour
    {
        private void Update()
        {
            float scale = 1f + Mathf.Sin(Time.time * 4f) * 0.2f;
            transform.localScale = new Vector3(scale, scale, scale);
        }
    }

    #endregion
}
