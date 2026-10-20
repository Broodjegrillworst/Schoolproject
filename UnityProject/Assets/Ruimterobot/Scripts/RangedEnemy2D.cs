using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class RangedEnemy2D : MonoBehaviour
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float detectionRange = 8f;
    [SerializeField] private float fireInterval = 2.35f;
    [SerializeField] private float warningSeconds = 0.48f;
    [SerializeField] private float projectileSpeed = 4.6f;

    private PlayerHealth player;
    private SpriteRenderer spriteRenderer;
    private Color originalColor = Color.white;
    private float nextShotTime;
    private bool windingUp;

    public void Configure(GameObject projectile, Transform muzzle)
    {
        projectilePrefab = projectile;
        firePoint = muzzle;
    }

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null) originalColor = spriteRenderer.color;
    }

    private void Start()
    {
        player = FindAnyObjectByType<PlayerHealth>();
        fireInterval /= ProfileSaveSystem.EnemySpeedMultiplier;
        nextShotTime = Time.time + 1.2f;
    }

    private void Update()
    {
        if (Time.timeScale == 0f || player == null || player.IsGameOver) return;
        float horizontal = player.transform.position.x - transform.position.x;
        if (Mathf.Abs(horizontal) > detectionRange) return;
        if (spriteRenderer != null) spriteRenderer.flipX = horizontal < 0f;
        if (!windingUp && Time.time >= nextShotTime)
            StartCoroutine(WarningAndFire());
    }

    private IEnumerator WarningAndFire()
    {
        windingUp = true;
        nextShotTime = Time.time + fireInterval;
        if (spriteRenderer != null) spriteRenderer.color = new Color(1f, 0.38f, 0.31f);
        yield return new WaitForSeconds(warningSeconds);
        if (player != null && projectilePrefab != null)
        {
            Vector2 origin = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;
            Vector2 direction = ((Vector2)player.transform.position - origin).normalized;
            GameObject shot = ProjectilePool2D.Spawn(projectilePrefab, origin, Quaternion.identity);
            if (shot != null)
            {
                Collider2D enemyCollider = GetComponent<Collider2D>();
                Collider2D shotCollider = shot.GetComponent<Collider2D>();
                if (enemyCollider != null && shotCollider != null) Physics2D.IgnoreCollision(enemyCollider, shotCollider);
                BossProjectile2D projectile = shot.GetComponent<BossProjectile2D>();
                if (projectile != null) projectile.Launch(direction, projectileSpeed, new Color(1f, 0.39f, 0.33f), 0.78f);
            }
        }
        if (spriteRenderer != null) spriteRenderer.color = originalColor;
        windingUp = false;
    }
}
