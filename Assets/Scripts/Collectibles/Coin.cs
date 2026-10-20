// Coin.cs — Collectible coin that scrolls toward the player.
// Awards points when collected.

using UnityEngine;
using EndlessRunner.Core;

namespace EndlessRunner.Collectibles
{
    /// <summary>
    /// A rotating coin that scrolls toward the player and awards points on collection.
    /// Object-pooled by ObstacleSpawner.
    /// </summary>
    public class Coin : MonoBehaviour
    {
        [Header("Visual")]
        [SerializeField] private float rotationSpeed = 180f; // Degrees per second
        [SerializeField] private float bobAmplitude = 0.15f;
        [SerializeField] private float bobFrequency = 2f;

        private float startY;
        private float spawnTime;

        private void OnEnable()
        {
            startY = transform.position.y;
            spawnTime = Time.time;
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
                return;

            float speed = GameSpeed.Instance != null ? GameSpeed.Instance.Current : 10f;

            // Move toward the player
            Vector3 pos = transform.position;
            pos.z -= speed * Time.deltaTime;

            // Bob up and down
            pos.y = startY + Mathf.Sin((Time.time - spawnTime) * bobFrequency * Mathf.PI) * bobAmplitude;
            transform.position = pos;

            // Rotate
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

            // Deactivate when past camera
            if (pos.z < -15f)
            {
                gameObject.SetActive(false);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") || other.GetComponent<Player.PlayerController>() != null)
            {
                if (SfxPlayer.Instance == null)
                {
                    Debug.LogWarning("SfxPlayer.Instance is NULL");
                }
                else if (SfxPlayer.Instance.coinClip == null)
                {
                    Debug.LogWarning("Coin Clip is not assigned");
                }
                else
                {
                    Debug.Log("Playing coin sound");
                    SfxPlayer.Instance.Play(SfxPlayer.Instance.coinClip);
                }

                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Initialize coin position. Called by spawner.
        /// </summary>
        public void Setup(Vector3 position)
        {
            transform.position = position;
            startY = position.y;
            spawnTime = Time.time;
            gameObject.SetActive(true);
        }
    }
}
