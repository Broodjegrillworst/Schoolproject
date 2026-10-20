using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class RuimterobotGameplayTests : InputTestFixture
{
    private Keyboard keyboard;

    [UnitySetUp]
    public IEnumerator SetUpLevel()
    {
        Time.timeScale = 1f;
        GameSession.StartNewGame();
        ProfileSaveSystem.MarkTutorialComplete();
        yield return LoadScene("Level1");
        yield return new WaitForSeconds(0.2f);
    }

    [UnityTearDown]
    public IEnumerator RestoreTimeScale()
    {
        Time.timeScale = 1f;
        yield return null;
    }

    [UnityTest]
    public IEnumerator KeyboardMovesAndJumpsPlayer()
    {
        keyboard = InputSystem.AddDevice<Keyboard>();
        yield return null;
        GameObject player = GameObject.Find("PulseHero");
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();

        Press(keyboard.spaceKey);
        Press(keyboard.dKey);
        yield return null;
        yield return new WaitForFixedUpdate();
        Assert.Greater(body.linearVelocity.y, 0.1f, "Spatie hoort de held vanaf de grond te laten springen.");
        Assert.Greater(body.linearVelocity.x, 0.1f, "D hoort de held naar rechts te bewegen.");
    }

    [UnityTest]
    public IEnumerator DashMovesPulseWithABurstOfSpeed()
    {
        keyboard = InputSystem.AddDevice<Keyboard>();
        yield return null;
        GameObject player = GameObject.Find("PulseHero");
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();

        Press(keyboard.leftShiftKey);
        yield return null;
        yield return new WaitForFixedUpdate();

        Assert.Greater(body.linearVelocity.x, 10f, "SHIFT hoort een snelle dash naar voren te geven.");
    }

    [UnityTest]
    public IEnumerator PlayerCanDoubleJumpWhileAirborne()
    {
        keyboard = InputSystem.AddDevice<Keyboard>();
        yield return null;
        GameObject player = GameObject.Find("PulseHero");
        PlayerController2D controller = player.GetComponent<PlayerController2D>();
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();

        Press(keyboard.spaceKey);
        yield return null;
        yield return new WaitForFixedUpdate();
        float firstJumpVelocity = body.linearVelocity.y;
        Assert.Greater(firstJumpVelocity, 1f, "De eerste druk op spatie hoort te springen.");

        controller.RequestJump();
        yield return null;
        yield return new WaitForFixedUpdate();

        Assert.Greater(body.linearVelocity.y, firstJumpVelocity - 2f,
            "De tweede sprongknop hoort ook in de lucht opnieuw omhoog te laten springen.");
    }

    [UnityTest]
    public IEnumerator StartButtonLoadsLevelOne()
    {
        yield return LoadScene("MainMenu");
        Button startButton = GameObject.Find("StartKnop").GetComponent<Button>();
        Assert.Greater(startButton.onClick.GetPersistentEventCount(), 0, "De startknop moet een opgeslagen klikactie hebben.");

        startButton.onClick.Invoke();
        yield return WaitForScene("Level1");

        Assert.AreEqual("Level1", SceneManager.GetActiveScene().name);
    }

    [UnityTest]
    public IEnumerator SettingsPanelChangesAndRemembersMusicVolume()
    {
        yield return LoadScene("MainMenu");
        ArcadeAudioManager audio = ArcadeAudioManager.Instance;
        Assert.IsNotNull(audio);
        GameObject settingsPanel = null;
        foreach (Transform child in GameObject.Find("Canvas").GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "SettingsPanel")
                settingsPanel = child.gameObject;
        }
        Assert.IsNotNull(settingsPanel);
        Button settingsButton = GameObject.Find("SettingsKnop").GetComponent<Button>();
        settingsButton.onClick.Invoke();
        yield return null;

        Assert.IsTrue(settingsPanel.activeSelf);
        Slider volume = GameObject.Find("MusicVolumeSlider").GetComponent<Slider>();
        float original = audio.MusicVolume;
        volume.value = 0.61f;
        Assert.That(audio.MusicVolume, Is.EqualTo(0.61f).Within(0.001f));
        Assert.That(PlayerPrefs.GetFloat("Pulse.City.MusicVolume"), Is.EqualTo(0.61f).Within(0.001f));
        volume.value = original;

        GameObject.Find("SettingsSluitenKnop").GetComponent<Button>().onClick.Invoke();
        yield return null;
        Assert.IsFalse(settingsPanel.activeSelf);
    }

    [UnityTest]
    public IEnumerator PauseButtonsPauseAndResume()
    {
        PauseMenuController pauseMenu = GameObject.Find("PauseController").GetComponent<PauseMenuController>();
        GameObject pausePanel = pauseMenu.PausePanel;
        pauseMenu.Pause();
        Assert.AreEqual(0f, Time.timeScale);
        Assert.IsTrue(pausePanel.activeSelf);

        Button resumeButton = null;
        foreach (Button candidate in pausePanel.GetComponentsInChildren<Button>(true))
        {
            if (candidate.name == "HervatKnop") resumeButton = candidate;
        }
        Assert.IsNotNull(resumeButton, "Het pauzepaneel moet een knop hebben om verder te spelen.");
        resumeButton.onClick.Invoke();
        yield return null;

        Assert.AreEqual(1f, Time.timeScale);
        Assert.IsFalse(pausePanel.activeSelf);
    }

    [UnityTest]
    public IEnumerator FallingRespawnsAtCheckpointAndCostsALife()
    {
        GameObject player = GameObject.Find("PulseHero");
        PlayerHealth health = player.GetComponent<PlayerHealth>();
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        Vector3 checkpoint = new Vector3(-4f, -2.08f, 0f);
        health.SetCheckpoint(checkpoint);
        body.position = new Vector2(-4f, -8f);
        Physics2D.SyncTransforms();

        yield return new WaitForFixedUpdate();
        yield return null;

        Assert.AreEqual(ProfileSaveSystem.StartingLives(3) - 1, health.LivesRemaining);
        Assert.IsTrue(GameObject.Find("Levens").GetComponent<Text>().text.StartsWith("VITALS"));
        Assert.That(body.position.x, Is.EqualTo(checkpoint.x).Within(0.05f));
        Assert.That(body.position.y, Is.EqualTo(checkpoint.y).Within(0.05f));
    }

    [UnityTest]
    public IEnumerator EnergyAndHeroTokenPickupsUpdateHud()
    {
        GameObject player = GameObject.Find("PulseHero");
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        GameObject crystal = GameObject.Find("Energiefragment");
        body.position = crystal.transform.position;
        Physics2D.SyncTransforms();
        yield return new WaitForFixedUpdate();
        yield return null;

        Assert.AreEqual(1, GameSession.CrystalsCollected);
        Assert.AreEqual("SHARDS  01", GameObject.Find("EnergieScore").GetComponent<Text>().text);

        GameObject life = GameObject.Find("Heldentoken");
        body.position = life.transform.position;
        Physics2D.SyncTransforms();
        yield return new WaitForFixedUpdate();
        yield return null;

        Assert.IsTrue(GameObject.Find("Levens").GetComponent<Text>().text.StartsWith("VITALS"));
    }

    [UnityTest]
    public IEnumerator BossGuardsExitUntilDefeated()
    {
        BossController2D boss = GameObject.Find("TITAN").GetComponent<BossController2D>();
        LevelExit2D exit = GameObject.Find("Uitgang").GetComponent<LevelExit2D>();
        Assert.IsTrue(exit.IsLocked, "De uitgang hoort dicht te blijven zolang de eindbaas leeft.");
        Assert.AreEqual(Mathf.Max(1, Mathf.RoundToInt(7 * ProfileSaveSystem.BossHealthMultiplier)), boss.MaximumHealth);

        boss.TakeDamage(1);
        Assert.AreEqual(3, boss.CurrentHealth);
        Assert.IsTrue(exit.IsLocked);

        boss.TakeDamage(10);
        Assert.IsTrue(boss.IsDefeated);
        Assert.IsFalse(exit.IsLocked, "Na het verslaan van de eindbaas hoort de uitgang open te gaan.");
        yield return null;
    }

    [UnityTest]
    public IEnumerator OverdriveUpgradeActivatesInLevelTwo()
    {
        GameSession.StartNewGame();
        yield return LoadScene("Level2");
        GameObject playerObject = GameObject.Find("PulseHero");
        PlayerController2D player = playerObject.GetComponent<PlayerController2D>();
        Rigidbody2D body = playerObject.GetComponent<Rigidbody2D>();
        GameObject core = GameObject.Find("OverdriveUpgrade");
        Assert.IsNotNull(core);
        Assert.IsFalse(player.HasRapidFire);

        body.position = core.transform.position;
        Physics2D.SyncTransforms();
        yield return new WaitForFixedUpdate();
        yield return null;

        Assert.IsTrue(player.HasRapidFire, "De krachtkern hoort tijdelijk sneller schieten te geven.");
        Assert.IsNull(GameObject.Find("OverdriveUpgrade"), "De verzamelde upgrade hoort te verdwijnen.");
    }

    [UnityTest]
    public IEnumerator MovingPlatformChangesPositionInLevelTwo()
    {
        GameSession.StartNewGame();
        yield return LoadScene("Level2");
        MovingPlatform2D platform = UnityEngine.Object.FindAnyObjectByType<MovingPlatform2D>();
        Assert.IsNotNull(platform);
        Vector3 startPosition = platform.transform.position;
        yield return new WaitForSeconds(0.6f);
        Assert.Greater(Vector3.Distance(startPosition, platform.transform.position), 0.1f,
            "Het platform in level 2 hoort tussen twee punten te bewegen.");
    }

    [UnityTest]
    public IEnumerator ExitsReachAllThreeLevelsAndVictory()
    {
        yield return ReachExit("Level1", "Level2");
        yield return ReachExit("Level2", "Level3");
        yield return ReachExit("Level3", "Victory");
        Assert.AreEqual("Victory", SceneManager.GetActiveScene().name);
    }

    [UnityTest]
    public IEnumerator GameOverOffersRestartAndReturnsToPlaying()
    {
        PlayerHealth health = GameObject.Find("PulseHero").GetComponent<PlayerHealth>();
        health.TakeDamage();
        yield return new WaitForSecondsRealtime(1.1f);
        health.TakeDamage();
        yield return new WaitForSecondsRealtime(1.1f);
        health.TakeDamage();

        Assert.AreEqual(0f, Time.timeScale);
        Assert.IsTrue(GameObject.Find("GameOverPaneel").activeSelf);
        GameObject.Find("OpnieuwNaGameOverKnop").GetComponent<Button>().onClick.Invoke();
        yield return WaitForScene("Level1");

        Assert.AreEqual(1f, Time.timeScale);
        Assert.AreEqual("Level1", SceneManager.GetActiveScene().name);
    }

    private static IEnumerator ReachExit(string levelName, string nextLevelName)
    {
        yield return LoadScene(levelName);
        BossController2D boss = UnityEngine.Object.FindAnyObjectByType<BossController2D>();
        Assert.IsNotNull(boss, "In " + levelName + " hoort een eindbaas te staan.");
        boss.TakeDamage(boss.MaximumHealth);
        GameObject player = GameObject.Find("PulseHero");
        GameObject exit = GameObject.Find("Uitgang");
        player.GetComponent<Rigidbody2D>().position = exit.transform.position;
        Physics2D.SyncTransforms();
        yield return new WaitForFixedUpdate();
        yield return WaitForScene(nextLevelName);
        Assert.AreEqual(nextLevelName, SceneManager.GetActiveScene().name, "De uitgang van " + levelName + " hoort " + nextLevelName + " te laden.");
    }

    private static IEnumerator WaitForScene(string sceneName)
    {
        float deadline = Time.realtimeSinceStartup + 3f;
        while (SceneManager.GetActiveScene().name != sceneName && Time.realtimeSinceStartup < deadline)
            yield return null;
    }

    private static IEnumerator LoadScene(string sceneName)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        yield return operation;
        yield return null;
    }
}
