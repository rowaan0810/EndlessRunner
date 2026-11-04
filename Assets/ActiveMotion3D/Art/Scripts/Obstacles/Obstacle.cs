// Obstacle.cs — Base obstacle behavior.
// Scrolls toward the player at game speed and triggers game over on collision.

using UnityEngine;
using ActiveMotion3D.EndlessRunner.Core;

namespace ActiveMotion3D.EndlessRunner.Obstacles
{
    public enum ObstacleType
    {
        BarrierLow,    // Must jump over
        BarrierHigh,   // Must duck under
        BarrierFull    // Blocks one lane, must dodge sideways
    }

    /// <summary>
    /// An obstacle that scrolls toward the player. When hit, triggers game over.
    /// Deactivates itself when it passes behind the camera for recycling.
    /// </summary>
    public class Obstacle : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private ObstacleType type;
        [SerializeField] private float despawnZ = -15f; // Z position to deactivate

        /// <summary>The type of this obstacle (for spawner logic).</summary>
        public ObstacleType Type => type;

        /// <summary>Which lane(s) this obstacle occupies (set by spawner).</summary>
        public int Lane { get; set; }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
                return;

            float speed = GameSpeed.Instance != null ? GameSpeed.Instance.Current : 10f;

            // Move toward the player (negative Z)
            transform.Translate(Vector3.back * speed * Time.deltaTime, Space.World);

            // Deactivate when past the camera
            if (transform.position.z < despawnZ)
            {
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Initialize obstacle position and lane. Called by ObstacleSpawner.
        /// </summary>
        public void Setup(Vector3 position, int lane, ObstacleType obstacleType)
        {
            transform.position = position;
            Lane = lane;
            type = obstacleType;
            gameObject.SetActive(true);
        }
    }
}
