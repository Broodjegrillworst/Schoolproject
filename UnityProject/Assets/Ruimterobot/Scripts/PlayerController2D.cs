using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2D : MonoBehaviour
{
    [Header("Bewegen en springen")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float groundAcceleration = 42f;
    [SerializeField] private float airAcceleration = 24f;
    [SerializeField] private float groundDeceleration = 55f;
    [SerializeField] private float airDeceleration = 11f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField, Range(0.2f, 0.9f)] private float jumpReleaseMultiplier = 0.55f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.12f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.15f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Schieten")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Text dashLabel;
    [SerializeField] private float fireCooldown = 0.25f;
    [SerializeField] private float rapidFireCooldown = 0.09f;
    [SerializeField] private int maximumJumps = 2;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 13f;
    [SerializeField] private float dashDuration = 0.16f;
    [SerializeField] private float dashCooldown = 1.1f;
    [SerializeField] private TrailRenderer dashTrail;

    [Header("Uiterlijk")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private HeroPowerController2D heroPowerController;

    private Rigidbody2D body;
    private float horizontalInput;
    private float nextFireTime;
    private float lastGroundedTime = -100f;
    private float jumpRequestedTime = -100f;
    private float rapidFireEndTime;
    private float dashEndTime;
    private float nextDashTime;
    private float normalGravityScale;
    private int jumpsUsed;
    private int facingDirection = 1;
    private float firePointStartX;
    private Vector3 visualRootStartPosition;
    private Vector3 visualRootStartScale;
    private bool isDashing;
    private bool wasGrounded;
    private bool jumpCutRequested;

    public bool IsFalling => body != null && body.linearVelocity.y < -0.1f;
    public bool IsDashing => isDashing;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        if (heroPowerController == null) heroPowerController = GetComponent<HeroPowerController2D>();
        normalGravityScale = body.gravityScale;
        if (GameSession.SkyboundUnlocked)
            maximumJumps = Mathf.Max(maximumJumps, 3);

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (firePoint != null)
        {
            firePointStartX = Mathf.Abs(firePoint.localPosition.x);
        }
        if (visualRoot != null)
        {
            visualRootStartPosition = visualRoot.localPosition;
            visualRootStartScale = visualRoot.localScale;
        }
    }

    private void Update()
    {
        if (Time.timeScale == 0f)
        {
            horizontalInput = 0f;
            jumpRequestedTime = -100f;
            return;
        }

        Keyboard keyboard = Keyboard.current;
        Gamepad gamepad = Gamepad.current;
        horizontalInput = 0f;

        if (keyboard != null)
        {
            if (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed)
            {
                horizontalInput = -1f;
            }
            if (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed)
            {
                horizontalInput = 1f;
            }

            if (keyboard.spaceKey.wasPressedThisFrame)
                RequestJump();
            if (keyboard.spaceKey.wasReleasedThisFrame)
                jumpCutRequested = true;
        }

        if (gamepad != null)
        {
            float stick = gamepad.leftStick.x.ReadValue();
            if (Mathf.Abs(stick) > 0.2f)
                horizontalInput = stick;
            if (gamepad.buttonSouth.wasPressedThisFrame)
                RequestJump();
            if (gamepad.buttonSouth.wasReleasedThisFrame)
                jumpCutRequested = true;
        }

        bool dashPressed = (keyboard != null && (keyboard.leftShiftKey.wasPressedThisFrame || keyboard.kKey.wasPressedThisFrame))
            || (gamepad != null && gamepad.leftShoulder.wasPressedThisFrame);
        if (dashPressed && !isDashing && Time.time >= nextDashTime)
            BeginDash();

        bool grounded = IsGrounded();
        if (grounded)
        {
            lastGroundedTime = Time.time;
            if (!wasGrounded)
                jumpsUsed = 0;
        }
        wasGrounded = grounded;

        bool jumpBuffered = Time.time - jumpRequestedTime <= jumpBufferTime;
        bool canJump = Time.time - lastGroundedTime <= coyoteTime;
        if (jumpBuffered && (canJump || jumpsUsed < Mathf.Max(1, maximumJumps)))
        {
            body.linearVelocity = new Vector2(body.linearVelocity.x, jumpForce);
            jumpRequestedTime = -100f;
            lastGroundedTime = -100f;
            jumpsUsed++;
            if (ArcadeAudioManager.Instance != null)
                ArcadeAudioManager.Instance.PlayJump();
        }
        else if (!jumpBuffered)
        {
            jumpRequestedTime = -100f;
        }

        if (horizontalInput > 0.01f)
        {
            SetFacingDirection(1);
        }
        else if (horizontalInput < -0.01f)
        {
            SetFacingDirection(-1);
        }

        bool pressedFire = (keyboard != null && keyboard.jKey.wasPressedThisFrame)
            || (gamepad != null && gamepad.rightTrigger.wasPressedThisFrame)
            || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
        bool fireHeld = (keyboard != null && keyboard.jKey.isPressed)
            || (gamepad != null && gamepad.rightTrigger.ReadValue() > 0.5f)
            || (Mouse.current != null && Mouse.current.leftButton.isPressed);
        if (pressedFire || fireHeld)
        {
            Shoot();
        }

        if (dashLabel != null)
        {
            float cooldownLeft = Mathf.Max(0f, nextDashTime - Time.time);
            dashLabel.text = isDashing ? "DASH!" : cooldownLeft > 0f
                ? "DASH  " + cooldownLeft.ToString("0.0") + " s"
                : "DASH GEREED  /  SHIFT";
        }

        if (visualRoot != null)
        {
            float bob = grounded && Mathf.Abs(horizontalInput) > 0.1f
                ? Mathf.Abs(Mathf.Sin(Time.time * 14f)) * 0.035f
                : Mathf.Sin(Time.time * 2.2f) * 0.012f;
            float movement = Mathf.Clamp01(Mathf.Abs(horizontalInput));
            float faceSign = visualRoot.localScale.x < 0f ? -1f : 1f;
            float squash = movement * 0.025f;
            visualRoot.localPosition = visualRootStartPosition + Vector3.up * bob;
            visualRoot.localScale = new Vector3(visualRootStartScale.x * faceSign * (1f - squash),
                visualRootStartScale.y * (1f + squash), visualRootStartScale.z);
            float tilt = grounded ? -horizontalInput * 2.8f * movement : Mathf.Clamp(-body.linearVelocity.y * 0.38f, -5f, 5f);
            visualRoot.localRotation = Quaternion.Euler(0f, 0f, tilt);
        }
    }

    private void FixedUpdate()
    {
        if (body == null || Time.timeScale == 0f)
        {
            return;
        }

        if (jumpCutRequested)
        {
            if (body.linearVelocity.y > 0f)
                body.linearVelocity = new Vector2(body.linearVelocity.x, body.linearVelocity.y * jumpReleaseMultiplier);
            jumpCutRequested = false;
        }

        if (isDashing)
        {
            if (Time.time < dashEndTime)
            {
                body.linearVelocity = new Vector2(facingDirection * dashSpeed, 0f);
                return;
            }

            isDashing = false;
            body.gravityScale = normalGravityScale;
            if (dashTrail != null)
                dashTrail.emitting = false;
        }

        bool grounded = IsGrounded();
        float targetSpeed = horizontalInput * moveSpeed;
        bool hasInput = Mathf.Abs(horizontalInput) > 0.01f;
        float acceleration = hasInput
            ? (grounded ? groundAcceleration : airAcceleration)
            : (grounded ? groundDeceleration : airDeceleration);
        float horizontalSpeed = Mathf.MoveTowards(body.linearVelocity.x, targetSpeed,
            Mathf.Max(0f, acceleration) * Time.fixedDeltaTime);
        body.linearVelocity = new Vector2(horizontalSpeed, body.linearVelocity.y);
    }

    private void BeginDash()
    {
        isDashing = true;
        dashEndTime = Time.time + Mathf.Max(0.08f, dashDuration);
        nextDashTime = Time.time + Mathf.Max(dashDuration, dashCooldown);
        body.gravityScale = 0f;
        body.linearVelocity = new Vector2(facingDirection * dashSpeed, 0f);
        if (dashTrail != null)
            dashTrail.emitting = true;
        if (ArcadeAudioManager.Instance != null)
            ArcadeAudioManager.Instance.PlayDash();
    }

    public void RequestJump()
    {
        jumpRequestedTime = Time.time;
    }

    private bool IsGrounded()
    {
        if (groundCheck == null)
        {
            return false;
        }

        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer) != null;
    }

    private void SetFacingDirection(int direction)
    {
        facingDirection = direction;

        if (visualRoot != null)
        {
            visualRoot.localScale = new Vector3(Mathf.Abs(visualRootStartScale.x) * direction,
                visualRootStartScale.y, visualRootStartScale.z);
        }
        else if (spriteRenderer != null)
        {
            spriteRenderer.flipX = facingDirection < 0;
        }

        if (firePoint != null && visualRoot == null)
        {
            Vector3 localPosition = firePoint.localPosition;
            localPosition.x = firePointStartX * facingDirection;
            firePoint.localPosition = localPosition;
        }
    }

    private void Shoot()
    {
        if (Time.time < nextFireTime) return;
        if (projectilePrefab == null || firePoint == null)
        {
            Debug.LogWarning("Koppel een Projectile Prefab en Fire Point aan PlayerController2D.", this);
            return;
        }

        float cooldown = Time.time < rapidFireEndTime ? rapidFireCooldown : fireCooldown;
        if (heroPowerController != null)
        {
            if (!heroPowerController.Fire(projectilePrefab, firePoint, facingDirection))
                return;
            cooldown *= heroPowerController.CooldownMultiplier;
            nextFireTime = Time.time + Mathf.Max(0.04f, cooldown);
            return;
        }

        GameObject projectileObject = ProjectilePool2D.Spawn(projectilePrefab, firePoint.position, Quaternion.identity);
        if (projectileObject == null)
            return;
        Projectile2D projectile = projectileObject.GetComponent<Projectile2D>();

        if (projectile == null)
        {
            Debug.LogError("Het projectile-prefab heeft geen Projectile2D-script.", projectileObject);
            Destroy(projectileObject);
            return;
        }

        Collider2D playerCollider = GetComponent<Collider2D>();
        Collider2D projectileCollider = projectileObject.GetComponent<Collider2D>();
        if (playerCollider != null && projectileCollider != null)
        {
            Physics2D.IgnoreCollision(projectileCollider, playerCollider);
        }

        projectile.Launch(facingDirection);
        if (ArcadeAudioManager.Instance != null)
            ArcadeAudioManager.Instance.PlayShot();
        nextFireTime = Time.time + Mathf.Max(0.04f, cooldown);
    }

    public void ActivateRapidFire(float durationSeconds)
    {
        if (durationSeconds > 0f)
            rapidFireEndTime = Mathf.Max(rapidFireEndTime, Time.time + durationSeconds);
    }

    public bool HasRapidFire => Time.time < rapidFireEndTime;

    public void UnlockSkybound()
    {
        maximumJumps = Mathf.Max(maximumJumps, 3);
        GameSession.UnlockSkybound();
    }

    public void BounceAfterStomp()
    {
        if (body == null)
            return;

        jumpsUsed = 0;
        lastGroundedTime = -100f;
        body.linearVelocity = new Vector2(body.linearVelocity.x, jumpForce * 0.78f);
    }

    private void OnDisable()
    {
        if (isDashing && body != null)
            body.gravityScale = normalGravityScale;
        isDashing = false;
        jumpCutRequested = false;
        if (dashTrail != null)
            dashTrail.emitting = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}



