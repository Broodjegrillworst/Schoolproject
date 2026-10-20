using UnityEngine;

public class ArcadeAudioManager : MonoBehaviour
{
    private const string MusicKey = "Pulse.City.MusicVolume";
    private const string EffectsKey = "Pulse.City.EffectsVolume";
    private const string FullscreenKey = "Pulse.City.Fullscreen";
    private const int SampleRate = 22050;

    public static ArcadeAudioManager Instance { get; private set; }

    private AudioSource musicSource;
    private AudioSource effectsSource;
    private AudioClip uiClip;
    private AudioClip jumpClip;
    private AudioClip shotClip;
    private AudioClip pickupClip;
    private AudioClip dashClip;
    private AudioClip hitClip;
    private AudioClip bossHitClip;
    private AudioClip enemyDefeatClip;
    private AudioClip arcClip;
    private AudioClip novaClip;
    private AudioClip checkpointClip;
    private float musicBaseVolume = 0.32f;
    private bool combatIntensity;

    public float MusicVolume => musicSource != null ? musicSource.volume : 0.35f;
    public float EffectsVolume => PlayerPrefs.GetFloat(EffectsKey, 0.75f);
    public bool Fullscreen => Screen.fullScreen;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        gameObject.name = "ArcadeAudioManager";
        DontDestroyOnLoad(gameObject);

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
        musicBaseVolume = PlayerPrefs.GetFloat(MusicKey, 0.32f);
        musicSource.volume = musicBaseVolume;
        musicSource.clip = CreateMusicLoop();

        effectsSource = gameObject.AddComponent<AudioSource>();
        effectsSource.playOnAwake = false;
        effectsSource.spatialBlend = 0f;

        uiClip = CreateEffect(660f, 990f, 0.10f, 0.28f);
        jumpClip = CreateEffect(280f, 620f, 0.18f, 0.32f);
        shotClip = CreateEffect(820f, 240f, 0.12f, 0.22f);
        pickupClip = CreateEffect(520f, 1180f, 0.28f, 0.35f);
        dashClip = CreateEffect(1200f, 180f, 0.22f, 0.30f);
        hitClip = CreateEffect(240f, 75f, 0.20f, 0.35f);
        bossHitClip = CreateEffect(150f, 60f, 0.30f, 0.40f);
        enemyDefeatClip = CreateEffect(560f, 180f, 0.17f, 0.30f);
        arcClip = CreateEffect(420f, 1320f, 0.24f, 0.34f);
        novaClip = CreateEffect(180f, 820f, 0.46f, 0.42f);
        checkpointClip = CreateEffect(460f, 1040f, 0.32f, 0.28f);

        int fullscreenDefault = Application.isEditor ? 0 : 1;
        Screen.fullScreen = PlayerPrefs.GetInt(FullscreenKey, fullscreenDefault) == 1;
        musicSource.Play();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (musicSource == null) return;
        float targetPitch = combatIntensity ? 1.055f : 1f;
        float targetVolume = musicBaseVolume * (combatIntensity ? 1.12f : 1f);
        musicSource.pitch = Mathf.Lerp(musicSource.pitch, targetPitch, 1f - Mathf.Exp(-2.5f * Time.unscaledDeltaTime));
        musicSource.volume = Mathf.Lerp(musicSource.volume, targetVolume, 1f - Mathf.Exp(-2f * Time.unscaledDeltaTime));
    }

    private void OnApplicationQuit()
    {
        PlayerPrefs.Save();
    }

    public void SetMusicVolume(float value)
    {
        value = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MusicKey, value);
        musicBaseVolume = value;
        if (musicSource != null)
            musicSource.volume = value * (combatIntensity ? 1.12f : 1f);
    }

    public void SetEffectsVolume(float value)
    {
        PlayerPrefs.SetFloat(EffectsKey, Mathf.Clamp01(value));
    }

    public void SetFullscreen(bool enabled)
    {
        PlayerPrefs.SetInt(FullscreenKey, enabled ? 1 : 0);
        Screen.fullScreen = enabled;
    }

    public void SetCombatIntensity(bool active) => combatIntensity = active;

    public void PlayUi() => Play(uiClip, 0.6f);
    public void PlayJump() => Play(jumpClip);
    public void PlayShot() => Play(shotClip, 0.8f);
    public void PlayPickup() => Play(pickupClip);
    public void PlayDash() => Play(dashClip);
    public void PlayHit() => Play(hitClip);
    public void PlayBossHit() => Play(bossHitClip);
    public void PlayEnemyDefeat() => Play(enemyDefeatClip, 0.75f);
    public void PlayArc() => Play(arcClip, 0.85f);
    public void PlayNova() => Play(novaClip, 1f);
    public void PlayCheckpoint() => Play(checkpointClip, 0.8f);
    public void PlayAchievement() => Play(checkpointClip, 0.65f);

    private void Play(AudioClip clip, float volumeScale = 1f)
    {
        if (clip != null && effectsSource != null)
            effectsSource.PlayOneShot(clip, EffectsVolume * volumeScale);
    }

    private static AudioClip CreateMusicLoop()
    {
        // A bright, energetic hero theme with sustained chords, a rising motif and light percussion.
        const float bpm = 112f;
        const int beats = 32;
        float beatSeconds = 60f / bpm;
        float duration = beatSeconds * beats;
        int sampleCount = Mathf.CeilToInt(duration * SampleRate);
        float[] samples = new float[sampleCount * 2];
        float[] roots = { 65.41f, 73.42f, 58.27f, 65.41f };
        int[][] chords =
        {
            new[] { 0, 4, 7, 11 },
            new[] { 0, 3, 7, 10 },
            new[] { 0, 4, 7, 11 },
            new[] { 0, 4, 7, 11 }
        };
        int[] heroMotif = { 12, 16, 19, 23, 19, 16, 21, 19 };
        float barDuration = beatSeconds * 4f;

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)SampleRate;
            float beatPosition = time / beatSeconds;
            int beatIndex = Mathf.FloorToInt(beatPosition);
            float beatTime = (beatPosition - beatIndex) * beatSeconds;
            int barIndex = Mathf.FloorToInt(time / barDuration) % roots.Length;
            float barTime = time % barDuration;
            float transition = Mathf.Clamp01(Mathf.Min(barTime / 0.8f, (barDuration - barTime) / 0.9f));
            transition = Mathf.SmoothStep(0f, 1f, transition);

            float pad = 0f;
            for (int note = 0; note < chords[barIndex].Length; note++)
            {
                float frequency = roots[barIndex] * Mathf.Pow(2f, chords[barIndex][note] / 12f);
                float phase = 2f * Mathf.PI * frequency * time;
                float detunedPhase = 2f * Mathf.PI * (frequency * 1.003f) * time;
                float voice = Mathf.Sin(phase) * 0.72f + Mathf.Sin(detunedPhase) * 0.20f + Mathf.Sin(phase * 2f) * 0.08f;
                pad += voice * 0.033f;
            }
            pad *= transition * (0.88f + 0.12f * Mathf.Sin(2f * Mathf.PI * 0.08f * time));

            int motifNote = heroMotif[beatIndex % heroMotif.Length];
            float leadFrequency = roots[barIndex] * Mathf.Pow(2f, motifNote / 12f);
            float leadEnvelope = Mathf.Exp(-beatTime * 5.2f) * Mathf.Clamp01((beatSeconds - beatTime) * 5f);
            float heroLead = Mathf.Sin(2f * Mathf.PI * leadFrequency * time) * leadEnvelope * 0.055f;

            float root = roots[barIndex] * 0.5f;
            float bassPulse = Mathf.Exp(-beatTime * 2.2f) * Mathf.Sin(2f * Mathf.PI * root * time) * 0.11f;
            float kick = 0f;
            if (beatIndex % 4 == 0 && beatTime < 0.34f)
            {
                float sweep = 2f * Mathf.PI * (63f * beatTime + 5.5f * (1f - Mathf.Exp(-9f * beatTime)));
                kick = Mathf.Sin(sweep) * Mathf.Exp(-13f * beatTime) * 0.22f;
            }

            float percussion = 0f;
            int beatInBar = beatIndex % 4;
            if ((beatInBar == 1 || beatInBar == 3) && beatTime < 0.16f)
            {
                float noise = Mathf.Sin((i + 31) * 12.9898f) * 43758.5453f;
                noise = (noise - Mathf.Floor(noise)) * 2f - 1f;
                percussion = noise * Mathf.Exp(-31f * beatTime) * 0.055f;
            }
            if (beatIndex % 2 == 1 && beatTime < 0.045f)
            {
                float noise = Mathf.Sin((i + 149) * 78.233f) * 17341.387f;
                noise = (noise - Mathf.Floor(noise)) * 2f - 1f;
                percussion += noise * Mathf.Exp(-82f * beatTime) * 0.022f;
            }

            float mixed = Mathf.Clamp(pad + heroLead + bassPulse + kick + percussion, -0.78f, 0.78f);
            float stereoWidth = Mathf.Sin(2f * Mathf.PI * 0.17f * time) * pad * 0.12f;
            samples[i * 2] = mixed - stereoWidth;
            samples[i * 2 + 1] = mixed + stereoWidth;
        }

        AudioClip clip = AudioClip.Create("PULSE - Rise of the Rift Hero Theme", sampleCount, 2, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
    private static AudioClip CreateEffect(float startFrequency, float endFrequency, float duration, float amplitude)
    {
        int sampleCount = Mathf.Max(1, Mathf.CeilToInt(duration * SampleRate));
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)SampleRate;
            float progress = time / duration;
            float phase = 2f * Mathf.PI * (startFrequency * time + (endFrequency - startFrequency) * time * progress * 0.5f);
            float envelope = Mathf.Pow(1f - progress, 1.6f);
            samples[i] = Mathf.Sin(phase) * envelope * amplitude;
        }

        AudioClip clip = AudioClip.Create("PULSE Hero Effect", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}

