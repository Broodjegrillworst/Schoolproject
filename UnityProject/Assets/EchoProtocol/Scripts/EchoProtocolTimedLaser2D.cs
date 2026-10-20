using UnityEngine;

namespace EchoProtocol
{
    public sealed class EchoProtocolTimedLaser2D : MonoBehaviour
    {
        [SerializeField] private Collider2D damageVolume;
        [SerializeField] private SpriteRenderer beam;
        [SerializeField] private float cycleSeconds = 4.4f;
        [SerializeField] private float activeSeconds = 2.3f;
        [SerializeField] private float phaseOffset;
        private bool active;

        public void Configure(Collider2D volume, SpriteRenderer visual, float cycle, float onTime, float offset)
        {
            damageVolume = volume;
            beam = visual;
            cycleSeconds = cycle;
            activeSeconds = onTime;
            phaseOffset = offset;
        }

        private void Update()
        {
            float phase = Mathf.Repeat(Time.time + phaseOffset, cycleSeconds);
            bool nowActive = phase < activeSeconds;
            if (nowActive != active)
            {
                active = nowActive;
                if (damageVolume != null) damageVolume.enabled = active;
            }
            if (beam != null)
            {
                bool warning = phase > cycleSeconds - 0.65f;
                beam.color = active ? new Color(1f, 0.2f, 0.12f, 0.85f) : warning ? new Color(0.95f, 0.62f, 0.22f, 0.6f) : new Color(0.4f, 0.78f, 0.72f, 0.12f);
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (active && other.GetComponentInParent<EchoProtocolController2D>() is EchoProtocolController2D player)
                player.Damage(transform.position);
        }
    }
}

