using System.Collections;
using UnityEngine;

/// <summary>A temporary route that crumbles after a clear landing warning.</summary>
[RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
public class CollapsingPlatform2D : MonoBehaviour
{
    [SerializeField] private Collider2D platformCollider;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private SpriteRenderer visual;
    [SerializeField] private float warningSeconds = 0.68f;
    [SerializeField] private float resetSeconds = 1.7f;

    private Vector3 startPosition;
    private bool collapsing;
    private Color originalColor;

    public void ConfigureVisual(SpriteRenderer renderer)
    {
        visual = renderer;
        if (visual != null) originalColor = visual.color;
    }

    private void Awake()
    {
        startPosition = transform.position;
        if (platformCollider == null) platformCollider = GetComponent<Collider2D>();
        if (body == null) body = GetComponent<Rigidbody2D>();
        if (visual == null) visual = GetComponent<SpriteRenderer>();
        if (body != null)
        {
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.freezeRotation = true;
        }
        if (visual != null) originalColor = visual.color;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collapsing) return;
        PlayerHealth player = collision.collider.GetComponentInParent<PlayerHealth>();
        if (player == null || player.transform.position.y < transform.position.y + 0.3f) return;
        StartCoroutine(CollapseAndRestore());
    }

    private IEnumerator CollapseAndRestore()
    {
        collapsing = true;
        float warningEnd = Time.time + warningSeconds;
        while (Time.time < warningEnd)
        {
            if (visual != null)
            {
                float pulse = Mathf.PingPong(Time.time * 8f, 1f);
                visual.color = Color.Lerp(originalColor, new Color(1f, 0.38f, 0.2f), pulse);
                if (body != null)
                    body.MovePosition(startPosition + Vector3.right * Mathf.Sin(Time.time * 48f) * 0.035f);
            }
            yield return null;
        }

        transform.position = startPosition;
        if (platformCollider != null) platformCollider.enabled = false;
        if (body != null)
        {
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 3.5f;
            body.linearVelocity = Vector2.down * 2f;
        }
        if (visual != null) visual.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0.55f);
        FeedbackVFX2D.Burst(transform.position, new Color(1f, 0.65f, 0.31f), 9);
        yield return new WaitForSeconds(resetSeconds);

        if (body != null)
        {
            body.bodyType = RigidbodyType2D.Kinematic;
            body.linearVelocity = Vector2.zero;
            body.position = startPosition;
        }
        transform.position = startPosition;
        if (platformCollider != null) platformCollider.enabled = true;
        if (visual != null) visual.color = originalColor;
        collapsing = false;
    }
}
