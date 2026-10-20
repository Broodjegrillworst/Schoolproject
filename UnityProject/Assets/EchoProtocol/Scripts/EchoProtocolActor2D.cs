using UnityEngine;

namespace EchoProtocol
{
    public sealed class EchoProtocolActor2D : MonoBehaviour
    {
        private EchoProtocolFrame[] frames;
        private Transform visual;
        private SpriteRenderer[] renderers;
        private Vector3 visualBaseScale;
        private float startedAt;
        private int frameIndex;
        private int lastInteractionSequence;
        private bool completed;

        public bool IsComplete => completed;

        public void Initialize(EchoProtocolFrame[] trace, Transform visualSource)
        {
            frames = trace;
            startedAt = Time.time;
            if (visualSource != null)
            {
                visual = Instantiate(visualSource, transform);
                visual.name = "Recorded silhouette";
                visual.localPosition = Vector3.zero;
                visualBaseScale = visual.localScale;
                renderers = visual.GetComponentsInChildren<SpriteRenderer>(true);
                foreach (SpriteRenderer renderer in renderers)
                {
                    Color tint = new Color(0.5f, 0.82f, 0.84f, 0.42f);
                    renderer.color = tint;
                    renderer.sortingOrder += 8;
                }
            }
            lastInteractionSequence = frames.Length > 0 ? frames[0].interactionSequence : 0;
        }

        private void Update()
        {
            if (frames == null || frames.Length == 0 || completed) return;
            float elapsed = Time.time - startedAt;
            while (frameIndex < frames.Length - 2 && frames[frameIndex + 1].time < elapsed) frameIndex++;

            EchoProtocolFrame a = frames[frameIndex];
            EchoProtocolFrame b = frames[Mathf.Min(frameIndex + 1, frames.Length - 1)];
            float span = Mathf.Max(0.0001f, b.time - a.time);
            float blend = Mathf.Clamp01((elapsed - a.time) / span);
            transform.position = Vector3.Lerp(a.position, b.position, blend);
            int facing = blend < 0.5f ? a.facing : b.facing;
            if (visual != null)
                visual.localScale = new Vector3(Mathf.Abs(visualBaseScale.x) * facing, visualBaseScale.y, visualBaseScale.z);

            int sequence = b.interactionSequence;
            if (sequence > lastInteractionSequence)
            {
                for (int i = lastInteractionSequence; i < sequence; i++)
                    ReplayInteractionAt(transform.position, facing);
                lastInteractionSequence = sequence;
            }

            if (elapsed >= frames[frames.Length - 1].time)
            {
                transform.position = frames[frames.Length - 1].position;
                completed = true;
                EchoProtocolHUD2D.ShowMessage("ECHO TRACE COMPLETE // FINAL POSITION RETAINED", 1.8f);
            }
        }

        private static void ReplayInteractionAt(Vector3 position, int facing)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(position + Vector3.right * facing * 0.55f, 1.15f);
            foreach (Collider2D hit in hits)
            {
                EchoProtocolInteractable2D interactable = hit.GetComponentInParent<EchoProtocolInteractable2D>();
                if (interactable == null) continue;
                interactable.OnEchoInteract();
                return;
            }
        }
    }
}
