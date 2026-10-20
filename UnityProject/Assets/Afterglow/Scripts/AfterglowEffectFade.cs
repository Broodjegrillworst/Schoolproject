using UnityEngine;

namespace Afterglow
{
    public sealed class AfterglowEffectFade : MonoBehaviour
    {
        private float duration = .35f;
        private float age;
        private float scale = 1.4f;
        private SpriteRenderer[] renderers;
        private Color tint = Color.white;

        public void Configure(float lifetime, Color color, float finalScale)
        {
            duration = Mathf.Max(.05f, lifetime);
            tint = color;
            scale = finalScale;
            renderers = GetComponentsInChildren<SpriteRenderer>();
            foreach (var sr in renderers) sr.color = tint;
        }

        private void Update()
        {
            age += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(age / duration);
            transform.localScale = Vector3.one * Mathf.Lerp(.35f, scale, t);
            if (renderers != null)
            {
                Color c = tint;
                c.a *= 1f - t;
                foreach (var sr in renderers) sr.color = c;
            }
            if (age >= duration) Destroy(gameObject);
        }
    }
}
