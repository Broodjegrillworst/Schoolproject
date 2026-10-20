using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PatrolEnemy2D : MonoBehaviour
{
    public enum EnemyRole { Stalker, Charger }

    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [SerializeField] private float speed = 2f;
    [SerializeField] private float chaseRange = 4.2f;
    [SerializeField] private float chaseHeightRange = 1.8f;
    [SerializeField] private float stopDistance = 0.75f;
    [SerializeField] private float chaseSpeedMultiplier = 1.2f;
    [SerializeField] private EnemyRole role;
    [SerializeField] private float chargeCooldown = 2.6f;
    [SerializeField] private float chargeDuration = 0.55f;
    [SerializeField] private float chargeSpeedMultiplier = 2.55f;

    private Rigidbody2D body;
    private SpriteRenderer enemySprite;
    private PlayerHealth player;
    private bool movingToB = true;
    private float nextChargeTime;
    private float chargeWindupStart;
    private float chargeEndTime;
    private int chargeDirection;
    private bool charging;
    private bool chargeWindup;
    private Color originalColor = Color.white;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        enemySprite = GetComponentInChildren<SpriteRenderer>();
        if (enemySprite != null) originalColor = enemySprite.color;
    }

    private void Start()
    {
        player = FindAnyObjectByType<PlayerHealth>();
        speed *= ProfileSaveSystem.EnemySpeedMultiplier;
    }

    private void FixedUpdate()
    {
        if (body == null)
            return;

        if (player == null)
            player = FindAnyObjectByType<PlayerHealth>();

        if (player != null && !player.IsGameOver)
        {
            Vector2 playerPosition = player.transform.position;
            float horizontalDistance = playerPosition.x - body.position.x;
            float verticalDistance = Mathf.Abs(playerPosition.y - body.position.y);
            bool canChase = Mathf.Abs(horizontalDistance) <= chaseRange && verticalDistance <= chaseHeightRange;

            if (canChase)
            {
                FaceDirection(horizontalDistance);
                if (role == EnemyRole.Charger)
                {
                    if (!charging && !chargeWindup && Time.time >= nextChargeTime)
                    {
                        chargeDirection = horizontalDistance >= 0f ? 1 : -1;
                        chargeWindup = true;
                        chargeWindupStart = Time.time;
                        nextChargeTime = Time.time + chargeCooldown;
                        if (enemySprite != null) enemySprite.color = new Color(1f, 0.48f, 0.38f);
                    }
                    if (chargeWindup)
                    {
                        if (Time.time < chargeWindupStart + 0.38f) return;
                        chargeWindup = false;
                        charging = true;
                        chargeEndTime = Time.time + chargeDuration;
                    }
                    if (charging)
                    {
                        if (Time.time < chargeEndTime)
                        {
                            Vector2 chargePosition = body.position + Vector2.right * chargeDirection
                                * speed * chargeSpeedMultiplier * Time.fixedDeltaTime;
                            body.MovePosition(chargePosition);
                            return;
                        }
                        charging = false;
                        if (enemySprite != null) enemySprite.color = originalColor;
                    }
                }
                if (Mathf.Abs(horizontalDistance) > stopDistance)
                {
                    float chaseSpeed = Mathf.Max(0f, speed * chaseSpeedMultiplier);
                    Vector2 chasePosition = body.position + Vector2.right * Mathf.Sign(horizontalDistance)
                        * chaseSpeed * Time.fixedDeltaTime;
                    body.MovePosition(chasePosition);
                }
                return;
            }
            else if (chargeWindup)
            {
                chargeWindup = false;
                if (enemySprite != null) enemySprite.color = originalColor;
            }
        }

        if (pointA == null || pointB == null)
        {
            return;
        }

        Vector2 target = movingToB ? pointB.position : pointA.position;
        Vector2 nextPosition = Vector2.MoveTowards(body.position, target, speed * Time.fixedDeltaTime);
        body.MovePosition(nextPosition);
        FaceDirection(nextPosition.x - body.position.x);

        if (Vector2.Distance(body.position, target) < 0.05f)
        {
            movingToB = !movingToB;
        }
    }

    private void FaceDirection(float horizontalDirection)
    {
        if (enemySprite != null && Mathf.Abs(horizontalDirection) > 0.01f)
            enemySprite.flipX = horizontalDirection < 0f;
    }

    public void ConfigureRole(EnemyRole enemyRole) => role = enemyRole;
}

