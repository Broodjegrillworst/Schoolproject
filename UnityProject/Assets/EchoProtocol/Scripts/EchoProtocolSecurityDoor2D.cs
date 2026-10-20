using UnityEngine;

namespace EchoProtocol
{
    public sealed class EchoProtocolSecurityDoor2D : MonoBehaviour
    {
        [SerializeField] private Collider2D blockingCollider;
        [SerializeField] private SpriteRenderer indicator;
        private Vector3 closedPosition;
        private Vector3 openPosition;
        private bool heldOpen;
        private float openUntil;

        public void Configure(Collider2D collider, SpriteRenderer light)
        {
            blockingCollider = collider;
            indicator = light;
        }

        private void Awake()
        {
            closedPosition = transform.position;
            openPosition = closedPosition + Vector3.up * 3.4f;
        }

        public void SetHeldOpen(bool value) => heldOpen = value;
        public void OpenForSeconds(float seconds) => openUntil = Mathf.Max(openUntil, Time.time + seconds);

        private void Update()
        {
            bool open = heldOpen || Time.time < openUntil;
            Vector3 target = open ? openPosition : closedPosition;
            transform.position = Vector3.MoveTowards(transform.position, target, Time.deltaTime * (open ? 6.5f : 3.2f));
            float progress = Vector3.Distance(transform.position, closedPosition);
            if (blockingCollider != null) blockingCollider.enabled = progress < 2.9f;
            if (indicator != null) indicator.color = open ? new Color(0.55f, 0.88f, 0.73f) : new Color(0.92f, 0.32f, 0.18f);
        }
    }
}
