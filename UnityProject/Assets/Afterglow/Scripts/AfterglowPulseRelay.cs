using UnityEngine;

namespace Afterglow
{
    [RequireComponent(typeof(Collider2D), typeof(AfterglowReboundTarget))]
    public sealed class AfterglowPulseRelay : MonoBehaviour, IAfterglowReboundReceiver
    {
        private GameObject shortcutGate;
        private AfterglowUI ui;
        private SpriteRenderer[] renderers;
        private int requiredChain = 1;
        private bool activated;

        public void Configure(GameObject gate, AfterglowUI gameUi, Vector2 reboundNormal, int chainRequired)
        {
            shortcutGate = gate;
            ui = gameUi;
            requiredChain = Mathf.Max(1, chainRequired);
            var hitbox = GetComponent<Collider2D>();
            hitbox.isTrigger = true;
            GetComponent<AfterglowReboundTarget>().Configure(ReboundTargetKind.Relay, "BELL RELIC", reboundNormal);
            renderers = GetComponentsInChildren<SpriteRenderer>();
        }

        public void OnRebound(AfterglowPlayer player, Vector2 incoming, Vector2 outgoing, int chain, bool perfect)
        {
            if (activated) return;
            if (chain < requiredChain)
            {
                ui?.Notify("KETEN TE KORT  •  MINIMAAL " + requiredChain, new Color(.72f, .42f, .28f), 1.5f);
                ui?.PlayImpactEffect(transform.position, new Color(.62f, .28f, .19f));
                return;
            }

            activated = true;
            if (shortcutGate != null) shortcutGate.SetActive(false);
            if (renderers != null)
            {
                foreach (var renderer in renderers)
                {
                    renderer.color = new Color(.74f, .61f, .36f, .94f);
                    renderer.sortingOrder += 2;
                }
            }
            ui?.Notify(perfect ? "ZEGEL GEBROKEN  •  PERFECTE REBOUND" : "ZEGEL GEBROKEN  •  KETEN " + chain, new Color(.79f, .66f, .4f), 2.4f);
            AfterglowAudio.Instance?.Play("win", perfect ? 1.15f : .95f);
        }
    }
}
