using UnityEngine;

namespace Afterglow
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class AfterglowPulseProjectile : MonoBehaviour
    {
        private Rigidbody2D body;
        private CircleCollider2D hitbox;
        private AfterglowPlayer playerOwner;
        private Vector2 direction;
        private float speed;
        private float lifetime;
        private int damage;
        private bool charged;
        private bool hostile;
        private int remainingPierces;

        public bool IsHostile => hostile;

        public static AfterglowPulseProjectile FirePlayer(Vector2 origin, Vector2 heading, AfterglowPlayer owner, bool isCharged, int hitDamage = -1, int pierces = 0)
        {
            var projectile = new GameObject(isCharged ? "Oathroot • charged relic shot" : "Oathroot • pulse shot");
            projectile.transform.position = origin;
            var pulse = projectile.AddComponent<AfterglowPulseProjectile>();
            int damage = hitDamage > 0 ? hitDamage : isCharged ? 3 : 1;
            pulse.Configure(heading, isCharged ? 25f : 21f, 1.3f, damage, isCharged, false, owner, pierces);
            return pulse;
        }

        public static AfterglowPulseProjectile FireHostile(Vector2 origin, Vector2 heading, float speed, int damage, Color color)
        {
            var projectile = new GameObject("Relic guardian • hostile shard");
            projectile.transform.position = origin;
            var pulse = projectile.AddComponent<AfterglowPulseProjectile>();
            pulse.Configure(heading, speed, 3.4f, damage, false, true, null);
            pulse.SetHostilePalette(color);
            return pulse;
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            hitbox = GetComponent<CircleCollider2D>();
            hitbox.isTrigger = true;
        }

        private void Configure(Vector2 heading, float velocity, float seconds, int hitDamage, bool isCharged, bool isHostile, AfterglowPlayer owner, int pierces = 0)
        {
            direction = heading.sqrMagnitude > .01f ? heading.normalized : Vector2.right;
            speed = velocity;
            lifetime = seconds;
            damage = hitDamage;
            charged = isCharged;
            hostile = isHostile;
            playerOwner = owner;
            remainingPierces = Mathf.Max(0, pierces);
            float size = isCharged ? .26f : isHostile ? .18f : .14f;
            if (hitbox != null) hitbox.radius = size;
            CreateVisual();
        }

        private void SetHostilePalette(Color color)
        {
            foreach (var renderer in GetComponentsInChildren<SpriteRenderer>()) renderer.color = color;
        }

        private void FixedUpdate()
        {
            if (AfterglowUI.IsPaused) return;
            body.MovePosition(body.position + direction * (speed * Time.fixedDeltaTime));
            lifetime -= Time.fixedDeltaTime;
            if (lifetime <= 0f || Mathf.Abs(transform.position.x) > 180f || transform.position.y < -18f || transform.position.y > 30f)
                Destroy(gameObject);
        }

        private void CreateVisual()
        {
            Color core = hostile ? new Color(.88f, .23f, .31f) : new Color(.55f, .94f, 1f);
            Color rim = hostile ? new Color(.36f, .035f, .08f) : new Color(.12f, .5f, .72f);
            var halo = Child("Pulse halo", AfterglowArt.Disc("oathroot-projectile-halo", new Color(.2f, .5f, .65f, .26f), new Color(.8f, .9f, 1f, .75f)), 32);
            halo.transform.localScale = Vector3.one * (charged ? .56f : .34f);
            var bodySprite = Child("Cut crystal shell", AfterglowArt.Polygon("oathroot-projectile-shell", new Color(.07f, .12f, .17f), new Color(.72f, .65f, .42f),
                new Vector2(.04f, .5f), new Vector2(.35f, .12f), new Vector2(.78f, .18f), new Vector2(.98f, .5f), new Vector2(.78f, .82f), new Vector2(.35f, .88f)), 34);
            bodySprite.transform.localScale = new Vector3(charged ? .52f : .34f, charged ? .27f : .19f, 1f);
            var light = Child("Living crystal", AfterglowArt.Disc("oathroot-projectile-core", core, rim), 35);
            light.transform.localScale = Vector3.one * (charged ? .23f : .15f);
        }

        private SpriteRenderer Child(string childName, Sprite sprite, int order)
        {
            var child = new GameObject(childName);
            child.transform.SetParent(transform, false);
            var renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            if (AfterglowArt.SpriteMaterial != null) renderer.sharedMaterial = AfterglowArt.SpriteMaterial;
            return renderer;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null) return;
            if (hostile)
            {
                if (other.GetComponentInParent<AfterglowBoss>() != null) return;
                var hero = other.GetComponentInParent<AfterglowPlayer>();
                if (hero != null) { hero.TakeDamage(damage, transform.position); Destroy(gameObject); }
                else if (!other.isTrigger) Destroy(gameObject);
                return;
            }

            if (other.GetComponentInParent<AfterglowPlayer>() != null) return;
            var enemy = other.GetComponentInParent<AfterglowEnemy>();
            if (enemy != null)
            {
                var target = enemy.GetComponent<AfterglowReboundTarget>();
                bool marked = playerOwner != null && playerOwner.IsTargetMarked(target);
                enemy.ReceiveWeaponHit(damage, direction, charged, marked);
                if (marked) playerOwner.OnMarkedWeaponHit(target, transform.position, charged);
                playerOwner?.OnWeaponHit(enemy.transform.position, charged);
                if (remainingPierces > 0) remainingPierces--;
                else Destroy(gameObject);
                return;
            }
            var boss = other.GetComponentInParent<AfterglowBoss>();
            if (boss != null)
            {
                boss.ReceiveWeaponHit(damage, charged, playerOwner);
                playerOwner?.OnWeaponHit(boss.transform.position, charged);
                Destroy(gameObject);
                return;
            }

            var surfaceTarget = other.GetComponentInParent<AfterglowReboundTarget>();
            if (surfaceTarget != null && playerOwner != null && playerOwner.IsTargetMarked(surfaceTarget))
            {
                playerOwner.OnMarkedWeaponHit(surfaceTarget, transform.position, charged);
                Destroy(gameObject);
                return;
            }
            if (!other.isTrigger) Destroy(gameObject);
        }

        public void Disrupt()
        {
            if (!hostile) return;
            Destroy(gameObject);
        }
    }
}
