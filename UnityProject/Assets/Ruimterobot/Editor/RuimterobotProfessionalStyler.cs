using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class RuimterobotProfessionalStyler
{
    private const string Art = "Assets/Ruimterobot/Art/";
    private static Sprite hero;
    private static Sprite[] bosses;
    private static Sprite shadowMinion;
    private static Sprite chargerEnemy;
    private static Sprite sentryEnemy;
    private static Sprite[] platforms;
    private static Sprite energyShard;
    private static Sprite aegis;
    private static Sprite overdrive;
    private static Sprite skybound;
    private static Sprite cityPortal;
    private static Sprite hazard;
    private static Sprite energyBolt;
    private static Sprite comicPanel;
    private static Sprite[] backdrops;
    private static Sprite[] blurredBackdrops;
    private static Font interFont;

    public static void ApplyToBuiltScenes()
    {
        const string fontPath = "Assets/Ruimterobot/Fonts/Inter-Variable.ttf";
        if (File.Exists(fontPath)) AssetDatabase.ImportAsset(fontPath, ImportAssetOptions.ForceSynchronousImport);
        interFont = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
        hero = LoadSprite("PulseHero.png", 1024f);
        bosses = new[]
        {
            LoadSprite("BossTitan.png", 900f),
            LoadSprite("BossPyra.png", 900f),
            LoadSprite("BossNull.png", 900f)
        };
        shadowMinion = LoadSprite("ShadowMinion.png", 900f);
        chargerEnemy = LoadSprite("BossCrabPro.png", 700f);
        sentryEnemy = LoadSprite("DroneEnemyPro.png", 900f);
        platforms = new[]
        {
            LoadSprite("PlatformRooftop.png", 256f),
            LoadSprite("PlatformMetro.png", 256f),
            LoadSprite("PlatformTower.png", 256f)
        };
        energyShard = LoadSprite("EnergyShard.png", 512f);
        aegis = LoadSprite("UpgradeAegis.png", 512f);
        overdrive = LoadSprite("UpgradeOverdrive.png", 512f);
        skybound = LoadSprite("UpgradeSkybound.png", 512f);
        cityPortal = LoadSprite("CityPortal.png", 384f);
        hazard = LoadSprite("HazardSpikePro.png", 256f);
        energyBolt = LoadSprite("EnergyBolt.png", 512f);
        comicPanel = LoadSprite("ComicPanel.png", 100f, new Vector4(64f, 64f, 64f, 64f));
        backdrops = new[]
        {
            LoadSprite("BackdropPulseMenu.png", 102f),
            LoadSprite("BackdropRooftop.png", 102f),
            LoadSprite("BackdropMetro.png", 102f),
            LoadSprite("BackdropTower.png", 102f),
            LoadSprite("BackdropPulseVictory.png", 102f)
        };
        blurredBackdrops = new[]
        {
            LoadSprite(CreateDepthBlurAsset("BackdropRooftop.png"), 102f),
            LoadSprite(CreateDepthBlurAsset("BackdropMetro.png"), 102f),
            LoadSprite(CreateDepthBlurAsset("BackdropTower.png"), 102f)
        };

        string[] sceneNames = { "MainMenu", "Level1", "Level2", "Level3", "Victory" };
        for (int i = 0; i < sceneNames.Length; i++)
        {
            string scenePath = "Assets/Ruimterobot/Scenes/" + sceneNames[i] + ".unity";
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            ApplyWorldArt(scene, i);
            ApplyInterface(scene, sceneNames[i]);
            EditorSceneManager.SaveScene(scene);
        }

        ApplyProjectilePrefab("Assets/Ruimterobot/Prefabs/Projectile.prefab", energyBolt, new Color(0.56f, 0.96f, 1f));
        ApplyProjectilePrefab("Assets/Ruimterobot/Prefabs/BossProjectile.prefab", energyBolt, new Color(1f, 0.34f, 0.3f));
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("PULSE-art, drie stadswerelden en superheldeninterface zijn toegepast.");
    }

    private static Sprite LoadSprite(string fileName, float pixelsPerUnit, Vector4 border = default)
    {
        string path = Art + fileName;
        if (!File.Exists(path))
            throw new System.IO.FileNotFoundException("De PULSE-art ontbreekt.", path);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            throw new System.InvalidOperationException("Kon afbeelding niet als Unity-sprite laden: " + path);

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.maxTextureSize = 4096;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.spriteBorder = border;
        importer.SaveAndReimport();

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            throw new System.InvalidOperationException("Kon sprite niet laden: " + path);
        return sprite;
    }

    private static string CreateDepthBlurAsset(string sourceName)
    {
        string sourcePath = Art + sourceName;
        string blurredName = Path.GetFileNameWithoutExtension(sourceName) + "_DepthBlur.png";
        string blurredPath = Art + blurredName;
        bool needsRefresh = !File.Exists(blurredPath) ||
            File.GetLastWriteTimeUtc(sourcePath) > File.GetLastWriteTimeUtc(blurredPath);
        if (!needsRefresh) return blurredName;

        Texture2D source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        source.LoadImage(File.ReadAllBytes(sourcePath));
        Color32[] sourcePixels = source.GetPixels32();
        const int reduction = 5;
        int width = Mathf.Max(128, source.width / reduction);
        int height = Mathf.Max(72, source.height / reduction);
        Color32[] lowResolution = new Color32[width * height];
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            int sourceX = x * source.width / width;
            int sourceY = y * source.height / height;
            int endX = Mathf.Max(sourceX + 1, (x + 1) * source.width / width);
            int endY = Mathf.Max(sourceY + 1, (y + 1) * source.height / height);
            float red = 0f, green = 0f, blue = 0f, alpha = 0f;
            int samples = 0;
            for (int sy = sourceY; sy < endY; sy++)
            for (int sx = sourceX; sx < endX; sx++)
            {
                Color32 pixel = sourcePixels[sy * source.width + sx];
                red += pixel.r; green += pixel.g; blue += pixel.b; alpha += pixel.a; samples++;
            }
            lowResolution[y * width + x] = new Color32((byte)(red / samples), (byte)(green / samples),
                (byte)(blue / samples), (byte)(alpha / samples));
        }

        Texture2D blurred = new Texture2D(width, height, TextureFormat.RGBA32, false);
        blurred.SetPixels32(lowResolution);
        blurred.Apply();
        File.WriteAllBytes(blurredPath, blurred.EncodeToPNG());
        Object.DestroyImmediate(blurred);
        Object.DestroyImmediate(source);
        AssetDatabase.ImportAsset(blurredPath, ImportAssetOptions.ForceSynchronousImport);
        return blurredName;
    }

    private static void ApplyWorldArt(Scene scene, int theme)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (SpriteRenderer renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                string name = renderer.gameObject.name;
                if (name == "PulseHeroSprite" || name == "MenuPulseSprite")
                {
                    renderer.sprite = hero;
                    renderer.color = Color.white;
                    if (name == "PulseHeroSprite") renderer.transform.localScale = new Vector3(0.72f, 0.78f, 1f);
                }
                else if (name == "EnergyFocus")
                {
                    renderer.sprite = energyBolt;
                    renderer.color = new Color(0.78f, 0.98f, 1f, 0.95f);
                }
                else if (name == "BossSprite")
                {
                    renderer.sprite = bosses[Mathf.Clamp(theme - 1, 0, bosses.Length - 1)];
                    renderer.color = Color.white;
                }
                else if (name == "Schaduwwachter" || name == "RiftSwooper")
                {
                    renderer.sprite = shadowMinion;
                    renderer.color = Color.white;
                }
                else if (name == "RiftStalker")
                {
                    renderer.sprite = shadowMinion;
                    renderer.color = Color.white;
                }
                else if (name == "RiftCharger")
                {
                    renderer.sprite = chargerEnemy;
                    renderer.color = Color.white;
                }
                else if (name == "RiftSentry")
                {
                    renderer.sprite = sentryEnemy;
                    renderer.color = Color.white;
                }
                else if (name == "Energiefragment")
                {
                    renderer.sprite = energyShard;
                    renderer.color = Color.white;
                }
                else if (name == "Heldentoken" || name == "AegisUpgrade")
                {
                    renderer.sprite = aegis;
                    renderer.color = Color.white;
                }
                else if (name == "OverdriveUpgrade")
                {
                    renderer.sprite = overdrive;
                    renderer.color = Color.white;
                }
                else if (name == "SkyboundUpgrade")
                {
                    renderer.sprite = skybound;
                    renderer.color = Color.white;
                }
                else if (name == "Checkpoint" || name == "Uitgang")
                {
                    renderer.sprite = cityPortal;
                    renderer.color = name == "Checkpoint" ? new Color(0.56f, 0.92f, 1f) : Color.white;
                }
                else if (name.Contains("Spike"))
                {
                    renderer.sprite = hazard;
                    renderer.color = Color.white;
                }
                else if (IsPlatform(renderer.gameObject))
                {
                    ApplyPlatformSurface(renderer, Mathf.Clamp(theme - 1, 0, platforms.Length - 1));
                }
                else if (name.Contains("PlatformEdge"))
                {
                    renderer.enabled = false;
                }
            }
        }

        Camera camera = Camera.main;
        if (camera != null)
        {
            camera.backgroundColor = new Color(0.055f, 0.075f, 0.12f, 1f);
            camera.orthographicSize = 5.3f;
            GameObject backdrop = GameObject.Find("CinematicBackdrop");
            if (backdrop == null) backdrop = new GameObject("CinematicBackdrop");
            backdrop.transform.position = new Vector3(0f, camera.transform.position.y, 8f);
            backdrop.transform.rotation = Quaternion.identity;
            backdrop.transform.localScale = Vector3.one;
            SpriteRenderer backgroundRenderer = backdrop.GetComponent<SpriteRenderer>();
            if (backgroundRenderer == null) backgroundRenderer = backdrop.AddComponent<SpriteRenderer>();
            int backdropIndex = Mathf.Clamp(theme, 0, backdrops.Length - 1);
            bool layeredCity = theme > 0 && theme < 4;
            backgroundRenderer.sprite = layeredCity ? blurredBackdrops[theme - 1] : backdrops[backdropIndex];
            backgroundRenderer.color = Color.white;
            backgroundRenderer.sortingOrder = -100;
            float height = camera.orthographicSize * 2f;
            float width = height * camera.aspect;
            Vector2 size = backgroundRenderer.sprite.bounds.size;
            float cover = Mathf.Max(height / size.y, width / size.x);
            backdrop.transform.localScale = new Vector3(cover * (theme > 0 && theme < 4 ? 3.1f : 1f), cover, 1f);
            ParallaxBackground2D parallax = backdrop.GetComponent<ParallaxBackground2D>();
            if (layeredCity)
            {
                if (parallax == null) parallax = backdrop.AddComponent<ParallaxBackground2D>();
                parallax.Configure(0.12f);
                GameObject detail = GameObject.Find("CityDetailBackdrop");
                if (detail == null) detail = new GameObject("CityDetailBackdrop");
                detail.transform.position = new Vector3(0f, camera.transform.position.y, 7.8f);
                detail.transform.rotation = Quaternion.identity;
                detail.transform.localScale = backdrop.transform.localScale;
                SpriteRenderer detailRenderer = detail.GetComponent<SpriteRenderer>();
                if (detailRenderer == null) detailRenderer = detail.AddComponent<SpriteRenderer>();
                detailRenderer.sprite = backdrops[backdropIndex];
                detailRenderer.color = new Color(1f, 1f, 1f, 0.34f);
                detailRenderer.sortingOrder = -98;
                ParallaxBackground2D detailParallax = detail.GetComponent<ParallaxBackground2D>();
                if (detailParallax == null) detailParallax = detail.AddComponent<ParallaxBackground2D>();
                detailParallax.Configure(0.39f);
            }
            else if (parallax != null)
            {
                Object.DestroyImmediate(parallax);
            }
        }
        EnsureCityParticles(theme);
    }

    private static bool IsPlatform(GameObject gameObject)
    {
        string name = gameObject.name.ToLowerInvariant();
        return gameObject.GetComponent<Collider2D>() != null &&
               (name.StartsWith("platform ") || name.StartsWith("springplatform") ||
                name.StartsWith("hoge route") || name == "bewegend platform");
    }

    private static void ApplyPlatformSurface(SpriteRenderer baseRenderer, int platformIndex)
    {
        baseRenderer.enabled = false;
        Transform surface = baseRenderer.transform.Find("ProfessionalSurface");
        if (surface == null)
        {
            GameObject child = new GameObject("ProfessionalSurface");
            surface = child.transform;
            surface.SetParent(baseRenderer.transform, false);
        }
        surface.localPosition = Vector3.zero;
        Vector2 spriteSize = platforms[platformIndex].bounds.size;
        surface.localScale = new Vector3(1f / spriteSize.x, 1f / spriteSize.y, 1f);
        SpriteRenderer surfaceRenderer = surface.GetComponent<SpriteRenderer>();
        if (surfaceRenderer == null) surfaceRenderer = surface.gameObject.AddComponent<SpriteRenderer>();
        surfaceRenderer.sprite = platforms[platformIndex];
        surfaceRenderer.color = Color.white;
        surfaceRenderer.sortingOrder = baseRenderer.sortingOrder;
        CollapsingPlatform2D crumbling = baseRenderer.GetComponent<CollapsingPlatform2D>();
        if (crumbling != null) crumbling.ConfigureVisual(surfaceRenderer);
    }

    private static void EnsureCityParticles(int theme)
    {
        GameObject root = GameObject.Find("AtmosphericDust");
        if (root == null) root = new GameObject("CityLightParticles");
        root.name = "CityLightParticles";
        root.transform.position = new Vector3(0f, 0.6f, 1f);
        ParticleSystem particles = root.GetComponent<ParticleSystem>();
        if (particles == null) particles = root.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.loop = true;
        main.duration = 10f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 10f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.1f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.04f);
        main.startColor = theme == 2 ? new Color(1f, 0.56f, 0.32f, 0.5f) :
                          theme == 3 ? new Color(0.88f, 0.68f, 1f, 0.42f) :
                          new Color(0.58f, 0.86f, 1f, 0.45f);
        main.maxParticles = 260;
        main.playOnAwake = true;
        var emission = particles.emission;
        emission.rateOverTime = 18f;
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(110f, 13f, 0.1f);
        var renderer = root.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = -15;
        particles.Play(true);
    }

    private static void ApplyInterface(Scene scene, string sceneName)
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        foreach (Text text in canvas.GetComponentsInChildren<Text>(true))
        {
            if (interFont != null) text.font = interFont;
            text.fontStyle = text.fontSize >= 28 ? FontStyle.Bold : FontStyle.Normal;
            text.color = new Color(0.93f, 0.95f, 1f, 1f);
            text.supportRichText = false;
            if (text.name == "MenuControls" || text.name == "Bediening" || text.name == "SettingsUitleg")
                text.color = new Color(0.72f, 0.82f, 0.94f, 1f);
            if (text.name == "Subtitel" || text.name == "IntroBericht" || text.name == "BossDoel")
                text.color = new Color(1f, 0.79f, 0.37f, 1f);
            if (text.name == "Titel")
            {
                text.fontSize = 92;
                text.fontStyle = FontStyle.Bold;
                text.color = new Color(1f, 0.86f, 0.25f, 1f);
                Outline outline = text.GetComponent<Outline>();
                if (outline == null) outline = text.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0.03f, 0.1f, 0.22f, 0.9f);
                outline.effectDistance = new Vector2(3f, -3f);
            }
        }

        foreach (Image image in canvas.GetComponentsInChildren<Image>(true))
        {
            if (image.name == "SettingsKaart" || image.name == "UpgradeCard" || image.name == "StatisticsCard" ||
                image.name == "AchievementsCard" || image.name == "TutorialCard" || image.name == "BossHealthBackground" ||
                image.name == "AchievementToast" ||
                image.name == "ProfessionalMenuPanel" || image.name == "PauseCard" ||
                image.name == "GameOverCard" || image.name == "HudFrame")
            {
                image.sprite = comicPanel;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
            }
            else if (image.name == "MenuOverlay") image.color = new Color(0.02f, 0.045f, 0.1f, 0.14f);
            else if (image.name == "SettingsPanel") image.color = new Color(0.015f, 0.04f, 0.1f, 0.95f);
            else if (image.name == "Pauzepaneel") image.color = new Color(0.015f, 0.04f, 0.1f, 0.9f);
            else if (image.name == "GameOverPaneel") image.color = new Color(0.1f, 0.025f, 0.08f, 0.93f);
            else if (image.name == "Achtergrond") image.color = new Color(0.02f, 0.045f, 0.1f, 0.24f);
        }

        foreach (Button button in canvas.GetComponentsInChildren<Button>(true))
        {
            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = comicPanel;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
            }
            ColorBlock colors = button.colors;
            bool primary = button.name == "StartKnop";
            colors.normalColor = primary ? new Color(1f, 0.8f, 0.26f, 1f) : new Color(0.12f, 0.27f, 0.48f, 1f);
            colors.highlightedColor = primary ? new Color(1f, 0.91f, 0.48f, 1f) : new Color(0.2f, 0.43f, 0.69f, 1f);
            colors.pressedColor = new Color(0.25f, 0.57f, 0.82f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.fadeDuration = 0.12f;
            button.colors = colors;
            Text label = button.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.fontStyle = FontStyle.Bold;
                label.fontSize = Mathf.Min(label.fontSize, 24);
                label.color = primary ? new Color(0.1f, 0.13f, 0.2f, 1f) : Color.white;
            }
        }

        if (sceneName == "MainMenu") StyleMainMenu(canvas.transform);
        if (sceneName.StartsWith("Level")) StyleLevelHud(canvas.transform);
        if (sceneName == "Victory")
        {
            CreateText(canvas.transform, "VictoryTag", "METROVILLE  /  HELDEN VAN DE STAD", new Vector2(0f, 300f),
                new Vector2(900f, 38f), 20, new Color(1f, 0.84f, 0.32f, 1f));
        }
        if (sceneName.StartsWith("Level"))
        {
            StyleOverlayCard(canvas.transform, "Pauzepaneel", "PauseCard", new Vector2(0f, 20f), new Vector2(670f, 560f));
            StyleOverlayCard(canvas.transform, "GameOverPaneel", "GameOverCard", new Vector2(0f, 15f), new Vector2(740f, 560f));
            RectTransform hud = CreateFrame(canvas.transform, "HudFrame", new Vector2(15f, -10f), new Vector2(490f, 78f),
                new Vector2(0f, 1f), new Vector2(0f, 1f));
            hud.SetAsFirstSibling();
        }
        if (sceneName == "MainMenu")
        {
            GameObject settings = canvas.transform.Find("SettingsPanel")?.gameObject;
            if (settings != null)
                StyleOverlayCard(settings.transform, "", "SettingsKaart", Vector2.zero, new Vector2(900f, 720f));
        }
    }

    private static void StyleMainMenu(Transform canvas)
    {
        RectTransform panel = CreateFrame(canvas, "ProfessionalMenuPanel", new Vector2(-505f, 8f), new Vector2(1040f, 890f));
        panel.SetSiblingIndex(1);
        SetText(canvas, "Subtitel", "RISE OF THE RIFT  //  EEN ORIGINELE SUPERHELDENSAGA", 20, new Color(0.75f, 0.87f, 1f, 1f));
        SetText(canvas, "IntroBericht", "METROVILLE STAAT OP DE RAND VAN DE BREUK.", 22, new Color(1f, 0.62f, 0.36f, 1f));
        RectTransform controls = canvas.Find("MenuControls")?.GetComponent<RectTransform>();
        if (controls != null) { controls.anchoredPosition = new Vector2(-505f, -356f); controls.sizeDelta = new Vector2(960f, 72f); }
        SetText(canvas, "MenuControls", "A/D  BEWEGEN     SPATIE  DUBBELSPRONG     SHIFT  DASH\nJ  PULSE     1 / 2 / 3  POWERS     SPRING OP VIJANDEN     ESC  PAUZE", 15,
            new Color(0.75f, 0.84f, 0.95f, 1f));
        CreateText(canvas, "DivisionLabel", "METROVILLE  /  SUPERHELDENUNIVERSUM", new Vector2(-505f, 455f), new Vector2(900f, 32f),
            17, new Color(0.6f, 0.83f, 1f, 1f));
        CreateText(canvas, "MissionBrief", "Een kosmische breuk opent zich boven de stad.\nOntwaak drie krachten, versla de Rift-kampioenen en red Metroville.",
            new Vector2(520f, -318f), new Vector2(760f, 72f), 19, new Color(0.9f, 0.93f, 1f, 1f));
        CreateText(canvas, "MenuStatus", "3 WIJKEN     /     3 RIFT-KAMPIOENEN     /     3 KRACHTRANGEN", new Vector2(520f, -410f), new Vector2(720f, 36f),
            15, new Color(1f, 0.86f, 0.43f, 1f));
        SetText(canvas, "CreatorCredits", "MADE BY JAIDEN ASRUF", 16, new Color(1f, 0.82f, 0.35f, 1f));
        SetText(canvas, "Titel", "PULSE", 92, new Color(1f, 0.86f, 0.25f, 1f));
    }

    private static void StyleLevelHud(Transform canvas)
    {
        SetText(canvas, "Levens", "VITALS  03 / 05", 20, new Color(0.93f, 0.96f, 1f, 1f));
        SetText(canvas, "EnergieScore", "SHARDS  00", 20, new Color(1f, 0.85f, 0.36f, 1f));
        SetText(canvas, "Bediening", "SPATIE SPRONG/STOMP    SHIFT DASH    J PULSE    1/2/3 POWERS    ESC PAUZE", 13,
            new Color(0.78f, 0.86f, 0.96f, 1f));
        SetText(canvas, "PowerupStatus", "", 15, new Color(0.61f, 0.93f, 1f, 1f));
        SetText(canvas, "DashStatus", "DASH GEREED  /  SHIFT", 15, new Color(0.63f, 0.89f, 1f, 1f));
    }

    private static void StyleOverlayCard(Transform parent, string parentName, string cardName, Vector2 position, Vector2 size)
    {
        Transform target = string.IsNullOrEmpty(parentName) ? parent : parent.Find(parentName);
        if (target == null) return;
        RectTransform card = CreateFrame(target, cardName, position, size);
        card.SetAsFirstSibling();
    }

    private static RectTransform CreateFrame(Transform parent, string name, Vector2 position, Vector2 size,
        Vector2? anchorMin = null, Vector2? anchorMax = null)
    {
        Transform existing = parent.Find(name);
        GameObject gameObject = existing == null
            ? new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image))
            : existing.gameObject;
        gameObject.transform.SetParent(parent, false);
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin ?? new Vector2(0.5f, 0.5f);
        rect.anchorMax = anchorMax ?? new Vector2(0.5f, 0.5f);
        rect.pivot = rect.anchorMin == new Vector2(0f, 1f) ? new Vector2(0f, 1f) : new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = gameObject.GetComponent<Image>();
        image.sprite = comicPanel;
        image.type = Image.Type.Sliced;
        image.color = Color.white;
        image.raycastTarget = false;
        return rect;
    }

    private static Text CreateText(Transform parent, string name, string content, Vector2 position, Vector2 size, int fontSize, Color color)
    {
        Transform old = parent.Find(name);
        if (old != null) Object.DestroyImmediate(old.gameObject);
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        gameObject.transform.SetParent(parent, false);
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Text text = gameObject.GetComponent<Text>();
        text.text = content;
        text.font = interFont != null ? interFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    private static void SetText(Transform parent, string name, string content, int fontSize, Color color)
    {
        Transform target = parent.Find(name);
        Text text = target != null ? target.GetComponent<Text>() : null;
        if (text == null) return;
        text.text = content;
        text.fontSize = fontSize;
        text.color = color;
        text.fontStyle = FontStyle.Bold;
    }

    private static void ApplyProjectilePrefab(string path, Sprite sprite, Color tint)
    {
        if (!File.Exists(path)) return;
        GameObject prefab = PrefabUtility.LoadPrefabContents(path);
        try
        {
            SpriteRenderer renderer = prefab.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.sprite = sprite;
                renderer.color = tint;
            }
            PrefabUtility.SaveAsPrefabAsset(prefab, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
    }
}
