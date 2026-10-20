using UnityEngine;

namespace Afterglow
{
    public enum HazardKind { Spikes, PulseLaser, RiftField }

    [RequireComponent(typeof(Collider2D), typeof(AfterglowReboundTarget))]
    public sealed class AfterglowHazard : MonoBehaviour, IAfterglowReboundReceiver
    {
        [SerializeField] private HazardKind kind;
        private SpriteRenderer[] renderers;
        private float phase;
        private float hitCooldown;
        private float disabledTimer;
        private bool active = true;
        private AfterglowReboundTarget reboundTarget;
        public bool IsActive => active;

        public void Configure(HazardKind hazardKind, float offset = 0f)
        {
            kind = hazardKind;
            phase = offset;
            renderers = GetComponentsInChildren<SpriteRenderer>();
            reboundTarget = GetComponent<AfterglowReboundTarget>();
            reboundTarget?.Configure(ReboundTargetKind.Relay, hazardKind == HazardKind.Spikes ? "ROOT SNARE" : hazardKind == HazardKind.PulseLaser ? "RUNE SEAR" : "OSSUARY CENSER", Vector2.up);
        }

        private void Awake()
        {
            var collider = GetComponent<Collider2D>();
            collider.isTrigger = true;
            if (renderers == null) renderers = GetComponentsInChildren<SpriteRenderer>();
            if (reboundTarget == null) reboundTarget = GetComponent<AfterglowReboundTarget>();
        }

        private void Update()
        {
            hitCooldown -= Time.deltaTime;
            disabledTimer -= Time.deltaTime;
            if (disabledTimer > 0f)
            {
                active = false;
                if (renderers != null) foreach (var sr in renderers) sr.color = new Color(.65f, .55f, .35f, .35f);
                return;
            }
            active = true;
            if (kind == HazardKind.PulseLaser)
            {
                float cycle = (Time.time + phase) % 2.1f;
                active = cycle > .72f && cycle < 1.62f;
                Color c = cycle < .72f ? new Color(.63f, .43f, .22f, .52f) : active ? new Color(.58f, .14f, .115f, .9f) : new Color(.22f, .23f, .21f, .24f);
                if (renderers != null) foreach (var sr in renderers) sr.color = c;
            }
            else if (kind == HazardKind.RiftField)
            {
                float pulse = .4f + Mathf.Sin((Time.time + phase) * 2.4f) * .2f;
                if (renderers != null) foreach (var sr in renderers) sr.color = new Color(.55f, .25f, .2f, pulse);
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (!active || hitCooldown > 0f) return;
            var player = other.GetComponentInParent<AfterglowPlayer>();
            if (player == null) return;
            hitCooldown = kind == HazardKind.Spikes ? .7f : 1.05f;
            player.TakeDamage(kind == HazardKind.RiftField ? 2 : 1, transform.position);
        }

        public void OnRebound(AfterglowPlayer player, Vector2 incoming, Vector2 outgoing, int chain, bool perfect)
        {
            disabledTimer = perfect ? 4f : 2.6f;
            hitCooldown = 0f;
            active = false;
            AfterglowAudio.Instance?.Play("win", perfect ? 1.1f : .9f);
        }
    }
}
