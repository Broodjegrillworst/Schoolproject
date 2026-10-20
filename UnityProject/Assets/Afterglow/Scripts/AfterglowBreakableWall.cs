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
            if (broken) return;
            broken = true;
            if (wallCollider != null) wallCollider.enabled = false;
            var target = GetComponent<AfterglowReboundTarget>();
            if (target != null) target.SetCanBeMarked(false);
            if (wallRenderer != null) wallRenderer.enabled = false;
            if (revealedMemory != null) revealedMemory.SetActive(true);
            ui?.PlayImpactEffect(transform.position, perfect ? new Color(.95f, .74f, .43f) : new Color(.75f, .48f, .31f));
            ui?.Notify(perfect ? "GEHEIME MUUR GEBROKEN  •  PERFECTE REBOUND" : "VERBORGEN DOORGANG GEOPEND", new Color(.85f, .7f, .46f), 2.6f);
            AfterglowAudio.Instance?.Play("win", perfect ? 1.18f : .94f);
            Destroy(gameObject, .15f);
        }
    }
}
