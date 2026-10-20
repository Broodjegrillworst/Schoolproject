using UnityEngine;

namespace Afterglow
{
    public enum EnemyKind { Stalker, RiftHound, HexCaster, Breaker }

    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(AfterglowReboundTarget))]
    public sealed class AfterglowEnemy : MonoBehaviour, IAfterglowReboundReceiver
    {
        private EnemyKind kind;
        private AfterglowPlayer player;
        private AfterglowUI ui;
        private AfterglowReboundTarget reboundTarget;
        private Rigidbody2D body;
        private SpriteRenderer[] renderers;
        private EnemyAI brain;
        private float attackTimer;
        private float hurtTimer;
        private float recoverTimer;
        private float tollCenterX;
        private bool dead;
        private bool launched;
        private int health = 2;

        public bool IsDead => dead;
        public bool IsLaunched => launched;
        public EnemyKind Kind => kind;
        public Rigidbody2D Body => body;

        public void Configure(EnemyKind enemyKind, AfterglowPlayer hero, AfterglowUI gameUi)
        {
            kind = enemyKind;
            player = hero;
            ui = gameUi;
            health = enemyKind == EnemyKind.Breaker ? 4 : enemyKind == EnemyKind.HexCaster ? 3 : 2;
            reboundTarget = GetComponent<AfterglowReboundTarget>();
            if (reboundTarget != null)
                reboundTarget.Configure(ReboundTargetKind.Enemy, TargetLabel(), Vector2.zero);
            renderers = GetComponentsInChildren<SpriteRenderer>();
            brain = GetComponent<EnemyAI>() ?? gameObject.AddComponent<EnemyAI>();
            brain.Configure(this, hero);
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.freezeRotation = true;
                body.gravityScale = 3f;
                body.linearDamping = .85f;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }
            reboundTarget = GetComponent<AfterglowReboundTarget>();
            renderers = GetComponentsInChildren<SpriteRenderer>();
        }

        private void Update()
        {
            if (dead || AfterglowUI.IsPaused) return;
            float dt = Time.deltaTime;
            if (launched)
            {
                recoverTimer -= dt;
                if (recoverTimer > 0f) return;
                launched = false;
                reboundTarget?.SetCanBeMarked(true);
            }
            attackTimer -= dt;
            hurtTimer -= dt;
            if (renderers != null)
            {
                foreach (var renderer in renderers)
                {
                    renderer.color = hurtTimer > 0f ? Color.Lerp(Color.white, new Color(1f, .42f, .52f), .55f) : Color.white;
                }
            }
        }

        public void SetFacing(bool left)
        {
            if (renderers == null) return;
            foreach (var renderer in renderers) if (renderer != null) renderer.flipX = left;
        }

        public void TryAttack(float distance, float towardPlayer)
        {
            if (dead || attackTimer > 0f || player == null || player.IsDead) return;
            if (kind == EnemyKind.RiftHound && distance > 1.5f)
            {
                attackTimer = 1.55f;
                body.linearVelocity = new Vector2(towardPlayer * 8.2f, 3.4f);
                ui?.PlayEnemyTelegraph(transform.position);
            }
            else if (kind == EnemyKind.Stalker && distance > 1.1f)
            {
                attackTimer = 2.2f;
                tollCenterX = player.transform.position.x;
                // Keep the Warden markable while he rings. A rebound during the tell
                // cancels ResolveBellSweep in OnRebound, giving the player a real counter.
                reboundTarget?.SetCanBeMarked(true);
                ui?.WarnFloor(tollCenterX - 1.35f, tollCenterX + 1.35f);
                ui?.Notify("BELLENSWEEP  •  ONTSNAP OF KAATS DE BEL TERUG", new Color(.72f, .28f, .22f), 1f);
                Invoke(nameof(ResolveBellSweep), .46f);
            }
            else if (kind == EnemyKind.HexCaster && distance < 4.5f)
            {
                attackTimer = 2.1f;
                ui?.WarnFloor(transform.position.x - 3f, transform.position.x + 3f);
                Invoke(nameof(GroundPulse), .42f);
            }
            else if (kind == EnemyKind.Breaker && distance < 3.6f)
            {
                attackTimer = 2f;
                body.linearVelocity = new Vector2(towardPlayer * 5.4f, 2.2f);
                ui?.PlayEnemyTelegraph(transform.position);
            }
        }

        private string TargetLabel() => kind == EnemyKind.Breaker ? "STONEBOUND GUARDIAN" : kind == EnemyKind.RiftHound ? "ROOT HOUND" : kind == EnemyKind.HexCaster ? "MOSS SEER" : "TEMPLE WARDEN";

        private void GroundPulse()
        {
            if (dead || player == null || player.IsDead) return;
            if (player.IsGrounded && Mathf.Abs(player.transform.position.x - transform.position.x) < 3.6f)
                player.TakeDamage(1, transform.position);
        }

        private void ResolveBellSweep()
        {
            if (dead || launched || player == null || player.IsDead) return;
            if (Mathf.Abs(player.transform.position.x - tollCenterX) < 1.35f && player.transform.position.y < 3.2f)
                player.TakeDamage(1, transform.position);
            reboundTarget?.SetCanBeMarked(true);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (dead || launched || AfterglowUI.IsPaused || (player != null && !player.HasStartedGameplay)) return;
            var hero = collision.collider.GetComponentInParent<AfterglowPlayer>();
            if (hero != null && attackTimer <= 0f)
            {
                attackTimer = 1.05f;
                hero.ContactDamage(kind == EnemyKind.Breaker ? 2 : 1, transform.position);
            }
        }

        public void OnRebound(AfterglowPlayer reboundPlayer, Vector2 incoming, Vector2 outgoing, int chain, bool perfect)
        {
            if (dead || body == null) return;
            brain?.ReactToPlayerAttack(reboundPlayer);
            health -= perfect ? 2 : 1;
            if (health <= 0) { DefeatByWeapon(); return; }
            CancelInvoke(nameof(ResolveBellSweep));
            float launchForce = kind == EnemyKind.Breaker ? 10f : kind == EnemyKind.RiftHound ? 16f : 13f;
            body.linearVelocity = incoming * launchForce + Vector2.up * (perfect ? 4.5f : 2.5f);
            launched = true;
            recoverTimer = 1.15f;
            hurtTimer = .28f;
            if (reboundTarget != null) reboundTarget.SetCanBeMarked(false);
            CancelInvoke(nameof(GroundPulse));
            string response = kind == EnemyKind.Stalker ? "DE BELSTROOK IS ONDERBROKEN" :
                kind == EnemyKind.RiftHound ? "DE HOND IS TERUGGEKAATST" :
                kind == EnemyKind.HexCaster ? "DE VERVLOEKING IS VERSTOORD" :
                "DE IJZEREN STORM RAMT DOOR";
            ui?.Notify(perfect ? response + "  •  PERFECTE SLAG" : response + "  •  KETEN " + chain,
                new Color(.77f, .65f, .41f), 1.2f);
        }

        public void TakeDamage(int unusedAmount, Vector2 from)
        {
            if (dead || body == null) return;
            Vector2 launch = ((Vector2)transform.position - from).normalized;
            OnRebound(player, -launch, launch, 1, false);
        }

        public void ReceiveWeaponHit(int amount, Vector2 direction, bool charged, bool marked)
        {
            if (dead || body == null) return;
            brain?.ReactToPlayerAttack(player);
            health -= Mathf.Max(1, amount) + (marked ? 1 : 0);
            hurtTimer = .3f;
            launched = true;
            recoverTimer = charged ? .48f : .28f;
            reboundTarget?.SetCanBeMarked(true);
            body.linearVelocity = direction.normalized * (charged ? 7.2f : 4.5f) + Vector2.up * (charged ? 2.4f : .8f);
            CancelInvoke(nameof(ResolveBellSweep));
            CancelInvoke(nameof(GroundPulse));
            if (health <= 0) DefeatByWeapon();
        }

        public void DefeatByHazard()
        {
            DefeatByWeapon();
        }

        private void DefeatByWeapon()
        {
            if (dead) return;
            dead = true;
            CancelInvoke();
            reboundTarget?.SetCanBeMarked(false);
            ui?.PlayEnemyBurst(transform.position);
            Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!launched || dead) return;
            var hazard = other.GetComponentInParent<AfterglowHazard>();
            if (hazard != null && hazard.IsActive) DefeatByHazard();
        }
    }
}
