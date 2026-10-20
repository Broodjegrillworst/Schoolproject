using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private string firstLevelSceneName = "Level1";
    [SerializeField] private string menuSceneName = "MainMenu";
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider effectsSlider;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private GameObject upgradePanel;
    [SerializeField] private GameObject statisticsPanel;
    [SerializeField] private GameObject achievementsPanel;
    [SerializeField] private Text difficultyLabel;
    [SerializeField] private Text upgradeInfo;
    [SerializeField] private Text statisticsInfo;
    [SerializeField] private Text achievementsInfo;
    private GameObject menuSelectionBeforeSettings;
    private GameObject activeOverlay;

    private void Start()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
        if (upgradePanel != null) upgradePanel.SetActive(false);
        if (statisticsPanel != null) statisticsPanel.SetActive(false);
        if (achievementsPanel != null) achievementsPanel.SetActive(false);
        RefreshMenuPanels();

        Canvas menuCanvas = FindAnyObjectByType<Canvas>();
        SelectFirstButton(menuCanvas != null ? menuCanvas.gameObject : null);

        ArcadeAudioManager audioManager = ArcadeAudioManager.Instance;
        if (audioManager == null)
            return;

        if (musicSlider != null)
        {
            musicSlider.SetValueWithoutNotify(audioManager.MusicVolume);
            musicSlider.onValueChanged.AddListener(audioManager.SetMusicVolume);
        }
        if (effectsSlider != null)
        {
            effectsSlider.SetValueWithoutNotify(audioManager.EffectsVolume);
            effectsSlider.onValueChanged.AddListener(audioManager.SetEffectsVolume);
        }
        if (fullscreenToggle != null)
        {
            fullscreenToggle.SetIsOnWithoutNotify(audioManager.Fullscreen);
            fullscreenToggle.onValueChanged.AddListener(audioManager.SetFullscreen);
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (activeOverlay != null && activeOverlay.activeSelf)
        {
            Gamepad settingsGamepad = Gamepad.current;
            if ((keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                || (settingsGamepad != null && settingsGamepad.buttonEast.wasPressedThisFrame))
                CloseSettings();
            return;
        }

        Gamepad gamepad = Gamepad.current;
        bool startPressed = (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
            || (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame);
        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene == "MainMenu" && startPressed)
        {
            StartGame();
        }
        else if (currentScene == "Victory" && startPressed)
        {
            ReturnToMenu();
        }
    }

    public void StartGame()
    {
        if (ArcadeAudioManager.Instance != null)
            ArcadeAudioManager.Instance.PlayUi();
        Time.timeScale = 1f;
        GameSession.StartNewGame();
        ScreenTransition2D.LoadScene(firstLevelSceneName);
    }

    public void ContinueGame()
    {
        if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayUi();
        Time.timeScale = 1f;
        GameSession.StartContinuation();
        ScreenTransition2D.LoadScene(ProfileSaveSystem.GetContinueSceneName());
    }

    public void ReturnToMenu()
    {
        if (ArcadeAudioManager.Instance != null)
            ArcadeAudioManager.Instance.PlayUi();
        Time.timeScale = 1f;
        ScreenTransition2D.LoadScene(menuSceneName);
    }

    public void OpenSettings()
    {
        OpenOverlay(settingsPanel);
    }

    public void CloseSettings()
    {
        if (activeOverlay != null) activeOverlay.SetActive(false);
        activeOverlay = null;
        if (EventSystem.current != null)
        {
            GameObject selection = menuSelectionBeforeSettings;
            if (selection == null || !selection.activeInHierarchy)
            {
                Canvas menuCanvas = FindAnyObjectByType<Canvas>();
                selection = FindFirstSelectable(menuCanvas != null ? menuCanvas.gameObject : null);
            }
            EventSystem.current.SetSelectedGameObject(selection);
        }
        if (ArcadeAudioManager.Instance != null)
            ArcadeAudioManager.Instance.PlayUi();
    }

    public void OpenUpgrades() => OpenOverlay(upgradePanel);
    public void OpenStatistics() { RefreshStatistics(); OpenOverlay(statisticsPanel); }
    public void OpenAchievements() { RefreshAchievements(); OpenOverlay(achievementsPanel); }

    public void CycleDifficulty()
    {
        ProfileSaveSystem.CycleDifficulty();
        RefreshMenuPanels();
        if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayUi();
    }

    public void BuyPowerUpgrade()
    {
        if (ProfileSaveSystem.TryPurchasePowerRank(out int rank, out int shards))
        {
            AchievementSystem.OnUpgradePurchased();
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayPickup();
            if (upgradeInfo != null) upgradeInfo.text = GetUpgradeText(rank, shards);
        }
        else if (upgradeInfo != null)
        {
            upgradeInfo.text = ProfileSaveSystem.PowerRank >= 3
                ? "ALLE PULSE-KRACHTEN ZIJN VOLLEDIG ONTWAAKT."
                : "NIET GENOEG SHARDS. VERZAMEL MEER IN DE LEVELS.";
        }
    }

    private void OpenOverlay(GameObject panel)
    {
        if (panel == null) return;
        if (EventSystem.current != null)
            menuSelectionBeforeSettings = EventSystem.current.currentSelectedGameObject;
        if (activeOverlay != null) activeOverlay.SetActive(false);
        activeOverlay = panel;
        activeOverlay.SetActive(true);
        SelectFirstButton(activeOverlay);
        if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayUi();
    }

    private void RefreshMenuPanels()
    {
        if (difficultyLabel != null) difficultyLabel.text = "MOEILIJKHEID  //  " + ProfileSaveSystem.GetDifficultyLabel();
        if (upgradeInfo != null) upgradeInfo.text = GetUpgradeText(ProfileSaveSystem.PowerRank, ProfileSaveSystem.BankedShards);
        RefreshStatistics();
        RefreshAchievements();
    }

    private void RefreshStatistics()
    {
        if (statisticsInfo == null) return;
        ProfileSaveSystem.ProfileData profile = ProfileSaveSystem.Data;
        statisticsInfo.text = "RUNS VOLTOOID  " + profile.totalRuns +
            "\nBESTE SCORE  " + profile.bestScore +
            "\nLAATSTE RUN  " + FormatTime(profile.lastRunSeconds) +
            "\nSHARDS  " + profile.lastRunShards + "     VIJANDEN  " + profile.lastRunEnemies +
            "\nBAZEN  " + profile.lastRunBosses + "     UITVALMOMENTEN  " + profile.lastRunDeaths +
            "\nBANK  " + ProfileSaveSystem.BankedShards + " SHARDS";
    }

    private void RefreshAchievements()
    {
        if (achievementsInfo == null) return;
        string[] ids = { AchievementSystem.FirstShard, AchievementSystem.FirstEnemy, AchievementSystem.FirstBoss,
            AchievementSystem.UpgradeHero, AchievementSystem.RiftClosed, AchievementSystem.CleanRun,
            AchievementSystem.ShardCollector };
        System.Text.StringBuilder text = new System.Text.StringBuilder();
        foreach (string id in ids)
            text.Append(ProfileSaveSystem.HasAchievement(id) ? "◆  " : "◇  ")
                .Append(AchievementSystem.GetDisplayName(id)).Append(" — ")
                .Append(AchievementSystem.GetDescription(id)).Append('\n');
        achievementsInfo.text = text.ToString();
    }

    private static string GetUpgradeText(int rank, int shards)
    {
        string reward = rank == 0 ? "RANG I — ontgrendel de Arc-golf (toets 2)." :
            rank == 1 ? "RANG II — ontgrendel de Nova-aanval (toets 3)." :
            rank == 2 ? "RANG III — versterk Pulse en Nova." : "MAXIMALE KRACHT — alle technieken zijn beschikbaar.";
        string price = rank >= 3 ? "" : "\nVOLGENDE PRIJS  " + ProfileSaveSystem.NextPowerPrice + " SHARDS";
        return "PULSE-KERN  //  RANG " + rank + "/3\n" + reward + price + "\nBANK  " + shards + " SHARDS";
    }

    private static string FormatTime(float seconds)
    {
        int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
        return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
    }

    private static void SelectFirstButton(GameObject root)
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(FindFirstSelectable(root));
    }

    private static GameObject FindFirstSelectable(GameObject root)
    {
        if (root == null)
            return null;

        Selectable[] selectables = root.GetComponentsInChildren<Selectable>();
        for (int i = 0; i < selectables.Length; i++)
            if (selectables[i] != null && selectables[i].gameObject.activeInHierarchy && selectables[i].interactable)
                return selectables[i].gameObject;
        return null;
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
