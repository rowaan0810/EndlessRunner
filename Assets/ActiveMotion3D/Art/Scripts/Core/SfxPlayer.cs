using UnityEngine;
using System.Collections.Generic;

namespace ActiveMotion3D.EndlessRunner.Core
{
    /// <summary>Scene-local sound effects, separate from the looping music source.</summary>
    [RequireComponent(typeof(AudioSource))]
    public class SfxPlayer : MonoBehaviour
    {
        public static SfxPlayer Instance { get; private set; }

        [SerializeField] private AudioSource source;
        [Header("Overall Effects Level")]
        [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
        [Tooltip("Match active audio levels while leaving peak headroom. Music is controlled separately.")]
        [SerializeField] private bool normalizeClipLevels = true;
        [SerializeField, Range(0.05f, 0.3f)] private float targetRms = 0.16f;

        [Header("Collision / Human Tetris Failure")]
        public AudioClip hitClip;
        [SerializeField, Range(0f, 1f)] private float hitVolume = 1f;
        [Header("Coin Collection")]
        public AudioClip coinClip;
        [SerializeField, Range(0f, 1f)] private float coinVolume = 1f;
        [Header("Punch Target")]
        [Tooltip("Uses the coin clip if no separate punch clip is assigned.")]
        public AudioClip punchClip;
        [SerializeField, Range(0f, 1f)] private float punchVolume = 1f;
        [Header("Human Tetris Success")]
        public AudioClip successClip;
        [SerializeField, Range(0f, 1f)] private float successVolume = 1f;
        [Header("Jump")]
        public AudioClip jumpClip;
        [Tooltip("Raised slightly by default because jump feedback must cut through the background music.")]
        [SerializeField, Range(0f, 2f)] private float jumpVolume = 1.4f;
        [Header("Slide / Duck")]
        public AudioClip slideClip;
        [SerializeField, Range(0f, 1f)] private float slideVolume = 1f;

        private readonly Dictionary<AudioClip, Vector2> clipLevels = new Dictionary<AudioClip, Vector2>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (source == null) source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
        }

        public void Play(AudioClip clip, float volume = 1f)
        {
            if (clip != null && source != null)
                source.PlayOneShot(clip, masterVolume * Mathf.Clamp(volume, 0f, 2f) * GetLevelGain(clip));
        }

        public void PlayCoin() => Play(coinClip, coinVolume);
        public void PlayHit() => Play(hitClip, hitVolume);
        public void PlayPunch() => Play(punchClip != null ? punchClip : coinClip, punchVolume);
        public void PlaySuccess() => Play(successClip, successVolume);
        public void PlayJump() => Play(jumpClip, jumpVolume);
        public void PlaySlide() => Play(slideClip, slideVolume);

        private float GetLevelGain(AudioClip clip)
        {
            if (!normalizeClipLevels) return 1f;
            if (!clipLevels.TryGetValue(clip, out Vector2 levels))
            {
                // SFX imports use Decompress On Load. If a replacement is streamed
                // or not ready, play at its configured level without blocking gameplay.
                if (clip.loadState != AudioDataLoadState.Loaded ||
                    clip.loadType == AudioClipLoadType.Streaming) return 1f;
                float[] samples = new float[clip.samples * clip.channels];
                if (!clip.GetData(samples, 0)) return 1f;
                double energy = 0;
                int activeSamples = 0;
                float peak = 0f;
                foreach (float sample in samples)
                {
                    float amplitude = Mathf.Abs(sample);
                    peak = Mathf.Max(peak, amplitude);
                    // Ignore padding silence so longer files are not over-amplified.
                    if (amplitude < 0.01f) continue;
                    energy += sample * sample;
                    activeSamples++;
                }
                levels = new Vector2(activeSamples > 0 ? Mathf.Sqrt((float)(energy / activeSamples)) : 0f, peak);
                clipLevels[clip] = levels;
            }
            if (levels.x <= 0f || levels.y <= 0f) return 1f;
            return Mathf.Min(targetRms / levels.x, 0.9f / levels.y);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
