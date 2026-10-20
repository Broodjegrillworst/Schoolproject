using UnityEngine;

// Zet dit script op een gevaar of vijand met een Collider2D.
public class DamagePlayerOnTouch : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamagePlayer(other.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryDamagePlayer(collision.gameObject);
    }

    private void TryDamagePlayer(GameObject other)
    {
        PlayerHealth player = other.GetComponentInParent<PlayerHealth>();
        if (player != null)
        {
            PlayerController2D controller = player.GetComponent<PlayerController2D>();
            if (controller != null && controller.IsDashing)
                return;
            EnemyHealth enemy = GetComponentInParent<EnemyHealth>();
            BossController2D boss = GetComponentInParent<BossController2D>();
            bool stompable = (enemy != null && !enemy.IsDefeated) || (boss != null && !boss.IsDefeated);
            bool landedFromAbove = controller != null && controller.IsFalling &&
                                   player.transform.position.y > transform.position.y + 0.2f;
            if (stompable && landedFromAbove)
            {
                if (enemy != null) enemy.TakeDamage(1);
                if (boss != null) boss.TakeDamage(1);
                controller.BounceAfterStomp();
                HeroPowerController2D powers = player.GetComponent<HeroPowerController2D>();
                if (powers != null) powers.AddEnergy(6f);
                FeedbackVFX2D.Burst(player.transform.position + Vector3.down * 0.45f,
                    new Color(1f, 0.8f, 0.34f), 9);
                return;
            }

            player.TakeDamage();
        }
    }
}

