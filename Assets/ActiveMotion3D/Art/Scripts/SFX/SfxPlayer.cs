using UnityEngine;

public class SfxPlayer : MonoBehaviour
{
    public static SfxPlayer Instance;

    [SerializeField] private AudioSource source;
    public AudioClip hitClip;
    public AudioClip coinClip;

    private void Awake()
    {
        Instance = this;
        if (source == null) source = GetComponent<AudioSource>();
    }

    public void Play(AudioClip clip, float volume = 1f)
    {
        if (clip != null) source.PlayOneShot(clip, volume);
    }
}
