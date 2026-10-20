using UnityEngine;

namespace Afterglow
{
    public enum BossStyle { Vandal, CinderMatriarch, Eclipse }

    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(AfterglowReboundTarget))]
    public class BossController : MonoBehaviour, IAfterglowReboundReceiver
    {
        private const int MaxHealth = 6;
        private const int PhaseCount = 3;

        private BossStyle style;
        private AfterglowPlayer player;
        private AfterglowUI ui;
        private AfterglowExit exit;
        private AfterglowReboundTarget reboundTarget;
        private Rigidbody2D body;
        private float attackTimer = 1.1f;
        private float windupTimer;
        private float exposedTimer;
        private float arenaCenter;
        private float baseY;
        private float attackCenterX;
        private float attackHalfWidth;
        private float armorFeedbackTimer;
        private int health = MaxHealth;
        private int attackPattern;
        private int activePattern;
        private int combatPhase = 1;
        private bool dead;
        private bool fightStarted;
        private bool exposed;
        private bool attackHitsGround;
        private SpriteRenderer[] renderers;

        public void Configure(BossStyle bossStyle, AfterglowPlayer hero, AfterglowUI gameUi, AfterglowExit stageExit, int stage)
        {
            style = bossStyle;
            player = hero;
            ui = gameUi;
            exit = stageExit;
            arenaCenter = transform.position.x;
            baseY = transform.position.y + .15f;
            health = MaxHealth;
            reboundTarget = GetComponent<AfterglowReboundTarget>();
            if (reboundTarget != null)
            {
                reboundTarget.Configure(ReboundTargetKind.Boss, "EXPOSED CORE", Vector2.zero, false);
                reboundTarget.SetCanBeMarked(false);
            }
            renderers = GetComponentsInChildren<SpriteRenderer>();
        }

        protected virtual void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.bodyType = RigidbodyType2D.Kinematic;
            reboundTarget = GetComponent<AfterglowReboundTarget>();
            renderers = GetComponentsInChildren<SpriteRenderer>();
        }

        protected virtual void Update()
        {
            if (dead || AfterglowUI.IsPaused || player == null || player.IsDead) return;
            if (!fightStarted)
            {
                if (Mathf.Abs(player.transform.position.x - arenaCenter) >= 14f) return;
                BeginFight();
            }

            float dt = Time.deltaTime;
            float pace = 1.45f + combatPhase * .2f;
            float sway = style == BossStyle.Eclipse ? 1.15f : style == BossStyle.CinderMatriarch ? .6f : .32f;
            transform.position = new Vector3(arenaCenter + Mathf.Sin(Time.time * .52f + (int)style) * sway,
                baseY + Mathf.Sin(Time.time * pace) * .2f, 0f);

            armorFeedbackTimer -= dt;
            if (windupTimer > 0f)
            {
                windupTimer -= dt;
                if (windupTimer <= 0f) ResolveAttack();
            }
            else if (exposed)
            {
                exposedTimer -= dt;
                if (exposedTimer <= 0f) CloseWindow();
            }
            else
            {
                attackTimer -= dt;
                if (attackTimer <= 0f) StartAttack();
            }

            if (renderers == null) return;
            Color tint = exposed
                ? Color.Lerp(new Color(1f, .88f, .55f), Color.white, Mathf.PingPong(Time.time * 5f, .3f))
                : windupTimer > 0f
                    ? Color.Lerp(Color.white, new Color(1f, .34f, .2f), .58f + Mathf.Sin(Time.time * 18f) * .12f)
                    : Color.white;
            foreach (SpriteRenderer renderer in renderers)
            {
                if (renderer == null) continue;
                renderer.flipX = player.transform.position.x < transform.position.x;
                renderer.color = Color.Lerp(renderer.color, tint, dt * 9f);
            }
        }

        private void BeginFight()
        {
            fightStarted = true;
            attackTimer = .75f;
            ui?.ShowBossHealth(BossName(), health, MaxHealth, combatPhase, PhaseCount);
            ui?.BeginBossCinematic();
            string introduction = style == BossStyle.Vandal
                ? "HALDEN LAAT ELKE BELSLAG BETALEN"
                : style == BossStyle.CinderMatriarch
                    ? "SERA VANGT JE VOETSTAPPEN IN WORTELS"
                    : "ASTER VERVORMT DE TELLING VAN DE SLAGEN";
            ui?.BossIntroduction(BossName(), introduction);
        }

        private void StartAttack()
        {
            activePattern = attackPattern++ % 3;
            attackCenterX = transform.position.x;
            attackHalfWidth = 0f;
            attackHitsGround = true;
            string tell;

            if (style == BossStyle.Vandal)
            {
                if (activePattern == 0)
                {
                    attackHalfWidth = 6.1f;
                    tell = "DE KLOKSLAG ROLT OVER DE VLOER  •  SPRING EN BLIJF LOPEN";
                }
                else if (activePattern == 1)
                {
                    attackCenterX = player.transform.position.x;
                    attackHalfWidth = 1.35f;
                    tell = "HALDEN LAAT ZIJN KLOK VALLEN  •  VERLAAT HET LICHT EN WIJK UIT";
                }
                else
                {
                    attackHitsGround = false;
                    tell = "DRIE BELSCHERVEN KOMEN OP JE AF  •  BLIJF IN BEWEGING";
                }
            }
            else if (style == BossStyle.CinderMatriarch)
            {
                if (activePattern == 0)
                {
                    attackCenterX = player.transform.position.x;
                    attackHalfWidth = 1.5f;
                    tell = "SERA SLUIT JE OP IN EEN WORTELKLEM  •  VERLAAT HET LICHT";
                }
                else if (activePattern == 1)
                {
                    attackHalfWidth = 4.2f;
                    tell = "TWEE OUDE WORTELS BREKEN HET PAD OPEN  •  SPRING";
                }
                else
                {
                    attackHitsGround = false;
                    tell = "DOORNSKIEMEN ZOEKEN JE OP  •  BLIJF LOPEN";
                }
            }
            else
            {
                if (activePattern == 0)
                {
                    attackHalfWidth = 7.2f;
                    tell = "DE SCHADUW GOLFT DOOR DE ZAAL  •  SPRING LAAT";
                }
                else if (activePattern == 1)
                {
                    attackCenterX = player.transform.position.x;
                    attackHalfWidth = 1.65f;
                    tell = "ASTER SNIJDT DE OPLICHTENDE PLEK DOOR  •  VERPLAATS JE";
                }
                else
                {
                    attackHitsGround = false;
                    tell = "DE GODMACHINE VUURT EEN ROODKLEURIGE SALVO AF  •  WIJK UIT";
                }
            }

            if (attackHitsGround) ui?.WarnFloor(attackCenterX - attackHalfWidth, attackCenterX + attackHalfWidth);
            ui?.Notify(tell, new Color(.83f, .34f, .25f), 1.2f);
            float baseWindup = style == BossStyle.Vandal ? 1.03f : style == BossStyle.CinderMatriarch ? .88f : .78f;
            windupTimer = Mathf.Max(.48f, baseWindup - (combatPhase - 1) * .15f);
            exposed = false;
            if (reboundTarget != null) reboundTarget.SetCanBeMarked(false);
        }

        private void ResolveAttack()
        {
            bool hit = attackHitsGround &&
                Mathf.Abs(player.transform.position.x - attackCenterX) < attackHalfWidth &&
                player.transform.position.y < 3.65f;
            if (hit) player.TakeDamage(style == BossStyle.Eclipse ? 2 : 1, transform.position);

            LaunchBossPattern();
            Color impactColor = style == BossStyle.CinderMatriarch ? new Color(.54f, .78f, .4f) :
                style == BossStyle.Eclipse ? new Color(.83f, .29f, .45f) : new Color(.87f, .59f, .34f);
            ui?.PlayBossAttackImpact(new Vector2(attackCenterX, .3f), impactColor);
            exposed = true;
            exposedTimer = combatPhase == PhaseCount ? 1.35f : 1.75f;
            if (reboundTarget != null) reboundTarget.SetCanBeMarked(true);
            ui?.Notify("KERN BLOOT  •  SCHIET NU", new Color(.95f, .82f, .49f), 1.35f);
        }

        private void LaunchBossPattern()
        {
            Vector2 origin = transform.position;
            Vector2 toPlayer = (Vector2)player.transform.position + Vector2.up * .8f - origin;
            float aim = Mathf.Atan2(toPlayer.y, toPlayer.x);
            Color color = style == BossStyle.CinderMatriarch ? new Color(.54f, .77f, .43f) :
                style == BossStyle.Eclipse ? new Color(.88f, .33f, .5f) : new Color(.88f, .59f, .34f);

            if (style == BossStyle.Vandal)
            {
                int count = activePattern == 2 ? (combatPhase == 3 ? 5 : 3) : combatPhase >= 2 ? 3 : 2;
                FireFan(origin, aim, count, activePattern == 2 ? .56f : .32f, 7f + combatPhase * .45f, 1, color);
            }
            else if (style == BossStyle.CinderMatriarch)
            {
                // A low pair spreads across the floor so the player can jump through the gap.
                float lowAim = Mathf.Atan2(-.12f, player.transform.position.x < origin.x ? -1f : 1f);
                FireFan(origin, lowAim, activePattern == 2 ? 2 + combatPhase : 2, .3f, 6.2f + combatPhase * .4f, 1, color);
            }
            else
            {
                // Aster's shots are fast and narrow. Read the launch point, then sidestep.
                FireFan(origin, aim, activePattern == 2 ? 3 + combatPhase : 2 + combatPhase, .42f, 8.1f + combatPhase * .55f, 1, color);
            }
        }

        private void FireFan(Vector2 origin, float angle, int count, float spread, float speed, int damage, Color color)
        {
            for (int i = 0; i < count; i++)
            {
                float fraction = count <= 1 ? .5f : i / (float)(count - 1);
                float shotAngle = angle + Mathf.Lerp(-spread, spread, fraction);
                Vector2 direction = new Vector2(Mathf.Cos(shotAngle), Mathf.Sin(shotAngle));
                AfterglowPulseProjectile.FireHostile(origin + direction * .82f, direction, speed, damage, color);
            }
        }

        public void ReceiveWeaponHit(int amount, bool charged, AfterglowPlayer attacker)
        {
            if (dead) return;
            if (!exposed)
            {
                if (armorFeedbackTimer <= 0f)
                {
                    armorFeedbackTimer = .8f;
                    ui?.PlayImpactEffect(transform.position, new Color(.65f, .69f, .72f));
                    ui?.Notify("DE WACHTER IS GESLOTEN  •  ONTSNAP AAN DE AANVAL", new Color(.77f, .73f, .65f), 1.15f);
                }
                return;
            }

            // A charged shot is a meaningful punish, while rapid fire still works.
            int damage = Mathf.Min(2, Mathf.Max(1, amount));
            health = Mathf.Max(0, health - damage);
            exposedTimer = Mathf.Max(.4f, exposedTimer - .34f);
            ui?.PlayImpactEffect(transform.position, charged ? new Color(.74f, .97f, 1f) : new Color(.82f, .91f, .9f));
            int nextPhase = Mathf.Clamp((MaxHealth - health) / 2 + 1, 1, PhaseCount);
            if (health <= 0)
            {
                Defeat();
                return;
            }

            if (nextPhase > combatPhase)
            {
                combatPhase = nextPhase;
                exposed = false;
                exposedTimer = 0f;
                attackTimer = .48f;
                if (reboundTarget != null) reboundTarget.SetCanBeMarked(false);
                ui?.BossPhaseChange(PhaseMessage());
                Color phaseColor = style == BossStyle.CinderMatriarch ? new Color(.54f, .84f, .48f) :
                    style == BossStyle.Eclipse ? new Color(.91f, .32f, .49f) : new Color(.94f, .7f, .38f);
                ui?.PlayPulseWave(transform.position, 5.8f, phaseColor);
            }
            ui?.UpdateBossHealth(BossName(), health, MaxHealth, combatPhase, PhaseCount);
        }

        private string PhaseMessage() => style == BossStyle.Vandal
            ? "HALDEN BREEKT ZIJN EIGEN BEL  •  FASE " + combatPhase
            : style == BossStyle.CinderMatriarch
                ? "DE WORTELS KEREN ZICH TEGEN SERA  •  FASE " + combatPhase
                : "ASTER VERLIEST DE MAAT  •  FASE " + combatPhase;

        private void CloseWindow()
        {
            exposed = false;
            if (reboundTarget != null) reboundTarget.SetCanBeMarked(false);
            attackTimer = combatPhase == PhaseCount ? .36f : .62f;
        }

        public void OnRebound(AfterglowPlayer reboundPlayer, Vector2 incoming, Vector2 outgoing, int chain, bool perfect)
        {
            if (dead || !perfect) return;
            if (windupTimer > 0f)
            {
                windupTimer = 0f;
                attackHitsGround = false;
                exposed = true;
                exposedTimer = 1.9f;
                if (reboundTarget != null) reboundTarget.SetCanBeMarked(true);
                Color counterColor = style == BossStyle.CinderMatriarch ? new Color(.54f, .84f, .48f) :
                    style == BossStyle.Eclipse ? new Color(.91f, .32f, .49f) : new Color(.94f, .7f, .38f);
                ui?.PlayPulseWave(transform.position, 4.8f, counterColor);
                ui?.BossPhaseChange("DE BELSLAG KEERT OM  •  KERN OPEN");
                return;
            }
            if (exposed) ReceiveWeaponHit(1, false, reboundPlayer);
        }

        private void Defeat()
        {
            if (dead) return;
            dead = true;
            ui?.PlayEnemyBurst(transform.position);
            ui?.HideBossHealth();
            ui?.BossDefeated(BossName());
            if (exit != null) exit.Unlock();
            Destroy(gameObject);
        }

        private string BossName() => style == BossStyle.Vandal ? "HALDEN • DE TEMPELWACHTER" :
            style == BossStyle.CinderMatriarch ? "SERA VOSS • DE WORTELMOEDER" : "ASTER NULL • DE GODMACHINE";
    }
}
