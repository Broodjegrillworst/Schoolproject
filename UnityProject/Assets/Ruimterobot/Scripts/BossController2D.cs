using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
public class BossController2D : MonoBehaviour
{
    private enum AttackStyle { Titan, Pyra, Null }

    [SerializeField] private string bossName = "Superschurk";
    [SerializeField] private int maximumHealth = 4;
    [SerializeField] private float patrolMinX = 4f;
    [SerializeField] private float patrolMaxX = 6.1f;
    [SerializeField] private float moveSpeed = 0.7f;
    [SerializeField] private float attackInterval = 2.2f;
    [SerializeField] private int shotsPerVolley = 1;
    [SerializeField] private float spreadDegrees = 18f;
    [SerializeField] private float projectileSpeed = 5f;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private LevelExit2D levelExit;
    [SerializeField] private Image healthFill;
    [SerializeField] private Text healthLabel;
    [SerializeField] private Text objectiveLabel;
    [SerializeField] private AttackStyle attackStyle;
    [SerializeField] private float activationDistance = 10.5f;

    private Rigidbody2D body;
    private SpriteRenderer bossSprite;
    private PlayerHealth player;
    private int currentHealth;
    private int moveDirection = -1;
    private float nextAttackTime;
    private bool defeated;
    private bool secondPhase;
    private bool charging;
    private int chargeDirection;
    private float chargeEndTime;
    private Color originalBossColor = Color.white;
    private int nullPatternIndex;
    private bool encountered;
    private Coroutine impactRoutine;

    public int CurrentHealth => currentHealth;
    public int MaximumHealth => maximumHealth;
    public bool IsDefeated => defeated;
    public string BossName => bossName;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        bossSprite = GetComponentInChildren<SpriteRenderer>();
        if (bossSprite != null) originalBossColor = bossSprite.color;
        maximumHealth = Mathf.Max(1, Mathf.RoundToInt(maximumHealth * ProfileSaveSystem.BossHealthMultiplier));
        currentHealth = maximumHealth;
        if (body != null)
        {
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.freezeRotation = true;
        }
    }

    private void Start()
    {
        if (levelExit != null)
            levelExit.SetLocked(true);
        player = FindAnyObjectByType<PlayerHealth>();
        moveSpeed *= ProfileSaveSystem.EnemySpeedMultiplier;
        nextAttackTime = Time.time + Mathf.Max(0.8f, attackInterval);
        SetBossHudActive(false);
        RefreshHealthBar();
    }

    private void Update()
    {
        if (defeated || Time.timeScale == 0f)
            return;
        if (player == null)
            player = FindAnyObjectByType<PlayerHealth>();

        if (player != null && bossSprite != null)
        {
            bool facingRight = player.transform.position.x > transform.position.x;
            bossSprite.flipX = facingRight;
            if (firePoint != null)
            {
                Vector3 point = firePoint.localPosition;
                point.x = Mathf.Abs(point.x) * (facingRight ? 1f : -1f);
                firePoint.localPosition = point;
            }
        }

        if (player == null || Mathf.Abs(player.transform.position.x - transform.position.x) > activationDistance)
            return;
        if (!encountered)
        {
            encountered = true;
            nextAttackTime = Time.time + 1.2f;
            SetBossHudActive(true);
            CameraFollow2D cameraFollow = Camera.main != null ? Camera.main.GetComponent<CameraFollow2D>() : null;
            if (cameraFollow != null) cameraFollow.PlayBossIntro(transform);
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.SetCombatIntensity(true);
            if (objectiveLabel != null) objectiveLabel.text = bossName + "  //  BEREID JE VOOR";
        }

        if (Time.time >= nextAttackTime)
        {
            if (attackStyle == AttackStyle.Titan)
                StartCoroutine(TitanCharge());
            else
                FireVolley();
            nextAttackTime = Time.time + attackInterval * (secondPhase ? 0.68f : 1f);
        }
    }

    private void FixedUpdate()
    {
        if (defeated || Time.timeScale == 0f || body == null)
            return;
        if (charging)
        {
            if (Time.time < chargeEndTime)
            {
                float chargeSpeed = moveSpeed * (secondPhase ? 7f : 5.7f);
                float x = Mathf.Clamp(body.position.x + chargeDirection * chargeSpeed * Time.fixedDeltaTime,
                    patrolMinX, patrolMaxX);
                body.MovePosition(new Vector2(x, body.position.y));
                return;
            }
            charging = false;
            if (bossSprite != null) bossSprite.color = originalBossColor;
        }
        float phaseSpeed = moveSpeed * (secondPhase ? 1.18f : 1f);
        Vector2 nextPosition = body.position + Vector2.right * moveDirection * phaseSpeed * Time.fixedDeltaTime;
        if (nextPosition.x <= patrolMinX)
        {
            nextPosition.x = patrolMinX;
            moveDirection = 1;
        }
        else if (nextPosition.x >= patrolMaxX)
        {
            nextPosition.x = patrolMaxX;
            moveDirection = -1;
        }
        body.MovePosition(nextPosition);
    }

    public void TakeDamage(int amount)
    {
        if (defeated || amount <= 0)
            return;
        if (ArcadeAudioManager.Instance != null)
            ArcadeAudioManager.Instance.PlayBossHit();
        FeedbackVFX2D.Shake(0.045f, 0.055f);
        FeedbackVFX2D.Burst(transform.position, new Color(1f, 0.52f, 0.28f), 5);
        if (impactRoutine != null) StopCoroutine(impactRoutine);
        impactRoutine = StartCoroutine(FlashOnImpact());
        currentHealth = Mathf.Max(0, currentHealth - amount);
        if (!secondPhase && currentHealth > 0 && currentHealth <= Mathf.CeilToInt(maximumHealth * 0.5f))
        {
            secondPhase = true;
            nextAttackTime = Mathf.Min(nextAttackTime, Time.time + 0.45f);
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayNova();
            FeedbackVFX2D.Shake(0.2f, 0.32f);
            FeedbackVFX2D.Burst(transform.position, new Color(1f, 0.38f, 0.26f), 28);
            if (objectiveLabel != null)
            {
                objectiveLabel.text = bossName + " // FASE 2: AANVAL GEËSCALEERD";
                objectiveLabel.color = new Color(1f, 0.63f, 0.37f);
            }
        }
        RefreshHealthBar();
        if (currentHealth == 0)
            DefeatBoss();
    }

    private IEnumerator FlashOnImpact()
    {
        if (bossSprite == null) yield break;
        Color original = bossSprite.color;
        Color flash = new Color(1f, 0.88f, 0.66f, original.a);
        for (int i = 0; i < 3; i++)
        {
            bossSprite.color = flash;
            yield return new WaitForSecondsRealtime(0.045f);
            bossSprite.color = original;
            yield return new WaitForSecondsRealtime(0.045f);
        }
        impactRoutine = null;
    }
    private void FireVolley()
    {
        if (projectilePrefab == null || player == null)
            return;
        Vector2 origin = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;
        Vector2 aim = ((Vector2)player.transform.position - origin).normalized;
        int count;
        float spread;
        float speedScale = secondPhase ? 1.15f : 1f;
        Color projectileColor;
        switch (attackStyle)
        {
            case AttackStyle.Pyra:
                count = secondPhase ? shotsPerVolley + 5 : shotsPerVolley + 3;
                spread = secondPhase ? 78f : 58f;
                projectileColor = new Color(1f, 0.35f, 0.22f);
                break;
            case AttackStyle.Null:
                nullPatternIndex++;
                count = secondPhase ? (nullPatternIndex % 2 == 0 ? shotsPerVolley + 2 : shotsPerVolley) : shotsPerVolley;
                spread = nullPatternIndex % 2 == 0 ? 92f : 40f;
                speedScale = secondPhase ? 1.28f : 1.08f;
                projectileColor = new Color(0.76f, 0.48f, 1f);
                break;
            default:
                count = secondPhase ? Mathf.Max(2, shotsPerVolley + 1) : Mathf.Max(1, shotsPerVolley);
                spread = 18f;
                projectileColor = new Color(1f, 0.65f, 0.28f);
                speedScale = secondPhase ? 1.25f : 1.05f;
                break;
        }
        for (int i = 0; i < count; i++)
        {
            float activeSpread = attackStyle == AttackStyle.Pyra || attackStyle == AttackStyle.Null ? spread : spreadDegrees;
            float offset = count == 1 ? 0f : -activeSpread * 0.5f + activeSpread * i / (count - 1f);
            float radians = offset * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(
                aim.x * Mathf.Cos(radians) - aim.y * Mathf.Sin(radians),
                aim.x * Mathf.Sin(radians) + aim.y * Mathf.Cos(radians));
            GameObject shot = ProjectilePool2D.Spawn(projectilePrefab, origin, Quaternion.identity);
            if (shot == null)
                continue;

            Collider2D bossCollider = GetComponent<Collider2D>();
            Collider2D shotCollider = shot.GetComponent<Collider2D>();
            if (bossCollider != null && shotCollider != null)
                Physics2D.IgnoreCollision(bossCollider, shotCollider);

            BossProjectile2D projectile = shot.GetComponent<BossProjectile2D>();
            if (projectile != null)
                projectile.Launch(direction, projectileSpeed * speedScale, projectileColor,
                    attackStyle == AttackStyle.Titan ? 1.15f : 0.8f);
            else
                Destroy(shot);
        }
    }

    private IEnumerator TitanCharge()
    {
        if (player == null) yield break;
        chargeDirection = player.transform.position.x >= transform.position.x ? 1 : -1;
        if (bossSprite != null) bossSprite.color = new Color(1f, 0.32f, 0.22f);
        if (objectiveLabel != null)
        {
            objectiveLabel.text = "TITAN LAADT OP — DASH OM DE AANVAL TE ONTWIJKEN";
            objectiveLabel.color = new Color(1f, 0.55f, 0.36f);
        }
        yield return new WaitForSeconds(0.48f);
        if (defeated) yield break;
        charging = true;
        chargeEndTime = Time.time + (secondPhase ? 0.72f : 0.48f);
        FeedbackVFX2D.Shake(0.12f, 0.22f);
        yield return new WaitForSeconds(secondPhase ? 0.74f : 0.5f);
        charging = false;
        if (bossSprite != null) bossSprite.color = originalBossColor;
        if (objectiveLabel != null)
        {
            objectiveLabel.text = "Versla de superschurk en open de uitgang.";
            objectiveLabel.color = new Color(1f, 0.83f, 0.45f);
        }
    }

    private void RefreshHealthBar()
    {
        if (healthFill != null)
            healthFill.fillAmount = maximumHealth <= 0 ? 0f : (float)currentHealth / maximumHealth;
        if (healthLabel != null)
            healthLabel.text = bossName + (secondPhase ? "  // FASE 2" : "")
                + "  " + currentHealth + " / " + maximumHealth;
    }

    private void DefeatBoss()
    {
        defeated = true;
        if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.SetCombatIntensity(false);
        GameSession.RecordBossDefeated();
        FeedbackVFX2D.Shake(0.22f, 0.38f);
        FeedbackVFX2D.Burst(transform.position, new Color(1f, 0.8f, 0.4f), 36);
        if (ArcadeAudioManager.Instance != null)
            ArcadeAudioManager.Instance.PlayPickup();
        if (levelExit != null)
            levelExit.SetLocked(false);
        if (objectiveLabel != null)
        {
            objectiveLabel.text = "Superschurk verslagen! De uitgang is open.";
            objectiveLabel.color = new Color(0.55f, 1f, 0.7f);
        }
        foreach (Collider2D bossCollider in GetComponentsInChildren<Collider2D>())
            bossCollider.enabled = false;
        if (body != null)
            body.simulated = false;
        Destroy(gameObject, 0.18f);
    }

    private void SetBossHudActive(bool active)
    {
        if (healthLabel != null) healthLabel.gameObject.SetActive(active);
        if (healthFill != null && healthFill.transform.parent != null)
            healthFill.transform.parent.gameObject.SetActive(active);
        if (objectiveLabel != null) objectiveLabel.gameObject.SetActive(active);
    }
}


