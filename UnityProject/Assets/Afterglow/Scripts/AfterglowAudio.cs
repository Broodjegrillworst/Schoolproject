using System.Collections.Generic;
using UnityEngine;

namespace Afterglow
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class AfterglowAudio : MonoBehaviour
    {
        private AudioSource music;
        private AudioSource effects;
        private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        private static AfterglowAudio instance;

        public static AfterglowAudio Instance => instance;

        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            music = GetComponent<AudioSource>();
            music.loop = true;
            music.spatialBlend = 0f;
            effects = gameObject.AddComponent<AudioSource>();
            effects.spatialBlend = 0f;
            effects.playOnAwake = false;
            CreateEffects();
            ApplyVolumes();
        }

        public void PlayStage(int stage)
        {
            if (music == null) return;
            music.clip = BuildMusic(stage);
            music.volume = AfterglowSession.Profile.masterVolume * AfterglowSession.Profile.musicVolume;
            music.Play();
        }

        public void Play(string name, float pitch = 1f)
        {
            if (effects == null || !clips.TryGetValue(name, out AudioClip clip)) return;
            effects.pitch = pitch;
            effects.PlayOneShot(clip, AfterglowSession.Profile.masterVolume * AfterglowSession.Profile.effectsVolume);
        }

        public void ApplyVolumes()
        {
            AfterglowSession.Load();
            if (music != null) music.volume = AfterglowSession.Profile.masterVolume * AfterglowSession.Profile.musicVolume;
            if (effects != null) effects.volume = 1f;
        }

        private AudioClip BuildMusic(int stage)
        {
            const int rate = 22050;
            const float duration = 14f;
            int samples = Mathf.CeilToInt(rate * duration);
            float[] data = new float[samples];
            float[] scale = stage == 0 ? new[] { 0f, 3f, 7f, 10f, 12f, 10f, 7f, 3f } : stage == 1 ? new[] { 0f, 2f, 5f, 9f, 12f, 9f, 5f, 2f } : new[] { 0f, 3f, 6f, 10f, 12f, 10f, 6f, 3f };
            float bpm = stage == 2 ? 112f : stage == 1 ? 102f : 94f;
            float beat = 60f / bpm;
            float root = stage == 0 ? 55f : stage == 1 ? 49f : 46.25f;
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)rate;
                int chord = Mathf.FloorToInt(t / (beat * 4f)) % 4;
                int phrase = Mathf.FloorToInt(t / (beat * .5f));
                float tonic = root * Mathf.Pow(2f, new[] { 0f, 5f, 3f, 7f }[chord] / 12f);
                float third = tonic * Mathf.Pow(2f, (stage == 2 ? 3f : 4f) / 12f);
                float fifth = tonic * 1.4983f;
                float pad = Mathf.Sin(2f * Mathf.PI * tonic * t) * .085f + Mathf.Sin(2f * Mathf.PI * third * t) * .052f + Mathf.Sin(2f * Mathf.PI * fifth * t) * .038f;
                float phrasePos = (t % (beat * 4f)) / (beat * 4f);
                float envelope = Mathf.SmoothStep(0f, .16f, phrasePos) * (1f - Mathf.SmoothStep(.82f, 1f, phrasePos));
                float arpFreq = root * 2f * Mathf.Pow(2f, scale[phrase % scale.Length] / 12f);
                float arpEnv = Mathf.Exp(-((t % (beat * .5f)) * 12f));
                float arp = (Mathf.Sin(2f * Mathf.PI * arpFreq * t) + .24f * Mathf.Sin(2f * Mathf.PI * arpFreq * 2f * t)) * arpEnv * .11f;
                float low = Mathf.Sin(2f * Mathf.PI * (tonic * .5f) * t) * .08f;
                float kickPhase = t % beat;
                float kickEnv = Mathf.Exp(-kickPhase * 13f);
                float kick = Mathf.Sin(2f * Mathf.PI * (48f + 38f * kickPhase) * kickPhase) * kickEnv * .13f;
                float transition = t < .3f ? t / .3f : t > duration - .3f ? (duration - t) / .3f : 1f;
                data[i] = Mathf.Clamp((pad * envelope + arp + low + kick) * Mathf.Clamp01(transition), -.9f, .9f);
            }
            var clip = AudioClip.Create("Afterglow score " + stage, samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private void CreateEffects()
        {
            clips["jump"] = Tone("jump", 450f, .13f, .16f, true);
            clips["dash"] = Tone("dash", 260f, .16f, .24f, true);
            clips["pulse"] = Tone("mark", 820f, .12f, .17f, true);
            clips["shot"] = Tone("Oathroot pulse", 610f, .11f, .26f, true);
            clips["charge"] = Tone("Oathroot charge", 190f, .34f, .28f, false);
            clips["special"] = Tone("relic burst", 330f, .28f, .26f, true);
            clips["markhit"] = Tone("mark resonance", 960f, .25f, .22f, true);
            clips["ultimate"] = Tone("rootheart release", 110f, .72f, .34f, false);
            clips["impact"] = Tone("impact", 150f, .12f, .24f, false);
            clips["rebound"] = Tone("rebound", 240f, .18f, .25f, true);
            clips["perfect"] = Tone("perfect rebound", 660f, .34f, .22f, true);
            clips["boss"] = Tone("boss", 105f, .75f, .22f, false);
            clips["menu"] = Tone("menu", 580f, .11f, .12f, true);
            clips["hurt"] = Tone("hurt", 180f, .21f, .17f, false);
            clips["win"] = Tone("win", 523f, .5f, .18f, true);
        }

        private static AudioClip Tone(string name, float frequency, float seconds, float volume, bool bright)
        {
            const int rate = 22050;
            int samples = Mathf.CeilToInt(rate * seconds);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)rate;
                float attack = Mathf.Clamp01(t / .015f);
                float decay = Mathf.Exp(-t * (bright ? 5f : 9f));
                float wave = Mathf.Sin(2f * Mathf.PI * frequency * t);
                if (bright) wave = (wave + .24f * Mathf.Sin(2f * Mathf.PI * frequency * 2.01f * t)) * .78f;
                data[i] = wave * attack * decay * volume;
            }
            var clip = AudioClip.Create("Afterglow effect " + name, samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
