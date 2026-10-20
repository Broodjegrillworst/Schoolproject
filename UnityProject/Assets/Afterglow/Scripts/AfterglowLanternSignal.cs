using UnityEngine;

namespace Afterglow
{
    /// <summary>Gives Mara's Last Bell a readable idle glow and a visible response to every pulse.</summary>
    public sealed class AfterglowLanternSignal : MonoBehaviour
    {
        private Transform halo;
        private Transform ring;
        private SpriteRenderer haloRenderer;
        private SpriteRenderer ringRenderer;
        private float pulseRemaining;
        private bool perfectPulse;

        public void Initialize()
        {
            var haloObject = new GameObject("Last Bell • warm lantern bloom");
            halo = haloObject.transform;
            halo.SetParent(transform, false);
            halo.localPosition = new Vector3(-.27f, .42f, .1f);
            halo.localScale = Vector3.one * .72f;
            haloRenderer = haloObject.AddComponent<SpriteRenderer>();
            haloRenderer.sprite = AfterglowArt.Disc("last-bell-halo", new Color(.67f, .37f, .12f, .12f), new Color(1f, .79f, .39f, .78f));
            haloRenderer.color = new Color(1f, .77f, .38f, .44f);
            haloRenderer.sortingOrder = 19;
            if (AfterglowArt.SpriteMaterial != null) haloRenderer.sharedMaterial = AfterglowArt.SpriteMaterial;

            var ringObject = new GameObject("Last Bell • resonant pulse ring");
            ring = ringObject.transform;
            ring.SetParent(transform, false);
            ring.localPosition = new Vector3(-.27f, .42f, .08f);
            ringRenderer = ringObject.AddComponent<SpriteRenderer>();
            ringRenderer.sprite = AfterglowArt.Disc("last-bell-ring", new Color(.72f, .44f, .15f, .08f), new Color(1f, .82f, .49f, .9f));
            ringRenderer.color = new Color(1f, .84f, .51f, 0f);
            ringRenderer.sortingOrder = 21;
            if (AfterglowArt.SpriteMaterial != null) ringRenderer.sharedMaterial = AfterglowArt.SpriteMaterial;
        }

        public void Pulse(bool perfect)
        {
            pulseRemaining = perfect ? .48f : .34f;
            perfectPulse = perfect;
            if (ring != null) ring.localScale = Vector3.one * .45f;
        }

        private void Update()
        {
            if (halo == null || ring == null) return;
            float time = Time.time;
            float idle = .72f + Mathf.Sin(time * 2.6f) * .045f;
            halo.localScale = Vector3.one * idle;
            pulseRemaining -= Time.deltaTime;
            if (pulseRemaining <= 0f)
            {
                haloRenderer.color = new Color(1f, .77f, .38f, .4f);
                ringRenderer.color = new Color(1f, .84f, .51f, 0f);
                ring.localScale = Vector3.one * .45f;
                return;
            }

            float duration = perfectPulse ? .48f : .34f;
            float progress = 1f - Mathf.Clamp01(pulseRemaining / duration);
            Color pulseColor = perfectPulse ? new Color(.74f, 1f, .83f) : new Color(1f, .8f, .47f);
            haloRenderer.color = Color.Lerp(new Color(1f, .94f, .71f, .95f), pulseColor, progress * .45f);
            ringRenderer.color = new Color(pulseColor.r, pulseColor.g, pulseColor.b, (1f - progress) * .86f);
            ring.localScale = Vector3.one * Mathf.Lerp(.45f, perfectPulse ? 2.2f : 1.7f, progress);
        }
    }
}
