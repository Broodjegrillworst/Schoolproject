using UnityEngine;

/// <summary>Pressing this plate buys a short safe window through the linked laser gate.</summary>
public class PressurePlate2D : MonoBehaviour
{
    [SerializeField] private TimedLaserHazard2D[] linkedLasers;
    [SerializeField] private SpriteRenderer visual;
    [SerializeField] private float safeSeconds = 5.5f;
    [SerializeField] private float cooldownSeconds = 1.4f;
    private float nextPressTime;
    private Color idleColor = Color.white;

    public void Configure(TimedLaserHazard2D laser, SpriteRenderer renderer, float duration)
    {
        linkedLasers = laser == null ? new TimedLaserHazard2D[0] : new[] { laser };
        visual = renderer;
        safeSeconds = duration;
        if (visual != null) idleColor = visual.color;
    }

    public void Configure(TimedLaserHazard2D[] lasers, SpriteRenderer renderer, float duration)
    {
        linkedLasers = lasers;
        visual = renderer;
        safeSeconds = duration;
        if (visual != null) idleColor = visual.color;
    }

    private void Awake()
    {
        if (visual == null) visual = GetComponent<SpriteRenderer>();
        if (visual != null) idleColor = visual.color;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        PlayerHealth player = collision.collider.GetComponentInParent<PlayerHealth>();
        if (player == null || player.transform.position.y < transform.position.y + 0.22f || Time.time < nextPressTime)
            return;
        nextPressTime = Time.time + cooldownSeconds;
        if (linkedLasers != null)
            foreach (TimedLaserHazard2D laser in linkedLasers)
                if (laser != null) laser.SuppressFor(safeSeconds);
        if (visual != null) visual.color = new Color(0.3f, 1f, 0.72f, 1f);
        FeedbackVFX2D.Burst(transform.position + Vector3.up * 0.12f, new Color(0.35f, 1f, 0.78f), 10);
        if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayCheckpoint();
        CancelInvoke(nameof(ResetColor));
        Invoke(nameof(ResetColor), 0.55f);
    }

    private void ResetColor()
    {
        if (visual != null) visual.color = idleColor;
    }
}
