using UnityEngine;

namespace EchoProtocol
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class EchoProtocolAmbientAudio2D : MonoBehaviour
    {
        private void Start()
        {
            const int sampleRate = 22050;
            const int seconds = 8;
            float[] samples = new float[sampleRate * seconds];
            for (int i = 0; i < samples.Length; i++)
            {
                float time = i / (float)sampleRate;
                float drift = 0.5f + 0.5f * Mathf.Sin(time * 0.31f);
                float low = Mathf.Sin(2f * Mathf.PI * (48f + drift * 2.8f) * time) * 0.38f;
                float harmonic = Mathf.Sin(2f * Mathf.PI * (72f + drift * 1.1f) * time) * 0.12f;
                float pulse = Mathf.Sin(2f * Mathf.PI * 0.22f * time) * 0.08f;
                samples[i] = (low + harmonic) * (0.76f + pulse);
            }
            AudioClip clip = AudioClip.Create("Facility low-frequency ventilation", samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            AudioSource source = GetComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.volume = 0.12f;
            source.spatialBlend = 0f;
            source.playOnAwake = true;
            source.Play();
        }
    }
}
