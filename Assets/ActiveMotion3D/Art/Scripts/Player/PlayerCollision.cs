// PlayerCollision.cs — Handles collision detection for the player.
// When the player hits an obstacle, triggers game over.

using UnityEngine;
using ActiveMotion3D.EndlessRunner.Core;
using ActiveMotion3D.EndlessRunner.Obstacles;

namespace ActiveMotion3D.EndlessRunner.Player
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
            Obstacle obstacle = other.GetComponentInParent<Obstacle>();



            if (obstacle != null)
            {
                obstacle.RecordImpact();
                GameManager game = GameManager.Instance;
                SheerHeartAttack explosive = other.GetComponentInParent<SheerHeartAttack>();
                if (explosive != null) explosive.Detonate();
                if (IsShielded)
                {
                    game.Feedback.ShieldHits++;
                    IsShielded = false;
                    Debug.Log("Shield absorbed a collision!");
                    if (explosive == null) other.gameObject.SetActive(false);
                    return;
                }

                Debug.Log($"Player hit obstacle: {other.gameObject.name}");

                Debug.Log($"Player hit obstacle: {other.gameObject.name}");

                if (SfxPlayer.Instance != null && SfxPlayer.Instance.hitClip != null)
                    SfxPlayer.Instance.Play(SfxPlayer.Instance.hitClip);

                PlayerController player = GetComponent<PlayerController>();
                game.Feedback.EndAtObstacle(obstacle.Type, obstacle.Lane, game.TimeSurvived,
                    player != null && player.IsGrounded, player != null && player.IsDucking);
                game.TriggerGameOver(explosive != null ? explosive.ImpactPresentationSeconds : 0f);
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
