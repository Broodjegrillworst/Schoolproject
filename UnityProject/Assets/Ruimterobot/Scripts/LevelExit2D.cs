using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class LevelExit2D : MonoBehaviour
{
    [SerializeField] private string nextSceneName = "Level2";
    [SerializeField] private bool locked;
    private bool isLoading;
    private SpriteRenderer spriteRenderer;
    private Color unlockedColor = Color.white;

    public bool IsLocked => locked;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            unlockedColor = spriteRenderer.color;
        UpdateAppearance();
    }

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (locked || isLoading || other.GetComponentInParent<PlayerController2D>() == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            Debug.LogError("Vul bij LevelExit2D de naam van de volgende scene in.", this);
            return;
        }

        isLoading = true;
        Time.timeScale = 1f;
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene.StartsWith("Level") && int.TryParse(currentScene.Substring(5), out int levelNumber))
        {
            ProfileSaveSystem.RecordLevelUnlocked(Mathf.Min(3, levelNumber + 1));
            ProfileSaveSystem.ClearCheckpoint();
        }
        if (nextSceneName == "Victory")
        {
            int score = GameSession.CrystalsCollected * 10 + RunStatistics.EnemiesDefeated * 25 + RunStatistics.BossesDefeated * 100;
            RunStatistics.CompleteRun(score);
            ProfileSaveSystem.ClearCheckpoint();
        }
        ScreenTransition2D.LoadScene(nextSceneName);
    }

    public void SetLocked(bool value)
    {
        locked = value;
        UpdateAppearance();
    }

    private void UpdateAppearance()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = locked ? new Color(0.42f, 0.48f, 0.58f) : unlockedColor;
    }
}

