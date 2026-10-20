using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
public sealed class VesperPlayerController2D : MonoBehaviour
{
    [SerializeField] private float runSpeed = 8.5f;
    [SerializeField] private float acceleration = 72f;
    [SerializeField] private float jumpForce = 14f;
    [SerializeField] private float coyoteSeconds = 0.18f;
    [SerializeField] private float jumpBufferSeconds = 0.18f;
    [SerializeField] private float dashSpeed = 18f;
    [SerializeField] private float dashSeconds = 0.19f;
    [SerializeField] private float dashCooldown = 0.48f;
    [SerializeField] private float attackCooldown = 0.25f;
    [SerializeField] private Transform visual;

    private Rigidbody2D body;
    private CapsuleCollider2D capsule;
    private SpriteRenderer sprite;
    private Vector3 visualOrigin;
    private Vector3 visualBaseScale;
    private float axis;
    private float lastGrounded = -10f;
    private float jumpBuffered = -10f;
    private float nextDash;
    private float dashEnd;
    private float nextAttack;
    private float invulnerableUntil;
    private int facing = 1;
    private int airJumps;
    private int combo;
    private bool dashing;
    private bool wasGrounded;
    private int memoryShardChain;
    private float memoryShardChainExpires;
    private Vector2 checkpoint;
    private float pulseEnergy = 100f;
    private float energyRegenTime;

    public int Facing => facing;
    public bool IsDashing => dashing;
    public float PulseEnergy => pulseEnergy;
    public event System.Action<float, float> EnergyChanged;
    public event System.Action<int, int> HealthChanged;
    public int Health { get; private set; }
    public int MaximumHealth => 5 + VesperProgress.VitalityTier * 2;
    private float MaximumEnergy => 100f + VesperProgress.ReservoirTier * 25f;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        capsule = GetComponent<CapsuleCollider2D>();
        sprite = GetComponentInChildren<SpriteRenderer>();
        body.gravityScale = 3.7f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.linearDamping = 0f;
        if (visual == null) visual = transform.Find("Visual");
        if (visual != null) { visualOrigin = visual.localPosition; visualBaseScale = visual.localScale; }
        Health = MaximumHealth;
        pulseEnergy = MaximumEnergy;
        checkpoint = transform.position;
    }

    private void Update()
    {
        Keyboard k = Keyboard.current;
        Gamepad pad = Gamepad.current;
        if (k == null && pad == null) return;
        if (k != null && k.mKey.wasPressedThisFrame) VesperGameUI.ToggleMap();
        if (k != null && k.iKey.wasPressedThisFrame) VesperGameUI.ToggleInventory();
        if (k != null && k.tabKey.wasPressedThisFrame) VesperGameUI.ToggleSkillTree();
        if ((k != null && k.escapeKey.wasPressedThisFrame) || (pad != null && pad.startButton.wasPressedThisFrame)) VesperGameUI.TogglePause();
        if (Time.timeScale == 0f) return;
        axis = k == null ? 0f : (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1f : 0f) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1f : 0f);
        if (pad != null && Mathf.Abs(pad.leftStick.x.ReadValue()) > 0.2f) axis = pad.leftStick.x.ReadValue();
        if (Mathf.Abs(axis) > 0.05f) facing = axis > 0f ? 1 : -1;
        bool jumpPressed = (k != null && (k.spaceKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame || k.upArrowKey.wasPressedThisFrame))
            || (pad != null && pad.buttonSouth.wasPressedThisFrame);
        if (jumpPressed) RequestJump();
        if ((k != null && (k.leftShiftKey.wasPressedThisFrame || k.kKey.wasPressedThisFrame)) || (pad != null && pad.leftShoulder.wasPressedThisFrame)) TryDash();
        if (k != null && (k.jKey.wasPressedThisFrame || k.jKey.isPressed)) TryMelee();
        if (k != null && k.lKey.wasPressedThisFrame) TryPulse();
        if (k != null && k.eKey.wasPressedThisFrame) TryInteract();

        bool grounded = IsGrounded();
        if (grounded)
        {
            lastGrounded = Time.time;
            if (!wasGrounded) airJumps = 0;
        }
        else if (!HasAbility(VesperAbility.DoubleJump)) airJumps = Mathf.Min(airJumps, 0);
        wasGrounded = grounded;
        bool wall = !grounded && IsAgainstWall();
        if (Time.time - jumpBuffered <= jumpBufferSeconds)
        {
            if (Time.time - lastGrounded <= coyoteSeconds)
            {
                body.linearVelocity = new Vector2(body.linearVelocity.x, jumpForce);
                jumpBuffered = -10f;
                airJumps = 0;
                VesperGameUI.FlashMessage("JUMP // IMPULSE", 0.35f);
                PlayJumpFeedback();
            }
            else if (wall && HasAbility(VesperAbility.WallJump))
            {
                body.linearVelocity = new Vector2(-facing * runSpeed * 1.28f, jumpForce * 1.04f);
                facing *= -1;
                jumpBuffered = -10f;
            }
            else if (HasAbility(VesperAbility.DoubleJump) && airJumps < 1)
            {
                body.linearVelocity = new Vector2(body.linearVelocity.x, jumpForce * 0.9f);
                airJumps++;
                jumpBuffered = -10f;
                SpawnBurstAt(capsule.bounds.min + Vector3.up * 0.04f, new Color(0.5f, 0.93f, 0.79f), 8);
                VesperAtmosphereAudio.PlayJump();
                VesperCamera2D.Shake(0.035f, 0.07f);
            }
        }
        else if (Time.time - jumpBuffered > jumpBufferSeconds) jumpBuffered = -10f;

        if (((k != null && k.spaceKey.wasReleasedThisFrame) || (pad != null && pad.buttonSouth.wasReleasedThisFrame)) && body.linearVelocity.y > 0.2f)
            body.linearVelocity = new Vector2(body.linearVelocity.x, body.linearVelocity.y * 0.5f);

        if (dashing && Time.time >= dashEnd)
        {
            dashing = false;
            body.gravityScale = 3.7f;
            body.linearVelocity = new Vector2(facing * runSpeed, Mathf.Min(body.linearVelocity.y, 2.4f));
        }
        if (!grounded && body.position.y < -15f) Respawn();
        if (visual != null)
        {
            float movement = Mathf.Clamp01(Mathf.Abs(axis));
            float bob = grounded && movement > 0.1f ? Mathf.Abs(Mathf.Sin(Time.time * 15f)) * 0.075f : Mathf.Sin(Time.time * 2.1f) * 0.018f;
            float squash = grounded ? movement * 0.025f : 0f;
            visual.localPosition = visualOrigin + Vector3.up * bob;
            visual.localScale = new Vector3(Mathf.Abs(visualBaseScale.x) * facing * (1f + squash), visualBaseScale.y * (1f - squash), visualBaseScale.z);
            if (sprite != null && !dashing)
            {
                float lean = grounded ? -axis * 4f : Mathf.Clamp(-body.linearVelocity.y * 0.9f, -12f, 12f);
                visual.localRotation = Quaternion.Euler(0f, 0f, lean);
            }
        }
        pulseEnergy = Mathf.Min(MaximumEnergy, pulseEnergy + Time.deltaTime * (Time.time > energyRegenTime ? 6f * (1f + VesperProgress.ReservoirTier * 0.25f) : 0f));
        EnergyChanged?.Invoke(pulseEnergy, MaximumEnergy);
    }

    private void FixedUpdate()
    {
        if (dashing) return;
        Vector2 v = body.linearVelocity;
        float target = axis * runSpeed;
        v.x = Mathf.MoveTowards(v.x, target, acceleration * Time.fixedDeltaTime);
        body.linearVelocity = v;
    }

    public void SetCheckpoint(Vector2 point)
    {
        checkpoint = point;
        PlayerPrefs.SetFloat("VESPER_CheckpointX_" + VesperProgress.CurrentRegion, point.x);
        PlayerPrefs.SetFloat("VESPER_CheckpointY_" + VesperProgress.CurrentRegion, point.y);
        PlayerPrefs.Save();
        VesperGameUI.FlashMessage("ANCHOR // MEMORY SAVED", 1.5f);
    }

    public void RequestJump()
    {
        jumpBuffered = Time.time;
    }

    public int RegisterMemoryShard()
    {
        if (Time.time > memoryShardChainExpires) memoryShardChain = 0;
        memoryShardChain++;
        memoryShardChainExpires = Time.time + 2.75f;

        bool chainReward = memoryShardChain % 5 == 0;
        SpawnBurstAt(transform.position, new Color(0.72f, 1f, 0.82f), chainReward ? 14 : 7);
        VesperAtmosphereAudio.PlayReward(memoryShardChain);
        VesperCamera2D.Shake(chainReward ? 0.09f : 0.035f, chainReward ? 0.13f : 0.06f);
        VesperGameUI.FlashMessage(chainReward
            ? "ECHO CHAIN x" + memoryShardChain + "  //  BONUS MEMORY +1"
            : "MEMORY +1  //  ECHO CHAIN x" + memoryShardChain, chainReward ? 1.1f : 0.75f);
        return chainReward ? 1 : 0;
    }

    public void Damage(int amount, Vector2 source)
    {
        if (Time.time < invulnerableUntil || dashing) return;
        Health = Mathf.Max(0, Health - amount);
        invulnerableUntil = Time.time + 0.85f;
        body.linearVelocity = new Vector2(Mathf.Sign(transform.position.x - source.x) * 8f, 7.5f);
        if (sprite != null) StartCoroutine(FlashDamage());
        VesperCamera2D.Shake(0.19f, 0.28f);
        HealthChanged?.Invoke(Health, MaximumHealth);
        if (Health == 0) Respawn();
    }

    public void Heal(int amount)
    {
        Health = Mathf.Min(MaximumHealth, Health + amount);
        HealthChanged?.Invoke(Health, MaximumHealth);
    }

    public void Respawn()
    {
        Health = MaximumHealth;
        HealthChanged?.Invoke(Health, MaximumHealth);
        transform.position = checkpoint;
        body.linearVelocity = Vector2.zero;
        invulnerableUntil = Time.time + 1.3f;
        VesperGameUI.FlashMessage("ANCHOR RESTORED", 1.2f);
    }

    private void TryDash()
    {
        if (Time.time < nextDash || dashing) return;
        if (!IsGrounded() && !HasAbility(VesperAbility.AirDash)) return;
        dashing = true;
        dashEnd = Time.time + dashSeconds * (HasAbility(VesperAbility.SuperDash) ? 1.8f : 1f);
        nextDash = Time.time + dashCooldown;
        body.gravityScale = 0f;
        body.linearVelocity = new Vector2(facing * dashSpeed * (HasAbility(VesperAbility.SuperDash) ? 1.42f : 1f), 0f);
        SpawnBurst(new Color(0.55f, 0.94f, 0.83f), 5);
        VesperCamera2D.Shake(0.08f, 0.12f);
    }

    private void TryMelee()
    {
        if (Time.time < nextAttack) return;
        nextAttack = Time.time + attackCooldown;
        combo = (combo + 1) % 3;
        float height = transform.position.y + (body.linearVelocity.y > 1f ? 0.35f : 0.1f);
        Vector2 center = new Vector2(transform.position.x + facing * (0.82f + combo * 0.05f), height);
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, new Vector2(1.5f, 1.25f), 0f);
        int damage = 1 + (VesperProgress.SkillTier > 0 ? 1 : 0);
        bool connected = false;
        foreach (Collider2D hit in hits)
        {
            VesperEnemy2D enemy = hit.GetComponentInParent<VesperEnemy2D>();
            if (enemy == null) continue;
            enemy.TakeDamage(damage, new Vector2(facing * 8f, 3f));
            connected = true;
        }
        SpawnBurst(connected ? new Color(0.73f, 1f, 0.86f) : new Color(0.33f, 0.7f, 0.74f), connected ? 9 : 5);
        if (connected) VesperCamera2D.Shake(0.06f, 0.1f);
    }

    private void TryPulse()
    {
        if (!HasAbility(VesperAbility.EnergyWave))
        {
            VesperGameUI.FlashMessage("ENERGY WAVE // LOCKED", 1f);
            return;
        }
        if (pulseEnergy < 25f)
        {
            VesperGameUI.FlashMessage("ENERGY LOW", 0.65f);
            return;
        }
        pulseEnergy -= 25f;
        energyRegenTime = Time.time + 1.5f;
        VesperEffectPool.Fire(transform.position + Vector3.right * facing * 0.8f, Vector2.right * facing, true, 1 + VesperProgress.SkillTier);
    }

    private void TryInteract()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 1.4f);
        foreach (Collider2D hit in hits)
        {
            VesperRuneRelay2D relay = hit.GetComponentInParent<VesperRuneRelay2D>();
            if (relay != null) { relay.Interact(); return; }
            VesperDoor2D door = hit.GetComponentInParent<VesperDoor2D>();
            if (door != null) { door.Use(); return; }
            VesperCheckpoint2D checkpointObject = hit.GetComponentInParent<VesperCheckpoint2D>();
            if (checkpointObject != null) { SetCheckpoint(checkpointObject.transform.position); return; }
        }
    }

    public void ApplyProgressionChange()
    {
        Health = MaximumHealth;
        pulseEnergy = MaximumEnergy;
        HealthChanged?.Invoke(Health, MaximumHealth);
        EnergyChanged?.Invoke(pulseEnergy, MaximumEnergy);
    }

    private bool IsGrounded()
    {
        Bounds bounds = capsule.bounds;
        Vector2 feet = new Vector2(bounds.center.x, bounds.min.y + 0.045f);
        Vector2 footSize = new Vector2(Mathf.Max(0.14f, bounds.size.x * 0.72f), 0.1f);
        Collider2D[] contacts = Physics2D.OverlapBoxAll(feet, footSize, 0f);
        foreach (Collider2D contact in contacts)
            if (contact != capsule && !contact.isTrigger) return true;

        RaycastHit2D[] hits = Physics2D.CapsuleCastAll(bounds.center, bounds.size * 0.94f, capsule.direction, 0f, Vector2.down, 0.18f);
        foreach (RaycastHit2D hit in hits)
            if (hit.collider != null && hit.collider != capsule && !hit.collider.isTrigger && hit.normal.y > 0.2f) return true;
        return false;
    }

    private bool IsAgainstWall()
    {
        RaycastHit2D[] hits = Physics2D.CapsuleCastAll(capsule.bounds.center, capsule.size * 0.92f, capsule.direction, 0f, Vector2.right * facing, 0.14f);
        foreach (RaycastHit2D hit in hits)
            if (hit.collider != null && hit.collider != capsule && !hit.collider.isTrigger && Mathf.Abs(hit.normal.x) > 0.3f) return true;
        return false;
    }

    private bool HasAbility(VesperAbility ability) => VesperProgress.Has(ability);

    private void SpawnBurst(Color color, int amount)
    {
        SpawnBurstAt(transform.position + Vector3.up * 0.65f, color, amount);
    }

    private void SpawnBurstAt(Vector3 origin, Color color, int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            VesperEffectPool.Mote(origin,
                new Vector2(Random.Range(-1f, 1f), Random.Range(0.2f, 1f)) * Random.Range(2f, 5f),
                color, Random.Range(0.25f, 0.65f), Random.Range(0.035f, 0.09f));
        }
    }

    private void PlayJumpFeedback()
    {
        SpawnBurstAt(capsule.bounds.min + Vector3.up * 0.04f, new Color(0.63f, 0.98f, 0.82f), 6);
        VesperAtmosphereAudio.PlayJump();
        VesperCamera2D.Shake(0.035f, 0.07f);
    }

    private System.Collections.IEnumerator FlashDamage()
    {
        float t = 0f;
        while (t < 0.75f)
        {
            sprite.color = (Mathf.FloorToInt(t * 15f) % 2 == 0) ? new Color(1f, 0.36f, 0.42f) : Color.white;
            t += Time.deltaTime;
            yield return null;
        }
        sprite.color = Color.white;
    }
}

public sealed class VesperMote : MonoBehaviour
{
    private Vector2 velocity;
    private float life;
    private float age;
    private SpriteRenderer sr;
    public void Initialize(Vector2 v, float duration) { velocity = v; life = duration; sr = GetComponent<SpriteRenderer>(); }
    private void Update()
    {
        age += Time.deltaTime;
        transform.position += (Vector3)(velocity * Time.deltaTime);
        velocity *= 1f - Time.deltaTime * 2f;
        if (sr != null) { Color c = sr.color; c.a = 1f - age / life; sr.color = c; }
        if (age >= life) Destroy(gameObject);
    }
}

public sealed class VesperEchoBolt : MonoBehaviour
{
    private int direction;
    private int damage;
    private float age;
    public void Launch(int dir, int value) { direction = dir; damage = value; }
    private void Update()
    {
        age += Time.deltaTime;
        transform.position += Vector3.right * direction * 12f * Time.deltaTime;
        if (age > 1.4f) Destroy(gameObject);
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        VesperEnemy2D enemy = other.GetComponentInParent<VesperEnemy2D>();
        if (enemy != null) { enemy.TakeDamage(damage, new Vector2(direction * 4f, 1f)); Destroy(gameObject); }
        else if (!other.isTrigger && !other.GetComponentInParent<VesperPlayerController2D>()) Destroy(gameObject);
    }
}
