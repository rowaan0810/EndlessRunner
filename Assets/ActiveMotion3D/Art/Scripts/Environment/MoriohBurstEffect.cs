using UnityEngine;

namespace ActiveMotion3D.EndlessRunner.Environment
{
    /// <summary>Short visual punctuation; never awards points or changes game state.</summary>
    public sealed class MoriohBurstEffect : MonoBehaviour
    {
        [Min(.05f)] public float lifetime = .55f;
        public Vector3 drift = new Vector3(0f, 1f, 0f);
        [Min(.01f)] public float endScale = 1.35f;
        private float age;
        private Vector3 initialScale;
        private void Awake() => initialScale = transform.localScale;
        private void Update()
        {
            age += Time.deltaTime;
            transform.position += drift * Time.deltaTime;
            transform.localScale = initialScale * Mathf.Lerp(1f, endScale, age / lifetime);
            if (age >= lifetime) Destroy(gameObject);
        }
    }
}
