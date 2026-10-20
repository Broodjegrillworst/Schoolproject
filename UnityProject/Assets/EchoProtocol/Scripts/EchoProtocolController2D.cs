using UnityEngine;
using UnityEngine.InputSystem;

namespace EchoProtocol
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public sealed class EchoProtocolController2D : MonoBehaviour
    {
        [SerializeField] private Transform visual;
        [SerializeField] private float moveSpeed = 6.2f;
        [SerializeField] private float acceleration = 48f;
        [SerializeField] private float jumpVelocity = 12.2f;
        [SerializeField] private LayerMask groundMask = ~0;
        private Rigidbody2D body;
        private CapsuleCollider2D capsule;
        private Vector3 visualScale;
        private float horizontal;
        private float jumpBufferedUntil;
        private float coyoteUntil;
        private float damageUntil;
        private Vector2 checkpoint;
        private int facing = 1;

        public int Facing => facing;
        public int InteractionSequence { get; private set; }
        public Rigidbody2D Body => body;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            capsule = GetComponent<CapsuleCollider2D>();
            body.gravityScale = 3.2f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.linearDamping = 0f;
            if (visual == null) visual = transform.Find("Visual");
            if (visual != null) visualScale = visual.localScale;
            checkpoint = transform.position;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || Time.timeScale == 0f) return;
            horizontal = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f)
                       - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
            if (Mathf.Abs(horizontal) > 0.05f) facing = horizontal > 0f ? 1 : -1;

            bool grounded = IsGrounded();
            if (grounded) coyoteUntil = Time.time + 0.13f;
            if (keyboard.spaceKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame)
                jumpBufferedUntil = Time.time + 0.14f;
            if (Time.time <= jumpBufferedUntil && (grounded || Time.time <= coyoteUntil))
            {
                body.linearVelocity = new Vector2(body.linearVelocity.x, jumpVelocity);
                jumpBufferedUntil = 0f;
            }
            if (keyboard.spaceKey.wasReleasedThisFrame && body.linearVelocity.y > 0.5f)
                body.linearVelocity = new Vector2(body.linearVelocity.x, body.linearVelocity.y * 0.52f);
            if (keyboard.eKey.wasPressedThisFrame)
            {
                InteractionSequence++;
                InteractNearby(false);
            }
            UpdateVisual(grounded);
            if (transform.position.y < -12f) Respawn();
        }

        private void FixedUpdate()
        {
            Vector2 velocity = body.linearVelocity;
            velocity.x = Mathf.MoveTowards(velocity.x, horizontal * moveSpeed, acceleration * Time.fixedDeltaTime);
            body.linearVelocity = velocity;
        }

        private bool IsGrounded()
        {
            Vector2 origin = capsule.bounds.center;
            Vector2 size = new Vector2(capsule.bounds.size.x * 0.88f, 0.18f);
            RaycastHit2D[] hits = Physics2D.CapsuleCastAll(origin, size, CapsuleDirection2D.Vertical, 0f, Vector2.down, 0.12f, groundMask);
            foreach (RaycastHit2D hit in hits)
                if (hit.collider != null && hit.collider != capsule && !hit.collider.isTrigger) return true;
            return false;
        }

        private void UpdateVisual(bool grounded)
        {
            if (visual == null) return;
            visual.localScale = new Vector3(Mathf.Abs(visualScale.x) * facing, visualScale.y, visualScale.z);
            float bob = grounded && Mathf.Abs(horizontal) > 0.1f ? Mathf.Abs(Mathf.Sin(Time.time * 13f)) * 0.035f : 0f;
            visual.localPosition = new Vector3(0f, bob, 0f);
            float lean = grounded ? -horizontal * 3.5f : Mathf.Clamp(-body.linearVelocity.y * 0.55f, -9f, 9f);
            visual.localRotation = Quaternion.Euler(0f, 0f, lean);
        }

        public void Configure(Transform visualRoot)
        {
            visual = visualRoot;
            if (visual != null) visualScale = visual.localScale;
        }

        public void SetCheckpoint(Vector2 point) => checkpoint = point;

        public void Damage(Vector2 source)
        {
            if (Time.time < damageUntil) return;
            damageUntil = Time.time + 1.1f;
            body.linearVelocity = new Vector2(Mathf.Sign(transform.position.x - source.x) * 4.5f, 5.5f);
            EchoProtocolHUD2D.ShowMessage("SECURITY CONTACT // RETURNING TO LAST SAFE POSITION", 2.2f);
            Invoke(nameof(Respawn), 0.75f);
        }

        private void Respawn()
        {
            CancelInvoke(nameof(Respawn));
            body.linearVelocity = Vector2.zero;
            transform.position = checkpoint;
            damageUntil = Time.time + 0.6f;
        }

        private void InteractNearby(bool fromEcho)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position + Vector3.right * facing * 0.55f, 1.15f);
            foreach (Collider2D hit in hits)
            {
                EchoProtocolInteractable2D interactable = hit.GetComponentInParent<EchoProtocolInteractable2D>();
                if (interactable == null) continue;
                if (fromEcho) interactable.OnEchoInteract();
                else interactable.OnPlayerInteract();
                return;
            }
            if (!fromEcho) EchoProtocolHUD2D.ShowMessage("NO INTERACTABLE SIGNAL IN RANGE", 1f);
        }
    }

    public abstract class EchoProtocolInteractable2D : MonoBehaviour
    {
        public abstract void OnPlayerInteract();
        public abstract void OnEchoInteract();
    }
}

