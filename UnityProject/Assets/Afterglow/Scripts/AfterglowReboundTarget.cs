using UnityEngine;

namespace Afterglow
{
    public enum ReboundTargetKind { Surface, Enemy, Relay, Boss }

    public interface IAfterglowReboundReceiver
    {
        void OnRebound(AfterglowPlayer player, Vector2 incoming, Vector2 outgoing, int chain, bool perfect);
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class AfterglowReboundTarget : MonoBehaviour
    {
        [SerializeField] private ReboundTargetKind kind = ReboundTargetKind.Surface;
        [SerializeField] private string displayName = "SURFACE";
        [SerializeField] private Vector2 normalOverride;
        [SerializeField] private bool canBeMarked = true;

        private Collider2D hitbox;
        private SpriteRenderer markGlyph;
        private IAfterglowReboundReceiver receiver;
        private AfterglowPlayer markedBy;

        public ReboundTargetKind Kind => kind;
        public string DisplayName => displayName;
        public bool CanBeMarked => canBeMarked && isActiveAndEnabled;
        public bool IsMarked => markedBy != null;
        public Collider2D Hitbox => hitbox;
        public Vector2 FixedReboundDirection => normalOverride.sqrMagnitude > .01f ? normalOverride.normalized : Vector2.zero;

        private void Awake()
        {
            hitbox = GetComponent<Collider2D>();
            CacheReceiver();
            CreateMarkGlyph();
        }

        public void Configure(ReboundTargetKind targetKind, string label, Vector2 fixedNormal = default, bool markable = true)
        {
            kind = targetKind;
            displayName = string.IsNullOrWhiteSpace(label) ? targetKind.ToString().ToUpperInvariant() : label;
            normalOverride = fixedNormal;
            canBeMarked = markable;
            CacheReceiver();
        }

        public void SetCanBeMarked(bool value)
        {
            canBeMarked = value;
            if (!value) SetMarked(null, Vector2.zero);
        }

        public Vector2 AimPoint(Vector2 from, Vector2 aim)
        {
            if (hitbox == null) hitbox = GetComponent<Collider2D>();
            Vector2 probe = from + aim.normalized * .8f;
            Vector2 point = hitbox != null ? hitbox.ClosestPoint(probe) : (Vector2)transform.position;
            if ((point - from).sqrMagnitude < .01f) point = transform.position;
            return point;
        }

        public void SetMarked(AfterglowPlayer owner, Vector2 point)
        {
            if (owner != null && markedBy != null && markedBy != owner) return;
            markedBy = owner;
            if (markGlyph != null)
            {
                markGlyph.gameObject.SetActive(owner != null);
                if (owner != null) markGlyph.transform.position = new Vector3(point.x, point.y, transform.position.z - .2f);
            }
        }

        public Vector2 SurfaceNormal(Vector2 playerPosition, Vector2 contactPoint, Vector2 incoming)
        {
            if (normalOverride.sqrMagnitude > .01f) return normalOverride.normalized;
            if (kind == ReboundTargetKind.Enemy || kind == ReboundTargetKind.Boss)
            {
                Vector2 away = playerPosition - (Vector2)transform.position;
                return away.sqrMagnitude > .01f ? away.normalized : -incoming.normalized;
            }

            Bounds bounds = hitbox != null ? hitbox.bounds : new Bounds(transform.position, Vector3.one);
            float left = Mathf.Abs(contactPoint.x - bounds.min.x);
            float right = Mathf.Abs(contactPoint.x - bounds.max.x);
            float bottom = Mathf.Abs(contactPoint.y - bounds.min.y);
            float top = Mathf.Abs(contactPoint.y - bounds.max.y);
            float closest = Mathf.Min(left, right, bottom, top);
            if (closest == top) return Vector2.up;
            if (closest == bottom) return Vector2.down;
            if (closest == left) return Vector2.left;
            return Vector2.right;
        }

        public void NotifyRebound(AfterglowPlayer player, Vector2 incoming, Vector2 outgoing, int chain, bool perfect)
        {
            if (receiver == null) CacheReceiver();
            receiver?.OnRebound(player, incoming, outgoing, chain, perfect);
        }

        private void CacheReceiver()
        {
            receiver = null;
            var behaviours = GetComponents<MonoBehaviour>();
            foreach (var behaviour in behaviours)
            {
                if (behaviour is IAfterglowReboundReceiver reboundReceiver)
                {
                    receiver = reboundReceiver;
                    break;
                }
            }
        }

        private void CreateMarkGlyph()
        {
            if (markGlyph != null) return;
            var glyph = new GameObject("Afterglow mark indicator");
            glyph.transform.SetParent(transform, true);
            glyph.transform.localScale = Vector3.one * .52f;
            markGlyph = glyph.AddComponent<SpriteRenderer>();
            markGlyph.sprite = AfterglowArt.Disc("afterglow-reticle", new Color(.18f, .14f, .085f, .2f), new Color(.83f, .68f, .39f, .95f));
            if (AfterglowArt.SpriteMaterial != null) markGlyph.sharedMaterial = AfterglowArt.SpriteMaterial;
            markGlyph.color = new Color(.86f, .76f, .54f, .98f);
            markGlyph.sortingOrder = 40;
            markGlyph.enabled = true;
            markGlyph.gameObject.SetActive(false);
        }
    }
}
