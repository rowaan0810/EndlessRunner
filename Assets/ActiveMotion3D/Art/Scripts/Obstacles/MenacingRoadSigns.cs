using UnityEngine;
using ActiveMotion3D.EndlessRunner.Core;

namespace ActiveMotion3D.EndlessRunner.Obstacles
{
    /// <summary>One collidable, lane-blocking sign per encounter.</summary>
    public sealed class MenacingRoadSigns : Obstacle
    {
        [SerializeField] private Transform[] variants;
        [SerializeField] private Transform floatingBoard;
        [SerializeField] private Vector3 floatingRest = new Vector3(0f, 2.75f, -.12f);
        private float phase;
        public int VariantIndex { get; private set; }

        public void Configure(Transform[] models, Transform hoveringBoard)
        {
            variants = models;
            floatingBoard = hoveringBoard;
            if (floatingBoard != null) floatingRest = floatingBoard.localPosition;
        }

        private void OnEnable()
        {
            phase = 0f;
            SelectVariant(Random.Range(0, variants != null ? variants.Length : 0));
        }

        public void SelectVariant(int index)
        {
            if (variants == null || variants.Length == 0) return;
            VariantIndex = Mathf.Clamp(index, 0, variants.Length - 1);
            for (int i = 0; i < variants.Length; i++)
                if (variants[i] != null) variants[i].gameObject.SetActive(i == VariantIndex);
            if (floatingBoard != null) floatingBoard.localPosition = floatingRest;
        }

        protected override void Update()
        {
            base.Update();
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;
            phase += Time.deltaTime;
            if (floatingBoard != null && VariantIndex == 3)
                floatingBoard.localPosition = floatingRest + Vector3.up * (Mathf.Sin(phase * 1.8f) * .10f);
        }
    }
}
