using UnityEngine;

namespace Afterglow
{
    public enum BossStyle { Vandal, CinderMatriarch, Eclipse }

    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(AfterglowReboundTarget))]
    public class BossController : MonoBehaviour, IAfterglowReboundReceiver
    {
        private const int MasteryHitsRequired = 3;
        private BossStyle style;
        private AfterglowPlayer player;
        private AfterglowUI ui;
        private AfterglowExit exit;
        private AfterglowReboundTarget reboundTarget;
        private Rigidbody2D body;
        private float attackTimer = 1.25f;
        private float windupTimer;
        private float exposedTimer;
        private float arenaCenter;
        private float attackCenterX;
        private int requiredChain;
        private int masteryHits;
        private int attackPattern;
        private int combatPhase = 1;
        private bool dead;
        private bool fightStarted;
        private bool exposed;
        private bool haldenBellDrop;
        private bool seraRootSnare;
        private bool eclipseStrike;
        private SpriteRenderer[] renderers;

        public void Configure(BossStyle bossStyle, AfterglowPlayer hero, AfterglowUI gameUi, AfterglowExit stageExit, int stage)
        {
            style = bossStyle;
            player = hero;
            ui = gameUi;
            exit = stageExit;
            arenaCenter = transform.position.x;
            requiredChain = Mathf.Clamp(stage + 1, 1, 3);
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
            float bob = Mathf.Sin(Time.time * (style == BossStyle.Eclipse ? 2.4f : 1.5f)) * .18f;
            transform.position = new Vector3(arenaCenter + Mathf.Sin(Time.time * .55f) * .35f, 2.45f + bob, 0f);

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

            if (renderers != null)
            {
                Color tint = exposed ? new Color(.98f, .76f, .45f) : windupTimer > 0f ? new Color(.72f, .27f, .22f) : Color.white;
                foreach (var renderer in renderers)
                {
                    renderer.flipX = player.transform.position.x < transform.position.x;
                    renderer.color = Color.Lerp(renderer.color, tint, dt * 7f);
                }
            }
        }

        private void BeginFight()
        {
            fightStarted = true;
            attackTimer = .8f;
            ui?.ShowBossMastery(BossName(), 0, MasteryHitsRequired, requiredChain);
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
            attackCenterX = transform.position.x;
            if (style == BossStyle.Vandal)
            {
                // Halden's tells alternate between a tolling floor wave and a bell drop
                // aimed at Mara. During the tell, one bell dash can counter his attack.
                haldenBellDrop = attackPattern++ % 2 == 1;
                if (haldenBellDrop)
                {
                    attackCenterX = player.transform.position.x;
                    ui?.WarnFloor(attackCenterX - 1.4f, attackCenterX + 1.4f);
                    ui?.Notify("HALDENS KLOKSLAG  •  SPRING, OF KAATS HET PERFECT TERUG", new Color(.69f, .27f, .2f), 1.2f);
                }
                else
                {
                    ui?.WarnFloor(attackCenterX - 6f, attackCenterX + 6f);
                    ui?.Notify("DE LAATSTE TOL  •  SPRING OVER DE GOLF OF KEER DE BELSLAG", new Color(.69f, .27f, .2f), 1.2f);
                }
            }
            else if (style == BossStyle.CinderMatriarch)
            {
                // Sera alternates a precise root snare with a broad root surge.
                // The first punishes standing still; the second rewards a clean jump.
                seraRootSnare = attackPattern++ % 2 == 0;
                if (seraRootSnare) attackCenterX = player.transform.position.x;
                float halfWidth = seraRootSnare ? 1.45f : 4.5f;
                ui?.WarnFloor(attackCenterX - halfWidth, attackCenterX + halfWidth);
                ui?.Notify(seraRootSnare ? "WORTELKLEM  •  VERLAAT DE GEMERKTE PLEK" : "WORTELGOLF  •  SPRING OVER DE BREDE AANVAL", new Color(.69f, .27f, .2f), 1.1f);
            }
            else if (style == BossStyle.Eclipse)
            {
                eclipseStrike = attackPattern++ % 2 == 1;
                if (eclipseStrike)
                {
                    ui?.WarnFloor(attackCenterX - 1.55f, attackCenterX + 1.55f);
                    ui?.Notify("STILTESLAG  •  REBOUND OPZIJ OF OMHOOG", new Color(.69f, .27f, .2f), 1f);
                }
                else
                {
                    ui?.WarnFloor(attackCenterX - 7.5f, attackCenterX + 7.5f);
                    ui?.Notify("SCHADUWGOLF  •  SPRING OF REBOUND HOOG", new Color(.69f, .27f, .2f), 1f);
                }
            }
            else
            {
                float reach = style == BossStyle.Eclipse ? 7.5f : 6f;
                ui?.WarnFloor(attackCenterX - reach, attackCenterX + reach);
                ui?.Notify("GOLVENDE INSLAAG  •  SPRING OF REBOUND ER OVERHEEN", new Color(.69f, .27f, .2f), .9f);
            }
            float phaseSpeedUp = (combatPhase - 1) * .12f;
            windupTimer = Mathf.Max(.32f, (style == BossStyle.Vandal ? 1.05f : style == BossStyle.CinderMatriarch ? .72f : .48f) - phaseSpeedUp);
            if (reboundTarget != null) reboundTarget.SetCanBeMarked(style == BossStyle.Vandal);
            exposed = false;
        }

        private void ResolveAttack()
        {
            float dx = Mathf.Abs(player.transform.position.x - attackCenterX);
            bool hit = style == BossStyle.CinderMatriarch
                ? dx < (seraRootSnare ? 1.55f : 4.5f) && player.transform.position.y < 3.5f
                : style == BossStyle.Vandal && haldenBellDrop
                    ? dx < 1.4f && player.transform.position.y < 4.6f
                : style == BossStyle.Eclipse && eclipseStrike
                    ? dx < 1.55f && player.transform.position.y < 5.2f
                    : dx < (style == BossStyle.Eclipse ? 7.5f : 6f) && player.IsGrounded;
            if (hit) player.TakeDamage(style == BossStyle.Eclipse ? 2 : 1, transform.position);
            LaunchBossPattern();
            ui?.PlayBossAttackImpact(transform.position, style == BossStyle.CinderMatriarch ? new Color(.54f,.78f,.4f) : style == BossStyle.Eclipse ? new Color(.83f,.29f,.45f) : new Color(.87f,.59f,.34f));
            exposed = true;
            exposedTimer = style == BossStyle.Eclipse ? .95f : style == BossStyle.Vandal ? 1.45f : 1.25f;
            if (reboundTarget != null) reboundTarget.SetCanBeMarked(true);
            ui?.Notify(style == BossStyle.Vandal
                ? "HALDEN IS OPEN  •  DASH OP ZIJN ZEGEL"
                : style == BossStyle.CinderMatriarch
                    ? "DE WORTELS LATEN LOS  •  DASH OP HET ZEGEL"
                    : "DE STILTE BREEKT  •  DASH OP HET ZEGEL",
                new Color(.82f, .68f, .42f), 1.35f);
        }

        private void LaunchBossPattern()
        {
            Vector2 origin = transform.position;
            Vector2 toPlayer = (Vector2)player.transform.position + Vector2.up * .8f - origin;
            float angle = Mathf.Atan2(toPlayer.y, toPlayer.x);
            int count;
            float spread;
            float speed;
            Color color;
            if (style == BossStyle.Vandal)
            {
                count = haldenBellDrop ? 2 + masteryHits + combatPhase : 3 + masteryHits + combatPhase;
                spread = haldenBellDrop ? .3f : .58f;
                speed = 7.2f + masteryHits * .7f + (combatPhase - 1) * .55f;
                color = new Color(.88f,.59f,.34f);
            }
            else if (style == BossStyle.CinderMatriarch)
            {
                count = 3 + masteryHits + combatPhase;
                spread = .64f;
                speed = 6.7f + masteryHits * .55f + (combatPhase - 1) * .55f;
                color = new Color(.54f,.77f,.43f);
            }
            else
            {
                count = 5 + masteryHits * 2 + combatPhase;
                spread = .82f;
                speed = 8.1f + masteryHits * .5f + (combatPhase - 1) * .55f;
                color = new Color(.88f,.33f,.5f);
            }
            for (int i = 0; i < count; i++)
            {
                float fraction = count <= 1 ? .5f : i / (float)(count - 1);
                float shotAngle = angle + Mathf.Lerp(-spread, spread, fraction);
                Vector2 direction = new Vector2(Mathf.Cos(shotAngle), Mathf.Sin(shotAngle));
                AfterglowPulseProjectile.FireHostile(origin + direction * .82f, direction, speed, style == BossStyle.Eclipse ? 2 : 1, color);
            }
        }

        public void ReceiveWeaponHit(int amount, bool charged, AfterglowPlayer attacker)
        {
            if (dead) return;
            if (windupTimer > 0f)
            {
                windupTimer = Mathf.Min(1.5f, windupTimer + (charged ? .3f : .1f));
                ui?.PlayImpactEffect(transform.position, charged ? new Color(.58f,.9f,1f) : new Color(.54f,.72f,.82f));
            }
            if (exposed)
            {
                exposedTimer = Mathf.Min(style == BossStyle.Eclipse ? 1.5f : 2f, exposedTimer + (charged ? .2f : .08f));
                ui?.PlayImpactEffect(transform.position, charged ? new Color(.72f,.94f,1f) : new Color(.66f,.8f,.85f));
            }
            if (charged && !exposed && windupTimer <= 0f)
                ui?.Notify("DE ZEGELHOUDER SPANT ZICH IN  •  MARK EN REBOUND OM HET ZEGEL TE BREKEN", new Color(.8f,.68f,.48f), 1.2f);
        }

        private void CloseWindow()
        {
            exposed = false;
            if (reboundTarget != null) reboundTarget.SetCanBeMarked(false);
            attackTimer = style == BossStyle.Eclipse ? .75f : 1.05f;
        }

        public void OnRebound(AfterglowPlayer reboundPlayer, Vector2 incoming, Vector2 outgoing, int chain, bool perfect)
        {
            if (dead) return;

            // Halden's long, readable wind-up is a genuine counter window: a perfect
            // Last Bell rebound interrupts the attack instead of merely avoiding it.
            if (style == BossStyle.Vandal && windupTimer > 0f && perfect)
            {
                windupTimer = 0f;
                exposed = true;
                exposedTimer = 1.45f;
                if (reboundTarget != null) reboundTarget.SetCanBeMarked(true);
                ui?.BossPhaseChange("DE BELSLAG KEERT OM  •  HALDEN IS OPEN");
                AfterglowAudio.Instance?.Play("win", 1.08f);
            }
            else if (!exposed)
            {
                if (style == BossStyle.Vandal && windupTimer > 0f)
                {
                    ui?.Notify("HALDEN HOUDT STAND  •  DRUK SHIFT PRECIES OP DE BELSLAG", new Color(.75f, .43f, .3f), 1.3f);
                    ui?.PlayImpactEffect(transform.position, new Color(1f, .54f, .48f));
                }
                return;
            }

            if (chain < requiredChain)
            {
                ui?.Notify("KETEN TE KORT  •  MINIMAAL " + requiredChain, new Color(.75f, .43f, .3f), 1.5f);
                ui?.PlayImpactEffect(transform.position, new Color(1f, .54f, .48f));
                // Leave the seal open. The player can rebound away, build the required
                // chain, and return before the short vulnerability window closes.
                return;
            }

            CloseWindow();
            masteryHits++;
            ui?.UpdateBossMastery(masteryHits, MasteryHitsRequired);
            if (masteryHits < MasteryHitsRequired)
            {
                combatPhase = Mathf.Min(3, masteryHits + 1);
                ui?.BossPhaseChange(BossName() + "  •  FASE " + combatPhase + " — HET RIJK SLAAT TERUG");
            }
            ui?.PlayImpactEffect(transform.position, perfect ? new Color(.55f, 1f, .76f) : new Color(.55f, .88f, 1f));
            ui?.Notify((perfect ? "PERFECTE KETEN" : "ZUIVERE KETEN") + "  •  PATROON GEBROKEN " + masteryHits + " / " + MasteryHitsRequired,
                new Color(.82f, .68f, .42f), 1.6f);
            if (masteryHits >= MasteryHitsRequired) Defeat();
        }

        private void Defeat()
        {
            if (dead) return;
            dead = true;
            ui?.PlayEnemyBurst(transform.position);
            ui?.HideBossMastery();
            ui?.BossDefeated(BossName());
            if (exit != null) exit.Unlock();
            Destroy(gameObject);
        }

        private string BossName() => style == BossStyle.Vandal ? "HALDEN • DE TEMPELWACHTER" :
            style == BossStyle.CinderMatriarch ? "SERA VOSS • DE WORTELMOEDER" : "ASTER NULL • DE GODMACHINE";
    }
}
