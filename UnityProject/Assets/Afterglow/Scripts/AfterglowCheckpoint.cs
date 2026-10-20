using UnityEngine;

namespace Afterglow
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class AfterglowCheckpoint : MonoBehaviour
    {
        [SerializeField] private int checkpointId;
        private AfterglowUI ui;
        private bool activated;

        public void Configure(int id, AfterglowUI gameUi) { checkpointId = id; ui = gameUi; }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (activated || other.GetComponentInParent<AfterglowPlayer>() == null) return;
            activated = true;
            AfterglowSession.SetCheckpoint(checkpointId, transform.position + Vector3.up * 1.1f);
            ui?.Notify("HERINNERING BEWAARD", new Color(.78f, .66f, .4f));
            ui?.PlayCheckpointEffect(transform.position);
            var beacon = GetComponentInChildren<SpriteRenderer>();
            if (beacon != null) beacon.color = new Color(.84f, .68f, .4f);
        }
    }
}
