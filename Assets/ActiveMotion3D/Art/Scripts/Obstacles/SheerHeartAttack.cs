using UnityEngine;
using ActiveMotion3D.EndlessRunner.Core;

namespace ActiveMotion3D.EndlessRunner.Obstacles
{
    /// <summary>Lane-locked explosive. Uses the normal obstacle movement, pooling and collision rules.</summary>
    public sealed class SheerHeartAttack : Obstacle
    {
        [SerializeField, Min(0f)] private float advanceSpeed = 4f;
        [Tooltip("Keep the impact visible before the Game Over panel appears.")]
        [SerializeField, Range(.3f, 2f)] private float impactPresentationSeconds = .85f;
        [SerializeField] private Transform model;
        [SerializeField] private Transform[] wheels;
        [SerializeField] private ParticleSystem exhaust;
        [SerializeField] private ParticleSystem explosion;
        private bool detonated;
        private float travel;

        protected override float ApproachSpeed => base.ApproachSpeed + advanceSpeed;

        public float ImpactPresentationSeconds => impactPresentationSeconds;

        public void Configure(Transform visual, Transform[] rollers, ParticleSystem trail, ParticleSystem burst)
        {
            model = visual;
            wheels = rollers;
            exhaust = trail;
            explosion = burst;
        }

        private void OnEnable()
        {
            detonated = false;
            travel = 0f;
            if (model != null) model.gameObject.SetActive(true);
            if (exhaust != null) exhaust.Play(true);
            if (explosion != null) explosion.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            Collider trigger = GetComponent<Collider>();
            if (trigger != null) trigger.enabled = true;
        }

        protected override void Update()
        {
            if (detonated)
            {
                if (explosion == null || !explosion.IsAlive(true)) gameObject.SetActive(false);
                return;
            }
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;
            base.Update();
            travel += ApproachSpeed * Time.deltaTime;
            if (model != null) model.localPosition = Vector3.up * (Mathf.Sin(travel * 9f) * .018f);
            if (wheels != null)
                foreach (Transform wheel in wheels)
                    if (wheel != null) wheel.Rotate(Vector3.up, ApproachSpeed * Time.deltaTime * 220f, Space.Self);
        }

        public void Detonate()
        {
            if (detonated) return;
            detonated = true;
            if (model != null) model.gameObject.SetActive(false);
            if (exhaust != null) exhaust.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            Collider trigger = GetComponent<Collider>();
            if (trigger != null) trigger.enabled = false;
            if (explosion != null)
            {
                // Results/pause code may freeze game time. The final burst must still finish.
                foreach (ParticleSystem system in explosion.GetComponentsInChildren<ParticleSystem>(true))
                {
                    var main = system.main;
                    main.useUnscaledTime = true;
                }
                explosion.gameObject.SetActive(true);
                explosion.Play(true);
            }
        }

        private void OnDisable()
        {
            if (exhaust != null) exhaust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (explosion != null) explosion.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
