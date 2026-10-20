using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int hitPoints = 1;
    [SerializeField] private float defeatAnimationSeconds = 0.18f;
    private bool defeated;
    private SpriteRenderer[] renderers;
    private Color[] originalColors;
    private Vector3 originalScale;

    public bool IsDefeated => defeated;

    private void Awake()
    {
        hitPoints = Mathf.Max(1, Mathf.RoundToInt(hitPoints * ProfileSaveSystem.EnemyHealthMultiplier));
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            originalColors[i] = renderers[i] != null ? renderers[i].color : Color.white;
        originalScale = transform.localScale;
    }

    public void TakeDamage(int amount)
    {
        if (defeated || amount <= 0)
            return;

        hitPoints -= amount;

        if (hitPoints <= 0)
        {
            defeated = true;
            GameSession.RecordEnemyDefeated();
            if (ArcadeAudioManager.Instance != null)
                ArcadeAudioManager.Instance.PlayEnemyDefeat();
            FeedbackVFX2D.Burst(transform.position, new Color(0.4f, 0.83f, 1f), 14);
            DisableCombat();
            StartCoroutine(PlayDefeatAnimation());
        }
    }

    private void DisableCombat()
    {
        foreach (Collider2D enemyCollider in GetComponentsInChildren<Collider2D>())
            enemyCollider.enabled = false;

        Rigidbody2D body = GetComponent<Rigidbody2D>();
        if (body != null)
            body.simulated = false;

        PatrolEnemy2D patrol = GetComponent<PatrolEnemy2D>();
        if (patrol != null)
            patrol.enabled = false;

        RangedEnemy2D ranged = GetComponent<RangedEnemy2D>();
        if (ranged != null) ranged.enabled = false;

        SwoopEnemy2D swooper = GetComponent<SwoopEnemy2D>();
        if (swooper != null) swooper.enabled = false;

        DamagePlayerOnTouch contactDamage = GetComponent<DamagePlayerOnTouch>();
        if (contactDamage != null)
            contactDamage.enabled = false;
    }

    private IEnumerator PlayDefeatAnimation()
    {
        float duration = Mathf.Max(0.06f, defeatAnimationSeconds);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress = progress * progress;
            transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, easedProgress);

            if (renderers != null)
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    SpriteRenderer sprite = renderers[i];
                    if (sprite != null)
                    {
                        sprite.color = Color.Lerp(originalColors[i], new Color(0.65f, 0.95f, 1f, 0f), progress);
                    }
                }
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}

