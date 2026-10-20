using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class RuimterobotDemoBuilder
{
    private const string Root = "Assets/Ruimterobot";
    private const string ArtFolder = Root + "/Art";
    private const string PrefabFolder = Root + "/Prefabs";
    private const string SceneFolder = Root + "/Scenes";
    private const string SquarePath = ArtFolder + "/Square.png";
    private const string GlowPath = ArtFolder + "/PulseSoftGlow.png";
    private const string ProjectilePath = PrefabFolder + "/Projectile.prefab";
    private const string BossProjectilePath = PrefabFolder + "/BossProjectile.prefab";
    private const string HeroSpritePath = ArtFolder + "/PulseHero.png";
    private const string WeaponSpritePath = ArtFolder + "/EnergyBolt.png";
    private const string OverdriveSpritePath = ArtFolder + "/UpgradeOverdrive.png";
    private const string SpikeSpritePath = ArtFolder + "/HazardSpikePro.png";
    private static readonly string[] BossSpritePaths =
    {
        ArtFolder + "/BossTitan.png",
        ArtFolder + "/BossPyra.png",
        ArtFolder + "/BossNull.png"
    };

    private static Sprite squareSprite;
    private static Sprite softGlowSprite;
    private static Sprite heroSprite;
    private static Sprite energyBoltSprite;
    private static Sprite overdriveSprite;
    private static Sprite spikeSprite;
    private static Sprite[] bossSprites;
    private static int groundLayer;

    [MenuItem("PULSE/Bouw superhelden-platformgame")]
    public static void BuildDemoGame()
    {
        EnsureFolder(ArtFolder);
        EnsureFolder(PrefabFolder);
        EnsureFolder(SceneFolder);

        string[] sceneNames = { "MainMenu", "Level1", "Level2", "Level3", "Victory" };
        foreach (string sceneName in sceneNames)
        {
            if (File.Exists(SceneFolder + "/" + sceneName + ".unity"))
            {
                Debug.LogError("De PULSE-demo bestaat al. Verwijder of hernoem eerst de bestanden in " + SceneFolder + ".");
                return;
            }
        }

        groundLayer = GetOrCreateGroundLayer();
        squareSprite = CreateSquareSprite();
        softGlowSprite = CreateGlowSprite();
        LoadCharacterSprites();
        GameObject projectilePrefab = CreateProjectilePrefab();
        GameObject bossProjectilePrefab = CreateBossProjectilePrefab();

        CreateMenuScene();
        CreateLevelScene(1, "Level2", projectilePrefab, bossProjectilePrefab);
        CreateLevelScene(2, "Level3", projectilePrefab, bossProjectilePrefab);
        CreateLevelScene(3, "Victory", projectilePrefab, bossProjectilePrefab);
        CreateVictoryScene();
        ConfigureBuildScenes(sceneNames);
        ConfigureProductSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        RuimterobotProfessionalStyler.ApplyToBuiltScenes();
        ValidateDemoScenes(sceneNames);

        EditorSceneManager.OpenScene(SceneFolder + "/MainMenu.unity", OpenSceneMode.Single);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("PULSE: Rise of the Rift is opgebouwd. Open MainMenu en begin de heldenrun.");
    }

    [MenuItem("PULSE/Herbouw en exporteer voor Windows")]
    public static void RebuildAndBuildWindows()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string[] sceneNames = { "MainMenu", "Level1", "Level2", "Level3", "Victory" };
        string backupFolder = Path.Combine(projectRoot, "Library", "PulseSceneBackup_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        Directory.CreateDirectory(backupFolder);

        foreach (string sceneName in sceneNames)
        {
            string scenePath = Path.Combine(projectRoot, SceneFolder, sceneName + ".unity");
            if (File.Exists(scenePath))
                File.Copy(scenePath, Path.Combine(backupFolder, sceneName + ".unity"), true);
        }

        EnsureFolder(ArtFolder);
        EnsureFolder(PrefabFolder);
        EnsureFolder(SceneFolder);
        groundLayer = GetOrCreateGroundLayer();
        squareSprite = CreateSquareSprite();
        softGlowSprite = CreateGlowSprite();
        LoadCharacterSprites();
        GameObject projectilePrefab = CreateProjectilePrefab();
        GameObject bossProjectilePrefab = CreateBossProjectilePrefab();

        CreateMenuScene();
        CreateLevelScene(1, "Level2", projectilePrefab, bossProjectilePrefab);
        CreateLevelScene(2, "Level3", projectilePrefab, bossProjectilePrefab);
        CreateLevelScene(3, "Victory", projectilePrefab, bossProjectilePrefab);
        CreateVictoryScene();
        ConfigureBuildScenes(sceneNames);
        ConfigureProductSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        RuimterobotProfessionalStyler.ApplyToBuiltScenes();
        ValidateDemoScenes(sceneNames);
        EditorSceneManager.OpenScene(SceneFolder + "/MainMenu.unity", OpenSceneMode.Single);
        BuildWindowsStandalone();
        Debug.Log("PULSE is opnieuw opgebouwd, gecontroleerd en geëxporteerd voor Windows. De vorige scenes staan in " + backupFolder);
    }

    private static void BuildWindowsStandalone()
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
            throw new System.InvalidOperationException("Windows x64 build support ontbreekt in deze Unity-installatie.");

        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string outputFolder = Path.Combine(projectRoot, "Builds", "PULSE Rise of the Rift Windows");
        Directory.CreateDirectory(outputFolder);
        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = Path.Combine(outputFolder, "PULSE.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
            throw new System.InvalidOperationException("Windows-export mislukt: " + report.summary.result + ". Bekijk Logs/Editor.log.");

        Debug.Log("Windows-export klaar: " + options.locationPathName + " (" + report.summary.totalSize + " bytes).");
    }

    private static void ConfigureProductSettings()
    {
        PlayerSettings.companyName = "Smart ICT Engineer";
        PlayerSettings.productName = "PULSE: Rise of the Rift";
        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.runInBackground = true;
    }

    private static void ValidateDemoScenes(string[] sceneNames)
    {
        foreach (string sceneName in sceneNames)
        {
            Scene scene = EditorSceneManager.OpenScene(SceneFolder + "/" + sceneName + ".unity", OpenSceneMode.Single);
            if (!scene.IsValid() || !scene.isLoaded)
                throw new System.InvalidOperationException("Scene kon niet worden geopend: " + sceneName);

            if (sceneName.StartsWith("Level"))
            {
                Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include);
                if (Object.FindAnyObjectByType<PlayerController2D>() == null ||
                    Object.FindAnyObjectByType<HeroPowerController2D>() == null ||
                    Object.FindAnyObjectByType<PlayerHealth>() == null ||
                    Object.FindAnyObjectByType<CameraFollow2D>() == null ||
                    Object.FindAnyObjectByType<TutorialOverlayController>() == null ||
                    Object.FindAnyObjectByType<GroundShadow2D>() == null ||
                    Object.FindAnyObjectByType<RangedEnemy2D>() == null ||
                    Object.FindAnyObjectByType<PauseMenuController>() == null ||
                    Object.FindAnyObjectByType<BossController2D>() == null ||
                    Object.FindAnyObjectByType<LevelExit2D>() == null ||
                    Object.FindAnyObjectByType<ArcadeAudioManager>() == null ||
                    Object.FindAnyObjectByType<EventSystem>() == null ||
                    Object.FindObjectsByType<DamagePlayerOnTouch>().Length == 0 ||
                    Object.FindObjectsByType<CrystalPickup2D>().Length == 0 ||
                    (Object.FindObjectsByType<HeroUpgradePickup2D>().Length == 0 &&
                     Object.FindObjectsByType<RapidFirePickup2D>().Length == 0) ||
                    Object.FindAnyObjectByType<LifePickup2D>() == null ||
                    Object.FindAnyObjectByType<Checkpoint2D>() == null ||
                    buttons.Length < 5 || buttons.Any(button => button.onClick.GetPersistentEventCount() == 0))
                    throw new System.InvalidOperationException("Een verplicht gameplay-onderdeel ontbreekt in " + sceneName);

                if (sceneName == "Level2" &&
                    (Object.FindAnyObjectByType<MovingPlatform2D>() == null ||
                     Object.FindAnyObjectByType<RapidFirePickup2D>() == null ||
                     Object.FindObjectsByType<TimedLaserHazard2D>().Length < 2 ||
                     Object.FindAnyObjectByType<PressurePlate2D>() == null))
                    throw new System.InvalidOperationException("De bewegende platformroute, dubbele laser of drukplaat ontbreekt in Level2.");

                if (sceneName == "Level1" &&
                    (Object.FindAnyObjectByType<CollapsingPlatform2D>() == null ||
                     Object.FindAnyObjectByType<FallingHazard2D>() == null ||
                     Object.FindAnyObjectByType<RotatingHazard2D>() == null ||
                     Object.FindAnyObjectByType<SwoopEnemy2D>() == null ||
                     Object.FindAnyObjectByType<TimedLaserHazard2D>() == null))
                    throw new System.InvalidOperationException("De rooftoptimingroute mist een van de nieuwe obstakel- of vijandtypes.");

                if (sceneName == "Level3" &&
                    (Object.FindObjectsByType<TimedLaserHazard2D>().Length < 2 ||
                     Object.FindAnyObjectByType<PressurePlate2D>() == null ||
                     Object.FindAnyObjectByType<SwoopEnemy2D>() == null))
                    throw new System.InvalidOperationException("De rifttoren mist de laserketen, drukplaat of luchtjager.");

                if (!SceneContainsObject("Depth Far Architecture") ||
                    !SceneContainsObject("Depth Mid Architecture") ||
                    !SceneContainsObject("Depth Foreground Structures"))
                    throw new System.InvalidOperationException("Een van de parallax- of voorgrondlagen ontbreekt in " + sceneName);
            }
            else if (Object.FindAnyObjectByType<MainMenuController>() == null ||
                     Object.FindAnyObjectByType<ArcadeAudioManager>() == null ||

                     Object.FindAnyObjectByType<EventSystem>() == null ||
                     Object.FindObjectsByType<Button>(FindObjectsInactive.Include)
                         .All(button => button.onClick.GetPersistentEventCount() == 0))
            {
                throw new System.InvalidOperationException("Menu of klikbesturing ontbreekt in " + sceneName);
            }

            if (sceneName == "MainMenu" &&
                (Object.FindObjectsByType<Slider>(FindObjectsInactive.Include).Length < 2 ||
                 Object.FindObjectsByType<Toggle>(FindObjectsInactive.Include).Length < 1 ||
                 GameObject.Find("MenuPulseHero") == null ||
                 !SceneContainsObject("UpgradePanel") ||
                 !SceneContainsObject("StatisticsPanel") ||
                 !SceneContainsObject("AchievementsPanel") ||
                 !SceneContainsObject("DifficultyCycleKnop")))
                throw new System.InvalidOperationException("Startmenu, instellingen, upgrades, statistieken of achievements ontbreken.");

            if (sceneName.StartsWith("Level") &&
                (GameObject.Find("AchievementToast") == null || GameObject.Find("HeroEnergyBar") == null))
                throw new System.InvalidOperationException("Krachtmeter of achievement feedback ontbreekt in " + sceneName);
        }
    }

    private static bool SceneContainsObject(string objectName)
    {
        return Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include)
            .Any(gameObject => gameObject != null && gameObject.name == objectName);
    }

    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }

    private static int GetOrCreateGroundLayer()
    {
        Object[] tagAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (tagAssets == null || tagAssets.Length == 0)
        {
            Debug.LogWarning("Ground-layer niet gevonden; de standaardlaag wordt gebruikt.");
            return 0;
        }

        SerializedObject tagManager = new SerializedObject(tagAssets[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");
        for (int i = 8; i < layers.arraySize; i++)
        {
            if (layers.GetArrayElementAtIndex(i).stringValue == "Ground")
            {
                return i;
            }
        }

        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty layer = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(layer.stringValue))
            {
                layer.stringValue = "Ground";
                tagManager.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssets();
                return i;
            }
        }

        Debug.LogWarning("Er is geen vrije Unity-layer; de standaardlaag wordt gebruikt.");
        return 0;
    }

    private static Sprite CreateSquareSprite()
    {
        if (!File.Exists(SquarePath))
        {
            Texture2D texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    texture.SetPixel(x, y, Color.white);
                }
            }
            texture.Apply();
            File.WriteAllBytes(SquarePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }

        AssetDatabase.ImportAsset(SquarePath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(SquarePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SquarePath);
        if (sprite == null)
        {
            throw new System.InvalidOperationException("De demo-sprite kon niet worden aangemaakt.");
        }
        return sprite;
    }

    private static Sprite CreateGlowSprite()
    {
        if (!File.Exists(GlowPath))
        {
            const int size = 96;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float radius = Vector2.Distance(new Vector2(x, y), center) / (size * 0.5f);
                float alpha = Mathf.Pow(Mathf.Clamp01(1f - radius), 2.3f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
            texture.Apply();
            File.WriteAllBytes(GlowPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }

        AssetDatabase.ImportAsset(GlowPath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(GlowPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
        Sprite glow = AssetDatabase.LoadAssetAtPath<Sprite>(GlowPath);
        if (glow == null) throw new System.InvalidOperationException("De atmosferische gloed-sprite kon niet worden geladen.");
        return glow;
    }

    private static void LoadCharacterSprites()
    {
        heroSprite = LoadSpriteAsset(HeroSpritePath, 1024f);
        energyBoltSprite = LoadSpriteAsset(WeaponSpritePath, 512f);
        overdriveSprite = LoadSpriteAsset(OverdriveSpritePath, 512f);
        spikeSprite = LoadSpriteAsset(SpikeSpritePath, 256f);
        bossSprites = new Sprite[3];
        for (int i = 0; i < bossSprites.Length; i++)
            bossSprites[i] = LoadSpriteAsset(BossSpritePaths[i], 240f);
    }

    private static Sprite LoadSpriteAsset(string path, float pixelsPerUnit)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            throw new System.InvalidOperationException("Afbeelding kan niet als Unity-sprite worden geïmporteerd: " + path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.maxTextureSize = 4096;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            throw new System.InvalidOperationException("Sprite kon niet worden geladen: " + path);
        return sprite;
    }

    private static GameObject CreateProjectilePrefab()
    {
        GameObject projectile = CreateSpriteObject("Schot", Vector3.zero, new Vector3(0.28f, 0.16f, 1f), new Color(1f, 0.85f, 0.2f), 5);
        Rigidbody2D body = projectile.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.freezeRotation = true;
        CircleCollider2D collider = projectile.AddComponent<CircleCollider2D>();
        collider.radius = 0.5f;
        collider.isTrigger = true;
        projectile.AddComponent<Projectile2D>();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(projectile, ProjectilePath);
        Object.DestroyImmediate(projectile);
        AssetDatabase.Refresh();
        return prefab;
    }

    private static GameObject CreateBossProjectilePrefab()
    {
        GameObject projectile = CreateSpriteObject("Baasschot", Vector3.zero,
            new Vector3(0.3f, 0.3f, 1f), new Color(1f, 0.28f, 0.18f), 7);
        Rigidbody2D body = projectile.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        CircleCollider2D collider = projectile.AddComponent<CircleCollider2D>();
        collider.radius = 0.5f;
        collider.isTrigger = true;
        projectile.AddComponent<BossProjectile2D>();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(projectile, BossProjectilePath);
        Object.DestroyImmediate(projectile);
        AssetDatabase.Refresh();
        return prefab;
    }

    private static void CreateMenuScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateCamera(new Color(0.018f, 0.026f, 0.09f));

        CreateAudioManager();
        Canvas canvas = CreateCanvas();
        Image background = CreatePanel("MenuOverlay", canvas.transform, new Color(0.018f, 0.026f, 0.09f, 0.12f), true);
        background.transform.SetAsFirstSibling();
        Text title = CreateText("Titel", canvas.transform, "PULSE", 80, new Color(1f, 0.86f, 0.25f),
            TextAnchor.MiddleCenter, new Vector2(-505f, 385f), new Vector2(900f, 100f));
        CreateText("Subtitel", canvas.transform, "RISE OF THE RIFT  //  EEN ORIGINELE SUPERHELDENSAGA", 20,
            new Color(0.58f, 0.78f, 1f), TextAnchor.MiddleCenter, new Vector2(-505f, 319f), new Vector2(900f, 36f));
        Text introText = CreateText("IntroBericht", canvas.transform, "METROVILLE STAAT OP DE RAND VAN DE BREUK.", 24,
            new Color(1f, 0.6f, 0.34f), TextAnchor.MiddleCenter, new Vector2(-505f, 257f), new Vector2(900f, 48f));

        GameObject hero = new GameObject("MenuPulseHero");
        hero.transform.position = new Vector3(3.7f, -0.48f, 0f);
        hero.transform.localScale = new Vector3(2.2f, 2.2f, 1f);
        CreateChildSprite(hero.transform, "MenuPulseSprite", Vector3.zero, Vector3.one, heroSprite, Color.white, 2);

        GameObject menu = new GameObject("MenuController");
        MainMenuController controller = menu.AddComponent<MainMenuController>();
        SetString(controller, "firstLevelSceneName", "Level1");
        SetString(controller, "menuSceneName", "MainMenu");
        CreateButton("StartKnop", canvas.transform, "NIEUWE HELDENRUN", new Vector2(-510f, 157f), new Vector2(500f, 62f), controller.StartGame);
        CreateButton("ContinueKnop", canvas.transform, "GA VERDER IN METROVILLE", new Vector2(-510f, 83f), new Vector2(500f, 58f), controller.ContinueGame);
        CreateButton("UpgradeKnop", canvas.transform, "ONTWAAK JE KRACHTEN", new Vector2(-510f, 12f), new Vector2(500f, 58f), controller.OpenUpgrades);
        CreateButton("StatisticsKnop", canvas.transform, "HELDENARCHIEF", new Vector2(-510f, -59f), new Vector2(500f, 58f), controller.OpenStatistics);
        CreateButton("AchievementsKnop", canvas.transform, "PRESTATIES", new Vector2(-510f, -130f), new Vector2(500f, 58f), controller.OpenAchievements);
        CreateButton("SettingsKnop", canvas.transform, "INSTELLINGEN", new Vector2(-510f, -201f), new Vector2(500f, 58f), controller.OpenSettings);
        CreateButton("AfsluitKnop", canvas.transform, "AFSLUITEN", new Vector2(-510f, -272f), new Vector2(500f, 58f), controller.QuitGame);
        CreateText("MenuControls", canvas.transform,
            "A/D = BEWEGEN     SPATIE = DUBBELSPRONG\nSHIFT = DASH     J = KRACHT     1/2/3 = POWERS     ESC = PAUZE",
            16, new Color(0.7f, 0.84f, 1f), TextAnchor.MiddleCenter, new Vector2(-500f, -350f), new Vector2(970f, 66f));
        CreateText("CreatorCredits", canvas.transform, "MADE BY JAIDEN ASRUF", 16, new Color(1f, 0.79f, 0.4f),
            TextAnchor.MiddleRight, new Vector2(880f, -492f), new Vector2(420f, 32f));

        GameObject settingsPanel = CreateOverlay("SettingsPanel", canvas.transform, new Vector2(880f, 780f), "SettingsKaart");
        CreateText("SettingsTitel", settingsPanel.transform, "GAME-INSTELLINGEN", 38, new Color(0.1f, 0.92f, 1f),
            TextAnchor.MiddleCenter, new Vector2(0f, 285f), new Vector2(800f, 68f));
        CreateText("MusicLabel", settingsPanel.transform, "MUZIEKVOLUME", 20, Color.white, TextAnchor.MiddleCenter,
            new Vector2(0f, 190f), new Vector2(480f, 35f));
        Slider musicSlider = CreateSlider("MusicVolumeSlider", settingsPanel.transform, new Vector2(0f, 142f), 0.32f);
        CreateText("EffectsLabel", settingsPanel.transform, "GELUIDSEFFECTEN", 20, Color.white, TextAnchor.MiddleCenter,
            new Vector2(0f, 67f), new Vector2(480f, 35f));
        Slider effectsSlider = CreateSlider("EffectsVolumeSlider", settingsPanel.transform, new Vector2(0f, 19f), 0.75f);
        CreateText("FullscreenLabel", settingsPanel.transform, "VOLLEDIG SCHERM", 20, Color.white, TextAnchor.MiddleCenter,
            new Vector2(-120f, -66f), new Vector2(310f, 44f));
        Toggle fullscreenToggle = CreateToggle("FullscreenToggle", settingsPanel.transform, new Vector2(130f, -66f));
        Text difficultyLabel = CreateText("DifficultyLabel", settingsPanel.transform, "MOEILIJKHEID", 19,
            new Color(0.84f, 0.9f, 1f), TextAnchor.MiddleCenter, new Vector2(0f, -134f), new Vector2(700f, 36f));
        CreateButton("DifficultyCycleKnop", settingsPanel.transform, "WIS MOEILIJKHEID", new Vector2(0f, -184f), new Vector2(420f, 52f), controller.CycleDifficulty);
        CreateText("SettingsUitleg", settingsPanel.transform, "INSTELLINGEN WORDEN AUTOMATISCH BEWAARD.\nESC sluit dit scherm.",
            16, new Color(0.68f, 0.79f, 1f), TextAnchor.MiddleCenter, new Vector2(0f, -236f), new Vector2(740f, 58f));
        CreateButton("SettingsSluitenKnop", settingsPanel.transform, "TERUG NAAR HET MENU", new Vector2(0f, -315f),
            new Vector2(430f, 62f), controller.CloseSettings);

        GameObject upgradePanel = CreateOverlay("UpgradePanel", canvas.transform, new Vector2(880f, 670f), "UpgradeCard");
        CreateText("UpgradeTitel", upgradePanel.transform, "PULSE-KERN  //  VAARDIGHEDEN", 38, new Color(1f, 0.83f, 0.3f),
            TextAnchor.MiddleCenter, new Vector2(0f, 245f), new Vector2(780f, 60f));
        Text upgradeInfo = CreateText("UpgradeInfo", upgradePanel.transform, "", 22, Color.white,
            TextAnchor.MiddleCenter, new Vector2(0f, 100f), new Vector2(720f, 140f));
        CreateText("UpgradeDetails", upgradePanel.transform,
            "RANG I: Arc-golf  //  RANG II: Nova-kern\nRANG III: versterkte Pulse- en Nova-aanvallen\nKristallen in levels worden ook permanente upgrade-shards.",
            17, new Color(0.72f, 0.84f, 0.97f), TextAnchor.MiddleCenter, new Vector2(0f, -30f), new Vector2(720f, 96f));
        CreateButton("UpgradeBuyKnop", upgradePanel.transform, "KOOP VOLGENDE RANG", new Vector2(0f, -135f), new Vector2(450f, 60f), controller.BuyPowerUpgrade);
        CreateButton("UpgradeSluitenKnop", upgradePanel.transform, "TERUG", new Vector2(0f, -235f), new Vector2(350f, 56f), controller.CloseSettings);

        GameObject statsPanel = CreateOverlay("StatisticsPanel", canvas.transform, new Vector2(880f, 680f), "StatisticsCard");
        CreateText("StatisticsTitel", statsPanel.transform, "METROVILLE  //  HELDENARCHIEF", 38, new Color(1f, 0.83f, 0.3f),
            TextAnchor.MiddleCenter, new Vector2(0f, 245f), new Vector2(780f, 60f));
        Text statisticsInfo = CreateText("StatisticsInfo", statsPanel.transform, "", 23, Color.white,
            TextAnchor.MiddleCenter, new Vector2(0f, 35f), new Vector2(740f, 270f));
        CreateButton("StatisticsSluitenKnop", statsPanel.transform, "TERUG", new Vector2(0f, -235f), new Vector2(350f, 56f), controller.CloseSettings);

        GameObject achievementsPanel = CreateOverlay("AchievementsPanel", canvas.transform, new Vector2(1080f, 800f), "AchievementsCard");
        CreateText("AchievementsTitel", achievementsPanel.transform, "DE HELDENMUUR", 38, new Color(1f, 0.83f, 0.3f),
            TextAnchor.MiddleCenter, new Vector2(0f, 300f), new Vector2(900f, 58f));
        Text achievementsInfo = CreateText("AchievementsInfo", achievementsPanel.transform, "", 18, Color.white,
            TextAnchor.MiddleLeft, new Vector2(-420f, 20f), new Vector2(840f, 480f));
        CreateButton("AchievementsSluitenKnop", achievementsPanel.transform, "TERUG", new Vector2(0f, -330f), new Vector2(350f, 56f), controller.CloseSettings);

        SetObject(controller, "settingsPanel", settingsPanel);
        SetObject(controller, "musicSlider", musicSlider);
        SetObject(controller, "effectsSlider", effectsSlider);
        SetObject(controller, "fullscreenToggle", fullscreenToggle);
        SetObject(controller, "upgradePanel", upgradePanel);
        SetObject(controller, "statisticsPanel", statsPanel);
        SetObject(controller, "achievementsPanel", achievementsPanel);
        SetObject(controller, "difficultyLabel", difficultyLabel);
        SetObject(controller, "upgradeInfo", upgradeInfo);
        SetObject(controller, "statisticsInfo", statisticsInfo);
        SetObject(controller, "achievementsInfo", achievementsInfo);
        settingsPanel.SetActive(false);
        upgradePanel.SetActive(false);
        statsPanel.SetActive(false);
        achievementsPanel.SetActive(false);

        GameObject intro = new GameObject("MenuIntroAnimator");
        MenuIntroAnimator2D introAnimator = intro.AddComponent<MenuIntroAnimator2D>();
        introAnimator.Configure(hero.transform, title, introText);

        EditorSceneManager.SaveScene(scene, SceneFolder + "/MainMenu.unity");
    }

    private static void CreateVictoryScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateCamera(new Color(0.018f, 0.055f, 0.11f));

        CreateAudioManager();
        Canvas canvas = CreateCanvas();
        CreatePanel("Achtergrond", canvas.transform, new Color(0.035f, 0.13f, 0.11f, 1f), true);
        CreateText("Overwinning", canvas.transform, "METROVILLE IS GERED!", 50, Color.white,
            TextAnchor.MiddleCenter, new Vector2(0, 220), new Vector2(1100, 100));
        CreateText("VictoryStory", canvas.transform, "De Rift-kampioenen zijn verslagen.\nDe stad is weer van haar inwoners.",
            25, new Color(0.83f, 0.91f, 1f), TextAnchor.MiddleCenter, new Vector2(0, 126), new Vector2(1000, 92));
        Text scoreText = CreateText("Eindscore", canvas.transform, "SHARDS  00", 30, new Color(1f, 0.82f, 0.3f),
            TextAnchor.MiddleCenter, new Vector2(0, 24), new Vector2(620, 60));
        Text summaryText = CreateText("RunSummary", canvas.transform, "", 19, Color.white,
            TextAnchor.MiddleCenter, new Vector2(0, -65), new Vector2(1000, 75));
        GameObject scoreObject = new GameObject("ScoreController");
        ScoreHud scoreHud = scoreObject.AddComponent<ScoreHud>();
        SetObject(scoreHud, "scoreLabel", scoreText);
        SetObject(scoreHud, "summaryLabel", summaryText);
        GameObject menu = new GameObject("MenuController");
        MainMenuController controller = menu.AddComponent<MainMenuController>();
        SetString(controller, "menuSceneName", "MainMenu");
        CreateButton("TerugNaarMenuKnop", canvas.transform, "TERUG NAAR START", new Vector2(0, -180), new Vector2(420, 68), controller.ReturnToMenu);
        CreateText("CreatorCredits", canvas.transform, "MADE BY JAIDEN ASRUF", 18, new Color(1f, 0.79f, 0.4f),
            TextAnchor.MiddleCenter, new Vector2(0f, -355f), new Vector2(500f, 36f));
        EditorSceneManager.SaveScene(scene, SceneFolder + "/Victory.unity");
    }

    private static void CreateDepthLayers(int levelNumber)
    {
        Color farColor = levelNumber == 2 ? new Color(0.15f, 0.06f, 0.22f, 0.72f) :
                         levelNumber == 3 ? new Color(0.09f, 0.08f, 0.22f, 0.76f) :
                         new Color(0.07f, 0.15f, 0.23f, 0.72f);
        Color midColor = levelNumber == 2 ? new Color(0.14f, 0.035f, 0.18f, 0.68f) :
                         levelNumber == 3 ? new Color(0.12f, 0.055f, 0.22f, 0.7f) :
                         new Color(0.055f, 0.13f, 0.19f, 0.68f);
        Color glowColor = levelNumber == 2 ? new Color(1f, 0.35f, 0.66f, 0.2f) :
                          levelNumber == 3 ? new Color(0.62f, 0.47f, 1f, 0.21f) :
                          new Color(0.28f, 0.79f, 1f, 0.2f);

        GameObject farRoot = new GameObject("Depth Far Architecture");
        farRoot.AddComponent<ParallaxBackground2D>().Configure(0.12f);
        for (int i = 0; i < 12; i++)
        {
            float x = -15f + i * 6.35f;
            float width = 3.2f + (i % 3) * 0.55f;
            float height = 4.1f + (i % 4) * 0.78f;
            float centerY = 0.55f + height * 0.23f;
            GameObject building = CreateLayerSprite(farRoot.transform, "FarTower_" + i,
                new Vector3(x, centerY, 0f), new Vector3(width, height, 1f), farColor, squareSprite, -94);
            AddWindowGrid(building.transform, 3, 3, new Color(glowColor.r, glowColor.g, glowColor.b, 0.46f), -93);
            CreateLayerSprite(farRoot.transform, "FarTowerGlow_" + i,
                new Vector3(x + (i % 2 == 0 ? -0.45f : 0.55f), centerY + height * 0.33f, 0f),
                new Vector3(2.2f, 1.55f, 1f), glowColor, softGlowSprite, -92);
        }

        GameObject midRoot = new GameObject("Depth Mid Architecture");
        midRoot.AddComponent<ParallaxBackground2D>().Configure(0.34f);
        for (int i = 0; i < 9; i++)
        {
            float x = -17f + i * 8.7f;
            float width = 4.4f + (i % 2) * 0.65f;
            float height = 3.5f + (i % 3) * 0.78f;
            GameObject building = CreateLayerSprite(midRoot.transform, "MidTower_" + i,
                new Vector3(x, -0.8f + height * 0.5f, 0f), new Vector3(width, height, 1f), midColor, squareSprite, -84);
            AddWindowGrid(building.transform, 3, 2, new Color(glowColor.r, glowColor.g, glowColor.b, 0.36f), -83);
        }

        GameObject foreground = new GameObject("Depth Foreground Structures");
        foreground.AddComponent<ParallaxBackground2D>().Configure(1.14f);
        for (int i = 0; i < 6; i++)
        {
            float x = -5f + i * 11f;
            CreateLayerSprite(foreground.transform, "NearCableSupport_" + i,
                new Vector3(x, 3.55f, 0f), new Vector3(0.22f, 4.1f, 1f),
                new Color(0.025f, 0.035f, 0.09f, 0.63f), squareSprite, 14);
            CreateLayerSprite(foreground.transform, "NearCrossbeam_" + i,
                new Vector3(x + 1.35f, 5.25f, 0f), new Vector3(2.8f, 0.12f, 1f),
                new Color(0.035f, 0.045f, 0.12f, 0.58f), squareSprite, 14);
            CreateLayerSprite(foreground.transform, "NearLampGlow_" + i,
                new Vector3(x + 0.4f, 1.42f, 0f), new Vector3(1.15f, 0.95f, 1f),
                new Color(glowColor.r, glowColor.g, glowColor.b, 0.48f), softGlowSprite, 15);
        }
    }

    private static GameObject CreateLayerSprite(Transform parent, string name, Vector3 localPosition,
        Vector3 localScale, Color color, Sprite sprite, int sortingOrder)
    {
        GameObject item = new GameObject(name);
        item.transform.SetParent(parent, false);
        item.transform.localPosition = localPosition;
        item.transform.localScale = localScale;
        SpriteRenderer renderer = item.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return item;
    }

    private static void AddWindowGrid(Transform building, int columns, int rows, Color color, int sortingOrder)
    {
        Vector3 buildingScale = building.localScale;
        float left = -buildingScale.x * 0.5f;
        float top = buildingScale.y * 0.36f;
        for (int row = 0; row < rows; row++)
        for (int column = 0; column < columns; column++)
        {
            if ((row + column) % 4 == 3) continue;
            float x = left + (column + 0.65f) * buildingScale.x / (columns + 0.25f);
            float y = top - row * 0.62f;
            CreateLayerSprite(building, "Window", new Vector3(x, y, -0.01f),
                new Vector3(0.13f, 0.19f, 1f), color, squareSprite, sortingOrder);
        }
    }

    private static void CreateLevelScene(int levelNumber, string nextScene, GameObject projectilePrefab, GameObject bossProjectilePrefab)
    {
        string sceneName = "Level" + levelNumber;
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Color background = levelNumber == 1 ? new Color(0.018f, 0.04f, 0.12f) :
                           levelNumber == 2 ? new Color(0.08f, 0.012f, 0.11f) : new Color(0.06f, 0.02f, 0.12f);
        CreateCamera(background);
        CreateDepthLayers(levelNumber);

        CreateAudioManager();
        CreateWorldFeedback();
        Canvas canvas = CreateCanvas();
        Text livesText = CreateText("Levens", canvas.transform, "VITALS  03 / 05", 24, Color.white, TextAnchor.UpperLeft, new Vector2(28, -22), new Vector2(230, 52));
        livesText.rectTransform.anchorMin = new Vector2(0, 1);
        livesText.rectTransform.anchorMax = new Vector2(0, 1);
        livesText.rectTransform.pivot = new Vector2(0, 1);
        Text scoreText = CreateText("EnergieScore", canvas.transform, "SHARDS  00", 23, new Color(1f, 0.82f, 0.3f), TextAnchor.UpperLeft, new Vector2(275, -22), new Vector2(260, 50));
        scoreText.rectTransform.anchorMin = new Vector2(0, 1);
        scoreText.rectTransform.anchorMax = new Vector2(0, 1);
        scoreText.rectTransform.pivot = new Vector2(0, 1);
        GameObject scoreObject = new GameObject("ScoreController");
        ScoreHud scoreHud = scoreObject.AddComponent<ScoreHud>();
        SetObject(scoreHud, "scoreLabel", scoreText);
        CreateText("Bediening", canvas.transform, "SPATIE  SPRONG/STOMP     SHIFT  DASH     J  PULSE     1/2/3  POWERS     ESC  PAUZE", 15, Color.white,
            TextAnchor.UpperRight, new Vector2(-24f, -24f), new Vector2(1060f, 38f), new Vector2(1f, 1f), new Vector2(1f, 1f));
        string[] districtNames = { "", "01  /  METROVILLE — DAKEN", "02  /  METROVILLE — ONDERGRONDS", "03  /  METROVILLE — RIFTTOREN" };
        CreateText("Stadswijk", canvas.transform, districtNames[levelNumber], 17, new Color(1f, 0.82f, 0.3f),
            TextAnchor.UpperRight, new Vector2(-24f, -66f), new Vector2(630f, 30f), new Vector2(1f, 1f), new Vector2(1f, 1f));
        Text powerupLabel = CreateText("PowerupStatus", canvas.transform, "KRACHT  PULSE-BURST", 15, new Color(0.82f, 0.92f, 1f), TextAnchor.UpperRight, new Vector2(-24f, -104f), new Vector2(500f, 30f), new Vector2(1f, 1f), new Vector2(1f, 1f));
        Image energyFill = CreateMeter("HeroEnergyBar", canvas.transform, new Vector2(-24f, -139f), new Vector2(340f, 13f),
            new Color(0.16f, 0.75f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f));
        Text dashLabel = CreateText("DashStatus", canvas.transform, "SHIFT  DASH READY", 17, new Color(0.15f, 0.92f, 1f),
            TextAnchor.LowerCenter, new Vector2(0f, 28f), new Vector2(360f, 34f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));

        GameObject pausePanel = new GameObject("Pauzepaneel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        pausePanel.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = pausePanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        pausePanel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);
        GameObject pauseObject = new GameObject("PauseController");
        PauseMenuController pauseController = pauseObject.AddComponent<PauseMenuController>();
        SetObject(pauseController, "pausePanel", pausePanel);
        SetString(pauseController, "menuSceneName", "MainMenu");
        CreateText("PauzeUitleg", pausePanel.transform, "GEPAUZEERD\nESC, R of M kan ook met het toetsenbord.", 32, Color.white, TextAnchor.MiddleCenter, new Vector2(0, 205), new Vector2(980, 150));
        CreateButton("HervatKnop", pausePanel.transform, "VERDER SPELEN", new Vector2(0, 70), new Vector2(420, 64), pauseController.Resume);
        CreateButton("OpnieuwKnop", pausePanel.transform, "LEVEL OPNIEUW", new Vector2(0, -20), new Vector2(420, 64), pauseController.RestartLevel);
        CreateButton("MenuKnop", pausePanel.transform, "NAAR STARTSCHERM", new Vector2(0, -110), new Vector2(420, 64), pauseController.ReturnToMenu);
        pausePanel.SetActive(false);

        GameObject gameOverPanel = new GameObject("GameOverPaneel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        gameOverPanel.transform.SetParent(canvas.transform, false);
        RectTransform gameOverRect = gameOverPanel.GetComponent<RectTransform>();
        gameOverRect.anchorMin = Vector2.zero;
        gameOverRect.anchorMax = Vector2.one;
        gameOverRect.offsetMin = Vector2.zero;
        gameOverRect.offsetMax = Vector2.zero;
        gameOverPanel.GetComponent<Image>().color = new Color(0.09f, 0.025f, 0.06f, 0.92f);
        CreateText("GameOverTitel", gameOverPanel.transform, "HELD VERSLAGEN\n\nMetroville heeft je nodig. Probeer het opnieuw!", 38, Color.white, TextAnchor.MiddleCenter, new Vector2(0, 190), new Vector2(1000, 230));
        CreateButton("OpnieuwNaGameOverKnop", gameOverPanel.transform, "OPNIEUW PROBEREN", new Vector2(0, 30), new Vector2(460, 66), pauseController.RestartLevel);
        CreateButton("MenuNaGameOverKnop", gameOverPanel.transform, "NAAR STARTSCHERM", new Vector2(0, -70), new Vector2(460, 66), pauseController.ReturnToMenu);
        gameOverPanel.SetActive(false);

        CreateLevelGeometry(levelNumber, nextScene, canvas, bossProjectilePrefab);
        CreatePlayer(projectilePrefab, livesText, gameOverPanel, pauseController, powerupLabel, dashLabel, energyFill);
        CreateHazard("Valzone", new Vector3(17f, -8f, 0f), new Vector2(80f, 1f), new Color(0f, 0f, 0f, 0f), false);
        CreateTutorialOverlay(canvas);
        CreateAchievementToast(canvas);

        EditorSceneManager.SaveScene(scene, SceneFolder + "/" + sceneName + ".unity");
    }

    private static void CreateLevelGeometry(int levelNumber, string nextScene, Canvas canvas, GameObject bossProjectilePrefab)
    {
        Color groundColor = levelNumber == 2 ? new Color(0.22f, 0.075f, 0.3f) :
                            levelNumber == 3 ? new Color(0.17f, 0.12f, 0.34f) : new Color(0.055f, 0.23f, 0.34f);
        Color platformColor = levelNumber == 2 ? new Color(0.8f, 0.13f, 0.55f) :
                              levelNumber == 3 ? new Color(0.59f, 0.22f, 0.76f) : new Color(0.09f, 0.52f, 0.68f);

        // Long rooftop/metro/tower spine. Each scene removes a different piece of the safe floor.
        float[] groundCenters = { -6.3f, -0.1f, 6.1f, 12.3f, 18.5f, 24.7f, 30.9f, 37.1f, 43.3f, 49.5f, 55.4f };
        float[] groundWidths  = { 5.0f, 5.0f, 5.0f, 5.0f, 5.0f, 5.0f, 5.0f, 5.0f, 5.0f, 5.4f, 4.4f };
        for (int i = 0; i < groundCenters.Length; i++)
        {
            if (levelNumber == 2 && i == 2) continue;
            if (levelNumber == 3 && (i == 3 || i == 4)) continue;
            string floorName = "Platform route " + (i + 1).ToString("00");
            Vector3 floorPosition = new Vector3(groundCenters[i], -3f, 0f);
            Vector2 floorSize = new Vector2(groundWidths[i], 0.62f);
            if ((levelNumber == 1 && i == 3) || (levelNumber == 2 && i == 6))
                CreateCollapsingPlatform(floorName + " crumbling", floorPosition, floorSize, groundColor);
            else
                CreatePlatform(floorName, floorPosition, floorSize, groundColor);
        }

        CreatePlatform("Platform tutorial upper", new Vector3(-1.2f, -1.52f, 0f), new Vector2(2.6f, 0.3f), platformColor);
        CreatePlatform("Platform risk reward", new Vector3(4.9f, -0.76f, 0f), new Vector2(2.8f, 0.3f), platformColor);
        CreatePlatform("Platform upper route A", new Vector3(15.6f, -1.32f, 0f), new Vector2(3.0f, 0.3f), platformColor);
        CreatePlatform("Platform upper route B", new Vector3(20.3f, -0.58f, 0f), new Vector2(2.6f, 0.3f), platformColor);
        CreatePlatform("Platform boss overlook", new Vector3(39.4f, -1.12f, 0f), new Vector2(3.2f, 0.3f), platformColor);

        CreateHazard("Rift spikes approach", new Vector3(-3.7f, -2.58f, 0f), new Vector2(1.1f, 0.3f), new Color(1f, 0.25f, 0.24f), true);
        CreateHazard("Rift spikes mid", new Vector3(7.3f, -2.58f, 0f), new Vector2(1.2f, 0.3f), new Color(1f, 0.25f, 0.24f), true);
        CreateHazard("Rift spikes late", new Vector3(24.6f, -2.58f, 0f), new Vector2(1.3f, 0.3f), new Color(1f, 0.25f, 0.24f), true);

        CreateCheckpoint(new Vector3(8.8f, -2.12f, 0f));
        CreateCheckpoint(new Vector3(31.4f, -2.12f, 0f));
        CreateCrystal(new Vector3(-1.2f, -0.95f, 0f));
        CreateCrystal(new Vector3(4.9f, -0.12f, 0f));
        CreateCrystal(new Vector3(16.1f, -0.55f, 0f));
        CreateCrystal(new Vector3(20.3f, 0.1f, 0f));
        CreateCrystal(new Vector3(39.4f, -0.34f, 0f));
        CreateCrystal(new Vector3(12.1f, -2.05f, 0f));
        CreateCrystal(new Vector3(34.2f, -2.05f, 0f));
        CreateLifePickup(new Vector3(10.5f, -2.05f, 0f));

        if (levelNumber == 1)
        {
            CreateText("WorldCallout", canvas.transform, "EERSTE CONTACT  //  RIFT-SOLDATEN IN AANTOCHT", 19,
                new Color(1f, 0.7f, 0.38f), TextAnchor.MiddleCenter, new Vector2(0f, -150f), new Vector2(700f, 38f));
            CreateAegisPickup(new Vector3(5f, -0.2f, 0f));
            CreateEnemy(new Vector3(2.7f, -2.02f, 0f), new Vector3(1.8f, -2.02f, 0f), new Vector3(4.4f, -2.02f, 0f), PatrolEnemy2D.EnemyRole.Stalker, false, bossProjectilePrefab);
            CreateEnemy(new Vector3(14.4f, -2.02f, 0f), new Vector3(12.3f, -2.02f, 0f), new Vector3(15.4f, -2.02f, 0f), PatrolEnemy2D.EnemyRole.Charger, false, bossProjectilePrefab);
            CreateEnemy(new Vector3(21.4f, -2.02f, 0f), null, null, PatrolEnemy2D.EnemyRole.Stalker, true, bossProjectilePrefab);
            CreateFallingHazard(new Vector3(18.5f, 2.85f, 0f), new Color(1f, 0.48f, 0.26f));
            CreateRotatingHazard(new Vector3(34.5f, -0.8f, 0f), 2.35f, 92f, false);
            TimedLaserHazard2D rooftopLaser = CreateLaser("Rooftop timing laser", new Vector3(27.2f, -2.02f, 0f),
                new Vector2(4.4f, 0.15f), 3.5f, 1.05f, 0.62f, 0.8f);
            CreatePressurePlate(new Vector3(22.3f, -2.68f, 0f), new[] { rooftopLaser }, 5.8f);
            CreateSwooperEnemy(new Vector3(37.4f, 2.65f, 0f));

            CreatePlatform("Platform secret roof west", new Vector3(13.7f, -0.35f, 0f), new Vector2(2.5f, 0.3f), platformColor);
            CreatePlatform("Platform secret roof mid", new Vector3(16.8f, 0.45f, 0f), new Vector2(2.3f, 0.3f), platformColor);
            CreatePlatform("Platform secret roof east", new Vector3(19.9f, 0.85f, 0f), new Vector2(2.5f, 0.3f), platformColor);
            CreateCrystal(new Vector3(13.7f, 0.15f, 0f));
            CreateCrystal(new Vector3(16.8f, 0.95f, 0f));
            CreateLifePickup(new Vector3(19.9f, 1.45f, 0f));
        }
        else if (levelNumber == 2)
        {
            CreateHazard("Metro steam vents", new Vector3(22f, -2.58f, 0f), new Vector2(2.2f, 0.3f), new Color(1f, 0.38f, 0.25f), true);
            CreatePlatform("Platform metro lift west", new Vector3(8.8f, -1.45f, 0f), new Vector2(2.0f, 0.3f), platformColor);
            CreatePlatform("Platform metro lift east", new Vector3(12.4f, -0.66f, 0f), new Vector2(2.3f, 0.3f), platformColor);
            CreateMovingPlatform(new Vector2(8.6f, -1.8f), new Vector2(10.4f, 0.45f));
            CreateRapidFirePickup(new Vector3(12.4f, 0.05f, 0f));
            CreateEnemy(new Vector3(2.8f, -2.02f, 0f), new Vector3(1.5f, -2.02f, 0f), new Vector3(4.6f, -2.02f, 0f), PatrolEnemy2D.EnemyRole.Stalker, false, bossProjectilePrefab);
            CreateEnemy(new Vector3(17.7f, -2.02f, 0f), null, null, PatrolEnemy2D.EnemyRole.Stalker, true, bossProjectilePrefab);
            CreateEnemy(new Vector3(30.4f, -2.02f, 0f), new Vector3(29f, -2.02f, 0f), new Vector3(32f, -2.02f, 0f), PatrolEnemy2D.EnemyRole.Charger, false, bossProjectilePrefab);
            TimedLaserHazard2D firstMetroLaser = CreateLaser("Metro laser gate A", new Vector3(17.1f, -1.4f, 0f),
                new Vector2(0.17f, 2.7f), 3.2f, 0.95f, 0.62f, 0f);
            TimedLaserHazard2D secondMetroLaser = CreateLaser("Metro laser gate B", new Vector3(20.2f, -1.4f, 0f),
                new Vector2(0.17f, 2.7f), 3.2f, 0.95f, 0.62f, 1.55f);
            CreatePressurePlate(new Vector3(14.4f, -2.68f, 0f), new[] { firstMetroLaser, secondMetroLaser }, 5.6f);
            CreateFallingHazard(new Vector3(25.2f, 2.7f, 0f), new Color(1f, 0.4f, 0.3f));
            CreateRotatingHazard(new Vector3(35.3f, -0.82f, 0f), 2.45f, 106f, true);
            CreateSwooperEnemy(new Vector3(38.5f, 2.8f, 0f));

            CreatePlatform("Platform secret tunnel high A", new Vector3(22.3f, 0.35f, 0f), new Vector2(2.4f, 0.3f), platformColor);
            CreatePlatform("Platform secret tunnel high B", new Vector3(25.2f, 1.2f, 0f), new Vector2(2.4f, 0.3f), platformColor);
            CreatePlatform("Platform secret tunnel high C", new Vector3(28.3f, 1.45f, 0f), new Vector2(2.6f, 0.3f), platformColor);
            CreateCrystal(new Vector3(22.3f, 0.85f, 0f));
            CreateCrystal(new Vector3(25.2f, 1.7f, 0f));
            CreateLifePickup(new Vector3(28.3f, 1.95f, 0f));
        }
        else
        {
            CreatePlatform("Platform tower route lower", new Vector3(8.6f, -1.65f, 0f), new Vector2(2.6f, 0.3f), platformColor);
            CreateCollapsingPlatform("Platform tower route middle", new Vector3(12.8f, -0.82f, 0f), new Vector2(2.8f, 0.3f), platformColor);
            CreateCollapsingPlatform("Platform tower route high", new Vector3(17.1f, -1.2f, 0f), new Vector2(2.8f, 0.3f), platformColor);
            CreateMovingPlatform(new Vector2(22.7f, -1.65f), new Vector2(22.7f, 1.3f));
            CreateSkyboundPickup(new Vector3(17.1f, -0.55f, 0f));
            CreateEnemy(new Vector3(2.8f, -2.02f, 0f), new Vector3(1.5f, -2.02f, 0f), new Vector3(4.6f, -2.02f, 0f), PatrolEnemy2D.EnemyRole.Charger, false, bossProjectilePrefab);
            CreateEnemy(new Vector3(15f, -2.02f, 0f), null, null, PatrolEnemy2D.EnemyRole.Stalker, true, bossProjectilePrefab);
            CreateEnemy(new Vector3(29.5f, -2.02f, 0f), new Vector3(28f, -2.02f, 0f), new Vector3(31f, -2.02f, 0f), PatrolEnemy2D.EnemyRole.Stalker, false, bossProjectilePrefab);
            TimedLaserHazard2D towerLaserA = CreateLaser("Rift tower laser A", new Vector3(29.5f, -1.3f, 0f),
                new Vector2(0.17f, 2.7f), 3f, 0.86f, 0.6f, 0.4f);
            TimedLaserHazard2D towerLaserB = CreateLaser("Rift tower laser B", new Vector3(33.1f, -1.3f, 0f),
                new Vector2(0.17f, 2.7f), 3f, 0.86f, 0.6f, 1.8f);
            CreatePressurePlate(new Vector3(26.3f, -2.68f, 0f), new[] { towerLaserA, towerLaserB }, 5.3f);
            CreateFallingHazard(new Vector3(24.7f, 2.9f, 0f), new Color(0.87f, 0.48f, 1f));
            CreateRotatingHazard(new Vector3(37.1f, -0.82f, 0f), 2.55f, 118f, false);
            CreateSwooperEnemy(new Vector3(36.4f, 2.9f, 0f));

            CreatePlatform("Platform hidden lift route A", new Vector3(20.8f, 0.45f, 0f), new Vector2(2.4f, 0.3f), platformColor);
            CreatePlatform("Platform hidden lift route B", new Vector3(24.1f, 1.1f, 0f), new Vector2(2.3f, 0.3f), platformColor);
            CreateCrystal(new Vector3(20.8f, 0.95f, 0f));
            CreateCrystal(new Vector3(24.1f, 1.6f, 0f));
            CreateLifePickup(new Vector3(27.2f, 1.2f, 0f));
        }

        LevelExit2D exit = CreateExit(nextScene);
        CreateBoss(levelNumber, exit, canvas, bossProjectilePrefab);
    }

    private static void CreatePlayer(GameObject projectilePrefab, Text livesText, GameObject gameOverPanel,
        PauseMenuController pauseController, Text powerupLabel, Text dashLabel, Image energyFill)
    {
        GameObject player = new GameObject("PulseHero");
        player.transform.position = new Vector3(-7f, -2.08f, 0f);
        Rigidbody2D body = player.AddComponent<Rigidbody2D>();
        body.gravityScale = 3.5f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        CapsuleCollider2D collider = player.AddComponent<CapsuleCollider2D>();
        collider.size = new Vector2(0.74f, 1.04f);
        TrailRenderer dashTrail = player.AddComponent<TrailRenderer>();
        dashTrail.time = 0.22f;
        dashTrail.startWidth = 0.54f;
        dashTrail.endWidth = 0f;
        dashTrail.startColor = new Color(0.1f, 0.9f, 1f, 0.78f);
        dashTrail.endColor = new Color(0.95f, 0.2f, 0.85f, 0f);
        dashTrail.sortingOrder = 8;
        Shader spriteShader = Shader.Find("Sprites/Default");
        if (spriteShader != null)
            dashTrail.material = new Material(spriteShader);
        dashTrail.emitting = false;

        GameObject visualObject = new GameObject("HeroVisual");
        visualObject.transform.SetParent(player.transform, false);
        SpriteRenderer heroRenderer = CreateChildSprite(visualObject.transform, "PulseHeroSprite", Vector3.zero,
            Vector3.one, heroSprite, Color.white, 10);
        CreateChildSprite(visualObject.transform, "EnergyFocus", new Vector3(0.49f, 0.06f, 0f),
            new Vector3(0.18f, 0.18f, 1f), energyBoltSprite, new Color(0.68f, 0.96f, 1f, 0.9f), 12);
        Transform groundCheck = CreateChildPoint(player.transform, "GroundCheck", new Vector3(0f, -0.52f, 0f));
        Transform firePoint = CreateChildPoint(visualObject.transform, "FirePoint", new Vector3(0.76f, 0.09f, 0f));
        PlayerController2D controller = player.AddComponent<PlayerController2D>();
        SetFloat(controller, "moveSpeed", 5f);
        SetFloat(controller, "jumpForce", 10f);
        SetInt(controller, "maximumJumps", 2);
        SetObject(controller, "groundCheck", groundCheck);
        SetFloat(controller, "groundCheckRadius", 0.16f);
        SetInt(controller, "groundLayer", 1 << groundLayer);
        SetObject(controller, "projectilePrefab", projectilePrefab);
        SetObject(controller, "firePoint", firePoint);
        SetObject(controller, "visualRoot", visualObject.transform);
        SetObject(controller, "spriteRenderer", heroRenderer);
        SetObject(controller, "dashLabel", dashLabel);
        SetObject(controller, "dashTrail", dashTrail);
        SetFloat(controller, "dashSpeed", 13f);
        SetFloat(controller, "dashDuration", 0.16f);
        SetFloat(controller, "dashCooldown", 1.1f);

        HeroPowerController2D powers = player.AddComponent<HeroPowerController2D>();
        SetObject(powers, "powerLabel", powerupLabel);
        SetObject(powers, "energyFill", energyFill);

        PlayerHealth health = player.AddComponent<PlayerHealth>();
        SetInt(health, "startingLives", 3);
        SetInt(health, "maximumLives", 5);
        SetFloat(health, "fallDeathY", -6.5f);
        SetObject(health, "livesLabel", livesText);
        SetObject(health, "gameOverPanel", gameOverPanel);
        SetObject(health, "pauseMenu", pauseController);

        CameraFollow2D cameraFollow = Camera.main != null ? Camera.main.GetComponent<CameraFollow2D>() : null;
        if (cameraFollow != null) cameraFollow.Configure(player.transform, -2f, 43f);
        CreateGroundShadow(player.transform);
    }

    private static void CreateGroundShadow(Transform player)
    {
        GameObject shadow = new GameObject("HeroGroundShadow");
        SpriteRenderer renderer = shadow.AddComponent<SpriteRenderer>();
        renderer.sprite = softGlowSprite;
        renderer.color = new Color(0.015f, 0.03f, 0.08f, 0.3f);
        renderer.sortingOrder = 7;
        GroundShadow2D groundShadow = shadow.AddComponent<GroundShadow2D>();
        SetObject(groundShadow, "target", player);
        SetObject(groundShadow, "shadow", renderer);
        SetInt(groundShadow, "groundLayer", 1 << groundLayer);
    }

    private static Transform CreateChildPoint(Transform parent, string name, Vector3 localPosition)
    {
        GameObject point = new GameObject(name);
        point.transform.SetParent(parent, false);
        point.transform.localPosition = localPosition;
        return point.transform;
    }

    private static SpriteRenderer CreateChildSprite(Transform parent, string name, Vector3 localPosition,
        Vector3 localScale, Sprite sprite, Color color, int sortingOrder)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        child.transform.localPosition = localPosition;
        child.transform.localScale = localScale;
        SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    private static Transform CreateWorldPoint(string name, Vector3 position)
    {
        GameObject point = new GameObject(name);
        point.transform.position = position;
        return point.transform;
    }

    private static GameObject CreatePlatform(string name, Vector3 position, Vector2 size, Color color)
    {
        CreateSpriteObject(name + " PlatformDepth", position + Vector3.down * 0.08f,
            new Vector3(size.x + 0.08f, size.y + 0.08f, 1f), new Color(0.025f, 0.04f, 0.11f, 0.8f), -1);
        GameObject platform = CreateSpriteObject(name, position, new Vector3(size.x, size.y, 1f), color, 0);
        platform.layer = groundLayer;
        BoxCollider2D collider = platform.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
        CreateSpriteObject(name + " PlatformEdge", position + Vector3.up * (size.y * 0.5f - 0.025f),
            new Vector3(size.x * 0.86f, 0.028f, 1f), new Color(0.52f, 0.96f, 1f, 0.9f), 1);
        return platform;
    }

    private static GameObject CreateCollapsingPlatform(string name, Vector3 position, Vector2 size, Color color)
    {
        GameObject platform = CreatePlatform(name, position, size, color);
        Rigidbody2D body = platform.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        CollapsingPlatform2D crumble = platform.AddComponent<CollapsingPlatform2D>();
        SetObject(crumble, "platformCollider", platform.GetComponent<Collider2D>());
        SetObject(crumble, "body", body);
        SetObject(crumble, "visual", platform.GetComponent<SpriteRenderer>());
        return platform;
    }

    private static TimedLaserHazard2D CreateLaser(string name, Vector3 position, Vector2 size,
        float cycle, float active, float warning, float phaseOffset)
    {
        GameObject laser = CreateSpriteObject(name, position, new Vector3(size.x, size.y, 1f),
            new Color(1f, 0.18f, 0.2f, 0.96f), 6);
        BoxCollider2D collider = laser.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
        collider.isTrigger = true;
        laser.AddComponent<DamagePlayerOnTouch>();
        TimedLaserHazard2D timing = laser.AddComponent<TimedLaserHazard2D>();
        timing.Configure(collider, laser.GetComponent<SpriteRenderer>(), cycle, active, warning, phaseOffset);
        return timing;
    }

    private static void CreatePressurePlate(Vector3 position, TimedLaserHazard2D[] linkedLasers, float safeSeconds)
    {
        GameObject plate = CreatePlatform("PressurePlate", position, new Vector2(1.12f, 0.22f), new Color(0.98f, 0.65f, 0.19f));
        PressurePlate2D control = plate.AddComponent<PressurePlate2D>();
        control.Configure(linkedLasers, plate.GetComponent<SpriteRenderer>(), safeSeconds);
    }

    private static void CreateFallingHazard(Vector3 position, Color color)
    {
        GameObject hazard = CreateSpriteObject("Falling Rift Debris", position, new Vector3(0.78f, 0.78f, 1f), color, 7);
        SpriteRenderer visual = hazard.GetComponent<SpriteRenderer>();
        visual.sprite = spikeSprite;
        visual.sortingOrder = 7;
        BoxCollider2D collider = hazard.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
        collider.isTrigger = true;
        hazard.AddComponent<DamagePlayerOnTouch>();
        GameObject warning = CreateLayerSprite(hazard.transform, "DropWarning", new Vector3(0f, -3f, 0f),
            new Vector3(0.8f, 0.12f, 1f), new Color(1f, 0.2f, 0.18f, 0.4f), squareSprite, 6);
        SpriteRenderer warningRenderer = warning.GetComponent<SpriteRenderer>();
        warningRenderer.enabled = false;
        FallingHazard2D falling = hazard.AddComponent<FallingHazard2D>();
        falling.Configure(visual, warningRenderer, collider, 1.7f, 0.7f, 12f, 5.7f);
    }

    private static void CreateRotatingHazard(Vector3 position, float armLength, float rotationSpeed, bool reverse)
    {
        GameObject rotor = new GameObject("Rotating Rift Slicer");
        rotor.transform.position = position;
        Rigidbody2D body = rotor.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        RotatingHazard2D rotating = rotor.AddComponent<RotatingHazard2D>();
        rotating.Configure(rotationSpeed, reverse);
        CreateChildSprite(rotor.transform, "Rift Slicer Core", Vector3.zero, new Vector3(0.36f, 0.36f, 1f),
            energyBoltSprite, new Color(1f, 0.76f, 0.31f), 8);
        GameObject arm = new GameObject("Rift Slicer Arm");
        arm.transform.SetParent(rotor.transform, false);
        arm.transform.localPosition = new Vector3(armLength * 0.46f, 0f, 0f);
        arm.transform.localScale = new Vector3(armLength, 0.16f, 1f);
        SpriteRenderer renderer = arm.AddComponent<SpriteRenderer>();
        renderer.sprite = squareSprite;
        renderer.color = new Color(1f, 0.28f, 0.35f, 0.96f);
        renderer.sortingOrder = 6;
        BoxCollider2D collider = arm.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
        collider.isTrigger = true;
        arm.AddComponent<DamagePlayerOnTouch>();
    }

    private static void CreateHazard(string name, Vector3 position, Vector2 size, Color color, bool visible)
    {
        if (!visible)
        {
            GameObject fallZone = CreateSpriteObject(name, position, new Vector3(size.x, size.y, 1f), color, 3);
            fallZone.GetComponent<SpriteRenderer>().enabled = false;
            BoxCollider2D fallCollider = fallZone.AddComponent<BoxCollider2D>();
            fallCollider.size = Vector2.one;
            fallCollider.isTrigger = true;
            fallZone.AddComponent<DamagePlayerOnTouch>();
            return;
        }

        GameObject spikeRow = new GameObject(name);
        spikeRow.transform.position = position;
        int count = Mathf.Max(2, Mathf.RoundToInt(size.x / 0.3f));
        float spacing = size.x / count;
        for (int i = 0; i < count; i++)
        {
            GameObject spike = new GameObject(name + " Spike");
            spike.transform.SetParent(spikeRow.transform, false);
            spike.transform.localPosition = new Vector3((i - (count - 1) * 0.5f) * spacing, 0f, 0f);
            spike.transform.localScale = new Vector3(spacing * 1.08f, 0.5f, 1f);
            SpriteRenderer renderer = spike.AddComponent<SpriteRenderer>();
            renderer.sprite = spikeSprite;
            renderer.color = color;
            renderer.sortingOrder = 4;
            BoxCollider2D collider = spike.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.84f, 0.92f);
            collider.isTrigger = true;
            spike.AddComponent<DamagePlayerOnTouch>();
        }
    }

    private static void CreateCheckpoint(Vector3 position)
    {
        GameObject checkpoint = CreateSpriteObject("Checkpoint", position, new Vector3(0.32f, 0.8f, 1f), new Color(0.25f, 0.95f, 0.55f), 4);
        BoxCollider2D collider = checkpoint.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
        collider.isTrigger = true;
        checkpoint.AddComponent<Checkpoint2D>();
    }

    private static void CreateCrystal(Vector3 position)
    {
        GameObject crystal = CreateSpriteObject("Energiefragment", position, new Vector3(0.38f, 0.48f, 1f), new Color(1f, 0.78f, 0.2f), 8);
        CircleCollider2D collider = crystal.AddComponent<CircleCollider2D>();
        collider.radius = 0.5f;
        collider.isTrigger = true;
        crystal.AddComponent<CrystalPickup2D>();
        crystal.AddComponent<FloatingPickup2D>();
    }

    private static void CreateLifePickup(Vector3 position)
    {
        GameObject life = CreateSpriteObject("Heldentoken", position, new Vector3(0.38f, 0.38f, 1f), new Color(1f, 0.35f, 0.48f), 8);
        CircleCollider2D collider = life.AddComponent<CircleCollider2D>();
        collider.radius = 0.5f;
        collider.isTrigger = true;
        life.AddComponent<LifePickup2D>();
        life.AddComponent<FloatingPickup2D>();
    }

    private static void CreateEnemy(Vector3 position, Vector3? pointA, Vector3? pointB,
        PatrolEnemy2D.EnemyRole role, bool ranged, GameObject enemyProjectilePrefab)
    {
        string enemyName = ranged ? "RiftSentry" : role == PatrolEnemy2D.EnemyRole.Charger ? "RiftCharger" : "RiftStalker";
        GameObject enemy = CreateSpriteObject(enemyName, position, new Vector3(0.82f, 0.82f, 1f),
            ranged ? new Color(0.4f, 0.77f, 1f) : new Color(0.85f, 0.38f, 0.92f), 6);
        BoxCollider2D collider = enemy.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.9f, 0.9f);
        EnemyHealth health = enemy.AddComponent<EnemyHealth>();
        SetInt(health, "hitPoints", role == PatrolEnemy2D.EnemyRole.Charger ? 2 : 1);
        enemy.AddComponent<DamagePlayerOnTouch>();
        Rigidbody2D body = enemy.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (ranged)
        {
            Transform muzzle = CreateChildPoint(enemy.transform, "RangedFirePoint", new Vector3(-0.48f, 0.05f, 0f));
            RangedEnemy2D sentry = enemy.AddComponent<RangedEnemy2D>();
            SetObject(sentry, "projectilePrefab", enemyProjectilePrefab);
            SetObject(sentry, "firePoint", muzzle);
            return;
        }

        if (pointA.HasValue && pointB.HasValue)
        {
            PatrolEnemy2D patrol = enemy.AddComponent<PatrolEnemy2D>();
            SetInt(patrol, "role", (int)role);
            Transform a = CreateWorldPoint("PatrolA", pointA.Value);
            Transform b = CreateWorldPoint("PatrolB", pointB.Value);
            SetObject(patrol, "pointA", a);
            SetObject(patrol, "pointB", b);
        }
    }

    private static void CreateSwooperEnemy(Vector3 position)
    {
        GameObject enemy = new GameObject("RiftSwooper");
        enemy.transform.position = position;
        SpriteRenderer renderer = enemy.AddComponent<SpriteRenderer>();
        renderer.sprite = squareSprite;
        renderer.color = new Color(0.62f, 0.42f, 1f);
        renderer.sortingOrder = 8;
        BoxCollider2D collider = enemy.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(1.05f, 0.9f);
        collider.isTrigger = true;
        enemy.AddComponent<EnemyHealth>();
        enemy.AddComponent<DamagePlayerOnTouch>();
        Rigidbody2D body = enemy.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        SwoopEnemy2D swooper = enemy.AddComponent<SwoopEnemy2D>();
        swooper.Configure(9f, 8.2f, 0.7f, 2.45f);
    }

    private static void CreateBoss(int levelNumber, LevelExit2D exit, Canvas canvas, GameObject bossProjectilePrefab)
    {
        string[] bossNames = { "TITAN", "PYRA", "NULL" };
        int[] healthValues = { 7, 9, 12 };
        int[] volleySizes = { 1, 2, 3 };
        float[] attackIntervals = { 2.5f, 1.9f, 1.45f };
        float[] moveSpeeds = { 0.42f, 0.62f, 0.82f };
        float[] projectileSpeeds = { 4.8f, 5.4f, 6.1f };

        string bossName = bossNames[levelNumber - 1];
        float startY = levelNumber == 3 ? -1.92f : -2.08f;
        GameObject boss = new GameObject(bossName);
        boss.transform.position = new Vector3(43.2f, startY, 0f);
        CreateChildSprite(boss.transform, "BossSprite", Vector3.zero, Vector3.one,
            bossSprites[levelNumber - 1], Color.white, 9);

        Rigidbody2D body = boss.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        BoxCollider2D collider = boss.AddComponent<BoxCollider2D>();
        collider.size = levelNumber == 3 ? new Vector2(1.65f, 1.4f) : new Vector2(1.3f, 1.05f);
        boss.AddComponent<DamagePlayerOnTouch>();
        Transform firePoint = CreateChildPoint(boss.transform, "BossFirePoint", new Vector3(-0.72f, 0.08f, 0f));

        BossController2D controller = boss.AddComponent<BossController2D>();
        SetString(controller, "bossName", bossName);
        SetInt(controller, "maximumHealth", healthValues[levelNumber - 1]);
        SetFloat(controller, "patrolMinX", 40.3f);
        SetFloat(controller, "patrolMaxX", 46.5f);
        SetFloat(controller, "moveSpeed", moveSpeeds[levelNumber - 1]);
        SetFloat(controller, "attackInterval", attackIntervals[levelNumber - 1]);
        SetInt(controller, "shotsPerVolley", volleySizes[levelNumber - 1]);
        SetFloat(controller, "spreadDegrees", 26f);
        SetFloat(controller, "projectileSpeed", projectileSpeeds[levelNumber - 1]);
        SetInt(controller, "attackStyle", levelNumber - 1);
        SetObject(controller, "projectilePrefab", bossProjectilePrefab);
        SetObject(controller, "firePoint", firePoint);
        SetObject(controller, "levelExit", exit);
        CreateBossHud(canvas, out Image healthFill, out Text healthLabel, out Text objectiveLabel);
        SetObject(controller, "healthFill", healthFill);
        SetObject(controller, "healthLabel", healthLabel);
        SetObject(controller, "objectiveLabel", objectiveLabel);
        SetBool(exit, "locked", true);
    }

    private static void CreateBossHud(Canvas canvas, out Image healthFill, out Text healthLabel, out Text objectiveLabel)
    {
        healthLabel = CreateText("BossHealthLabel", canvas.transform, "SUPERSCHURK", 22, Color.white,
            TextAnchor.MiddleCenter, new Vector2(0f, -48f), new Vector2(580f, 32f),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        Image background = CreatePanel("BossHealthBackground", canvas.transform, new Color(0.08f, 0.1f, 0.17f, 0.96f), false);
        RectTransform backgroundRect = background.rectTransform;
        backgroundRect.anchorMin = new Vector2(0.5f, 1f);
        backgroundRect.anchorMax = new Vector2(0.5f, 1f);
        backgroundRect.pivot = new Vector2(0.5f, 1f);
        backgroundRect.anchoredPosition = new Vector2(0f, -79f);
        backgroundRect.sizeDelta = new Vector2(500f, 22f);

        Image fill = CreatePanel("BossHealthFill", background.transform, new Color(0.95f, 0.23f, 0.29f, 1f), false);
        RectTransform fillRect = fill.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(4f, 4f);
        fillRect.offsetMax = new Vector2(-4f, -4f);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 1f;
        healthFill = fill;

        objectiveLabel = CreateText("BossDoel", canvas.transform, "Versla de superschurk en open de uitgang.",
            17, new Color(1f, 0.83f, 0.45f), TextAnchor.MiddleCenter, new Vector2(0f, -111f),
            new Vector2(820f, 28f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
    }

    private static Image CreateMeter(string name, Transform parent, Vector2 position, Vector2 size,
        Color fillColor, Vector2 anchorMin, Vector2 anchorMax)
    {
        Image background = CreatePanel(name + "Background", parent, new Color(0.035f, 0.065f, 0.12f, 0.94f), false);
        RectTransform backgroundRect = background.rectTransform;
        backgroundRect.anchorMin = anchorMin;
        backgroundRect.anchorMax = anchorMax;
        backgroundRect.pivot = anchorMax.y > 0.5f ? new Vector2(1f, 1f) : new Vector2(0.5f, 0.5f);
        backgroundRect.anchoredPosition = position;
        backgroundRect.sizeDelta = size;
        Image fill = CreatePanel(name, background.transform, fillColor, false);
        RectTransform fillRect = fill.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(2f, 2f);
        fillRect.offsetMax = new Vector2(-2f, -2f);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 1f;
        return fill;
    }

    private static void CreateWorldFeedback()
    {
        GameObject feedback = new GameObject("PULSE Feedback Particles");
        ParticleSystem particles = feedback.AddComponent<ParticleSystem>();
        ParticleSystemRenderer renderer = feedback.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 24;
        feedback.AddComponent<FeedbackVFX2D>();
    }

    private static void CreateTutorialOverlay(Canvas canvas)
    {
        GameObject panel = CreateOverlay("TutorialPanel", canvas.transform, new Vector2(860f, 560f), "TutorialCard");
        CreateText("TutorialTitle", panel.transform, "JE EERSTE NACHT IN METROVILLE", 34,
            new Color(1f, 0.83f, 0.3f), TextAnchor.MiddleCenter, new Vector2(0f, 195f), new Vector2(760f, 68f));
        Text body = CreateText("TutorialBody", panel.transform, "", 23, Color.white, TextAnchor.MiddleCenter,
            new Vector2(0f, 24f), new Vector2(700f, 190f));
        Text step = CreateText("TutorialStep", panel.transform, "BRIEFING  1 / 5", 16,
            new Color(0.42f, 0.85f, 1f), TextAnchor.MiddleCenter, new Vector2(0f, -110f), new Vector2(380f, 30f));
        GameObject tutorialRoot = new GameObject("TutorialController");
        TutorialOverlayController tutorial = tutorialRoot.AddComponent<TutorialOverlayController>();
        SetObject(tutorial, "panel", panel);
        SetObject(tutorial, "title", panel.transform.Find("TutorialTitle")?.GetComponent<Text>());
        SetObject(tutorial, "body", body);
        SetObject(tutorial, "stepLabel", step);
        CreateButton("TutorialNextKnop", panel.transform, "VOLGENDE", new Vector2(0f, -185f), new Vector2(300f, 54f), tutorial.NextStep);
        CreateButton("TutorialSkipKnop", panel.transform, "OVERSLAAN", new Vector2(0f, -250f), new Vector2(250f, 46f), tutorial.Skip);
    }

    private static void CreateAchievementToast(Canvas canvas)
    {
        GameObject panel = new GameObject("AchievementToast", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-24f, -180f);
        rect.sizeDelta = new Vector2(620f, 92f);
        panel.GetComponent<Image>().color = new Color(0.045f, 0.075f, 0.16f, 0.97f);
        Text title = CreateText("AchievementToastTitle", panel.transform, "PRESTATIE ONTGRENDELD", 18,
            new Color(1f, 0.82f, 0.34f), TextAnchor.MiddleLeft, new Vector2(18f, -15f), new Vector2(580f, 34f),
            new Vector2(0f, 1f), new Vector2(0f, 1f));
        Text message = CreateText("AchievementToastMessage", panel.transform, "", 16,
            new Color(0.85f, 0.92f, 1f), TextAnchor.MiddleLeft, new Vector2(18f, -51f), new Vector2(580f, 28f),
            new Vector2(0f, 1f), new Vector2(0f, 1f));
        AchievementToast2D toast = canvas.gameObject.AddComponent<AchievementToast2D>();
        SetObject(toast, "panel", panel);
        SetObject(toast, "title", title);
        SetObject(toast, "message", message);
    }

    private static void CreateMovingPlatform(Vector2 pointA, Vector2 pointB)
    {
        GameObject platform = CreateSpriteObject("Bewegend platform", new Vector3(pointA.x, pointA.y, 0f),
            new Vector3(2f, 0.28f, 1f), new Color(0.19f, 0.78f, 0.78f), 2);
        platform.layer = groundLayer;
        BoxCollider2D collider = platform.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
        Rigidbody2D body = platform.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        MovingPlatform2D moving = platform.AddComponent<MovingPlatform2D>();
        SetVector2(moving, "pointA", pointA);
        SetVector2(moving, "pointB", pointB);
        SetFloat(moving, "speed", 0.62f);
    }

    private static void CreateRapidFirePickup(Vector3 position)
    {
        GameObject pickup = new GameObject("OverdriveUpgrade");
        pickup.transform.position = position;
        SpriteRenderer renderer = pickup.AddComponent<SpriteRenderer>();
        renderer.sprite = overdriveSprite;
        renderer.sortingOrder = 9;
        pickup.transform.localScale = new Vector3(0.42f, 0.42f, 1f);
        CircleCollider2D collider = pickup.AddComponent<CircleCollider2D>();
        collider.radius = 0.5f;
        collider.isTrigger = true;
        pickup.AddComponent<RapidFirePickup2D>();
        pickup.AddComponent<FloatingPickup2D>();
    }

    private static void CreateAegisPickup(Vector3 position)
    {
        CreateHeroUpgradePickup("AegisUpgrade", position, HeroUpgradePickup2D.UpgradeKind.AegisShield);
    }

    private static void CreateSkyboundPickup(Vector3 position)
    {
        CreateHeroUpgradePickup("SkyboundUpgrade", position, HeroUpgradePickup2D.UpgradeKind.Skybound);
    }

    private static void CreateHeroUpgradePickup(string objectName, Vector3 position,
        HeroUpgradePickup2D.UpgradeKind upgradeKind)
    {
        GameObject pickup = CreateSpriteObject(objectName, position, new Vector3(0.52f, 0.52f, 1f), Color.white, 9);
        CircleCollider2D collider = pickup.AddComponent<CircleCollider2D>();
        collider.radius = 0.55f;
        collider.isTrigger = true;
        pickup.AddComponent<HeroUpgradePickup2D>().Configure(upgradeKind);
        pickup.AddComponent<FloatingPickup2D>();
    }

    private static LevelExit2D CreateExit(string nextScene)
    {
        GameObject exit = CreateSpriteObject("Uitgang", new Vector3(50.6f, -1.72f, 0f), new Vector3(0.75f, 2.05f, 1f), new Color(0.22f, 0.9f, 0.62f), 4);
        BoxCollider2D collider = exit.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
        collider.isTrigger = true;
        LevelExit2D levelExit = exit.AddComponent<LevelExit2D>();
        SetString(levelExit, "nextSceneName", nextScene);
        return levelExit;
    }

    private static GameObject CreateSpriteObject(string name, Vector3 position, Vector3 scale, Color color, int sortingOrder)
    {
        GameObject gameObject = new GameObject(name);
        gameObject.transform.position = position;
        gameObject.transform.localScale = scale;
        SpriteRenderer renderer = gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = squareSprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return gameObject;
    }

    private static void CreateCamera(Color background)
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0.8f, -10f);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5.3f;
        camera.backgroundColor = background;
        camera.clearFlags = CameraClearFlags.SolidColor;
        cameraObject.AddComponent<AudioListener>();
        cameraObject.AddComponent<CameraFollow2D>();
    }

    private static void CreateAudioManager()
    {
        GameObject audioManager = new GameObject("ArcadeAudioManager");
        audioManager.AddComponent<ArcadeAudioManager>();
    }

    private static Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        CreateEventSystem();
        return canvas;
    }

    private static Slider CreateSlider(string name, Transform parent, Vector2 position, float defaultValue)
    {
        GameObject sliderObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Slider));
        sliderObject.transform.SetParent(parent, false);
        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.5f, 0.5f);
        sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
        sliderRect.pivot = new Vector2(0.5f, 0.5f);
        sliderRect.anchoredPosition = position;
        sliderRect.sizeDelta = new Vector2(500f, 32f);
        Image background = sliderObject.GetComponent<Image>();
        background.color = new Color(0.11f, 0.16f, 0.29f, 1f);
        background.raycastTarget = true;

        RectTransform fillArea = new GameObject("FillArea", typeof(RectTransform)).GetComponent<RectTransform>();
        fillArea.SetParent(sliderObject.transform, false);
        fillArea.anchorMin = new Vector2(0f, 0.5f);
        fillArea.anchorMax = new Vector2(1f, 0.5f);
        fillArea.pivot = new Vector2(0.5f, 0.5f);
        fillArea.anchoredPosition = Vector2.zero;
        fillArea.sizeDelta = new Vector2(-28f, 8f);

        GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fillObject.transform.SetParent(fillArea, false);
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        Image fillImage = fillObject.GetComponent<Image>();
        fillImage.color = new Color(0.08f, 0.88f, 1f, 1f);
        fillImage.raycastTarget = false;

        GameObject handleObject = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        handleObject.transform.SetParent(sliderObject.transform, false);
        RectTransform handleRect = handleObject.GetComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0f, 0.5f);
        handleRect.anchorMax = new Vector2(0f, 0.5f);
        handleRect.pivot = new Vector2(0.5f, 0.5f);
        handleRect.sizeDelta = new Vector2(26f, 30f);
        Image handleImage = handleObject.GetComponent<Image>();
        handleImage.color = new Color(0.92f, 0.98f, 1f, 1f);

        Slider slider = sliderObject.GetComponent<Slider>();
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = defaultValue;
        return slider;
    }

    private static Toggle CreateToggle(string name, Transform parent, Vector2 position)
    {
        GameObject toggleObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Toggle));
        toggleObject.transform.SetParent(parent, false);
        RectTransform toggleRect = toggleObject.GetComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(0.5f, 0.5f);
        toggleRect.anchorMax = new Vector2(0.5f, 0.5f);
        toggleRect.pivot = new Vector2(0.5f, 0.5f);
        toggleRect.anchoredPosition = position;
        toggleRect.sizeDelta = new Vector2(48f, 42f);
        Image background = toggleObject.GetComponent<Image>();
        background.color = new Color(0.11f, 0.16f, 0.29f, 1f);
        background.raycastTarget = true;

        GameObject mark = new GameObject("Checkmark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        mark.transform.SetParent(toggleObject.transform, false);
        RectTransform markRect = mark.GetComponent<RectTransform>();
        markRect.anchorMin = new Vector2(0.5f, 0.5f);
        markRect.anchorMax = new Vector2(0.5f, 0.5f);
        markRect.pivot = new Vector2(0.5f, 0.5f);
        markRect.sizeDelta = new Vector2(24f, 24f);
        Image markImage = mark.GetComponent<Image>();
        markImage.color = new Color(0.08f, 0.88f, 1f, 1f);
        markImage.raycastTarget = false;

        Toggle toggle = toggleObject.GetComponent<Toggle>();
        toggle.targetGraphic = background;
        toggle.graphic = markImage;
        toggle.isOn = false;
        return toggle;
    }

    private static void CreateEventSystem()
    {
        GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
        InputSystemUIInputModule inputModule = eventSystemObject.AddComponent<InputSystemUIInputModule>();
        inputModule.AssignDefaultActions();
    }

    private static Button CreateButton(string name, Transform parent, string label, Vector2 position, Vector2 size, UnityAction action)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.08f, 0.36f, 0.47f, 0.98f);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.08f, 0.36f, 0.47f, 0.98f);
        colors.highlightedColor = new Color(0.12f, 0.58f, 0.68f, 1f);
        colors.pressedColor = new Color(0.04f, 0.24f, 0.32f, 1f);
        button.colors = colors;
        UnityEventTools.AddPersistentListener(button.onClick, action);
        EditorUtility.SetDirty(button);
        buttonObject.AddComponent<UiButtonMotion2D>();

        CreateText("Label", buttonObject.transform, label, 24, Color.white, TextAnchor.MiddleCenter, Vector2.zero, size);
        return button;
    }

    private static GameObject CreateOverlay(string name, Transform parent, Vector2 cardSize, string cardName)
    {
        GameObject overlay = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        overlay.transform.SetParent(parent, false);
        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        Image backdrop = overlay.GetComponent<Image>();
        backdrop.color = new Color(0.018f, 0.026f, 0.09f, 0.94f);
        backdrop.raycastTarget = true;

        Image card = CreatePanel(cardName, overlay.transform, new Color(0.045f, 0.065f, 0.18f, 0.98f), false);
        card.raycastTarget = true;
        card.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        card.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        card.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        card.rectTransform.anchoredPosition = Vector2.zero;
        card.rectTransform.sizeDelta = cardSize;
        return overlay;
    }

    private static Image CreatePanel(string name, Transform parent, Color color, bool stretch)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        if (stretch)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
        panel.GetComponent<Image>().color = color;
        panel.GetComponent<Image>().raycastTarget = false;
        return panel.GetComponent<Image>();
    }

    private static Text CreateText(string name, Transform parent, string content, int fontSize, Color color, TextAnchor alignment, Vector2 position, Vector2 size, Vector2? anchorMin = null, Vector2? anchorMax = null)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        Vector2 min = anchorMin ?? new Vector2(0.5f, 0.5f);
        Vector2 max = anchorMax ?? new Vector2(0.5f, 0.5f);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.pivot = alignment == TextAnchor.UpperLeft ? new Vector2(0, 1) :
                     alignment == TextAnchor.UpperRight ? new Vector2(1, 1) : new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Text text = textObject.GetComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (text.font == null) text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    private static void ConfigureBuildScenes(string[] sceneNames)
    {
        List<EditorBuildSettingsScene> result = new List<EditorBuildSettingsScene>();
        foreach (string sceneName in sceneNames)
        {
            string path = SceneFolder + "/" + sceneName + ".unity";
            result.Add(new EditorBuildSettingsScene(path, true));
        }
        EditorBuildSettings.scenes = result.ToArray();
    }

    private static void SetObject(Object target, string propertyName, Object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null) property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetFloat(Object target, string propertyName, float value)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null) property.floatValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetInt(Object target, string propertyName, int value)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null) property.intValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetString(Object target, string propertyName, string value)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null) property.stringValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetBool(Object target, string propertyName, bool value)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null) property.boolValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetVector2(Object target, string propertyName, Vector2 value)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null) property.vector2Value = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}


