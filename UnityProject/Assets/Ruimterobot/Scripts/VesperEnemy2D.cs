using UnityEngine;

public enum VesperEnemyType
{
    Stalker,
    Crawler,
    Wisp,
    Sentry,
    Charger,
    ShieldBearer,
    Burrower,
    RiftHound,
    Elite,
    MiniBoss
}

[RequireComponent(typeof(Collider2D))]
public sealed class VesperEnemy2D : MonoBehaviour
{
    [SerializeField] private VesperEnemyType type;
    [SerializeField] private float patrolDistance = 2.4f;
    [SerializeField] private float speed = 2f;
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private bool boss;
    private Vector3 anchor;
    private int health;
    private int direction = 1;
    private float nextAttack;
    private float stunEnd;
    private float attackWindup;
    private Transform target;
    private SpriteRenderer sprite;
    private Color baseColor;
    private bool dead;
    private float phase;
    private float bobSeed;
    private string displayName;
    public int CurrentHealth => health;
    public int MaximumHealth => maxHealth;
    public bool IsBoss => boss;

    public void Configure(VesperEnemyType enemyType, Color color, bool asBoss, int healthOverride = 0, string title = "")
    {
        type = enemyType;
        baseColor = color;
        boss = asBoss;
        maxHealth = healthOverride > 0 ? healthOverride : asBoss ? 18 : (enemyType == VesperEnemyType.Elite ? 6 : 3);
        displayName = string.IsNullOrEmpty(title) ? enemyType.ToString().ToUpperInvariant() : title;
        health = maxHealth;
        if (sprite != null) sprite.color = baseColor;
    }

    private void Awake()
    {
        anchor = transform.position;
        health = maxHealth;
        sprite = GetComponentInChildren<SpriteRenderer>();
        if (sprite != null) baseColor = sprite.color;
        bobSeed = Random.value * 9f;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) target = player.transform;
        if (TryGetComponent(out Rigidbody2D rb)) { rb.gravityScale = 0f; rb.bodyType = RigidbodyType2D.Kinematic; }
        nextAttack = Time.time + Random.Range(0.7f, 1.8f);
    }

    private void Update()
    {
        if (dead) return;
        bool enraged = boss && health <= maxHealth * 0.5f;
        if (enraged && phase < 1f)
        {
            phase = 1f;
            VesperGameUI.FlashMessage("WARDEN // SECOND PHASE", 1.8f);
            VesperCamera2D.Shake(0.24f, 0.4f);
            if (sprite != null) baseColor = Color.Lerp(baseColor, new Color(1f, 0.34f, 0.38f), 0.38f);
        }
        float pace = speed * (enraged ? 1.5f : 1f);
        if (target == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) target = p.transform;
        }
        float t = Time.time + bobSeed;
        float distance = target == null ? 99f : Vector2.Distance(transform.position, target.position);
        Vector3 pos = anchor;
        switch (type)
        {
            case VesperEnemyType.Stalker:
            case VesperEnemyType.RiftHound:
                if (distance < (boss ? 18f : 10f)) pos.x = Mathf.MoveTowards(transform.position.x, target.position.x, pace * (type == VesperEnemyType.RiftHound ? 1.7f : 1f) * Time.deltaTime);
                else pos.x = anchor.x + Mathf.Sin(t * 0.7f) * patrolDistance;
                break;
            case VesperEnemyType.Crawler:
                pos.x = anchor.x + Mathf.Sin(t * speed) * patrolDistance;
                pos.y = anchor.y + Mathf.Abs(Mathf.Sin(t * 3f)) * 0.09f;
                break;
            case VesperEnemyType.Wisp:
                pos = anchor + new Vector3(Mathf.Sin(t * 0.8f) * patrolDistance, Mathf.Sin(t * 1.3f) * 0.65f, 0f);
                if (distance < 8f && Time.time >= nextAttack) FireBolt();
                break;
            case VesperEnemyType.Sentry:
                if (distance < 13f && Time.time >= nextAttack) { attackWindup = 0.42f; nextAttack = Time.time + 2.2f; }
                if (attackWindup > 0f) { attackWindup -= Time.deltaTime; if (attackWindup <= 0f) FireBolt(); }
                break;
            case VesperEnemyType.Charger:
                pos.x = anchor.x + Mathf.Sin(t * 0.45f) * patrolDistance * 0.4f;
                if (distance < 9f && Time.time >= nextAttack) { attackWindup = 0.55f; nextAttack = Time.time + 2.5f; }
                if (attackWindup > 0f) { attackWindup -= Time.deltaTime; if (attackWindup <= 0f && target != null) { direction = target.position.x >= transform.position.x ? 1 : -1; transform.position += Vector3.right * direction * 4f; } }
                break;
            case VesperEnemyType.ShieldBearer:
                if (distance < 5f && target != null) pos.x = Mathf.MoveTowards(transform.position.x, target.position.x - (target.position.x > transform.position.x ? 1.5f : -1.5f), pace * 0.5f * Time.deltaTime);
                else pos.x = anchor.x + Mathf.Sin(t * 0.5f) * patrolDistance;
                break;
            case VesperEnemyType.Burrower:
                pos.y = anchor.y + (Mathf.Sin(t * 0.55f) > 0.1f ? 0f : -1.2f);
                if (target != null && distance < 7f && pos.y <= anchor.y - 1f) pos = Vector3.Lerp(pos, target.position + Vector3.up * 0.7f, Time.deltaTime * 2f);
                break;
            case VesperEnemyType.Elite:
                pos.x = anchor.x + Mathf.Sin(t * 0.8f) * patrolDistance;
                pos.y = anchor.y + Mathf.Sin(t * 1.1f) * 0.4f;
                if (distance < 11f && Time.time >= nextAttack) FireBolt();
                break;
            case VesperEnemyType.MiniBoss:
                pos.x = anchor.x + Mathf.Sin(t * 0.42f) * patrolDistance * 0.6f;
                pos.y = anchor.y + Mathf.Sin(t * 1.5f) * 0.12f;
                if (distance < 13f && Time.time >= nextAttack) { attackWindup = 0.35f; nextAttack = Time.time + 1.6f; }
                if (attackWindup > 0f) { attackWindup -= Time.deltaTime; if (attackWindup <= 0f) { FireBolt(); if (target != null) { direction = target.position.x >= transform.position.x ? 1 : -1; FireBoltAngle(0.22f); FireBoltAngle(-0.22f); } } }
                break;
        }
        if (Time.time < stunEnd) pos = transform.position;
        transform.position = Vector3.Lerp(transform.position, pos, Time.deltaTime * (type == VesperEnemyType.Wisp ? 3f : 8f));
        if (sprite != null)
        {
            float flash = attackWindup > 0f ? Mathf.PingPong(Time.time * 10f, 0.6f) : 0f;
            sprite.color = Color.Lerp(baseColor, Color.white, flash);
            if (target != null && distance < 10f && type != VesperEnemyType.Crawler)
                sprite.flipX = target.position.x < transform.position.x;
        }
    }

    private void FireBolt() { nextAttack = Time.time + Random.Range(1.8f, 2.8f); FireBoltAngle(0f); }
    private void FireBoltAngle(float angle)
    {
        if (target == null) return;
        Vector2 dir = ((Vector2)(target.position - transform.position)).normalized;
        dir = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg) * dir;
        VesperEffectPool.Fire(transform.position, dir, false, 1, boss || type == VesperEnemyType.MiniBoss);
    }

    public void TakeDamage(int damage, Vector2 impulse)
    {
        if (dead) return;
        health -= Mathf.Max(1, damage);
        if (boss) VesperGameUI.SetBoss(displayName, Mathf.Clamp01(health / (float)maxHealth));
        stunEnd = Time.time + 0.1f;
        if (sprite != null) sprite.color = Color.white;
        transform.position += (Vector3)(impulse * 0.02f);
        if (health <= 0) Die();
    }

    private void Die()
    {
        dead = true;
        VesperProgress.AddShards(boss ? 6 : type == VesperEnemyType.Elite ? 2 : 1);
        if (boss)
        {
            VesperGameUI.SetBoss(displayName, 0f);
            VesperAtmosphereAudio.SetCombat(false);
            if (type != VesperEnemyType.MiniBoss)
                VesperProgress.MarkBossDefeated(VesperProgress.CurrentRegion);
        }
        VesperGameUI.FlashMessage(boss ? displayName + (type == VesperEnemyType.MiniBoss ? " // BROKEN" : " // MEMORY CLAIMED") : "ECHO DISPERSED", boss ? 2f : 0.45f);
        VesperCamera2D.Shake(boss ? 0.35f : 0.11f, boss ? 0.55f : 0.14f);
        for (int i = 0; i < (boss ? 28 : 8); i++)
        {
            VesperEffectPool.Mote(transform.position, Random.insideUnitCircle * Random.Range(2f, 6f), baseColor,
                Random.Range(0.35f, 0.85f), Random.Range(0.05f, boss ? 0.25f : 0.12f));
        }
        Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        VesperPlayerController2D player = collision.collider.GetComponentInParent<VesperPlayerController2D>();
        if (player != null) player.Damage(1, transform.position);
    }
}

public sealed class VesperHostileBolt : MonoBehaviour
{
    private Vector2 velocity;
    private float age;
    private bool heavy;
    public void Launch(Vector2 direction, bool stronger) { velocity = direction * (stronger ? 8f : 6f); heavy = stronger; }
    private void Update()
    {
        age += Time.deltaTime;
        transform.position += (Vector3)(velocity * Time.deltaTime);
        transform.Rotate(0f, 0f, 360f * Time.deltaTime);
        if (age > 4f) Destroy(gameObject);
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        VesperPlayerController2D player = other.GetComponentInParent<VesperPlayerController2D>();
        if (player != null) { player.Damage(heavy ? 2 : 1, transform.position); Destroy(gameObject); return; }
        if (!other.isTrigger && other.GetComponentInParent<VesperEnemy2D>() == null) Destroy(gameObject);
    }
}
