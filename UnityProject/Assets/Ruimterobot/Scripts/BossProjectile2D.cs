using UnityEngine;
using UnityEngine.Pool;

[RequireComponent(typeof(Rigidbody2D))]
public class BossProjectile2D : MonoBehaviour
{
    [SerializeField] private float lifeTime = 5f;
    private Rigidbody2D body;
    private ObjectPool<GameObject> pool;
    private float remainingLife;
    private bool launched;
    private bool returningToPool;
    private SpriteRenderer spriteRenderer;
    private Color originalColor = Color.white;
    private Vector3 originalScale;

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

    public void Launch(Vector2 direction, float speed)
    {
        Launch(direction, speed, originalColor, 1f);
    }

    public void Launch(Vector2 direction, float speed, Color tint, float sizeMultiplier)
    {
        if (body != null)
            body.linearVelocity = direction.normalized * speed;
        if (spriteRenderer != null) spriteRenderer.color = tint;
        transform.localScale = originalScale * Mathf.Clamp(sizeMultiplier, 0.5f, 2f);
        launched = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (returningToPool)
            return;

        PlayerHealth player = other.GetComponentInParent<PlayerHealth>();
        if (player != null)
            player.TakeDamage();
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
