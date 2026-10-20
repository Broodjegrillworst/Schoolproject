using UnityEngine;

/// <summary>A readable warning, active and recovery cycle for a laser obstacle.</summary>
public class TimedLaserHazard2D : MonoBehaviour
{
    [SerializeField] private Collider2D damageCollider;
    [SerializeField] private SpriteRenderer beam;
    [SerializeField] private float cycleSeconds = 3.4f;
    [SerializeField] private float activeSeconds = 1.05f;
    [SerializeField] private float warningSeconds = 0.62f;
    [SerializeField] private float phaseOffset;

    private float suppressedUntil;
    private Color baseColor = Color.white;

    public void Configure(Collider2D hitbox, SpriteRenderer visual, float cycle, float active,
        float warning, float offset)
    {
        damageCollider = hitbox;
        beam = visual;
        cycleSeconds = Mathf.Max(1f, cycle);
        activeSeconds = Mathf.Clamp(active, 0.2f, cycleSeconds - 0.2f);
        warningSeconds = Mathf.Clamp(warning, 0.15f, cycleSeconds - activeSeconds);
        phaseOffset = offset;
        if (beam != null) baseColor = beam.color;
    }

    public void SuppressFor(float seconds)
    {
        suppressedUntil = Mathf.Max(suppressedUntil, Time.time + Mathf.Max(0f, seconds));
        ApplyState(false, false, 0f);
    }

    private void Awake()
    {
        if (damageCollider == null) damageCollider = GetComponent<Collider2D>();
        if (beam == null) beam = GetComponent<SpriteRenderer>();
        if (beam != null) baseColor = beam.color;
    }

    private void Update()
    {
        if (Time.timeScale == 0f) return;
        if (Time.time < suppressedUntil)
        {
            ApplyState(false, false, 0f);
            return;
        }

        float phase = Mathf.Repeat(Time.time + phaseOffset, cycleSeconds);
        float activeStart = cycleSeconds - activeSeconds;
        float warningStart = activeStart - warningSeconds;
        bool active = phase >= activeStart;
        bool warning = phase >= warningStart && !active;
        ApplyState(active, warning, warning ? Mathf.PingPong(Time.time * 5f, 1f) : 1f);
    }

    private void ApplyState(bool active, bool warning, float pulse)
    {
        if (damageCollider != null) damageCollider.enabled = active;
        if (beam == null) return;
        beam.enabled = active || warning;
        if (active) beam.color = new Color(1f, 0.16f, 0.2f, 0.96f);
        else if (warning) beam.color = new Color(1f, 0.79f, 0.25f, Mathf.Lerp(0.2f, 0.95f, pulse));
        else beam.color = baseColor;
    }
}
