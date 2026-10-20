using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class VesperGameBootstrap : MonoBehaviour
{
    private static readonly Sprite[,] BackdropCache = new Sprite[6, 3];
    private static readonly string[] SceneNames = { "", "Region01", "Region02", "Region03", "Region04", "Region05" };
    private static readonly string[] RegionNames = { "", "HOLLOW GROVE", "DROWNED ARCHIVE", "EMBER VEINS", "MOONLIT CANOPY", "RIFT HEART" };
    private static readonly Color[] Accents =
    {
        Color.white,
        new Color(0.42f, 0.9f, 0.68f),
        new Color(0.37f, 0.75f, 0.92f),
        new Color(1f, 0.46f, 0.26f),
        new Color(0.76f, 0.58f, 1f),
        new Color(1f, 0.37f, 0.52f)
    };
    private const float WorldWidth = 104f;
    private int region;
    private Color accent;
    private Transform cameraTransform;
    private Transform worldRoot;
    private VesperPlayerController2D player;
    private int pickupCounter;

    private void Awake()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 1;
        AudioListener.volume = PlayerPrefs.GetFloat("VESPER_Volume", 0.8f);
        VesperProgress.Load();
        Time.timeScale = 1f;
        EnsureCamera();
        if (gameObject.GetComponent<VesperEffectPool>() == null) gameObject.AddComponent<VesperEffectPool>();
        gameObject.AddComponent<VesperUIUpdater>();
        if (SceneManager.GetActiveScene().name == "VesperMenu") BuildMenu();
        else BuildRegion();
    }

    private void EnsureCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            GameObject go = new GameObject("Main Camera"); go.tag = "MainCamera";
            camera = go.AddComponent<Camera>(); go.AddComponent<AudioListener>();
        }
        camera.orthographic = true;
        camera.orthographicSize = 6.4f;
        camera.transform.position = new Vector3(7f, 2f, -10f);
        cameraTransform = camera.transform;
    }

    private void BuildMenu()
    {
        region = Mathf.Clamp(VesperProgress.CurrentRegion, 1, 5);
        accent = Accents[1];
        cameraTransform.position = new Vector3(46f, 0.8f, -10f);
        cameraTransform.GetComponent<Camera>().backgroundColor = new Color(0.012f, 0.025f, 0.045f);
        CreateBackdrop(1, true);
        CreateMenuGate();
        CreateAmbientParticles(1);
        CreateAudio(1);
        VesperGameUI.BuildMenu();
    }

    private void BuildRegion()
    {
        region = ParseRegion(SceneManager.GetActiveScene().name);
        if (region < 1 || region > 5) region = 1;
        accent = Accents[region];
        VesperProgress.Discover(region);
        cameraTransform.GetComponent<Camera>().backgroundColor = Color.Lerp(new Color(0.012f, 0.018f, 0.032f), accent * 0.08f, 0.3f);
        VesperCamera2D cameraRig = cameraTransform.GetComponent<VesperCamera2D>();
        if (cameraRig == null) cameraRig = cameraTransform.gameObject.AddComponent<VesperCamera2D>();
        cameraRig.SetRegionWidth(WorldWidth);
        worldRoot = new GameObject("WORLD // " + RegionNames[region]).transform;
        pickupCounter = 0;
        CreateBackdrop(region, false);
        CreateRegionGeometry();
        CreateTransitions();
        CreateCheckpointsAndPickups();
        CreateEnemySet();
        CreateAmbientParticles(region);
        CreateAudio(region);
        CreatePlayer();
        VesperGameUI.BuildHud(region);
        VesperProgress.Changed += RefreshPanels;
        if (player != null)
        {
            player.HealthChanged += VesperGameUI.SetVitals;
            player.EnergyChanged += VesperGameUI.SetEnergy;
            VesperGameUI.SetVitals(player.Health, player.MaximumHealth);
            VesperGameUI.SetEnergy(player.PulseEnergy, 100f + VesperProgress.ReservoirTier * 25f);
        }
    }

    private void OnDestroy() { VesperProgress.Changed -= RefreshPanels; }
    private void RefreshPanels() { VesperGameUI.RefreshState(); }

    private static int ParseRegion(string sceneName)
    {
        if (sceneName.StartsWith("Region") && int.TryParse(sceneName.Substring(6), out int parsed)) return parsed;
        return 1;
    }

    private void CreateBackdrop(int area, bool menu)
    {
        Color color = Accents[area];
        SpriteRenderer sky = CreateSprite("Atmosphere // sky", new Vector3(WorldWidth * 0.5f, 1f, 0f), new Vector2(WorldWidth + 25f, 28f), VesperArtFactory.Square, new Color(0.025f, 0.04f, 0.065f), -30);
        SpriteRenderer haze = CreateBackdropSprite("Atmosphere // distant haze", new Vector3(WorldWidth * 0.5f, 1.5f, 0f), new Vector2(WorldWidth + 24f, 24f), MakeBackdrop(area, 0), Color.white, -28);
        SpriteRenderer mountains = CreateBackdropSprite("Distance // basalt silhouettes", new Vector3(WorldWidth * 0.5f, -0.1f, 0f), new Vector2(WorldWidth + 20f, 24f), MakeBackdrop(area, 1), Color.white, -22);
        SpriteRenderer arches = CreateBackdropSprite("Distance // ancient arches", new Vector3(WorldWidth * 0.5f, -0.1f, 0f), new Vector2(WorldWidth + 12f, 22f), MakeBackdrop(area, 2), Color.white, -16);
        if (!menu)
        {
            AddParallax(haze.transform, 0.1f);
            AddParallax(mountains.transform, 0.24f);
            AddParallax(arches.transform, 0.46f);
        }
        else
        {
            haze.color = new Color(0.7f, 0.84f, 0.82f, 1f);
            mountains.color = new Color(0.72f, 0.88f, 0.83f, 1f);
            arches.color = new Color(0.9f, 0.95f, 0.88f, 1f);
        }
        for (int i = 0; i < 8; i++)
        {
            float x = i * 9.5f + UnityEngine.Random.Range(-1f, 1f);
            float y = UnityEngine.Random.Range(-0.8f, 4.6f);
            SpriteRenderer mote = CreateSprite("Atmosphere // lantern", new Vector3(x, y, 0f), Vector2.one * UnityEngine.Random.Range(0.5f, 1.1f), VesperArtFactory.Glow, new Color(color.r, color.g, color.b, 0.38f), -8);
            mote.gameObject.AddComponent<VesperAmbientMotion>().Configure(UnityEngine.Random.Range(0.2f, 0.6f), UnityEngine.Random.Range(0.1f, 0.5f));
        }
        if (!menu)
        {
            for (int i = 0; i < 12; i++)
            {
                float x = i * 6f + 2f;
                float height = UnityEngine.Random.Range(5f, 11f);
                SpriteRenderer pillar = CreateSprite("Foreground // root shard", new Vector3(x, height * 0.12f - 2.4f, 0f), new Vector2(UnityEngine.Random.Range(0.12f, 0.3f), height), VesperArtFactory.Square, new Color(0.015f, 0.03f, 0.045f, 0.72f), 3);
                pillar.gameObject.AddComponent<VesperParallaxLayer>().Configure(cameraTransform, 1.14f);
                SpriteRenderer rim = CreateSprite("Foreground // biolume edge", pillar.transform.position + Vector3.right * 0.12f, new Vector2(0.035f, height * 0.78f), VesperArtFactory.Square, new Color(color.r, color.g, color.b, 0.26f), 4);
                rim.gameObject.AddComponent<VesperParallaxLayer>().Configure(cameraTransform, 1.14f);
            }
        }
    }

    private Sprite MakeBackdrop(int area, int layer)
    {
        if (BackdropCache[area, layer] != null) return BackdropCache[area, layer];
        const int width = 512, height = 256;
        Texture2D t = new Texture2D(width, height, TextureFormat.RGBA32, false);
        t.filterMode = FilterMode.Bilinear; t.wrapMode = TextureWrapMode.Clamp;
        Color accentColor = Accents[area];
        UnityEngine.Random.State prior = UnityEngine.Random.state;
        UnityEngine.Random.InitState(area * 103 + layer * 719);
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            float fy = y / (float)height;
            float noise = UnityEngine.Random.Range(-0.004f, 0.004f);
            Color top = new Color(0.012f, 0.022f, 0.045f, 1f);
            Color bottom = Color.Lerp(new Color(0.035f, 0.055f, 0.072f, 1f), accentColor * 0.17f, 0.35f);
            Color c = Color.Lerp(bottom, top, fy) + new Color(noise, noise, noise, 0f);
            if (layer == 0 && UnityEngine.Random.value < 0.0008f && fy > 0.45f) c = Color.Lerp(c, accentColor, 0.78f);
            t.SetPixel(x, y, c);
        }
        if (layer == 1)
        {
            Color silhouette = new Color(0.025f, 0.045f, 0.06f, 1f);
            if (area == 1)
            {
                for (int i = 0; i < 12; i++)
                {
                    float x = i * 48f - 16f, h = UnityEngine.Random.Range(90f, 205f), w = UnityEngine.Random.Range(12f, 28f);
                    VesperArtFactory.FillPolygon(t, new Vector2[] { new(x, 0), new(x + w, 0), new(x + w * 0.66f, h), new(x + w * 0.3f, h + 20f), new(x + w * 0.18f, h * 0.42f) }, silhouette, 1);
                    VesperArtFactory.FillPolygon(t, new Vector2[] { new(x - 22f, h * 0.57f), new(x + w * 0.3f, h * 0.61f), new(x + w + 22f, h * 0.82f), new(x + w + 4f, h * 0.9f) }, Color.Lerp(silhouette, accentColor, 0.18f), 1);
                }
            }
            else if (area == 2)
            {
                for (int i = 0; i < 10; i++)
                {
                    float x = i * 57f - 8f, w = UnityEngine.Random.Range(16f, 34f), top = UnityEngine.Random.Range(110f, 220f);
                    VesperArtFactory.FillPolygon(t, new Vector2[] { new(x, 0), new(x + w, 0), new(x + w * 0.9f, top - 12f), new(x + w * 0.5f, top), new(x + w * 0.1f, top - 12f) }, silhouette, 1);
                    for (int row = 0; row < 3; row++) VesperArtFactory.FillPolygon(t, new Vector2[] { new(x + w * 0.28f, 36f + row * 38f), new(x + w * 0.72f, 36f + row * 38f), new(x + w * 0.72f, 43f + row * 38f), new(x + w * 0.28f, 43f + row * 38f) }, new Color(accentColor.r, accentColor.g, accentColor.b, 0.34f), 1);
                }
            }
            else if (area == 4)
            {
                for (int i = 0; i < 13; i++)
                {
                    float x = i * 42f - 10f, w = UnityEngine.Random.Range(8f, 17f), top = UnityEngine.Random.Range(130f, 230f);
                    VesperArtFactory.FillPolygon(t, new Vector2[] { new(x, 0), new(x + w, 0), new(x + w * 0.7f, top), new(x + w * 0.24f, top + 8f) }, silhouette, 1);
                    VesperArtFactory.FillPolygon(t, new Vector2[] { new(x - 28f, top * 0.62f), new(x + w * 0.28f, top * 0.7f), new(x + w + 30f, top * 0.49f), new(x + w + 18f, top * 0.57f) }, Color.Lerp(silhouette, accentColor, 0.16f), 1);
                }
            }
            else if (area == 5)
            {
                for (int i = 0; i < 16; i++)
                {
                    float x = i * 33f - 14f, w = UnityEngine.Random.Range(9f, 25f), top = UnityEngine.Random.Range(78f, 208f);
                    VesperArtFactory.FillPolygon(t, new Vector2[] { new(x, 0), new(x + w, 0), new(x + w * 0.62f, top * 0.62f), new(x + w * 0.53f, top), new(x + w * 0.41f, top * 0.72f), new(x + w * 0.2f, top * 0.84f) }, silhouette, 1);
                }
            }
            else
            {
                float x = -40f;
                while (x < width + 20)
                {
                    float top = UnityEngine.Random.Range(42f, 165f);
                    float w = UnityEngine.Random.Range(24f, 78f);
                    VesperArtFactory.FillPolygon(t, new Vector2[] { new(x, 0), new(x + w, 0), new(x + w * 0.56f, top), new(x + w * 0.42f, top + UnityEngine.Random.Range(5f, 22f)) }, silhouette, 1);
                    x += w * UnityEngine.Random.Range(0.55f, 0.95f);
                }
            }
        }
        if (layer == 2)
        {
            Color dark = new Color(0.018f, 0.035f, 0.05f, 0.78f);
            for (int i = 0; i < 8; i++)
            {
                float x = UnityEngine.Random.Range(0f, width);
                float w = UnityEngine.Random.Range(18f, 43f);
                float top = UnityEngine.Random.Range(62f, 198f);
                if (area == 1 || area == 4)
                    VesperArtFactory.FillPolygon(t, new Vector2[] { new(x - w, top * 0.72f), new(x - w * 0.45f, top * 0.79f), new(x + w * 0.42f, top * 0.67f), new(x + w, top * 0.75f), new(x + w * 0.55f, top * 0.84f), new(x - w * 0.35f, top * 0.82f) }, dark, 1);
                else if (area == 2)
                    VesperArtFactory.FillPolygon(t, new Vector2[] { new(x - w * 0.2f, 0), new(x + w * 1.2f, 0), new(x + w, top), new(x + w * 0.72f, top + 18f), new(x + w * 0.35f, top + 11f), new(x, top + 28f) }, dark, 1);
                else if (area == 3)
                    VesperArtFactory.FillPolygon(t, new Vector2[] { new(x - w * 0.2f, 0), new(x + w * 1.2f, 0), new(x + w, top * 0.64f), new(x + w * 0.7f, top * 0.68f), new(x + w * 0.5f, top), new(x + w * 0.34f, top * 0.74f), new(x, top * 0.91f) }, dark, 1);
                else
                    VesperArtFactory.FillPolygon(t, new Vector2[] { new(x - w * 0.2f, 0), new(x + w * 1.2f, 0), new(x + w, top), new(x + w * 0.72f, top + 18f), new(x + w * 0.35f, top + 11f), new(x, top + 28f) }, dark, 1);
                for (int j = 0; j < 3; j++)
                {
                    float yy = top * (0.25f + j * 0.2f);
                    VesperArtFactory.FillPolygon(t, new Vector2[] { new(x + w * 0.46f, yy), new(x + w * 0.55f, yy), new(x + w * 0.55f, yy + 12f), new(x + w * 0.46f, yy + 12f) }, new Color(accentColor.r, accentColor.g, accentColor.b, 0.32f), 1);
                }
            }
        }
        t.Apply(false, true);
        UnityEngine.Random.state = prior;
        BackdropCache[area, layer] = Sprite.Create(t, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 8f);
        return BackdropCache[area, layer];
    }

    private void AddParallax(Transform target, float factor) => target.gameObject.AddComponent<VesperParallaxLayer>().Configure(cameraTransform, factor);

    private void CreateMenuGate()
    {
        CreateSprite("Ancient gate // shadow", new Vector3(48f, 1f, 0f), new Vector2(12f, 12f), VesperArtFactory.Glow, new Color(accent.r, accent.g, accent.b, 0.2f), -2);
        CreateSprite("Ancient gate // monolith", new Vector3(48f, -1.2f, 0f), new Vector2(5.4f, 8.2f), VesperArtFactory.Square, new Color(0.03f, 0.07f, 0.08f), 0);
        CreateSprite("Ancient gate // opening", new Vector3(48f, -0.1f, 0f), new Vector2(1.8f, 5.7f), VesperArtFactory.Glow, new Color(accent.r, accent.g, accent.b, 0.6f), 1);
        for (int i = 0; i < 6; i++)
        {
            SpriteRenderer ledge = CreateSprite("Menu // stone shelf", new Vector3(38f + i * 4f, -3.6f + (i % 2) * 0.5f, 0f), new Vector2(4.2f, 0.48f), VesperArtFactory.Square, new Color(0.08f, 0.13f, 0.15f), 1);
            CreateSprite("Menu // living edge", ledge.transform.position + Vector3.up * 0.23f, new Vector2(3.9f, 0.045f), VesperArtFactory.Square, accent, 2);
        }
        SpriteRenderer figure = CreateSprite("Menu // wanderer", new Vector3(48f, -3.45f, 0f), new Vector2(1.2f, 1.4f), VesperArtFactory.Hero, Color.white, 4);
        figure.gameObject.AddComponent<VesperAmbientMotion>().Configure(0.35f, 0.08f);
        CreateSprite("Menu // aura", new Vector3(48f, -1.4f, 0f), new Vector2(5.5f, 7f), VesperArtFactory.Glow, new Color(accent.r, accent.g, accent.b, 0.2f), 3);
    }

    private void CreateRegionGeometry()
    {
        Transform geo = new GameObject("Architecture // region traversal").transform;
        geo.SetParent(worldRoot, false);
        Color stone = Color.Lerp(new Color(0.035f, 0.065f, 0.075f), accent * 0.24f, 0.3f);

        // Every region now has its own ground contour and gaps. Side paths rise into
        // vertical rooms, then reconnect with the main route near a checkpoint.
        float[] x = { 3.8f, 12.2f, 21.2f, 30.5f, 40.1f, 50.2f, 60.4f, 70.3f, 80.5f, 90.1f, 99.4f };
        float[] y = { -3.1f, -3.1f, -2.65f, -3.1f, -2.25f, -3.1f, -2.55f, -3.1f, -2.15f, -3.1f, -3.1f };
        float[] w = { 8.5f, 8.1f, 7.7f, 7.8f, 7.3f, 7.8f, 7.4f, 8.2f, 7.3f, 8.1f, 8.3f };
        if (region == 2) { y[2] = -3.45f; y[4] = -3.35f; y[7] = -3.45f; }
        if (region == 3) { y[1] = -3.45f; y[5] = -3.55f; y[8] = -3.4f; }
        if (region == 4) { y[2] = -2.35f; y[5] = -2.25f; y[8] = -2.2f; }
        if (region == 5) { y[3] = -3.45f; y[6] = -3.4f; y[9] = -3.45f; }
        for (int i = 0; i < x.Length; i++)
        {
            bool fractured = (region == 2 && (i == 2 || i == 7)) || (region == 3 && (i == 1 || i == 5)) || (region == 5 && i == 6);
            Vector2 pos = new Vector2(x[i], y[i]);
            Vector2 size = new Vector2(w[i], 0.64f);
            if (fractured) CreatePlatform("Fractured causeway // collapse", pos, size, stone, geo, true);
            else CreatePlatform("Causeway // " + i.ToString("00"), pos, size, stone, geo);
        }

        switch (region)
        {
            case 1: // Hollow Grove: climbing roots, upper bough loop, and a drop-through shortcut.
                AddLedges(geo, stone, new[] { 10f, 14f, 18f, 23f, 28f, 34f, 40f, 46f, 57f, 67f, 76f, 84f, 92f }, new[] { -0.8f, 0.8f, 2.4f, 3.8f, 5.4f, 6.9f, 5.5f, 4.0f, 4.8f, 6.2f, 7.8f, 6.1f, 4.3f }, 2.7f);
                CreatePlatform("Ancient trunk // west wall", new Vector2(29f, 3f), new Vector2(0.65f, 12f), stone * 0.72f, geo);
                CreatePlatform("Ancient trunk // east wall", new Vector2(47.4f, 4.8f), new Vector2(0.65f, 11f), stone * 0.72f, geo);
                CreateMovingPlatform(new Vector2(53f, 0f), new Vector2(53f, 7.2f), geo);
                break;
            case 2: // Drowned Archive: submerged vaults, staggered lifts, and laser timing.
                AddLedges(geo, stone, new[] { 9f, 16f, 23f, 30f, 37f, 44f, 53f, 61f, 69f, 77f, 86f, 94f }, new[] { -0.7f, 1.2f, 3.1f, 1.6f, -0.2f, 2.0f, 4.4f, 6.4f, 4.1f, 2.0f, 3.8f, 5.5f }, 2.8f);
                CreateMovingPlatform(new Vector2(28f, -0.3f), new Vector2(28f, 5.5f), geo);
                CreateMovingPlatform(new Vector2(66f, 0.3f), new Vector2(66f, 7.5f), geo);
                CreateFallingHazard(new Vector2(27f, 8.2f), new Vector2(1.2f, 1.35f), geo);
                CreateFallingHazard(new Vector2(71f, 9.2f), new Vector2(1.45f, 1.6f), geo);
                CreatePlatform("Vault lip // risky upper route", new Vector2(81f, 7.2f), new Vector2(3.1f, 0.32f), stone, geo, true);
                break;
            case 3: // Ember Veins: collapsing bridge, kiln shafts, and saw corridor.
                AddLedges(geo, stone, new[] { 9f, 16f, 24f, 32f, 39f, 47f, 56f, 64f, 72f, 80f, 88f, 96f }, new[] { -0.5f, 1.1f, 3.0f, 4.8f, 2.7f, 0.7f, 3.2f, 5.5f, 7.5f, 5.6f, 3.1f, 1.1f }, 3.0f);
                for (int i = 0; i < 5; i++) CreatePlatform("Kiln buttress // " + i, new Vector2(21f + i * 15f, 1.4f + (i % 2) * 0.7f), new Vector2(0.7f, 8.4f), stone * 0.7f, geo);
                CreateMovingPlatform(new Vector2(49f, -0.5f), new Vector2(49f, 6.7f), geo);
                CreateFallingHazard(new Vector2(57f, 10f), new Vector2(1.5f, 1.8f), geo);
                break;
            case 4: // Moonlit Canopy: high branches and alternate low/high routes.
                AddLedges(geo, stone, new[] { 8f, 15f, 22f, 29f, 36f, 44f, 52f, 61f, 69f, 77f, 86f, 94f }, new[] { 0.1f, 2.5f, 4.9f, 7.0f, 8.8f, 7.0f, 4.7f, 6.5f, 8.5f, 7.0f, 4.7f, 2.6f }, 3.1f);
                AddLedges(geo, stone * 0.75f, new[] { 21f, 34f, 48f, 62f, 76f, 90f }, new[] { -0.2f, -0.5f, -0.1f, -0.5f, -0.2f, -0.4f }, 3.3f, true);
                CreateMovingPlatform(new Vector2(39f, 0f), new Vector2(39f, 9.5f), geo);
                CreateMovingPlatform(new Vector2(73f, 0f), new Vector2(73f, 9.3f), geo);
                break;
            default: // Rift Heart: converging routes, arena shelves, and a final vertical climb.
                AddLedges(geo, stone, new[] { 9f, 17f, 25f, 33f, 41f, 49f, 57f, 65f, 73f, 81f, 89f, 97f }, new[] { -0.2f, 1.8f, 3.5f, 5.2f, 3.4f, 1.5f, 3.4f, 5.5f, 7.5f, 9.4f, 7.2f, 4.8f }, 3f);
                CreatePlatform("Rift arena // west crown", new Vector2(83f, 2.3f), new Vector2(8f, 0.35f), stone, geo);
                CreatePlatform("Rift arena // east crown", new Vector2(96f, 2.3f), new Vector2(8f, 0.35f), stone, geo);
                CreateMovingPlatform(new Vector2(77f, 1f), new Vector2(77f, 10f), geo);
                CreateFallingHazard(new Vector2(33f, 9f), new Vector2(1.35f, 1.4f), geo);
                break;
        }

        // A second, elevated route forms a loop over the central chambers. The broken
        // ledges are optional risk/reward shortcuts and collapse after a landing.
        AddLedges(geo, stone * 0.9f, new[] { 14f, 25f, 37f, 49f, 62f, 74f, 86f }, new[] { 10.5f, 12.7f, 14.8f, 13f, 11.2f, 13.1f, 10.7f }, 2.35f, true);
        CreateMovingPlatform(new Vector2(34f, 4f), new Vector2(34f, 12f), geo);
        CreateMovingPlatform(new Vector2(70f, 3.5f), new Vector2(70f, 11.5f), geo);

        // Region-specific hazard compositions: the same traversal asks for different
        // timing, route choice, and combat responses in each biome.
        if (region == 1)
        {
            CreateHazard(new Vector2(13.5f, -2.55f), new Vector2(2.0f, 0.28f), false, false, 1.1f, 1.2f);
            CreateHazard(new Vector2(31f, 1.0f), new Vector2(0.28f, 4.5f), true, false, 1.0f, 1.4f);
            CreateHazard(new Vector2(62f, 4.1f), new Vector2(2.3f, 0.26f), false, true, 1.0f, 1.2f);
            CreateHazard(new Vector2(79f, 1.1f), new Vector2(0.3f, 4.4f), true, false, 0.8f, 1.5f);
        }
        else if (region == 2)
        {
            CreateHazard(new Vector2(18f, 2.8f), new Vector2(0.3f, 5.2f), true, false, 0.75f, 1.15f);
            CreateHazard(new Vector2(34f, 2.4f), new Vector2(0.28f, 5.1f), true, false, 0.85f, 1.05f);
            CreateHazard(new Vector2(50f, 2.1f), new Vector2(2.8f, 0.25f), false, true, 1f, 1.1f);
            CreateHazard(new Vector2(75f, 3f), new Vector2(0.3f, 5.8f), true, false, 0.65f, 1.2f);
            CreateHazard(new Vector2(90f, -2.55f), new Vector2(3.1f, 0.28f), false, false, 1f, 1.2f);
        }
        else if (region == 3)
        {
            CreateHazard(new Vector2(14f, -2.55f), new Vector2(2.8f, 0.3f), false, true, 1f, 1.1f);
            CreateHazard(new Vector2(34f, 2.7f), new Vector2(0.3f, 5.5f), true, false, 0.7f, 0.9f);
            CreateHazard(new Vector2(53f, 2.6f), new Vector2(3.1f, 0.28f), false, true, 1f, 1.1f);
            CreateHazard(new Vector2(74f, 2f), new Vector2(0.3f, 5.4f), true, false, 0.8f, 0.85f);
            CreateHazard(new Vector2(89f, -2.55f), new Vector2(3.3f, 0.3f), false, false, 1f, 1.1f);
        }
        else if (region == 4)
        {
            CreateHazard(new Vector2(22f, 5.6f), new Vector2(3f, 0.27f), false, true, 0.9f, 1f);
            CreateHazard(new Vector2(47f, 4.5f), new Vector2(0.3f, 5.3f), true, false, 0.65f, 1.2f);
            CreateHazard(new Vector2(67f, 8.1f), new Vector2(3.2f, 0.28f), false, true, 0.8f, 1f);
            CreateHazard(new Vector2(85f, 4.3f), new Vector2(0.3f, 5.4f), true, false, 0.7f, 1.25f);
        }
        else
        {
            CreateHazard(new Vector2(18f, 2.5f), new Vector2(0.3f, 5.5f), true, false, 0.65f, 0.95f);
            CreateHazard(new Vector2(39f, 5.6f), new Vector2(3.2f, 0.28f), false, true, 0.8f, 1f);
            CreateHazard(new Vector2(61f, 4.2f), new Vector2(0.3f, 5.5f), true, false, 0.6f, 0.9f);
            CreateHazard(new Vector2(80f, 5.1f), new Vector2(3.8f, 0.3f), false, true, 0.8f, 1f);
        }

        CreateHiddenVault(geo, stone);
        CreateRoomMarkers();
        CreateRegionalSilhouette(geo, stone);
        for (int i = 0; i < 16; i++)
        {
            float runeX = i * 6.4f + 3f;
            CreateSprite("Carved memory // environmental story", new Vector3(runeX, -2.72f, 0f), new Vector2(0.36f, 0.07f), VesperArtFactory.Square, new Color(accent.r, accent.g, accent.b, 0.34f), 3);
        }
    }

    private void AddLedges(Transform parent, Color color, float[] xs, float[] ys, float width, bool fragile = false)
    {
        for (int i = 0; i < xs.Length; i++)
            CreatePlatform((fragile ? "Risk route // brittle shelf " : "Traversal route // shelf ") + i, new Vector2(xs[i], ys[i]), new Vector2(width, 0.34f), color, parent, fragile && i % 3 == 1);
    }

    private void CreateHiddenVault(Transform parent, Color stone)
    {
        // Each biome hides its vault along a different branch and uses a different
        // memory order, so the secret is part of the region rather than a reused room.
        float[] vaultCenters = { 0f, 58f, 80f, 48f, 76f, 68f };
        float[] firstRelays = { 0f, 36f, 55f, 22f, 50f, 42f };
        int[][] sequences = { null, new[] { 2, 1, 3 }, new[] { 3, 1, 2 }, new[] { 1, 3, 2 }, new[] { 2, 3, 1 }, new[] { 3, 2, 1 } };
        float vaultX = vaultCenters[region];
        float relayStart = firstRelays[region];
        CreatePlatform("Hidden vault // floor", new Vector2(vaultX, 5.7f), new Vector2(12f, 0.38f), stone, parent);
        CreatePlatform("Hidden vault // ceiling", new Vector2(vaultX, 10.2f), new Vector2(12f, 0.42f), stone, parent);
        CreatePlatform("Hidden vault // east seal", new Vector2(vaultX + 6.1f, 7.9f), new Vector2(0.38f, 4.5f), stone, parent);
        GameObject barrier = new GameObject("Puzzle seal // resonant barrier");
        barrier.transform.SetParent(parent, false); barrier.transform.position = new Vector3(vaultX - 6f, 7.9f, 0f);
        BoxCollider2D sealCollider = barrier.AddComponent<BoxCollider2D>(); sealCollider.size = new Vector2(0.8f, 4.1f);
        CreateSprite("Puzzle seal // energy membrane", barrier.transform.position, new Vector2(0.72f, 4f), VesperArtFactory.Glow, new Color(accent.r, accent.g, accent.b, 0.75f), 6, barrier.transform);
        GameObject prize = new GameObject("Vault reward // memory shard"); prize.transform.position = new Vector3(vaultX, 6.9f, 0f);
        CircleCollider2D pickup = prize.AddComponent<CircleCollider2D>(); pickup.isTrigger = true; pickup.radius = 0.48f;
        CreateSprite("Vault reward // halo", prize.transform.position, Vector2.one, VesperArtFactory.Glow, new Color(accent.r, accent.g, accent.b, 0.7f), 6, prize.transform);
        CreateSprite("Vault reward // core", prize.transform.position, Vector2.one * 0.34f, VesperArtFactory.Orb, new Color(0.88f, 1f, 0.79f), 7, prize.transform);
        prize.AddComponent<VesperPickup2D>().Configure(VesperAbility.None, true, false, false, true);
        prize.AddComponent<VesperAmbientMotion>().Configure(0.6f, 0.1f);
        prize.SetActive(false);
        VesperPuzzleController2D puzzle = new GameObject("Puzzle // three echoes").AddComponent<VesperPuzzleController2D>();
        puzzle.transform.SetParent(worldRoot, false);
        puzzle.Configure(sequences[region], barrier, prize, region);
        float[] runeX = { relayStart, relayStart + 5f, relayStart + 10f };
        float[] runeY = region switch
        {
            1 => new[] { 1.4f, 3.1f, 4.8f },
            2 => new[] { 4.4f, 6.4f, 4.1f },
            3 => new[] { 3f, 4.8f, 2.7f },
            4 => new[] { 4.7f, 6.5f, 8.5f },
            _ => new[] { 3.4f, 1.5f, 3.4f }
        };
        for (int i = 0; i < runeX.Length; i++)
        {
            GameObject relay = new GameObject("Resonance dial // " + (i + 1)); relay.transform.position = new Vector3(runeX[i], runeY[i], 0f);
            CircleCollider2D range = relay.AddComponent<CircleCollider2D>(); range.isTrigger = true; range.radius = 1.05f;
            CreateSprite("Resonance dial // halo", relay.transform.position, Vector2.one * 1.05f, VesperArtFactory.Glow, new Color(accent.r, accent.g, accent.b, 0.22f), 4, relay.transform);
            CreateSprite("Resonance dial // rune", relay.transform.position, Vector2.one * 0.44f, VesperArtFactory.Orb, Color.Lerp(accent, Color.white, 0.35f), 6, relay.transform);
            relay.AddComponent<VesperRuneRelay2D>().Configure(puzzle, i + 1);
        }
    }

    private void CreateRoomMarkers()
    {
        float[] positions = { 8f, 22f, 36f, 50f, 64f, 78f };
        for (int i = 0; i < positions.Length; i++)
        {
            GameObject marker = new GameObject("Cartography // room " + (i + 1)); marker.transform.position = new Vector3(positions[i], 1.3f, 0f);
            BoxCollider2D trigger = marker.AddComponent<BoxCollider2D>(); trigger.isTrigger = true; trigger.size = new Vector2(0.65f, 40f);
            marker.AddComponent<VesperMapRoomTrigger2D>().Configure(region, i);
        }
    }

    private void CreateRegionalSilhouette(Transform parent, Color stone)
    {
        float[] xs = { 10f, 27f, 45f, 64f, 83f, 98f };
        for (int i = 0; i < xs.Length; i++)
        {
            float height = 11f + (i % 3) * 4f;
            float width = region == 2 ? 1.7f : region == 3 ? 1.3f : 2.2f;
            Vector3 pos = new Vector3(xs[i], height * 0.25f - 2.4f, 0f);
            Color dark = new Color(0.012f, 0.025f, 0.035f, 0.82f);
            SpriteRenderer pillar = CreateSprite("Regional silhouette // monolith", pos, new Vector2(width, height), VesperArtFactory.Square, dark, 3, parent);
            pillar.gameObject.AddComponent<VesperParallaxLayer>().Configure(cameraTransform, 1.08f);
            SpriteRenderer seam = CreateSprite("Regional silhouette // seam light", pos + Vector3.right * width * 0.34f, new Vector2(0.055f, height * 0.68f), VesperArtFactory.Glow, new Color(accent.r, accent.g, accent.b, 0.2f), 4, parent);
            seam.gameObject.AddComponent<VesperParallaxLayer>().Configure(cameraTransform, 1.08f);
        }
        for (int i = 0; i < 6; i++)
        {
            float x = 16f + i * 15f;
            SpriteRenderer shaft = CreateSprite("Atmosphere // light shaft", new Vector3(x, 4f, 0f), new Vector2(0.45f + i % 2 * 0.4f, 18f), VesperArtFactory.Glow, new Color(accent.r, accent.g, accent.b, 0.045f), -4, parent);
            shaft.transform.rotation = Quaternion.Euler(0f, 0f, (i % 2 == 0 ? -7f : 8f));
            shaft.gameObject.AddComponent<VesperAmbientMotion>().Configure(0.18f, 0.12f);
        }
    }

    private void CreatePlatform(string objectName, Vector2 pos, Vector2 size, Color color, Transform parent, bool collapsing = false)
    {
        GameObject platform = new GameObject(objectName);
        platform.transform.SetParent(parent, true);
        platform.transform.position = pos;
        BoxCollider2D collider = platform.AddComponent<BoxCollider2D>(); collider.size = size;
        CreateSprite(objectName + " // basalt face", pos, size, VesperArtFactory.Square, color, 0, platform.transform);
        Color edge = Color.Lerp(accent, new Color(0.85f, 0.93f, 0.78f), 0.3f);
        CreateSprite(objectName + " // living seam", pos + Vector2.up * (size.y * 0.5f - 0.035f), new Vector2(size.x * 0.92f, 0.055f), VesperArtFactory.Square, edge, 1, platform.transform);
        int marks = Mathf.Clamp(Mathf.FloorToInt(size.x / 1.7f), 1, 5);
        for (int i = 0; i < marks; i++)
        {
            float mx = pos.x - size.x * 0.35f + (marks == 1 ? 0f : i * size.x * 0.7f / (marks - 1));
            CreateSprite(objectName + " // chisel mark", new Vector3(mx, pos.y - size.y * 0.1f, 0f), new Vector2(0.6f, 0.045f), VesperArtFactory.Square, new Color(0.25f, 0.39f, 0.39f, 0.45f), 2, platform.transform);
        }
        if (collapsing)
        {
            Rigidbody2D body = platform.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic; body.interpolation = RigidbodyInterpolation2D.Interpolate;
            platform.AddComponent<VesperCollapsePlatform2D>();
        }
    }

    private void CreateMovingPlatform(Vector2 a, Vector2 b, Transform parent)
    {
        GameObject go = new GameObject("Moving traverse // lift"); go.transform.SetParent(parent, false); go.transform.position = a;
        BoxCollider2D box = go.AddComponent<BoxCollider2D>(); box.size = new Vector2(3f, 0.28f);
        Rigidbody2D body = go.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic; body.interpolation = RigidbodyInterpolation2D.Interpolate;
        CreateSprite("Lift // stone", go.transform.position, new Vector2(3f, 0.28f), VesperArtFactory.Square, new Color(0.11f, 0.2f, 0.23f), 1, go.transform);
        CreateSprite("Lift // biolume", go.transform.position + Vector3.up * 0.14f, new Vector2(2.8f, 0.04f), VesperArtFactory.Square, accent, 2, go.transform);
        go.AddComponent<VesperMovingPlatform2D>().Configure(a, b, 1.8f + region * 0.12f);
    }

    private void CreateFallingHazard(Vector2 position, Vector2 size, Transform parent)
    {
        GameObject debris = new GameObject("Falling hazard // triggered debris");
        debris.transform.SetParent(parent, false); debris.transform.position = position;
        Rigidbody2D body = debris.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic; body.gravityScale = 2.2f; body.freezeRotation = true; body.interpolation = RigidbodyInterpolation2D.Interpolate;
        BoxCollider2D collider = debris.AddComponent<BoxCollider2D>(); collider.size = size * 0.86f;
        CreateSprite("Falling hazard // fractured stone", position, size, VesperArtFactory.Square, new Color(0.22f, 0.12f, 0.13f), 5, debris.transform);
        CreateSprite("Falling hazard // warning seam", position + Vector2.down * size.y * 0.42f, new Vector2(size.x * 0.74f, 0.06f), VesperArtFactory.Square, new Color(1f, 0.48f, 0.34f, 0.72f), 6, debris.transform);
        VesperFallingHazard2D drop = debris.AddComponent<VesperFallingHazard2D>();
        GameObject sensor = new GameObject("Falling hazard // proximity sensor"); sensor.transform.SetParent(debris.transform, false);
        CircleCollider2D trigger = sensor.AddComponent<CircleCollider2D>(); trigger.isTrigger = true; trigger.radius = 4.4f;
        sensor.AddComponent<VesperFallTrigger2D>().Configure(drop);
    }

    private void CreateHazard(Vector2 pos, Vector2 size, bool timed, bool rotating, float active, float safe)
    {
        GameObject go = new GameObject(rotating ? "Rotating blade // hazard" : timed ? "Timed resonance gate" : "Rift thorn // hazard");
        go.transform.SetParent(worldRoot, true); go.transform.position = pos; go.transform.localScale = new Vector3(size.x, size.y, 1f);
        BoxCollider2D collider = go.AddComponent<BoxCollider2D>(); collider.isTrigger = true;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>(); sr.sprite = VesperArtFactory.Square; sr.color = new Color(1f, 0.32f, 0.42f, 0.86f); sr.sortingOrder = 5;
        if (rotating)
        {
            SpriteRenderer blade = CreateSprite("Blade // core", pos, new Vector2(size.y, size.x), VesperArtFactory.Square, new Color(0.98f, 0.71f, 0.54f, 0.86f), 6, go.transform);
            blade.transform.localPosition = Vector3.zero;
            blade.transform.localScale = new Vector3(size.y / Mathf.Max(0.01f, size.x), size.x / Mathf.Max(0.01f, size.y), 1f);
        }
        go.AddComponent<VesperHazard2D>().Configure(timed, rotating, active, safe);
    }

    private void CreateTransitions()
    {
        int previous = region - 1;
        int next = region + 1;
        if (previous <= 0)
            CreateDoor(new Vector2(1.6f, -1.4f), "VesperMenu", VesperAbility.None, true, "RETURN TO THRESHOLD");
        else
            CreateDoor(new Vector2(1.6f, -1.4f), SceneNames[previous], VesperAbility.None, true, "ROOT GATE // " + RegionNames[previous]);
        if (next <= 5)
        {
            VesperAbility gate = region switch
            {
                1 => VesperAbility.DoubleJump,
                2 => VesperAbility.WallJump,
                3 => VesperAbility.AirDash,
                4 => VesperAbility.SuperDash,
                _ => VesperAbility.None
            };
            CreateDoor(new Vector2(101.4f, -1.4f), SceneNames[next], gate, false, "ROOT GATE // " + RegionNames[next], region);
        }
        else CreateDoor(new Vector2(101.4f, -1.4f), "VesperMenu", VesperAbility.None, false, "RETURN TO THRESHOLD", region);

        // Two ability-gated cross-roots make the five biomes a backtracking network,
        // not just a corridor of sequential levels.
        if (region == 1)
            CreateLoopDoor(new Vector2(45.8f, 6f), SceneNames[4], VesperAbility.WallJump, new Vector2(52f, 7.2f), 1, "HIGH ROOT // MOONLIT CANOPY");
        else if (region == 4)
            CreateLoopDoor(new Vector2(64f, 8.4f), SceneNames[1], VesperAbility.None, new Vector2(46f, 5.4f), 0, "RETURN ROOT // HOLLOW GROVE");
        else if (region == 2)
            CreateLoopDoor(new Vector2(82f, 6.8f), SceneNames[5], VesperAbility.AirDash, new Vector2(65f, 7.2f), 2, "RIFT DESCENT // RIFT HEART");
        else if (region == 5)
            CreateLoopDoor(new Vector2(61f, 5.4f), SceneNames[2], VesperAbility.None, new Vector2(78f, 3.2f), 0, "RETURN ROOT // DROWNED ARCHIVE");
    }

    private void CreateLoopDoor(Vector2 pos, string scene, VesperAbility requirement, Vector2 destinationSpawn, int bossRequirement, string label)
    {
        GameObject door = new GameObject("Cross-root gate // " + label); door.transform.position = pos;
        BoxCollider2D trigger = door.AddComponent<BoxCollider2D>(); trigger.size = new Vector2(1.8f, 3.6f); trigger.isTrigger = true;
        door.AddComponent<VesperDoor2D>().ConfigureToPoint(scene, requirement, destinationSpawn, bossRequirement);
        CreateSprite("Cross-root // outer ring", pos + Vector2.up * 0.5f, new Vector2(1.5f, 3.2f), VesperArtFactory.Square, new Color(0.04f, 0.09f, 0.11f), 3, door.transform);
        CreateSprite("Cross-root // passage light", pos + Vector2.up * 0.5f, new Vector2(0.26f, 2.3f), VesperArtFactory.Glow, new Color(accent.r, accent.g, accent.b, 0.88f), 4, door.transform);
        CreateSprite("Cross-root // spill", pos + Vector2.up * 0.5f, new Vector2(2.2f, 4.2f), VesperArtFactory.Glow, new Color(accent.r, accent.g, accent.b, 0.18f), 2, door.transform);
        door.AddComponent<VesperAmbientMotion>().Configure(0.7f, 0.12f);
    }

    private void CreateDoor(Vector2 pos, string scene, VesperAbility requirement, bool enterFromRight, string label, int bossRequirement = 0)
    {
        GameObject door = new GameObject("Root gate // " + label); door.transform.position = pos;
        BoxCollider2D trigger = door.AddComponent<BoxCollider2D>(); trigger.size = new Vector2(1.8f, 3.6f); trigger.isTrigger = true;
        VesperDoor2D travel = door.AddComponent<VesperDoor2D>(); travel.Configure(scene, requirement, enterFromRight, bossRequirement);
        CreateSprite("Gate // outer ring", pos + Vector2.up * 0.6f, new Vector2(1.45f, 3.15f), VesperArtFactory.Square, new Color(0.04f, 0.1f, 0.12f), 3, door.transform);
        CreateSprite("Gate // inner light", pos + Vector2.up * 0.58f, new Vector2(0.28f, 2.35f), VesperArtFactory.Glow, new Color(accent.r, accent.g, accent.b, 0.9f), 4, door.transform);
        CreateSprite("Gate // soft spill", pos + Vector2.up * 0.6f, new Vector2(2.2f, 4.1f), VesperArtFactory.Glow, new Color(accent.r, accent.g, accent.b, 0.18f), 2, door.transform);
        door.AddComponent<VesperAmbientMotion>().Configure(0.7f, 0.12f);
    }

    private void CreateCheckpointsAndPickups()
    {
        CreateCheckpoint(new Vector2(2.4f, -2.3f));
        CreateCheckpoint(new Vector2(37f, -1.15f));
        CreateCheckpoint(new Vector2(72f, -1.05f));
        CreatePickup(new Vector2(17.7f, 2.3f), VesperAbility.None, true, false, false);
        CreatePickup(new Vector2(29f, 7.3f), VesperAbility.None, true, false, false);
        CreatePickup(new Vector2(43f, 5.3f), VesperAbility.None, true, false, false);
        CreatePickup(new Vector2(67f, 2.4f), VesperAbility.None, true, false, false);
        CreatePickup(new Vector2(89f, 1.5f), VesperAbility.None, true, false, false);
        VesperAbility unlock = region switch
        {
            1 => VesperAbility.DoubleJump,
            2 => VesperAbility.WallJump,
            3 => VesperAbility.AirDash,
            4 => VesperAbility.SuperDash,
            5 => VesperAbility.EnergyWave,
            _ => VesperAbility.None
        };
        CreatePickup(new Vector2(20f, -1.25f), unlock, false, false, false);
        CreatePickup(new Vector2(45f, 6.1f), VesperAbility.None, false, true, false);
        CreatePickup(new Vector2(77f, -1.4f), VesperAbility.None, false, false, true);
    }

    private void CreateCheckpoint(Vector2 pos)
    {
        GameObject go = new GameObject("Memory anchor // checkpoint"); go.transform.position = pos;
        CircleCollider2D col = go.AddComponent<CircleCollider2D>(); col.radius = 0.9f; col.isTrigger = true;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>(); sr.sprite = VesperArtFactory.Orb; sr.color = new Color(accent.r, accent.g, accent.b, 0.6f); sr.sortingOrder = 4;
        go.AddComponent<VesperCheckpoint2D>();
        CreateSprite("Anchor // light", pos, new Vector2(2.5f, 2.5f), VesperArtFactory.Glow, new Color(accent.r, accent.g, accent.b, 0.28f), 3, go.transform);
    }

    private void CreatePickup(Vector2 pos, VesperAbility ability, bool shard, bool upgrade, bool heal)
    {
        int pickupId = pickupCounter++;
        if (VesperProgress.IsPickupCollected(region, pickupId)) return;
        if (ability != VesperAbility.None && VesperProgress.Has(ability)) return;
        GameObject go = new GameObject(shard ? "Memory shard" : upgrade ? "Skill weave" : heal ? "Vital mote" : "Ability // " + ability);
        go.transform.position = pos;
        CircleCollider2D col = go.AddComponent<CircleCollider2D>(); col.isTrigger = true; col.radius = 0.48f;
        SpriteRenderer glow = CreateSprite("Collectible // glow", pos, Vector2.one * 1.1f, VesperArtFactory.Glow, new Color(accent.r, accent.g, accent.b, 0.58f), 5, go.transform);
        SpriteRenderer core = CreateSprite("Collectible // core", pos, Vector2.one * 0.36f, VesperArtFactory.Orb, heal ? new Color(1f, 0.55f, 0.45f) : new Color(0.74f, 1f, 0.84f), 6, go.transform);
        go.AddComponent<VesperPickup2D>().Configure(ability, shard, upgrade, heal, false, region, pickupId);
        go.AddComponent<VesperAmbientMotion>().Configure(0.4f, 0.08f);
    }

    private void CreateEnemySet()
    {
        VesperEnemyType[][] roster =
        {
            new[] { VesperEnemyType.Stalker, VesperEnemyType.Crawler, VesperEnemyType.Wisp, VesperEnemyType.Sentry, VesperEnemyType.Charger, VesperEnemyType.MiniBoss },
            new[] { VesperEnemyType.Burrower, VesperEnemyType.Wisp, VesperEnemyType.ShieldBearer, VesperEnemyType.RiftHound, VesperEnemyType.Sentry, VesperEnemyType.MiniBoss },
            new[] { VesperEnemyType.Crawler, VesperEnemyType.Charger, VesperEnemyType.Elite, VesperEnemyType.Sentry, VesperEnemyType.Wisp, VesperEnemyType.MiniBoss },
            new[] { VesperEnemyType.RiftHound, VesperEnemyType.Burrower, VesperEnemyType.ShieldBearer, VesperEnemyType.Elite, VesperEnemyType.Charger, VesperEnemyType.MiniBoss },
            new[] { VesperEnemyType.Elite, VesperEnemyType.Wisp, VesperEnemyType.RiftHound, VesperEnemyType.ShieldBearer, VesperEnemyType.Sentry, VesperEnemyType.MiniBoss }
        };
        VesperEnemyType[] types = roster[region - 1];
        float[] encounterX = { 12f, 24f, 33f, 48f, 61f };
        for (int i = 0; i < types.Length - 1; i++)
        {
            float x = encounterX[i];
            float y = i % 2 == 0 ? -1.8f : 2.1f + (i % 3) * 0.85f;
            CreateEnemy(new Vector2(x, y), types[i], false);
        }
        string[] miniBosses = { "ROOT SENTINEL", "DROWNED CURATOR", "KILN HUSK", "GLOAM HUNTER", "RIFT KNIGHT" };
        CreateEnemy(new Vector2(72f, -1.35f), VesperEnemyType.MiniBoss, true, miniBosses[region - 1]);
        VesperEnemyType[] bosses = { VesperEnemyType.Stalker, VesperEnemyType.Sentry, VesperEnemyType.Charger, VesperEnemyType.Wisp, VesperEnemyType.RiftHound };
        string[] bossNames = { "MOSS WARDEN", "CHOIR OF SALT", "KILN MOTHER", "LUNAR STAG", "THE HOLLOW KING" };
        CreateEnemy(new Vector2(90.5f, -1.5f), bosses[region - 1], true, bossNames[region - 1]);
    }

    private void CreateEnemy(Vector2 pos, VesperEnemyType kind, bool boss, string bossName = "")
    {
        GameObject go = new GameObject(boss ? "RIFT WARDEN // " + kind : "Echo // " + kind); go.transform.position = pos;
        BoxCollider2D col = go.AddComponent<BoxCollider2D>(); col.size = boss ? new Vector2(2.2f, 2.7f) : new Vector2(0.95f, 1.05f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>(); sr.sprite = VesperArtFactory.Orb; sr.color = boss ? Color.Lerp(accent, new Color(0.94f, 0.29f, 0.4f), 0.42f) : Color.Lerp(accent, new Color(0.35f, 0.2f, 0.5f), 0.5f); sr.sortingOrder = 5;
        go.transform.localScale = boss ? new Vector3(2.2f, 2.4f, 1f) : new Vector3(1.1f, 1.15f, 1f);
        CreateSprite("Echo // eye left", pos + Vector2.left * (boss ? 0.42f : 0.15f) + Vector2.up * 0.08f, Vector2.one * (boss ? 0.14f : 0.075f), VesperArtFactory.Glow, new Color(0.8f, 1f, 0.85f), 7, go.transform);
        CreateSprite("Echo // eye right", pos + Vector2.right * (boss ? 0.42f : 0.15f) + Vector2.up * 0.08f, Vector2.one * (boss ? 0.14f : 0.075f), VesperArtFactory.Glow, new Color(0.8f, 1f, 0.85f), 7, go.transform);
        Rigidbody2D body = go.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic; body.gravityScale = 0f;
        VesperEnemy2D enemy = go.AddComponent<VesperEnemy2D>(); enemy.Configure(kind, sr.color, boss, boss ? (kind == VesperEnemyType.MiniBoss ? 10 + region : 20 + region * 3) : 0, bossName);
        if (boss) go.AddComponent<VesperBossIntroTrigger>().Configure(string.IsNullOrEmpty(bossName) ? kind.ToString().ToUpperInvariant() : bossName, accent);
    }

    private void CreatePlayer()
    {
        bool enteredThroughGate = VesperProgress.SpawnFromSide >= 0 || VesperProgress.UseCustomSpawn;
        float x = VesperProgress.UseCustomSpawn ? VesperProgress.CustomSpawnPosition.x : VesperProgress.SpawnFromSide == 1 ? 98f : 3.2f;
        float y = VesperProgress.UseCustomSpawn ? VesperProgress.CustomSpawnPosition.y : -1.8f;
        VesperProgress.UseCustomSpawn = false;
        VesperProgress.SpawnFromSide = -1;
        float cx = PlayerPrefs.GetFloat("VESPER_CheckpointX_" + region, float.NaN);
        float cy = PlayerPrefs.GetFloat("VESPER_CheckpointY_" + region, float.NaN);
        if (!enteredThroughGate && !float.IsNaN(cx)) { x = cx; y = cy; }
        GameObject go = new GameObject("VESPER // WANDERER"); go.tag = "Player"; go.transform.position = new Vector3(x, y, 0f);
        Rigidbody2D rb = go.AddComponent<Rigidbody2D>(); rb.gravityScale = 3.7f; rb.freezeRotation = true;
        CapsuleCollider2D capsule = go.AddComponent<CapsuleCollider2D>(); capsule.size = new Vector2(0.72f, 1.55f); capsule.offset = new Vector2(0f, 0.76f);
        Transform visual = new GameObject("Visual").transform; visual.SetParent(go.transform, false); visual.localPosition = new Vector3(0f, 0.05f, 0f);
        SpriteRenderer sprite = visual.gameObject.AddComponent<SpriteRenderer>(); sprite.sprite = VesperArtFactory.Hero; sprite.color = Color.white; sprite.sortingOrder = 6;
        player = go.AddComponent<VesperPlayerController2D>();
        GameObject shadow = new GameObject("Contact shadow // depth cue");
        shadow.AddComponent<VesperContactShadow>().Configure(go.transform);
        rb.position = new Vector2(x, y);
        VesperCamera2D.Shake(0.08f, 0.14f);
    }

    private void CreateAmbientParticles(int area)
    {
        GameObject go = new GameObject("Ambient // drifting spores");
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var main = ps.main; main.loop = true; main.duration = 8f; main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 9f); main.startSpeed = new ParticleSystem.MinMaxCurve(0.08f, 0.35f); main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.11f); main.startColor = new ParticleSystem.MinMaxGradient(new Color(accent.r, accent.g, accent.b, 0.18f), new Color(0.75f, 0.9f, 0.78f, 0.48f)); main.simulationSpace = ParticleSystemSimulationSpace.World;
        var emission = ps.emission; emission.rateOverTime = 14f;
        var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(WorldWidth, 9f, 0.1f);
        var velocity = ps.velocityOverLifetime; velocity.enabled = true; velocity.x = new ParticleSystem.MinMaxCurve(-0.16f, 0.16f); velocity.y = new ParticleSystem.MinMaxCurve(0.06f, 0.32f);
        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>(); renderer.renderMode = ParticleSystemRenderMode.Billboard; renderer.material = new Material(Shader.Find("Sprites/Default"));
        go.transform.position = new Vector3(WorldWidth * 0.5f, 1f, 0f);
    }

    private void CreateAudio(int area)
    {
        GameObject go = new GameObject("Atmosphere // dynamic score");
        go.AddComponent<VesperAtmosphereAudio>().PlayRegion(area);
    }

    private SpriteRenderer CreateSprite(string objectName, Vector3 position, Vector2 size, Sprite sprite, Color color, int order, Transform parent = null)
    {
        GameObject go = new GameObject(objectName); if (parent != null) go.transform.SetParent(parent, true);
        go.transform.position = position; go.transform.localScale = new Vector3(size.x, size.y, 1f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.color = color; sr.sortingOrder = order;
        return sr;
    }

    private SpriteRenderer CreateBackdropSprite(string objectName, Vector3 position, Vector2 worldSize, Sprite sprite, Color color, int order)
    {
        GameObject go = new GameObject(objectName); go.transform.position = position;
        go.transform.localScale = new Vector3(worldSize.x / 64f, worldSize.y / 32f, 1f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.color = color; sr.sortingOrder = order; return sr;
    }
}

public sealed class VesperParallaxLayer : MonoBehaviour
{
    private Transform cameraTarget;
    private float factor;
    private Vector3 origin;
    public void Configure(Transform cam, float speed) { cameraTarget = cam; factor = speed; origin = transform.position; }
    private void LateUpdate()
    {
        if (cameraTarget == null) return;
        transform.position = new Vector3(origin.x + cameraTarget.position.x * (1f - factor), origin.y + cameraTarget.position.y * (1f - factor), origin.z);
    }
}

public sealed class VesperAmbientMotion : MonoBehaviour
{
    private Vector3 origin;
    private float speed;
    private float amplitude;
    private float phase;
    private Color baseColor;
    public void Configure(float rate, float amount) { origin = transform.position; speed = rate; amplitude = amount; phase = UnityEngine.Random.value * 6.28f; }
    private void Awake() { if (origin == Vector3.zero) origin = transform.position; SpriteRenderer sr = GetComponent<SpriteRenderer>(); if (sr != null) baseColor = sr.color; }
    private void Update()
    {
        transform.position = origin + Vector3.up * Mathf.Sin(Time.time * speed + phase) * amplitude;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) { Color c = baseColor; c.a = baseColor.a * (0.89f + Mathf.Sin(Time.time * 0.9f + phase) * 0.1f); sr.color = c; }
    }
}

public sealed class VesperMovingPlatform2D : MonoBehaviour
{
    private Rigidbody2D body;
    private Vector2 a, b;
    private float speed;
    private float offset;
    public void Configure(Vector2 start, Vector2 end, float movementSpeed) { a = start; b = end; speed = movementSpeed; offset = UnityEngine.Random.Range(0f, 10f); }
    private void Awake() { body = GetComponent<Rigidbody2D>(); if (a == Vector2.zero && b == Vector2.zero) a = transform.position; }
    private void FixedUpdate()
    {
        float t = Mathf.PingPong((Time.time + offset) * speed * 0.25f, 1f);
        Vector2 target = Vector2.Lerp(a, b, t);
        if (body != null) body.MovePosition(target); else transform.position = target;
    }
}

public sealed class VesperCollapsePlatform2D : MonoBehaviour
{
    private bool collapsing;
    private float age;
    private Vector3 home;
    private Vector3 nextPosition;
    private Rigidbody2D body;
    private void Awake() { home = transform.position; nextPosition = home; body = GetComponent<Rigidbody2D>(); }
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!collapsing && collision.collider.GetComponentInParent<VesperPlayerController2D>() != null) { collapsing = true; age = 0f; }
    }
    private void Update()
    {
        if (!collapsing) return;
        age += Time.deltaTime;
        if (age < 0.4f) nextPosition = home + Vector3.right * Mathf.Sin(age * 70f) * 0.035f;
        else if (age < 2.2f) nextPosition = transform.position + Vector3.down * Time.deltaTime * 8f;
        else { nextPosition = home; collapsing = false; age = 0f; }
    }
    private void FixedUpdate() { if (body != null && collapsing) body.MovePosition(nextPosition); else if (body != null && nextPosition == home) body.MovePosition(home); else if (body == null && collapsing) transform.position = nextPosition; }
}

public sealed class VesperContactShadow : MonoBehaviour
{
    private Transform target;
    private SpriteRenderer sr;
    public void Configure(Transform follow) { target = follow; }
    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) { sr = gameObject.AddComponent<SpriteRenderer>(); sr.sprite = VesperArtFactory.Glow; sr.color = new Color(0.01f, 0.025f, 0.035f, 0.7f); sr.sortingOrder = 5; }
    }
    private void LateUpdate()
    {
        if (target == null) return;
        RaycastHit2D[] hits = Physics2D.RaycastAll(target.position + Vector3.up * 1.65f, Vector2.down, 12f);
        RaycastHit2D ground = default;
        foreach (RaycastHit2D candidate in hits)
            if (candidate.collider != null && candidate.collider.transform != target && !candidate.collider.isTrigger) { ground = candidate; break; }
        float height = ground.collider == null ? 5f : Mathf.Max(0f, target.position.y - ground.point.y);
        transform.position = new Vector3(target.position.x, ground.collider == null ? target.position.y - 2.5f : ground.point.y - 0.02f, 0.8f);
        float size = Mathf.Lerp(0.9f, 0.26f, Mathf.Clamp01(height / 6f)); transform.localScale = new Vector3(size * 1.5f, size * 0.32f, 1f);
        Color c = sr.color; c.a = Mathf.Lerp(0.64f, 0.14f, Mathf.Clamp01(height / 5f)); sr.color = c;
    }
}

public sealed class VesperBossIntroTrigger : MonoBehaviour
{
    private Transform player;
    private string title;
    private Color accent;
    private bool shown;
    public void Configure(string bossName, Color tint) { title = bossName; accent = tint; }
    private void Update()
    {
        if (player == null) { GameObject go = GameObject.FindGameObjectWithTag("Player"); if (go != null) player = go.transform; }
        if (!shown && player != null && Vector2.Distance(player.position, transform.position) < 10f)
        {
            shown = true; VesperCamera2D.BossIntro(transform); VesperCamera2D.Shake(0.18f, 0.35f); VesperGameUI.FlashMessage("RIFT WARDEN // " + title, 2f); VesperGameUI.SetBoss(title, 1f); VesperAtmosphereAudio.SetCombat(true);
        }
    }
}
