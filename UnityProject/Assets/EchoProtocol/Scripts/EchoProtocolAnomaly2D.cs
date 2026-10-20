using UnityEngine;

namespace EchoProtocol
{
    public sealed class EchoProtocolAnomaly2D : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer[] renderers;
        [SerializeField] private float travelDistance = 4f;
        [SerializeField] private float travelSeconds = 4.5f;
        private Vector3 origin;
        private float startedAt;
        private bool visible;

        public void Configure(SpriteRenderer[] visuals)
        {
            renderers = visuals;
            origin = transform.position;
            SetVisible(false);
        }

        public void Appear()
        {
            origin = transform.position;
            startedAt = Time.time;
            visible = true;
            SetVisible(true);
        }

        private void Update()
        {
            if (!visible) return;
            float normalized = Mathf.Clamp01((Time.time - startedAt) / travelSeconds);
            transform.position = origin + Vector3.left * travelDistance * normalized;
            float flicker = Mathf.PerlinNoise(Time.time * 22f, 0.31f) > 0.16f ? 0.34f : 0.06f;
            if (renderers != null)
            {
                foreach (SpriteRenderer renderer in renderers)
                {
                    if (renderer == null) continue;
                    Color color = renderer.color;
                    color.a = flicker;
                    renderer.color = color;
                }
            }
            if (normalized >= 1f) { visible = false; SetVisible(false); }
        }

        private void SetVisible(bool value)
        {
            if (renderers == null) return;
            foreach (SpriteRenderer renderer in renderers) if (renderer != null) renderer.enabled = value;
        }
    }
}
