using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int startingLives = 3;
    [SerializeField] private int maximumLives = 5;
    [SerializeField] private float invulnerabilitySeconds = 1f;
    [SerializeField] private float fallDeathY = -6.5f;
    [SerializeField] private Text livesLabel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private PauseMenuController pauseMenu;

    private int livesRemaining;
    private Vector3 respawnPosition;
    private bool invulnerable;
    private bool gameOver;
    private Rigidbody2D body;
    private SpriteRenderer[] visualRenderers;

    public bool IsGameOver => gameOver;
    public int LivesRemaining => livesRemaining;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        visualRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        maximumLives = Mathf.Max(1, maximumLives);
        livesRemaining = GameSession.InitializePlayerLives(startingLives, maximumLives);
        respawnPosition = transform.position;
        if (ProfileSaveSystem.TryGetCheckpoint(SceneManager.GetActiveScene().name, out Vector2 savedCheckpoint))
        {
            respawnPosition = new Vector3(savedCheckpoint.x, savedCheckpoint.y, transform.position.z);
            transform.position = respawnPosition;
        }
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
        UpdateLivesLabel();
    }

    private void Update()
    {
        if (!gameOver && Time.timeScale > 0f && transform.position.y < fallDeathY)
            TakeDamage();
    }

    public void TakeDamage()
    {
        if (invulnerable || gameOver)
        {
            return;
        }

        PlayerController2D movement = GetComponent<PlayerController2D>();
        if (movement != null && movement.IsDashing) return;

        if (ArcadeAudioManager.Instance != null)
            ArcadeAudioManager.Instance.PlayHit();
        FeedbackVFX2D.Shake(0.16f, 0.16f);

        if (GameSession.ConsumeAegisShield())
        {
            invulnerable = true;
            StartCoroutine(FlashDuringInvulnerability());
            CancelInvoke(nameof(EndInvulnerability));
            Invoke(nameof(EndInvulnerability), invulnerabilitySeconds);
            UpdateLivesLabel();
            return;
        }

        livesRemaining--;
        RunStatistics.RecordDeath();
        GameSession.SetLivesRemaining(livesRemaining);
        UpdateLivesLabel();

        if (livesRemaining <= 0)
        {
            gameOver = true;
            if (body != null)
                body.linearVelocity = Vector2.zero;
            if (pauseMenu != null)
                pauseMenu.SetGameOver();
            Time.timeScale = 0f;
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
                PauseMenuController.SelectFirstButton(gameOverPanel);
            }
            else
            {
                GameSession.PrepareForLevelRetry();
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
            return;
        }

        invulnerable = true;
        StartCoroutine(FlashDuringInvulnerability());
        CancelInvoke(nameof(EndInvulnerability));
        Invoke(nameof(EndInvulnerability), invulnerabilitySeconds);

        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.position = new Vector2(respawnPosition.x, respawnPosition.y);
        }
        else
        {
            transform.position = respawnPosition;
        }
    }

    public void AddLife(int amount = 1)
    {
        if (gameOver || amount <= 0)
            return;

        livesRemaining = Mathf.Min(maximumLives, livesRemaining + amount);
        GameSession.SetLivesRemaining(livesRemaining);
        UpdateLivesLabel();
    }

    public void SetCheckpoint(Vector3 checkpointPosition)
    {
        respawnPosition = checkpointPosition;
        ProfileSaveSystem.SetCheckpoint(SceneManager.GetActiveScene().name,
            new Vector2(checkpointPosition.x, checkpointPosition.y));
    }

    private IEnumerator FlashDuringInvulnerability()
    {
        bool visible = true;
        float endTime = Time.unscaledTime + invulnerabilitySeconds;
        while (invulnerable && Time.unscaledTime < endTime)
        {
            visible = !visible;
            if (visualRenderers != null)
            {
                foreach (SpriteRenderer renderer in visualRenderers)
                    if (renderer != null) renderer.enabled = visible;
            }
            yield return new WaitForSecondsRealtime(0.09f);
        }
        if (visualRenderers != null)
        {
            foreach (SpriteRenderer renderer in visualRenderers)
                if (renderer != null) renderer.enabled = true;
        }
    }
    private void EndInvulnerability()
    {
        invulnerable = false;
    }

    private void UpdateLivesLabel()
    {
        if (livesLabel != null)
        {
            livesLabel.text = "LEVENS  " + livesRemaining.ToString("00") + " / " + maximumLives.ToString("00");
        }
    }
}




