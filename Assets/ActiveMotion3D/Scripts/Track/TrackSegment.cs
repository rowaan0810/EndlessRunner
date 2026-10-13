// TrackSegment.cs — Individual track segment that scrolls toward the camera.
// Managed and recycled by TrackManager.

using UnityEngine;
using ActiveMotion3D.EndlessRunner.Core;

namespace ActiveMotion3D.EndlessRunner.Track
{
    /// <summary>
    /// A single track segment that moves toward the camera at the current game speed.
    /// When it passes behind the camera, it notifies TrackManager for recycling.
    /// </summary>
    public class TrackSegment : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float segmentLength = 20f; // Length along Z axis

        /// <summary>Length of this segment along the Z axis.</summary>
        public float SegmentLength => segmentLength;

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
                return;

            float speed = GameSpeed.Instance != null ? GameSpeed.Instance.Current : 10f;

            // Move toward the camera (negative Z)
            transform.Translate(Vector3.back * speed * Time.deltaTime, Space.World);
        }

        /// <summary>
        /// Check if this segment has fully passed behind the camera.
        /// </summary>
        public bool IsBehindCamera(float cameraPosZ, float buffer = 5f)
        {
            return transform.position.z + segmentLength < cameraPosZ - buffer;
        }
    }
}
