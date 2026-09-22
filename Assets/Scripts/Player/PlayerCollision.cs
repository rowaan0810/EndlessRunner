// PlayerCollision.cs — Handles collision detection for the player.
// When the player hits an obstacle, triggers game over.

using UnityEngine;
using EndlessRunner.Core;
using EndlessRunner.Obstacles;

namespace EndlessRunner.Player
{
    /// <summary>
    /// Detects when the player collides with obstacles and triggers game over.
    /// Attach this to the player GameObject alongside PlayerController.
    /// </summary>
    public class PlayerCollision : MonoBehaviour
    {
        /// <summary>
        /// Whether the player is currently shielded (from Pose Challenge power-up).
        /// When shielded, the first collision is absorbed instead of ending the game.
        /// </summary>
        public bool IsShielded { get; set; }

        private void OnTriggerEnter(Collider other)
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
                return;

            // Check if it's an obstacle (by component or tag)
            bool isObstacle = other.GetComponent<Obstacle>() != null
                           || other.GetComponentInParent<Obstacle>() != null;



            if (isObstacle)
            {
                if (IsShielded)
                {
                    IsShielded = false;
                    Debug.Log("Shield absorbed a collision!");
                    other.gameObject.SetActive(false);
                    return;
                }

                Debug.Log($"Player hit obstacle: {other.gameObject.name}");
                GameManager.Instance.TriggerGameOver();
            }
        }

        /// <summary>
        /// Reset collision state. Called on game restart.
        /// </summary>
        public void ResetCollision()
        {
            IsShielded = false;
        }
    }
}
