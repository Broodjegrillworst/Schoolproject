using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class VesperDoor2D : MonoBehaviour
{
    [SerializeField] private string targetScene;
    [SerializeField] private VesperAbility requiredAbility;
    [SerializeField] private bool enterFromRight;
    [SerializeField] private int bossRegionRequirement;
    [SerializeField] private bool customSpawn;
    [SerializeField] private Vector2 destinationSpawn;
    public void Configure(string scene, VesperAbility requirement, bool fromRight, int bossRequirement = 0)
    {
        targetScene = scene; requiredAbility = requirement; enterFromRight = fromRight; bossRegionRequirement = bossRequirement; customSpawn = false;
    }
    public void ConfigureToPoint(string scene, VesperAbility requirement, Vector2 spawn, int bossRequirement = 0)
    {
        targetScene = scene; requiredAbility = requirement; destinationSpawn = spawn; bossRegionRequirement = bossRequirement; customSpawn = true;
    }
    public void Use()
    {
        if (string.IsNullOrEmpty(targetScene)) { VesperGameUI.FlashMessage("THE PATH ENDS HERE", 1f); return; }
        if (requiredAbility != VesperAbility.None && !VesperProgress.Has(requiredAbility))
        {
            VesperGameUI.FlashMessage("SEALED // " + requiredAbility.ToString().ToUpperInvariant() + " REQUIRED", 1.4f);
            return;
        }
        if (bossRegionRequirement > 0 && !VesperProgress.HasDefeatedBoss(bossRegionRequirement))
        {
            VesperGameUI.FlashMessage("THE WARDEN STILL HOLDS THIS ROOT", 1.4f);
            return;
        }
        VesperProgress.UseCustomSpawn = customSpawn;
        if (customSpawn)
        {
            VesperProgress.CustomSpawnPosition = destinationSpawn;
            VesperProgress.SpawnFromSide = -1;
        }
        else if (targetScene == "VesperMenu") VesperProgress.SpawnFromSide = -1;
        else VesperProgress.SpawnFromSide = enterFromRight ? 1 : 0;
        UnityEngine.SceneManagement.SceneManager.LoadScene(targetScene);
    }
    private void OnTriggerEnter2D(Collider2D other) { if (other.CompareTag("Player")) VesperGameUI.FlashPrompt("E  //  ENTER PASSAGE"); }
    private void OnTriggerExit2D(Collider2D other) { if (other.CompareTag("Player")) VesperGameUI.ClearPrompt(); }
}

public sealed class VesperPickup2D : MonoBehaviour
{
    [SerializeField] private VesperAbility ability;
    [SerializeField] private bool shard;
    [SerializeField] private bool upgrade;
    [SerializeField] private bool heal;
    [SerializeField] private bool vaultReward;
    private Vector3 home;
    private SpriteRenderer sr;
    private int region;
    private int pickupId = -1;
    public void Configure(VesperAbility unlock, bool isShard, bool isUpgrade, bool isHeal, bool isVaultReward = false, int area = -1, int stablePickupId = -1)
    {
        ability = unlock; shard = isShard; upgrade = isUpgrade; heal = isHeal; vaultReward = isVaultReward;
        region = area; pickupId = stablePickupId;
    }
    private void Awake() { home = transform.position; sr = GetComponentInChildren<SpriteRenderer>(); }
    private void Update()
    {
        transform.position = home + Vector3.up * Mathf.Sin(Time.time * 2f + home.x) * 0.12f;
        transform.Rotate(0f, 0f, 22f * Time.deltaTime);
        if (sr != null) { Color c = sr.color; c.a = 0.75f + Mathf.Sin(Time.time * 3f + home.x) * 0.2f; sr.color = c; }
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        VesperPlayerController2D player = other.GetComponentInParent<VesperPlayerController2D>();
        if (player == null) return;
        if (upgrade && !VesperProgress.BuyUpgrade())
        {
            VesperGameUI.FlashMessage("SKILL WEAVE // NEED MORE SHARDS", 1.3f);
            return;
        }
        if (pickupId >= 0) VesperProgress.MarkPickupCollected(region, pickupId);
        if (vaultReward) VesperProgress.MarkVaultRewardClaimed(VesperProgress.CurrentRegion);
        if (shard) VesperProgress.AddShards(1 + player.RegisterMemoryShard());
        if (upgrade)
        {
            VesperGameUI.FlashMessage("SKILL WEAVE // UPGRADE FORGED", 1.3f);
        }
        if (heal) { player.Heal(1); VesperGameUI.FlashMessage("VITALS RESTORED", 0.7f); }
        if (ability != VesperAbility.None)
        {
            bool unlocked = VesperProgress.Unlock(ability);
            VesperGameUI.FlashMessage(unlocked ? "ABILITY AWAKENED // " + ability.ToString().ToUpperInvariant() : "MEMORY ALREADY CLAIMED", 1.7f);
        }
        VesperCamera2D.Shake(0.11f, 0.14f);
        Destroy(gameObject);
    }
}

public sealed class VesperCheckpoint2D : MonoBehaviour
{
    private bool lit;
    private float nextTouch;
    private SpriteRenderer sr;
    private void Awake() { sr = GetComponentInChildren<SpriteRenderer>(); }
    private void Update()
    {
        if (sr != null) sr.color = lit ? new Color(0.55f, 1f, 0.78f) : Color.Lerp(new Color(0.22f, 0.7f, 0.62f), new Color(0.75f, 0.88f, 0.65f), Mathf.PingPong(Time.time * 0.8f, 1f));
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (Time.time < nextTouch) return;
        VesperPlayerController2D player = other.GetComponentInParent<VesperPlayerController2D>();
        if (player == null) return;
        nextTouch = Time.time + 1f;
        lit = true;
        player.SetCheckpoint(transform.position + Vector3.up * 0.8f);
    }
}

public sealed class VesperHazard2D : MonoBehaviour
{
    [SerializeField] private bool timed;
    [SerializeField] private float activeSeconds = 1.4f;
    [SerializeField] private float safeSeconds = 1.3f;
    [SerializeField] private bool rotating;
    private SpriteRenderer sr;
    private Collider2D hitbox;
    private float phase;
    private float nextDamage;
    public void Configure(bool isTimed, bool isRotating, float active, float safe)
    {
        timed = isTimed; rotating = isRotating; activeSeconds = active; safeSeconds = safe;
    }
    private void Awake() { sr = GetComponentInChildren<SpriteRenderer>(); hitbox = GetComponent<Collider2D>(); phase = Random.Range(0f, 4f); }
    private void Update()
    {
        if (rotating) transform.Rotate(0f, 0f, 95f * Time.deltaTime);
        bool active = !timed || (Time.time + phase) % (activeSeconds + safeSeconds) < activeSeconds;
        if (hitbox != null) hitbox.enabled = active;
        if (sr != null) sr.color = active ? new Color(1f, 0.36f, 0.45f, 0.92f) : new Color(0.35f, 0.85f, 0.8f, 0.28f);
        if (timed && !active) transform.localScale = Vector3.Lerp(transform.localScale, new Vector3(0.85f, 0.92f, 1f), Time.deltaTime * 6f);
        else transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one, Time.deltaTime * 6f);
    }
    private void OnTriggerStay2D(Collider2D other)
    {
        if (Time.time < nextDamage) return;
        VesperPlayerController2D player = other.GetComponentInParent<VesperPlayerController2D>();
        if (player != null) { nextDamage = Time.time + 0.7f; player.Damage(1, transform.position); }
    }
}

public sealed class VesperPuzzleController2D : MonoBehaviour
{
    private int[] order;
    private GameObject seal;
    private GameObject reward;
    private int region;
    private int progress;
    private float expiresAt;
    private bool clueShown;
    private bool solved;

    public void Configure(int[] sequence, GameObject barrier, GameObject prize, int area)
    {
        order = sequence;
        seal = barrier;
        reward = prize;
        region = area;
        solved = VesperProgress.IsVaultSolved(region);
        if (solved)
        {
            if (seal != null) seal.SetActive(false);
            if (reward != null) reward.SetActive(!VesperProgress.IsVaultRewardClaimed(region));
        }
    }

    public void Activate(int symbol)
    {
        if (solved || order == null || order.Length == 0) return;
        ShowHint();
        if (progress > 0 && Time.time > expiresAt) progress = 0;
        if (symbol != order[progress])
        {
            progress = 0;
            expiresAt = 0f;
            VesperGameUI.FlashMessage("WRONG ECHO // THE SEQUENCE RESET", 1.2f);
            VesperCamera2D.Shake(0.06f, 0.12f);
            return;
        }
        progress++;
        expiresAt = Time.time + 10f;
        VesperCamera2D.Shake(0.035f, 0.08f);
        if (progress < order.Length)
        {
            VesperGameUI.FlashMessage("ECHO " + progress + "/" + order.Length + " // KEEP THE RHYTHM", 0.7f);
            return;
        }
        solved = true;
        VesperProgress.MarkVaultSolved(region);
        if (seal != null) seal.SetActive(false);
        if (reward != null) reward.SetActive(true);
        VesperProgress.DiscoverRoom(region, 6);
        VesperCamera2D.Shake(0.24f, 0.38f);
        VesperGameUI.FlashMessage("HIDDEN VAULT OPEN // MEMORY REVEALED", 2.2f);
    }

    public void ShowHint()
    {
        if (clueShown || solved || order == null) return;
        clueShown = true;
        string clue = string.Join(" - ", System.Array.ConvertAll(order, value => value.ToString()));
        VesperGameUI.FlashMessage("ECHO ORDER   " + clue + "   //   TRACE BEFORE THE LIGHT FADES", 4f);
    }
}

public sealed class VesperRuneRelay2D : MonoBehaviour
{
    private VesperPuzzleController2D puzzle;
    private int symbol;
    private bool lit;
    private SpriteRenderer rune;

    public void Configure(VesperPuzzleController2D controller, int id)
    {
        puzzle = controller;
        symbol = id;
        rune = transform.Find("Resonance dial // rune")?.GetComponent<SpriteRenderer>();
    }

    public void Interact()
    {
        if (puzzle == null || lit) return;
        lit = true;
        if (rune != null) rune.color = new Color(0.84f, 1f, 0.72f);
        puzzle.Activate(symbol);
        Invoke(nameof(ResetGlow), 0.65f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<VesperPlayerController2D>() != null)
        {
            puzzle?.ShowHint();
            VesperGameUI.FlashPrompt("E  //  TOUCH ECHO DIAL " + symbol);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<VesperPlayerController2D>() != null) VesperGameUI.ClearPrompt();
    }

    private void ResetGlow() { lit = false; }
}

public sealed class VesperMapRoomTrigger2D : MonoBehaviour
{
    private int region;
    private int room;
    private bool recorded;

    public void Configure(int area, int roomIndex) { region = area; room = roomIndex; }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (recorded || other.GetComponentInParent<VesperPlayerController2D>() == null) return;
        recorded = true;
        VesperProgress.DiscoverRoom(region, room);
    }
}

public sealed class VesperFallingHazard2D : MonoBehaviour
{
    private Rigidbody2D body;
    private SpriteRenderer sprite;
    private Vector3 home;
    private float resetAt;
    private bool falling;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        sprite = GetComponentInChildren<SpriteRenderer>();
        home = transform.position;
    }

    public void Drop()
    {
        if (falling || body == null) return;
        falling = true;
        resetAt = Time.time + 4f;
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 2.2f;
        body.linearVelocity = Vector2.down * 1.5f;
        if (sprite != null) sprite.color = new Color(1f, 0.52f, 0.38f);
        VesperCamera2D.Shake(0.04f, 0.1f);
    }

    private void Update()
    {
        if (falling && (Time.time >= resetAt || transform.position.y < -18f))
        {
            body.linearVelocity = Vector2.zero;
            body.bodyType = RigidbodyType2D.Kinematic;
            body.position = home;
            transform.position = home;
            falling = false;
            if (sprite != null) sprite.color = new Color(0.22f, 0.12f, 0.13f);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        VesperPlayerController2D player = collision.collider.GetComponentInParent<VesperPlayerController2D>();
        if (player != null) player.Damage(1, transform.position);
    }
}

public sealed class VesperFallTrigger2D : MonoBehaviour
{
    private VesperFallingHazard2D hazard;
    public void Configure(VesperFallingHazard2D owner) { hazard = owner; }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<VesperPlayerController2D>() != null) hazard?.Drop();
    }
}
