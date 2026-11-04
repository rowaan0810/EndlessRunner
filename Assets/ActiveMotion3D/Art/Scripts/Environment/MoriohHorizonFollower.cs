using UnityEngine;

namespace ActiveMotion3D.EndlessRunner.Environment
{
    /// <summary>A separate continuous horizon, outside the recycling track pool.</summary>
    public sealed class MoriohHorizonFollower : MonoBehaviour
    {
        public Transform target;
        public bool followHorizontalCameraPosition = true;
        public Vector3 offset;
        private void LateUpdate()
        {
            if (!followHorizontalCameraPosition || target == null) return;
            Vector3 p = target.position;
            transform.position = new Vector3(p.x + offset.x, offset.y, p.z + offset.z);
        }
    }
}
