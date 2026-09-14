using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private float musicVolume = 0.25f;
    [SerializeField] private float sfxVolume = 0.6f;

    private AudioSource musicSource;
    private AudioSource sfxSource;

    private AudioClip jumpClip;
    private AudioClip coinClip;
    private AudioClip hurtClip;
    private AudioClip winClip;
    private AudioClip musicClip;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        musicSource = gameObject.AddComponent<AudioSource>();
        sfxSource = gameObject.AddComponent<AudioSource>();

        GenerateClips();

        musicSource.clip = musicClip;
        musicSource.loop = true;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    private void GenerateClips()
    {
        jumpClip = ToneGenerator.CreateTone("Jump", 700f, 0.15f, ToneGenerator.Wave.Square, fadeOut: true);
        coinClip = ToneGenerator.CreateArpeggio("Coin", new[] { 880f, 1174.66f, 1567.98f }, 0.07f);
        hurtClip = ToneGenerator.CreateTone("Hurt", 140f, 0.3f, ToneGenerator.Wave.Sawtooth, fadeOut: true);
        winClip = ToneGenerator.CreateArpeggio("Win", new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.15f);
        musicClip = ToneGenerator.CreateLoopMelody(
            "Music",
            new[] { 261.63f, 293.66f, 329.63f, 261.63f, 329.63f, 392.00f, 329.63f, 293.66f },
            0.35f);
    }

    public void PlayJump() => sfxSource.PlayOneShot(jumpClip, sfxVolume);
    public void PlayCoin() => sfxSource.PlayOneShot(coinClip, sfxVolume);
    public void PlayHurt() => sfxSource.PlayOneShot(hurtClip, sfxVolume);
    public void PlayWin() => sfxSource.PlayOneShot(winClip, sfxVolume);
}
