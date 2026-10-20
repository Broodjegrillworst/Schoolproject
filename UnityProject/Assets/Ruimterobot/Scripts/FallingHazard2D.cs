using UnityEngine;

/// <summary>Warns before dropping a heavy hazard into the player's route, then resets it.</summary>
public class FallingHazard2D : MonoBehaviour
{
    [SerializeField] private SpriteRenderer visual;
    [SerializeField] private SpriteRenderer warningMarker;
    [SerializeField] private Collider2D hitbox;
    [SerializeField] private float detectionRange = 1.8f;
    [SerializeField] private float warningSeconds = 0.72f;
    [SerializeField] private float fallSpeed = 12f;
    [SerializeField] private float fallDistance = 5.5f;
    [SerializeField] private float resetDelay = 1.1f;

    private PlayerHealth player;
    private Vector3 startPosition;
    private float warningStartedAt = -1f;
    private float hiddenUntil = -1f;
    private bool falling;
    private bool hidden;
    private Color originalColor = Color.white;

    public void Configure(SpriteRenderer sprite, SpriteRenderer marker, Collider2D collider,
        float range, float warning, float speed, float distance)
    {
        visual = sprite;
        warningMarker = marker;
        hitbox = collider;
        detectionRange = range;
        warningSeconds = warning;
        fallSpeed = speed;
        fallDistance = distance;
    }

    private void Awake()
    {
        startPosition = transform.position;
        if (visual == null) visual = GetComponent<SpriteRenderer>();
        if (hitbox == null) hitbox = GetComponent<Collider2D>();
        if (visual != null) originalColor = visual.color;
        if (warningMarker != null) warningMarker.enabled = false;
    }

    private void Start() => player = FindAnyObjectByType<PlayerHealth>();

    private void Update()
    {
        if (Time.timeScale == 0f) return;
        if (player == null) player = FindAnyObjectByType<PlayerHealth>();
        if (hidden)
        {
            if (Time.time >= hiddenUntil) ResetHazard();
            return;
        }

        if (!falling && player != null && !player.IsGameOver &&
            Mathf.Abs(player.transform.position.x - startPosition.x) <= detectionRange &&
            player.transform.position.y < transform.position.y - 0.9f)
        {
            if (warningStartedAt < 0f) warningStartedAt = Time.time;
            float pulse = Mathf.PingPong(Time.time * 6f, 1f);
            if (visual != null) visual.color = Color.Lerp(originalColor, new Color(1f, 0.28f, 0.2f), pulse);
            if (warningMarker != null) warningMarker.enabled = true;
            if (Time.time - warningStartedAt >= warningSeconds)
            {
                falling = true;
                warningStartedAt = -1f;
                if (warningMarker != null) warningMarker.enabled = false;
                FeedbackVFX2D.Shake(0.055f, 0.11f);
            }
        }
        else if (!falling && warningStartedAt >= 0f)
        {
            warningStartedAt = -1f;
            if (visual != null) visual.color = originalColor;
            if (warningMarker != null) warningMarker.enabled = false;
        }

        if (!falling) return;
        transform.position += Vector3.down * fallSpeed * Time.deltaTime;
        if (transform.position.y <= startPosition.y - fallDistance)
        {
            falling = false;
            hidden = true;
            hiddenUntil = Time.time + resetDelay;
            if (hitbox != null) hitbox.enabled = false;
            if (visual != null) visual.enabled = false;
        }
    }

    private void ResetHazard()
    {
        transform.position = startPosition;
        hidden = false;
        falling = false;
        warningStartedAt = -1f;
        if (hitbox != null) hitbox.enabled = true;
        if (visual != null) { visual.enabled = true; visual.color = originalColor; }
    }
}
