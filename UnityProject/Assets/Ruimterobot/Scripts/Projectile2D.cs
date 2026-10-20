using UnityEngine;
using UnityEngine.Pool;

[RequireComponent(typeof(Rigidbody2D))]
public class Projectile2D : MonoBehaviour
{
    [SerializeField] private float speed = 12f;
    [SerializeField] private int damage = 1;
    [SerializeField] private float lifeTime = 3f;

    private Rigidbody2D body;
    private ObjectPool<GameObject> pool;
    private float remainingLife;
    private bool launched;
    private bool returningToPool;
    private SpriteRenderer spriteRenderer;
    private Vector3 originalScale;
    private Color originalColor = Color.white;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalScale = transform.localScale;
        if (spriteRenderer != null) originalColor = spriteRenderer.color;
    }

    private void OnEnable()
    {
        remainingLife = Mathf.Max(0.1f, lifeTime);
        launched = false;
        returningToPool = false;
        speed = 12f;
        damage = 1;
        transform.localScale = originalScale;
        if (spriteRenderer != null) spriteRenderer.color = originalColor;
        if (body != null)
            body.linearVelocity = Vector2.zero;
    }

    private void Update()
    {
        if (!launched || returningToPool)
            return;

        remainingLife -= Time.deltaTime;
        if (remainingLife <= 0f)
            ReturnToPool();
    }

    public void SetPool(ObjectPool<GameObject> projectilePool)
    {
        pool = projectilePool;
    }

    public void Launch(int direction)
    {
        Launch(Vector2.right * Mathf.Sign(direction), speed, damage, originalColor, 1f);
    }

    public void Launch(Vector2 direction, float velocity, int shotDamage, Color tint, float sizeMultiplier)
    {
        if (body == null)
            body = GetComponent<Rigidbody2D>();

        speed = Mathf.Max(0.1f, velocity);
        damage = Mathf.Max(1, shotDamage);
        if (spriteRenderer != null) spriteRenderer.color = tint;
        transform.localScale = originalScale * Mathf.Clamp(sizeMultiplier, 0.5f, 2.5f);
        body.linearVelocity = direction.normalized * speed;
        launched = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (returningToPool)
            return;

        EnemyHealth enemy = other.GetComponentInParent<EnemyHealth>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
        }

        BossController2D boss = other.GetComponentInParent<BossController2D>();
        if (boss != null)
        {
            boss.TakeDamage(damage);
        }

        if (enemy != null || boss != null)
            FeedbackVFX2D.Burst(transform.position, spriteRenderer != null ? spriteRenderer.color : Color.cyan, 7);

        ReturnToPool();
    }

    private void ReturnToPool()
    {
        if (returningToPool)
            return;

        returningToPool = true;
        launched = false;
        if (body != null)
            body.linearVelocity = Vector2.zero;

        if (pool != null)
            pool.Release(gameObject);
        else
            Destroy(gameObject);
    }
}

