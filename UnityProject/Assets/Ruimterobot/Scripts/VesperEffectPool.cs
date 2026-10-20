using System.Collections.Generic;
using UnityEngine;

public sealed class VesperEffectPool : MonoBehaviour
{
    private static VesperEffectPool instance;
    private readonly Stack<VesperPooledBolt> bolts = new Stack<VesperPooledBolt>();
    private readonly Stack<VesperPooledMote> motes = new Stack<VesperPooledMote>();
    private Transform poolRoot;

    private void Awake()
    {
        instance = this;
        poolRoot = new GameObject("VESPER // OBJECT POOL").transform;
        poolRoot.SetParent(transform, false);
        for (int i = 0; i < 18; i++) CreateBolt();
        for (int i = 0; i < 48; i++) CreateMote();
    }

    public static void Fire(Vector3 position, Vector2 direction, bool friendly, int damage = 1, bool heavy = false)
    {
        if (instance == null) return;
        VesperPooledBolt bolt = instance.bolts.Count > 0 ? instance.bolts.Pop() : instance.CreateBolt();
        bolt.gameObject.SetActive(true);
        bolt.Launch(position, direction, friendly, damage, heavy);
    }

    public static void Mote(Vector3 position, Vector2 velocity, Color color, float life, float scale)
    {
        if (instance == null) return;
        VesperPooledMote mote = instance.motes.Count > 0 ? instance.motes.Pop() : instance.CreateMote();
        mote.gameObject.SetActive(true);
        mote.Launch(position, velocity, color, life, scale);
    }

    internal static void Recycle(VesperPooledBolt bolt)
    {
        if (instance == null) { Destroy(bolt.gameObject); return; }
        bolt.gameObject.SetActive(false);
        bolt.transform.SetParent(instance.poolRoot, false);
        instance.bolts.Push(bolt);
    }
    internal static void Recycle(VesperPooledMote mote)
    {
        if (instance == null) { Destroy(mote.gameObject); return; }
        mote.gameObject.SetActive(false);
        mote.transform.SetParent(instance.poolRoot, false);
        instance.motes.Push(mote);
    }

    private VesperPooledBolt CreateBolt()
    {
        GameObject go = new GameObject("Pooled echo bolt"); go.transform.SetParent(poolRoot, false); go.SetActive(false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>(); sr.sprite = VesperArtFactory.Orb; sr.sortingOrder = 9;
        CircleCollider2D collider = go.AddComponent<CircleCollider2D>(); collider.isTrigger = true; collider.radius = 0.45f;
        Rigidbody2D rb = go.AddComponent<Rigidbody2D>(); rb.gravityScale = 0f; rb.bodyType = RigidbodyType2D.Kinematic;
        VesperPooledBolt bolt = go.AddComponent<VesperPooledBolt>(); bolts.Push(bolt); return bolt;
    }
    private VesperPooledMote CreateMote()
    {
        GameObject go = new GameObject("Pooled mote"); go.transform.SetParent(poolRoot, false); go.SetActive(false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>(); sr.sprite = VesperArtFactory.Glow; sr.sortingOrder = 10;
        VesperPooledMote mote = go.AddComponent<VesperPooledMote>(); motes.Push(mote); return mote;
    }
}

public sealed class VesperPooledBolt : MonoBehaviour
{
    private Vector2 velocity;
    private float age;
    private bool friendly;
    private int damage;
    private bool heavy;
    private SpriteRenderer sr;
    private void Awake() { sr = GetComponent<SpriteRenderer>(); }
    public void Launch(Vector3 position, Vector2 direction, bool playerOwned, int amount, bool strong)
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        transform.position = position;
        transform.localScale = Vector3.one * (strong ? 0.65f : 0.46f);
        friendly = playerOwned; damage = amount; heavy = strong; age = 0f;
        velocity = direction.normalized * (playerOwned ? 13f : strong ? 8f : 6.5f);
        sr.color = playerOwned ? new Color(0.6f, 1f, 0.84f) : strong ? new Color(1f, 0.43f, 0.35f) : new Color(0.8f, 0.37f, 0.7f);
    }
    private void Update()
    {
        age += Time.deltaTime;
        transform.position += (Vector3)(velocity * Time.deltaTime);
        transform.Rotate(0f, 0f, 300f * Time.deltaTime);
        if (age > 3.5f) VesperEffectPool.Recycle(this);
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (friendly)
        {
            VesperEnemy2D enemy = other.GetComponentInParent<VesperEnemy2D>();
            if (enemy != null) { enemy.TakeDamage(damage, velocity.normalized * 5f); VesperEffectPool.Recycle(this); return; }
            if (!other.isTrigger && !other.GetComponentInParent<VesperPlayerController2D>()) VesperEffectPool.Recycle(this);
        }
        else
        {
            VesperPlayerController2D player = other.GetComponentInParent<VesperPlayerController2D>();
            if (player != null) { player.Damage(heavy ? 2 : 1, transform.position); VesperEffectPool.Recycle(this); return; }
            if (!other.isTrigger && !other.GetComponentInParent<VesperEnemy2D>()) VesperEffectPool.Recycle(this);
        }
    }
}

public sealed class VesperPooledMote : MonoBehaviour
{
    private Vector2 velocity;
    private float age, life;
    private SpriteRenderer sr;
    private Color baseColor;
    private void Awake() { sr = GetComponent<SpriteRenderer>(); }
    public void Launch(Vector3 position, Vector2 v, Color color, float duration, float scale)
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        transform.position = position; transform.localScale = Vector3.one * scale;
        velocity = v; age = 0f; life = Mathf.Max(0.1f, duration); baseColor = color; sr.color = color;
    }
    private void Update()
    {
        age += Time.deltaTime; transform.position += (Vector3)(velocity * Time.deltaTime); velocity *= 1f - Time.deltaTime * 2f;
        Color c = baseColor; c.a = 1f - Mathf.Clamp01(age / life); sr.color = c;
        if (age >= life) VesperEffectPool.Recycle(this);
    }
}
