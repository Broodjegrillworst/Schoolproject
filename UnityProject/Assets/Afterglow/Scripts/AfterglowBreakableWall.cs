using UnityEngine;

namespace Afterglow
{
    [RequireComponent(typeof(Collider2D), typeof(AfterglowReboundTarget))]
    public sealed class AfterglowBreakableWall : MonoBehaviour, IAfterglowReboundReceiver
    {
        private GameObject revealedMemory;
        private AfterglowUI ui;
        private SpriteRenderer wallRenderer;
        private Collider2D wallCollider;
        private bool broken;

        public bool IsBroken => broken;

        public void Configure(GameObject memory, AfterglowUI gameUi, string targetName)
        {
            revealedMemory = memory;
            ui = gameUi;
            wallRenderer = GetComponent<SpriteRenderer>();
            wallCollider = GetComponent<Collider2D>();
            var target = GetComponent<AfterglowReboundTarget>();
            target.Configure(ReboundTargetKind.Relay, targetName, Vector2.left);
            if (revealedMemory != null) revealedMemory.SetActive(false);
        }

        public void OnRebound(AfterglowPlayer player, Vector2 incoming, Vector2 outgoing, int chain, bool perfect)
        {
            Open(transform.position, perfect ? new Color(.95f, .74f, .43f) : new Color(.75f, .48f, .31f));
        }

        public void HitByWeapon(Vector2 hitPoint, bool charged)
        {
            Open(hitPoint, charged ? new Color(.74f, .93f, 1f) : new Color(.82f, .72f, .5f));
        }

        public void Interact()
        {
            Open(transform.position, new Color(.85f, .7f, .46f));
        }

        private void Open(Vector2 hitPoint, Color effectColor)
        {
            if (broken) return;
            broken = true;
            if (wallCollider != null) wallCollider.enabled = false;
            var target = GetComponent<AfterglowReboundTarget>();
            if (target != null) target.SetCanBeMarked(false);
            if (wallRenderer != null) wallRenderer.enabled = false;
            if (revealedMemory != null) revealedMemory.SetActive(true);
            ui?.PlayImpactEffect(hitPoint, effectColor);
            ui?.Notify("VERBORGEN HEILIGDOM ONTHULD", new Color(.85f, .7f, .46f), 2.6f);
            AfterglowAudio.Instance?.Play("win", 1f);
            Destroy(gameObject, .15f);
        }
    }
}
