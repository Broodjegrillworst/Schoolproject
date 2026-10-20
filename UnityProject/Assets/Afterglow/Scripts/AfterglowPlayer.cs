using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Afterglow
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public sealed class AfterglowPlayer : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 8.1f;
        [SerializeField] private float jumpVelocity = 14.8f;
        [SerializeField] private float gravity = 3.25f;
        [SerializeField] private LayerMask groundMask = 1;
        [SerializeField] private float markRadius = 10f;
        [SerializeField] private float dashSpeed = 24f;

        private static readonly Collider2D[] MarkBuffer = new Collider2D[64];
        private readonly Collider2D[] groundBuffer = new Collider2D[4];
        private Rigidbody2D body;
        private CapsuleCollider2D capsule;
        private AfterglowUI ui;
        private Transform visual;
        private Transform leftLegPivot;
        private Transform rightLegPivot;
        private Transform leftArmPivot;
        private Transform rightArmPivot;
        private float normalGravity;
        private float coyoteTimer;
        private float jumpBuffer;
        private float dashRemaining;
        private float reboundWindow;
        private float markRemaining;
        private float chainTimer;
        private float invulnerability;
        private float contactDamageCooldown;
        private int health = 4;
        private int facing = 1;
        private int chain;
        private int ultimateCharge;
        private bool onGround;
        private bool wasGrounded;
        private bool dead;
        private bool dashActive;
        private bool waitingForReboundTiming;
        private Vector2 moveInput;
        private Vector2 dashDirection;
        private Vector2 impactIncoming;
        private Vector2 impactPoint;
        private Vector3 visualHome;
        private AfterglowReboundTarget markedTarget;
        private AfterglowReboundTarget impactTarget;
        private AfterglowLanternSignal lanternSignal;
        private bool gameplayStarted;
        private bool jumpHeld;
        private bool jumpCutActive;
        private bool dashHasMarkedTarget;
        private float secondaryCooldown;
        private float ultimateFlash;
        private SpriteRenderer[] visualRenderers = Array.Empty<SpriteRenderer>();
        private Transform weaponRig;
        private SpriteRenderer weaponCore;
        private PlayerCombat combat;
        private WeaponSystem weaponSystem;
        private AfterglowMemory nearbyMemory;
        private AfterglowShortcut nearbyShortcut;
        private AfterglowBreakableWall nearbyBreakableWall;

        public event Action<int, int> HealthChanged;
        public bool IsDead => dead;
        public bool IsGrounded => onGround;
        public int Health => health;
        public int MaximumHealth { get; private set; } = 4;
        public Transform Visual => visual;
        public bool HasStartedGameplay => gameplayStarted;
        public int UltimatePercent => ultimateCharge;

        public void Configure(AfterglowUI gameUi, Transform characterVisual, bool showTutorial)
        {
            ui = gameUi;
            visual = characterVisual;
            gameplayStarted = !showTutorial;
            visualHome = visual != null ? visual.localPosition : Vector3.zero;
            visualRenderers = visual != null ? visual.GetComponentsInChildren<SpriteRenderer>() : Array.Empty<SpriteRenderer>();
            lanternSignal = visual != null ? visual.GetComponent<AfterglowLanternSignal>() : null;
            if (visual != null)
            {
                leftLegPivot = visual.Find("Leg Left Pivot");
                rightLegPivot = visual.Find("Leg Right Pivot");
                leftArmPivot = visual.Find("Arm Left Pivot");
                rightArmPivot = visual.Find("Arm Right Pivot");
            }
            BuildRelicWeapon();
            weaponSystem = GetComponent<WeaponSystem>() ?? gameObject.AddComponent<WeaponSystem>();
            weaponSystem.Configure(this, ui);
            combat = GetComponent<PlayerCombat>() ?? gameObject.AddComponent<PlayerCombat>();
            combat.Configure(this, weaponSystem, ui);
            HealthChanged?.Invoke(health, MaximumHealth);
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            capsule = GetComponent<CapsuleCollider2D>();
            body.freezeRotation = true;
            body.gravityScale = gravity;
            normalGravity = gravity;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            capsule.size = new Vector2(.72f, 1.65f);
            capsule.offset = new Vector2(0f, .82f);
            coyoteTimer = .14f;
        }

        private void Update()
        {
            if (dead || AfterglowUI.IsPaused) return;
            if (transform.position.y < -12f) { health = 0; Die(); return; }

            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;
            float dt = Time.deltaTime;
            AfterglowSession.Tick(dt);
            jumpBuffer -= dt;
            invulnerability -= dt;
            contactDamageCooldown -= dt;
            markRemaining -= dt;
            chainTimer -= dt;
            reboundWindow -= dt;
            secondaryCooldown -= dt;
            ultimateFlash -= dt;

            moveInput = ReadMoveInput(keyboard, gamepad);
            if (Mathf.Abs(moveInput.x) > .05f) facing = moveInput.x > 0f ? 1 : -1;
            Vector2 aim = ReadAimDirection(keyboard, gamepad);
            if (Mouse.current != null && Mathf.Abs(aim.x) > .12f) facing = aim.x > 0f ? 1 : -1;
            UpdateWeaponPose(aim);

            bool jumpPressed = (keyboard != null && keyboard.spaceKey.wasPressedThisFrame) ||
                               (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame);
            bool dashPressed = (keyboard != null && (keyboard.leftShiftKey.wasPressedThisFrame || keyboard.rightShiftKey.wasPressedThisFrame)) ||
                               (gamepad != null && gamepad.buttonEast.wasPressedThisFrame);
            bool interactPressed = keyboard != null && keyboard.eKey.wasPressedThisFrame;
            jumpHeld = (keyboard != null && keyboard.spaceKey.isPressed) ||
                       (gamepad != null && gamepad.buttonSouth.isPressed);

            if (!gameplayStarted && (moveInput.sqrMagnitude > .04f || jumpPressed || dashPressed))
                gameplayStarted = true;

            if (jumpPressed) jumpBuffer = .16f;
            if (chain > 0 && chainTimer <= 0f) SetChain(0);
            if (markedTarget != null && markRemaining <= 0f) ClearMark();
            nearbyMemory = FindNearbyMemory();
            nearbyShortcut = nearbyMemory == null ? FindNearbyShortcut() : null;
            nearbyBreakableWall = nearbyMemory == null && nearbyShortcut == null ? FindNearbyBreakableWall() : null;
            ui?.SetInteractionHint(nearbyMemory != null || nearbyShortcut != null || nearbyBreakableWall != null,
                nearbyShortcut != null, nearbyBreakableWall != null);
            if (dashPressed) TryDashOrPerfectRebound();
            if (interactPressed) Interact();
            if (waitingForReboundTiming && reboundWindow <= 0f) CompleteRebound(false);
            // Coyote time was present in the controller but unreachable: the old outer
            // condition required onGround before TryJump could inspect the coyote timer.
            if (jumpBuffer > 0f && (onGround || coyoteTimer > 0f) && !dashActive && !waitingForReboundTiming) TryJump();
            Animate(moveInput.x, dt);
        }

        private void FixedUpdate()
        {
            if (dead || body == null) return;

            Vector2 feet = new Vector2(capsule.bounds.center.x, capsule.bounds.min.y + .025f);
            var groundFilter = new ContactFilter2D { useTriggers = false };
            groundFilter.SetLayerMask(groundMask);
            int groundCount = Physics2D.OverlapBox(feet, new Vector2(capsule.bounds.size.x * .86f, .09f), 0f, groundFilter, groundBuffer);
            bool groundedNow = false;
            for (int i = 0; i < groundCount; i++)
            {
                if (groundBuffer[i] != null && groundBuffer[i].GetComponentInParent<AfterglowReboundTarget>() != null)
                {
                    groundedNow = true;
                    break;
                }
            }
            onGround = groundedNow;
            if (onGround) coyoteTimer = .13f;
            else coyoteTimer -= Time.fixedDeltaTime;

            if (waitingForReboundTiming)
            {
                body.linearVelocity = Vector2.zero;
                return;
            }

            if (dashActive)
            {
                if (dashHasMarkedTarget && (markedTarget == null || !markedTarget.CanBeMarked))
                {
                    StopMissedDash();
                    return;
                }
                if (dashHasMarkedTarget)
                {
                    Vector2 toTarget = markedTarget.AimPoint(body.position, dashDirection) - body.position;
                    if (toTarget.sqrMagnitude > .04f)
                        dashDirection = Vector2.Lerp(dashDirection, toTarget.normalized, Time.fixedDeltaTime * 1.1f).normalized;
                }
                body.linearVelocity = dashDirection * dashSpeed;
                dashRemaining -= Time.fixedDeltaTime;
                if (dashRemaining <= 0f) StopMissedDash();
            }
            else
            {
                Vector2 velocity = body.linearVelocity;
                if (jumpCutActive && !jumpHeld && velocity.y > 0f)
                {
                    velocity.y = Mathf.Min(velocity.y, jumpVelocity * .48f);
                    jumpCutActive = false;
                }
                if (velocity.y <= 0f) jumpCutActive = false;
                velocity.x = Mathf.MoveTowards(velocity.x, moveInput.x * moveSpeed, (onGround ? 58f : 30f) * Time.fixedDeltaTime);
                body.linearVelocity = velocity;
                if (!wasGrounded && onGround && visual != null) ui?.PlayLandingEffect(transform.position);
            }
            wasGrounded = onGround;
        }

        private static Vector2 ReadMoveInput(Keyboard keyboard, Gamepad gamepad)
        {
            Vector2 input = Vector2.zero;
            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1f;
            }
            if (gamepad != null && gamepad.leftStick.ReadValue().sqrMagnitude > .04f) input = gamepad.leftStick.ReadValue();
            return Vector2.ClampMagnitude(input, 1f);
        }

        public Vector2 AimDirection()
        {
            Vector2 mouseAim = MouseAimDirection();
            if (mouseAim.sqrMagnitude > .04f) return mouseAim;
            Gamepad gamepad = Gamepad.current;
            if (gamepad != null && gamepad.rightStick.ReadValue().sqrMagnitude > .06f) return gamepad.rightStick.ReadValue().normalized;
            return new Vector2(facing, 0f);
        }

        private Vector2 ReadAimDirection(Keyboard keyboard, Gamepad gamepad) => AimDirection();

        private Vector2 MouseAimDirection()
        {
            if (Mouse.current == null || Camera.main == null) return Vector2.zero;
            Vector2 screen = Mouse.current.position.ReadValue();
            Vector3 world = Camera.main.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -Camera.main.transform.position.z));
            Vector2 aim = (Vector2)world - new Vector2(body.position.x, body.position.y + .95f);
            return aim.sqrMagnitude > .04f ? aim.normalized : Vector2.zero;
        }

        private void BuildRelicWeapon()
        {
            if (weaponRig != null) return;
            weaponRig = new GameObject("Oathroot • Mara's ancestral pulse cannon").transform;
            weaponRig.SetParent(transform, false);
            weaponCore = WeaponPart("Oathroot carved casing", AfterglowArt.Polygon("oathroot-cannon-shell",
                new Color(.09f, .12f, .16f), new Color(.73f, .59f, .34f),
                new Vector2(.02f,.5f), new Vector2(.19f,.26f), new Vector2(.51f,.2f), new Vector2(.68f,.32f),
                new Vector2(.88f,.25f), new Vector2(.99f,.5f), new Vector2(.88f,.75f), new Vector2(.66f,.68f), new Vector2(.49f,.8f), new Vector2(.2f,.72f)), 24);
            weaponCore.transform.localScale = new Vector3(.96f, .32f, 1f);
            WeaponPart("Brass spine", AfterglowArt.Pixel("oathroot-cannon-brass"), 25).transform.localScale = new Vector3(.69f, .034f, 1f);
            var crystal = WeaponPart("Living blue crystal", AfterglowArt.Disc("oathroot-cannon-heart", new Color(.25f,.72f,.83f,.95f), new Color(.96f,.82f,.53f,1f)), 26);
            crystal.transform.localPosition = new Vector3(.12f, 0f, 0f);
            crystal.transform.localScale = new Vector3(.19f, .19f, 1f);
            var muzzle = WeaponPart("Relic muzzle ring", AfterglowArt.Disc("oathroot-cannon-muzzle", new Color(.07f,.1f,.14f,.95f), new Color(.85f,.68f,.38f,1f)), 25);
            muzzle.transform.localPosition = new Vector3(.69f, 0f, 0f);
            muzzle.transform.localScale = new Vector3(.21f, .21f, 1f);
            var muzzleCore = WeaponPart("Muzzle aperture", AfterglowArt.Disc("oathroot-cannon-aperture", new Color(.47f,.9f,1f,.95f), new Color(.2f,.48f,.65f,1f)), 27);
            muzzleCore.transform.localPosition = new Vector3(.69f, 0f, -.01f);
            muzzleCore.transform.localScale = new Vector3(.095f, .095f, 1f);
        }

        private SpriteRenderer WeaponPart(string label, Sprite sprite, int order)
        {
            var part = new GameObject(label);
            part.transform.SetParent(weaponRig, false);
            var renderer = part.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            if (AfterglowArt.SpriteMaterial != null) renderer.sharedMaterial = AfterglowArt.SpriteMaterial;
            return renderer;
        }

        private void UpdateWeaponPose(Vector2 aim)
        {
            if (weaponRig == null) return;
            aim = aim.sqrMagnitude > .01f ? aim.normalized : new Vector2(facing, 0f);
            float angle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
            Vector3 targetPosition = new Vector3(body.position.x + aim.x * .28f, body.position.y + .97f + aim.y * .22f, -.1f);
            weaponRig.position = Vector3.Lerp(weaponRig.position, targetPosition, Time.deltaTime * 24f);
            Quaternion targetRotation = Quaternion.Euler(0f, 0f, angle);
            weaponRig.rotation = Quaternion.RotateTowards(weaponRig.rotation, targetRotation, Time.deltaTime * 1080f);
            if (weaponCore != null)
                weaponCore.color = combat != null && combat.IsCharging
                    ? Color.Lerp(Color.white, new Color(.54f, .91f, 1f), Mathf.PingPong(Time.time * 4f, 1f))
                    : Color.white;
        }

        public void FirePrimary(bool charged)
        {
            if (dead || weaponSystem == null) return;
            Vector2 aim = AimDirection();
            if (aim.sqrMagnitude < .01f) aim = new Vector2(facing, 0f);
            Vector2 origin = body.position + Vector2.up * .97f + aim * .96f;
            if (!weaponSystem.Fire(origin, aim, charged)) return;
            if (weaponCore != null) weaponCore.color = charged ? new Color(.73f, 1f, 1f) : new Color(1f, .88f, .65f);
            if (charged) AddUltimateCharge(4);
        }

        public void FireSecondary()
        {
            if (ultimateCharge >= 100)
            {
                FireUltimate();
                return;
            }
            if (secondaryCooldown > 0f || dead) return;
            secondaryCooldown = 3.2f;
            const float radius = 4.7f;
            var filter = new ContactFilter2D { useTriggers = true, useLayerMask = false };
            int count = Physics2D.OverlapCircle(body.position, radius, filter, MarkBuffer);
            var hitEnemies = new HashSet<AfterglowEnemy>();
            var hitBosses = new HashSet<AfterglowBoss>();
            for (int i = 0; i < count; i++)
            {
                Collider2D candidate = MarkBuffer[i];
                MarkBuffer[i] = null;
                if (candidate == null || candidate.transform.root == transform.root) continue;
                var enemy = candidate.GetComponentInParent<AfterglowEnemy>();
                if (enemy != null && hitEnemies.Add(enemy))
                {
                    Vector2 direction = ((Vector2)enemy.transform.position - body.position).normalized;
                    bool marked = IsTargetMarked(enemy.GetComponent<AfterglowReboundTarget>());
                    enemy.ReceiveWeaponHit(2, direction, true, marked);
                    OnWeaponHit(enemy.transform.position, true);
                    if (marked) OnMarkedWeaponHit(enemy.GetComponent<AfterglowReboundTarget>(), enemy.transform.position, true);
                }
                var boss = candidate.GetComponentInParent<AfterglowBoss>();
                if (boss != null && hitBosses.Add(boss))
                {
                    boss.ReceiveWeaponHit(2, true, this);
                    OnWeaponHit(boss.transform.position, true);
                }
            }
            foreach (var projectile in FindObjectsByType<AfterglowPulseProjectile>())
                if (projectile.IsHostile && Vector2.Distance(projectile.transform.position, body.position) <= radius) projectile.Disrupt();
            ui?.PlayPulseWave(body.position, radius, new Color(.43f, .81f, .9f));
            AfterglowAudio.Instance?.Play("special", 1f);
        }

        public void FireUltimate()
        {
            if (ultimateCharge < 100)
            {
                ui?.Notify("WORTELHART LAADT OP MET ELKE TREFFER", new Color(.69f, .8f, .83f), 1.4f);
                return;
            }
            ultimateCharge = 0;
            var filter = new ContactFilter2D { useTriggers = true, useLayerMask = false };
            int count = Physics2D.OverlapCircle(body.position, 9f, filter, MarkBuffer);
            var hitEnemies = new HashSet<AfterglowEnemy>();
            var hitBosses = new HashSet<AfterglowBoss>();
            for (int i = 0; i < count; i++)
            {
                Collider2D candidate = MarkBuffer[i];
                MarkBuffer[i] = null;
                if (candidate == null || candidate.transform.root == transform.root) continue;
                var enemy = candidate.GetComponentInParent<AfterglowEnemy>();
                if (enemy != null && hitEnemies.Add(enemy))
                {
                    enemy.ReceiveWeaponHit(4, ((Vector2)enemy.transform.position - body.position).normalized, true, IsTargetMarked(enemy.GetComponent<AfterglowReboundTarget>()));
                    OnWeaponHit(enemy.transform.position, true);
                }
                var boss = candidate.GetComponentInParent<AfterglowBoss>();
                if (boss != null && hitBosses.Add(boss)) boss.ReceiveWeaponHit(4, true, this);
            }
            foreach (var projectile in FindObjectsByType<AfterglowPulseProjectile>())
                if (projectile.IsHostile && Vector2.Distance(projectile.transform.position, body.position) <= 9f) projectile.Disrupt();
            ui?.PlayPulseWave(body.position, 9f, new Color(.87f, .71f, .42f));
            ui?.Notify("WORTELHART ONTGRENDELD  •  RMB ONTLAAST DE KRACHT", new Color(.94f, .82f, .59f), 2f);
            AfterglowAudio.Instance?.Play("ultimate", .9f);
            UpdateUltimateUI();
        }

        private AfterglowMemory FindNearbyMemory()
        {
            var filter = new ContactFilter2D { useTriggers = true, useLayerMask = false };
            int count = Physics2D.OverlapCircle(body.position, 1.65f, filter, MarkBuffer);
            AfterglowMemory best = null;
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                Collider2D collider = MarkBuffer[i]; MarkBuffer[i] = null;
                if (collider == null) continue;
                var memory = collider.GetComponentInParent<AfterglowMemory>();
                if (memory == null) continue;
                float distance = ((Vector2)memory.transform.position - body.position).sqrMagnitude;
                if (distance < bestDistance) { best = memory; bestDistance = distance; }
            }
            return best;
        }

        private void Interact()
        {
            if (nearbyMemory != null)
            {
                nearbyMemory.TryInteract(this);
                nearbyMemory = null;
                ui?.SetInteractionHint(false);
                return;
            }
            if (nearbyShortcut != null)
            {
                nearbyShortcut.TryUse(this);
                return;
            }
            if (nearbyBreakableWall != null)
            {
                nearbyBreakableWall.Interact();
                nearbyBreakableWall = null;
                ui?.SetInteractionHint(false);
                return;
            }
            ui?.Notify("NIETS OM MEE TE INTERAGEREN", new Color(.69f, .65f, .56f), 1f);
        }

        private AfterglowBreakableWall FindNearbyBreakableWall()
        {
            var filter = new ContactFilter2D { useTriggers = true, useLayerMask = false };
            int count = Physics2D.OverlapCircle(body.position, 1.7f, filter, MarkBuffer);
            AfterglowBreakableWall best = null;
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                Collider2D collider = MarkBuffer[i]; MarkBuffer[i] = null;
                if (collider == null) continue;
                AfterglowBreakableWall wall = collider.GetComponentInParent<AfterglowBreakableWall>();
                if (wall == null || wall.IsBroken) continue;
                float distance = ((Vector2)wall.transform.position - body.position).sqrMagnitude;
                if (distance < bestDistance) { best = wall; bestDistance = distance; }
            }
            return best;
        }

        private AfterglowShortcut FindNearbyShortcut()
        {
            var filter = new ContactFilter2D { useTriggers = true, useLayerMask = false };
            int count = Physics2D.OverlapCircle(body.position, 1.6f, filter, MarkBuffer);
            AfterglowShortcut best = null;
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                Collider2D collider = MarkBuffer[i]; MarkBuffer[i] = null;
                if (collider == null) continue;
                var shortcut = collider.GetComponentInParent<AfterglowShortcut>();
                if (shortcut == null) continue;
                float distance = ((Vector2)shortcut.transform.position - body.position).sqrMagnitude;
                if (distance < bestDistance) { best = shortcut; bestDistance = distance; }
            }
            return best;
        }

        public void WarpToShortcut(Vector2 position)
        {
            transform.position = new Vector3(position.x, position.y, transform.position.z);
            if (body != null) body.linearVelocity = Vector2.zero;
            dashActive = false;
            waitingForReboundTiming = false;
            ClearMark();
        }

        public bool IsTargetMarked(AfterglowReboundTarget target) => target != null && target == markedTarget && target.CanBeMarked;

        public void OnWeaponHit(Vector2 position, bool charged)
        {
            AddUltimateCharge(charged ? 12 : 7);
            ui?.PlayImpactEffect(position, charged ? new Color(.62f, .94f, 1f) : new Color(.49f, .75f, .88f));
        }

        public void OnMarkedWeaponHit(AfterglowReboundTarget target, Vector2 position, bool charged)
        {
            if (target == null || target != markedTarget) return;
            AddUltimateCharge(charged ? 24 : 15);
            ui?.PlayImpactEffect(position, new Color(.83f, .72f, .45f));
            ui?.Notify("OATHROOT RESONEERT  •  WORTELHART GELADEN", new Color(.83f, .72f, .45f), 1.05f);
            AfterglowAudio.Instance?.Play("markhit", 1f);
        }

        public void AddUltimateCharge(int amount)
        {
            int before = ultimateCharge;
            ultimateCharge = Mathf.Clamp(ultimateCharge + amount, 0, 100);
            if (before != ultimateCharge) UpdateUltimateUI();
        }

        private void UpdateUltimateUI() => ui?.UpdateUltimate(ultimateCharge);

        public void StartCombat() { gameplayStarted = true; }

        public void UpgradeWeaponFromMemory(int recoveredMemoryCount)
        {
            if (weaponSystem == null) return;
            if (weaponSystem.SetUpgradeLevel(recoveredMemoryCount))
            {
                string[] names = { "", "LEVEND KRISTAL", "WORTELRESONANTIE", "BELHARMONIE" };
                ui?.Notify("OATHROOT VERSTERKT  •  " + names[Mathf.Clamp(weaponSystem.UpgradeLevel, 1, 3)], new Color(.78f, .83f, .66f), 2f);
                ui?.PlayImpactEffect(transform.position, new Color(.67f, .83f, .53f));
                AfterglowAudio.Instance?.Play("win", 1.12f);
            }
        }

        private void TryMarkTarget(bool notifyWhenMissing = true)
        {
            Vector2 aim = AimDirection();
            var filter = new ContactFilter2D { useTriggers = true, useLayerMask = false };
            int count = Physics2D.OverlapCircle(body.position, markRadius, filter, MarkBuffer);
            AfterglowReboundTarget best = null;
            Vector2 bestPoint = Vector2.zero;
            float bestScore = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                Collider2D candidate = MarkBuffer[i];
                MarkBuffer[i] = null;
                if (candidate == null || candidate.transform.root == transform.root) continue;
                var target = candidate.GetComponentInParent<AfterglowReboundTarget>();
                if (target == null || !target.CanBeMarked) continue;
                Vector2 point = target.AimPoint(body.position, aim);
                Vector2 toPoint = point - body.position;
                float distance = toPoint.magnitude;
                // Let the player mark a nearby floor or wall. The previous one-unit dead zone
                // made ground targets impossible to select while standing on the floor.
                if (distance < .18f || distance > markRadius) continue;
                float alignment = Vector2.Dot(aim, toPoint / distance);
                if (alignment < -.05f) continue;
                float score = distance + (1f - alignment) * 2.6f;
                if (score >= bestScore) continue;
                best = target;
                bestPoint = point;
                bestScore = score;
            }

            if (best == null)
            {
                if (notifyWhenMissing) ui?.Notify("GEEN DOEL BINNEN BEREIK  •  RICHT MET BEWEGING", new Color(.69f, .39f, .28f), 1.2f);
                return;
            }

            ClearMark();
            markedTarget = best;
            markRemaining = 4.5f;
            markedTarget.SetMarked(this, bestPoint);
            lanternSignal?.Pulse(false);
            ui?.ShowMarkedTarget(markedTarget.DisplayName);
            AfterglowAudio.Instance?.Play("pulse", 1.15f);
        }

        private void TryDashOrPerfectRebound()
        {
            if (waitingForReboundTiming && reboundWindow > 0f)
            {
                CompleteRebound(true);
                return;
            }
            if (dashActive || waitingForReboundTiming) return;
            dashHasMarkedTarget = markedTarget != null && markedTarget.CanBeMarked;
            Vector2 movementDirection = new Vector2(Mathf.Abs(moveInput.x) > .1f ? Mathf.Sign(moveInput.x) : facing, moveInput.y);
            dashDirection = dashHasMarkedTarget
                ? (markedTarget.AimPoint(body.position, AimDirection()) - body.position).normalized
                : movementDirection.normalized;
            if (dashDirection.sqrMagnitude < .1f) dashDirection = new Vector2(facing, 0f);
            jumpCutActive = false;
            dashActive = true;
            Vector2 toTarget = dashHasMarkedTarget ? markedTarget.AimPoint(body.position, AimDirection()) - body.position : dashDirection * 4.4f;
            dashRemaining = Mathf.Clamp(toTarget.magnitude / dashSpeed + .12f, .18f, .68f);
            normalGravity = body.gravityScale;
            body.gravityScale = 0f;
            body.linearVelocity = dashDirection * dashSpeed;
            invulnerability = Mathf.Max(invulnerability, dashRemaining);
            ui?.PlayDashEffect(transform.position, dashDirection.x >= 0f ? 1 : -1);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!dashActive || waitingForReboundTiming) return;
            var target = collision.collider.GetComponentInParent<AfterglowReboundTarget>();
            if (target == null) return;
            Vector2 point = collision.contactCount > 0 ? collision.GetContact(0).point : body.position;
            TryStartRebound(target, point);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!dashActive || waitingForReboundTiming) return;
            var target = other.GetComponentInParent<AfterglowReboundTarget>();
            if (target == null) return;
            TryStartRebound(target, other.ClosestPoint(body.position));
        }

        private void TryStartRebound(AfterglowReboundTarget target, Vector2 point)
        {
            if (target == null || target != markedTarget || !target.CanBeMarked) return;
            impactTarget = target;
            impactPoint = point;
            impactIncoming = body.linearVelocity.sqrMagnitude > .1f ? body.linearVelocity.normalized : dashDirection;
            dashActive = false;
            waitingForReboundTiming = true;
            reboundWindow = .18f;
            body.linearVelocity = Vector2.zero;
            body.gravityScale = 0f;
            ui?.PromptPerfectRebound();
            AfterglowAudio.Instance?.Play("impact", 1.35f);
        }

        private void CompleteRebound(bool perfect)
        {
            if (!waitingForReboundTiming || impactTarget == null) return;
            waitingForReboundTiming = false;
            reboundWindow = 0f;
            dashHasMarkedTarget = false;
            chain = chainTimer > 0f ? chain + 1 : 1;
            chainTimer = perfect ? 2.8f : 2.1f;

            Vector2 normal = impactTarget.SurfaceNormal(body.position, impactPoint, impactIncoming);
            Vector2 fixedDirection = impactTarget.FixedReboundDirection;
            Vector2 outgoing = fixedDirection.sqrMagnitude > .01f ? fixedDirection :
                impactTarget.Kind == ReboundTargetKind.Enemy || impactTarget.Kind == ReboundTargetKind.Boss
                    ? normal
                    : Vector2.Reflect(impactIncoming, normal).normalized;
            if (outgoing.sqrMagnitude < .1f) outgoing = normal;
            Vector2 steer = moveInput.sqrMagnitude > .05f ? moveInput.normalized : Vector2.zero;
            if (steer.sqrMagnitude > .05f)
            {
                outgoing = Vector2.Lerp(outgoing, steer, perfect ? .48f : .28f).normalized;
                if (Vector2.Dot(outgoing, normal) < .12f) outgoing = (outgoing + normal * .35f).normalized;
            }

            AfterglowReboundTarget hit = impactTarget;
            Vector2 incoming = impactIncoming;
            impactTarget = null;
            ClearMark();
            body.gravityScale = normalGravity;
            body.linearVelocity = outgoing * (perfect ? 27f : 18f);
            jumpCutActive = false;
            invulnerability = Mathf.Max(invulnerability, .24f);
            wasGrounded = false;
            SetChain(chain);
            AddUltimateCharge(perfect ? 18 : 12);
            lanternSignal?.Pulse(perfect);
            hit.NotifyRebound(this, incoming, outgoing, chain, perfect);
            ui?.PlayReboundEffect(impactPoint, outgoing, chain, perfect);
        }

        private void StopMissedDash()
        {
            dashActive = false;
            dashHasMarkedTarget = false;
            body.gravityScale = normalGravity;
            Vector2 velocity = body.linearVelocity;
            body.linearVelocity = new Vector2(velocity.x * .25f, velocity.y * .25f);
            if (markedTarget != null && markedTarget != impactTarget) ClearMark();
        }

        private void ClearMark()
        {
            if (markedTarget != null) markedTarget.SetMarked(null, Vector2.zero);
            markedTarget = null;
            markRemaining = 0f;
            ui?.ClearMarkedTarget();
        }

        private void SetChain(int value)
        {
            chain = value;
        }

        private void TryJump()
        {
            if (!onGround && coyoteTimer <= 0f) return;
            Vector2 velocity = body.linearVelocity;
            velocity.y = jumpVelocity;
            body.linearVelocity = velocity;
            jumpCutActive = true;
            onGround = false;
            coyoteTimer = 0f;
            jumpBuffer = 0f;
            ui?.PlayJumpSound();
        }

        public void TakeDamage(int amount, Vector2 source)
        {
            if (dead || invulnerability > 0f || dashActive || waitingForReboundTiming) return;
            health = Mathf.Max(0, health - Mathf.Max(1, amount));
            invulnerability = .85f;
            lanternSignal?.Pulse(false);
            Vector2 direction = ((Vector2)transform.position - source).normalized;
            body.linearVelocity = new Vector2(direction.x * 6.3f, 7f);
            HealthChanged?.Invoke(health, MaximumHealth);
            ui?.PlayDamageEffect(transform.position);
            if (health == 0) Die();
        }

        public void ContactDamage(int amount, Vector2 source)
        {
            if (contactDamageCooldown > 0f) return;
            contactDamageCooldown = .65f;
            TakeDamage(amount, source);
        }

        private void Die()
        {
            if (dead) return;
            dead = true;
            dashActive = false;
            waitingForReboundTiming = false;
            ClearMark();
            AfterglowSession.RecordDeath();
            ui?.ShowGameOver();
        }

        private void Animate(float horizontal, float dt)
        {
            if (visual == null) return;
            if (Mathf.Abs(horizontal) > .05f) facing = horizontal > 0f ? 1 : -1;
            float squash = dashActive ? .82f : waitingForReboundTiming ? 1.12f : 1f;
            float run = onGround && !dashActive ? Mathf.Sin(Time.time * 15f) * .045f : 0f;
            visual.localPosition = Vector3.Lerp(visual.localPosition, visualHome + new Vector3(0f, run, 0f), dt * 12f);
            visual.localScale = new Vector3(facing * squash, 1f / squash, 1f);
            float tilt = dashActive ? -dashDirection.y * 9f : !onGround ? Mathf.Clamp(-body.linearVelocity.x * .55f, -7f, 7f) : Mathf.Sin(Time.time * 15f) * 1.5f;
            visual.localRotation = Quaternion.Slerp(visual.localRotation, Quaternion.Euler(0f, 0f, tilt), dt * 10f);
            float gait = onGround && Mathf.Abs(horizontal) > .15f && !dashActive ? Mathf.Sin(Time.time * 13f) * 24f : 0f;
            float armSwing = onGround && Mathf.Abs(horizontal) > .15f && !dashActive ? -gait * .72f : 0f;
            if (leftLegPivot != null) leftLegPivot.localRotation = Quaternion.Euler(0f, 0f, gait);
            if (rightLegPivot != null) rightLegPivot.localRotation = Quaternion.Euler(0f, 0f, -gait);
            if (leftArmPivot != null) leftArmPivot.localRotation = Quaternion.Euler(0f, 0f, armSwing);
            if (rightArmPivot != null) rightArmPivot.localRotation = Quaternion.Euler(0f, 0f, -armSwing);
            bool blink = invulnerability > 0f && ((int)(Time.time * 22f) % 2 == 0);
            foreach (var renderer in visualRenderers) renderer.enabled = !blink;
        }
    }
}
