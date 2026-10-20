using UnityEngine;

public sealed class VesperAtmosphereAudio : MonoBehaviour
{
    private static VesperAtmosphereAudio instance;
    private AudioSource score;
    private AudioSource ambience;
    private AudioSource feedback;
    private AudioClip feedbackClip;
    private float combatTarget;
    private float combatMix;

    public void PlayRegion(int region)
    {
        instance = this;
        region = Mathf.Clamp(region, 1, 5);
        score = gameObject.AddComponent<AudioSource>();
        ambience = gameObject.AddComponent<AudioSource>();
        score.loop = true; score.spatialBlend = 0f; score.volume = 0.18f; score.clip = BuildScoreClip(region, false); score.Play();
        ambience.loop = true; ambience.spatialBlend = 0f; ambience.volume = 0.055f; ambience.clip = BuildScoreClip(region, true); ambience.Play();
        feedback = gameObject.AddComponent<AudioSource>();
        feedback.playOnAwake = false; feedback.spatialBlend = 0f; feedback.volume = 0.18f;
        feedbackClip = BuildFeedbackClip();
    }

    public static void SetCombat(bool active) { if (instance != null) instance.combatTarget = active ? 1f : 0f; }
    public static void PlayJump() { if (instance != null) instance.PlayCue(0.92f); }
    public static void PlayReward(int chain) { if (instance != null) instance.PlayCue(1.08f + Mathf.Clamp(chain, 0, 5) * 0.11f); }

    private void PlayCue(float pitch)
    {
        if (feedback == null || feedbackClip == null) return;
        feedback.pitch = pitch;
        feedback.PlayOneShot(feedbackClip);
    }

    private void Update()
    {
        combatMix = Mathf.MoveTowards(combatMix, combatTarget, Time.deltaTime * 0.42f);
        if (score != null)
        {
            score.pitch = Mathf.Lerp(0.92f, 1.04f, combatMix);
            score.volume = Mathf.Lerp(0.16f, 0.25f, combatMix);
        }
        if (ambience != null) ambience.volume = Mathf.Lerp(0.055f, 0.025f, combatMix);
    }

    private static AudioClip BuildScoreClip(int region, bool ambientOnly)
    {
        const int sampleRate = 22050;
        const int seconds = 12;
        int samples = sampleRate * seconds;
        AudioClip clip = AudioClip.Create(ambientOnly ? "VESPER // root ambience" : "VESPER // adaptive score", samples, 1, sampleRate, false);
        float[] data = new float[samples];
        float[] roots = { 55f, 61.74f, 65.41f, 73.42f, 49f };
        float root = roots[Mathf.Clamp(region - 1, 0, roots.Length - 1)];
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float drift = Mathf.Sin(t * 0.34f + region) * 0.7f + Mathf.Sin(t * 0.17f + 2.1f) * 0.3f;
            float low = Mathf.Sin(2f * Mathf.PI * root * t) * 0.32f + Mathf.Sin(2f * Mathf.PI * root * 1.5f * t + 0.4f) * 0.15f;
            float fifth = Mathf.Sin(2f * Mathf.PI * root * 2.0f * t + 0.8f) * 0.12f;
            float bell = Mathf.Sin(2f * Mathf.PI * root * 4f * t + Mathf.Sin(t * 0.25f)) * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t * 0.52f + region)), 14f) * 0.16f;
            float undertone = Mathf.Sin(2f * Mathf.PI * root * 0.5f * t) * 0.1f;
            float value = ambientOnly ? low * 0.35f + undertone * 0.4f + drift * 0.012f : low + fifth + bell + undertone;
            float fade = Mathf.Min(1f, Mathf.Min(t * 3f, (seconds - t) * 3f));
            data[i] = Mathf.Clamp(value * fade * (ambientOnly ? 0.7f : 0.58f), -0.8f, 0.8f);
        }
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip BuildFeedbackClip()
    {
        const int sampleRate = 22050;
        const float seconds = 0.16f;
        int samples = Mathf.RoundToInt(sampleRate * seconds);
        AudioClip clip = AudioClip.Create("VESPER // reward chime", samples, 1, sampleRate, false);
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float envelope = Mathf.Exp(-18f * t) * Mathf.Clamp01(t * 90f);
            data[i] = (Mathf.Sin(2f * Mathf.PI * 680f * t) * 0.55f + Mathf.Sin(2f * Mathf.PI * 1020f * t) * 0.25f) * envelope;
        }
        clip.SetData(data, 0);
        return clip;
    }
}
