using UnityEngine;

/// <summary>Airborne Rift hunter that telegraphs a dive, commits, then recovers to its post.</summary>
[RequireComponent(typeof(Rigidbody2D))]
public class SwoopEnemy2D : MonoBehaviour
{
    private enum State { Hover, Windup, Dive, Recover }

    [SerializeField] private float detectionRange = 9f;
    [SerializeField] private float hoverHeight = 0.3f;
    [SerializeField] private float hoverSpeed = 1.8f;
    [SerializeField] private float diveSpeed = 8f;
    [SerializeField] private float warningSeconds = 0.68f;
    [SerializeField] private float attackCooldown = 2.4f;
    [SerializeField] private float recoverSpeed = 5f;

    private Rigidbody2D body;
    private SpriteRenderer sprite;
    private PlayerHealth player;
    private Vector2 anchor;
    private Vector2 diveTarget;
    private State state;
    private float stateStartedAt;
    private float nextAttackAt;
    private Color originalColor = Color.white;

    public void Configure(float range, float speed, float warning, float cooldown)
    {
        detectionRange = range;
        diveSpeed = speed;
        warningSeconds = warning;
        attackCooldown = cooldown;
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        sprite = GetComponentInChildren<SpriteRenderer>();
        if (sprite != null) originalColor = sprite.color;
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        anchor = transform.position;
    }

    private void Start()
    {
        player = FindAnyObjectByType<PlayerHealth>();
        nextAttackAt = Time.time + 1.1f;
    }

    private void FixedUpdate()
    {
        if (Time.timeScale == 0f || body == null) return;
        if (player == null) player = FindAnyObjectByType<PlayerHealth>();
        if (player == null || player.IsGameOver) return;

        float horizontal = player.transform.position.x - body.position.x;
        if (sprite != null && Mathf.Abs(horizontal) > 0.05f) sprite.flipX = horizontal < 0f;
        switch (state)
        {
            case State.Hover:
                float hoverY = anchor.y + Mathf.Sin(Time.time * hoverSpeed + anchor.x) * hoverHeight;
                body.MovePosition(new Vector2(anchor.x + Mathf.Sin(Time.time * 0.62f + anchor.y) * 0.22f, hoverY));
                if (Mathf.Abs(horizontal) < detectionRange && Time.time >= nextAttackAt)
                {
                    state = State.Windup;
                    stateStartedAt = Time.time;
                    diveTarget = player.transform.position;
                    if (sprite != null) sprite.color = new Color(1f, 0.38f, 0.32f);
                }
                break;
            case State.Windup:
                if (Time.time >= stateStartedAt + warningSeconds)
                {
                    state = State.Dive;
                    stateStartedAt = Time.time;
                    nextAttackAt = Time.time + attackCooldown;
                }
                break;
            case State.Dive:
                Vector2 next = Vector2.MoveTowards(body.position, diveTarget, diveSpeed * Time.fixedDeltaTime);
                body.MovePosition(next);
                if (Vector2.Distance(next, diveTarget) <= 0.38f || Time.time >= stateStartedAt + 0.85f)
                {
                    state = State.Recover;
                    if (sprite != null) sprite.color = originalColor;
                }
                break;
            case State.Recover:
                Vector2 recovered = Vector2.MoveTowards(body.position, anchor, recoverSpeed * Time.fixedDeltaTime);
                body.MovePosition(recovered);
                if (Vector2.Distance(recovered, anchor) <= 0.16f)
                {
                    state = State.Hover;
                    if (sprite != null) sprite.color = originalColor;
                }
                break;
        }
    }
}
