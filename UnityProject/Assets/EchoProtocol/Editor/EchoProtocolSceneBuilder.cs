#if UNITY_EDITOR
using System.IO;
using EchoProtocol;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class EchoProtocolSceneBuilder
{
    private const string Root = "Assets/EchoProtocol";
    private const string Art = Root + "/Art";
    private const string SceneFolder = Root + "/Scenes";
    private static Sprite white;
    private static Sprite glow;
    private static Sprite operatorSprite;
    private static Transform world;

    [MenuItem("ECHO PROTOCOL/Export Windows Prototype")]
    public static void BuildWindowsPrototype()
    {
        string scenePath = SceneFolder + "/EchoProtocol_ResearchWing.unity";
        if (!File.Exists(scenePath)) BuildPrototype();
        string outputDirectory = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Builds", "ECHO PROTOCOL Research Wing Windows");
        Directory.CreateDirectory(outputDirectory);
        string outputPath = Path.Combine(outputDirectory, "ECHO PROTOCOL.exe");
        string oldName = PlayerSettings.productName;
        string oldCompany = PlayerSettings.companyName;
        string oldIdentifier = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Standalone);
        try
        {
            PlayerSettings.productName = "ECHO PROTOCOL";
            PlayerSettings.companyName = "Jaiden Asruf";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.jaidenasruf.echoprotocol");
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { scenePath },
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new System.InvalidOperationException("ECHO PROTOCOL Windows export failed: " + report.summary.result);
            Debug.Log("ECHO PROTOCOL Windows export ready: " + outputPath);
        }
        finally
        {
            PlayerSettings.productName = oldName;
            PlayerSettings.companyName = oldCompany;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, oldIdentifier);
        }
    }
    [MenuItem("ECHO PROTOCOL/Build Research Wing Prototype")]
    public static void BuildPrototype()
    {
        EnsureFolders();
        CreateArtAssets();
        white = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "/EP_White.png");
        glow = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "/EP_Glow.png");
        operatorSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "/EP_Operator.png");
        if (white == null || glow == null || operatorSprite == null)
            throw new System.InvalidOperationException("Could not import the generated Echo Protocol art.");

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        world = new GameObject("ECHO PROTOCOL // RESEARCH WING").transform;
        BuildCameraAndLayers();
        BuildArchitecture();
        EchoProtocolController2D player = BuildPlayer();
        BuildPuzzles();
        BuildAnomaly();
        BuildNarrativeTriggers();
        BuildAtmosphere();
        BuildHud();
        BuildAmbientAudio();
        EchoProtocolCamera2D cameraRig = Camera.main.GetComponent<EchoProtocolCamera2D>();
        cameraRig.Configure(player.transform, -4f, 36f);
        AssetDatabase.SaveAssets();
        string scenePath = SceneFolder + "/EchoProtocol_ResearchWing.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        Debug.Log("ECHO PROTOCOL prototype scene built: " + scenePath);
    }

    private static void EnsureFolders()
    {
        CreateFolderIfMissing("Assets", "EchoProtocol");
        CreateFolderIfMissing(Root, "Art");
        CreateFolderIfMissing(Root, "Scenes");
    }

    private static void CreateFolderIfMissing(string parent, string name)
    {
        string path = parent + "/" + name;
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, name);
    }

    private static void CreateArtAssets()
    {
        WriteSpriteIfMissing(Art + "/EP_White.png", CreateSolidTexture(64, 64, Color.white));
        WriteSpriteIfMissing(Art + "/EP_Glow.png", CreateGlowTexture(128));
        WriteSpriteIfMissing(Art + "/EP_Operator.png", CreateOperatorTexture(128, 256));
        AssetDatabase.Refresh();
        ConfigureSprite(Art + "/EP_White.png", 64f);
        ConfigureSprite(Art + "/EP_Glow.png", 128f);
        ConfigureSprite(Art + "/EP_Operator.png", 140f);
    }

    private static void WriteSpriteIfMissing(string path, Texture2D texture)
    {
        if (!File.Exists(path)) File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
    }

    private static void ConfigureSprite(string path, float pixelsPerUnit)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.filterMode = FilterMode.Bilinear;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
    }

    private static Texture2D CreateSolidTexture(int width, int height, Color color)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    private static Texture2D CreateGlowTexture(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = (x - size * 0.5f) / (size * 0.5f);
            float dy = (y - size * 0.5f) / (size * 0.5f);
            float alpha = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy)), 2.2f);
            texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
        }
        texture.Apply();
        return texture;
    }

    private static Texture2D CreateOperatorTexture(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color clear = new Color(0f, 0f, 0f, 0f);
        Color suit = new Color(0.09f, 0.13f, 0.15f, 1f);
        Color edge = new Color(0.25f, 0.35f, 0.35f, 1f);
        Color visor = new Color(0.57f, 0.75f, 0.71f, 1f);
        Color dark = new Color(0.025f, 0.04f, 0.05f, 1f);
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++) texture.SetPixel(x, y, clear);
        FillPolygon(texture, new Vector2[] { new(33, 4), new(62, 4), new(67, 111), new(57, 135), new(33, 130), new(26, 102) }, suit);
        FillPolygon(texture, new Vector2[] { new(39, 92), new(20, 116), new(14, 106), new(29, 74), new(45, 69) }, suit);
        FillPolygon(texture, new Vector2[] { new(57, 94), new(85, 113), new(79, 102), new(65, 72), new(49, 69) }, suit);
        FillPolygon(texture, new Vector2[] { new(28, 113), new(46, 115), new(43, 158), new(34, 163), new(23, 147) }, edge);
        FillPolygon(texture, new Vector2[] { new(53, 115), new(71, 111), new(78, 148), new(67, 161), new(56, 156) }, edge);
        FillEllipse(texture, 25, 163, 77, 236, suit);
        FillEllipse(texture, 31, 170, 71, 229, edge);
        FillEllipse(texture, 35, 182, 68, 211, dark);
        FillPolygon(texture, new Vector2[] { new(38, 193), new(64, 193), new(67, 202), new(41, 204) }, visor);
        FillRect(texture, 29, 89, 74, 97, dark);
        FillRect(texture, 35, 74, 61, 78, visor);
        FillRect(texture, 47, 21, 51, 66, edge);
        FillRect(texture, 36, 128, 40, 149, visor);
        FillRect(texture, 61, 128, 65, 149, visor);
        texture.Apply();
        return texture;
    }

    private static void FillRect(Texture2D texture, int x0, int y0, int x1, int y1, Color color)
    {
        for (int y = Mathf.Max(0, y0); y < Mathf.Min(texture.height, y1); y++)
        for (int x = Mathf.Max(0, x0); x < Mathf.Min(texture.width, x1); x++) texture.SetPixel(x, y, color);
    }

    private static void FillEllipse(Texture2D texture, int x0, int y0, int x1, int y1, Color color)
    {
        float cx = (x0 + x1) * 0.5f, cy = (y0 + y1) * 0.5f;
        float rx = (x1 - x0) * 0.5f, ry = (y1 - y0) * 0.5f;
        for (int y = y0; y <= y1; y++)
        for (int x = x0; x <= x1; x++)
        {
            float dx = (x - cx) / rx, dy = (y - cy) / ry;
            if (dx * dx + dy * dy <= 1f) texture.SetPixel(x, y, color);
        }
    }

    private static void FillPolygon(Texture2D texture, Vector2[] points, Color color)
    {
        int minX = Mathf.FloorToInt(points[0].x), maxX = minX;
        int minY = Mathf.FloorToInt(points[0].y), maxY = minY;
        foreach (Vector2 point in points)
        {
            minX = Mathf.Min(minX, Mathf.FloorToInt(point.x)); maxX = Mathf.Max(maxX, Mathf.CeilToInt(point.x));
            minY = Mathf.Min(minY, Mathf.FloorToInt(point.y)); maxY = Mathf.Max(maxY, Mathf.CeilToInt(point.y));
        }
        for (int y = minY; y <= maxY; y++)
        for (int x = minX; x <= maxX; x++)
        {
            bool inside = false;
            for (int i = 0, j = points.Length - 1; i < points.Length; j = i++)
            {
                bool crosses = ((points[i].y > y) != (points[j].y > y)) &&
                    (x < (points[j].x - points[i].x) * (y - points[i].y) / (points[j].y - points[i].y + 0.0001f) + points[i].x);
                if (crosses) inside = !inside;
            }
            if (inside && x >= 0 && x < texture.width && y >= 0 && y < texture.height) texture.SetPixel(x, y, color);
        }
    }

    private static void BuildCameraAndLayers()
    {
        GameObject cameraObject = new GameObject("Camera // facility tracking");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5.7f;
        camera.backgroundColor = new Color(0.012f, 0.018f, 0.023f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        cameraObject.AddComponent<AudioListener>();
        cameraObject.AddComponent<EchoProtocolCamera2D>();
        cameraObject.transform.position = new Vector3(5.6f, 0f, -10f);

        CreateSprite("Far wall // cold concrete", new Vector3(19f, 0.3f, 4f), new Vector2(48f, 13f), new Color(0.045f, 0.06f, 0.066f), -30, white)
            .gameObject.AddComponent<EchoProtocolParallaxLayer2D>().Configure(cameraObject.transform, 0.12f);
        CreateSprite("Mid wall // observation bays", new Vector3(19f, 0.2f, 3f), new Vector2(46f, 11f), new Color(0.075f, 0.09f, 0.091f), -23, white)
            .gameObject.AddComponent<EchoProtocolParallaxLayer2D>().Configure(cameraObject.transform, 0.3f);
        for (int i = 0; i < 13; i++)
        {
            float x = i * 3.35f;
            CreateSprite("Distant support // " + i.ToString("00"), new Vector3(x, 0.25f, 2f), new Vector2(0.16f, 9.2f), new Color(0.12f, 0.14f, 0.14f), -18, white)
                .gameObject.AddComponent<EchoProtocolParallaxLayer2D>().Configure(cameraObject.transform, 0.52f);
            CreateSprite("Distant luminaire // " + i.ToString("00"), new Vector3(x + 1.4f, 3.1f, 1.9f), new Vector2(1.45f, 0.07f), new Color(0.6f, 0.69f, 0.63f, 0.42f), -17, white)
                .gameObject.AddComponent<EchoProtocolParallaxLayer2D>().Configure(cameraObject.transform, 0.5f);
        }
    }

    private static void BuildArchitecture()
    {
        Color wall = new Color(0.075f, 0.09f, 0.091f);
        Color trim = new Color(0.15f, 0.18f, 0.17f);
        Color shadow = new Color(0.025f, 0.035f, 0.04f);
        CreateSprite("Facility ceiling", new Vector3(19f, 4.25f, 0f), new Vector2(41f, 0.8f), shadow, -5, white);
        CreateSprite("Cable trench", new Vector3(19f, -3.05f, 0f), new Vector2(41f, 0.7f), new Color(0.02f, 0.027f, 0.03f), 4, white);
        CreateSolid("Floor // initial bay", new Vector3(6.3f, -2.85f, 0f), new Vector2(12.6f, 0.5f), new Color(0.17f, 0.19f, 0.18f), -1);
        CreateSolid("Floor // main corridor", new Vector3(19.05f, -2.85f, 0f), new Vector2(10.5f, 0.5f), new Color(0.15f, 0.17f, 0.16f), -1);
        CreateSolid("Floor // archive observation", new Vector3(31.9f, -2.85f, 0f), new Vector2(13.8f, 0.5f), new Color(0.15f, 0.17f, 0.16f), -1);

        for (int i = 0; i < 19; i++)
        {
            float x = 0.5f + i * 2.05f;
            CreateSprite("Floor seam // " + i.ToString("00"), new Vector3(x, -2.575f, 0f), new Vector2(0.035f, 0.04f), new Color(0.35f, 0.38f, 0.35f), 1, white);
            CreateSprite("Ceiling rib // " + i.ToString("00"), new Vector3(x, 3.72f, 0f), new Vector2(0.28f, 0.52f), trim, -2, white);
            if (i % 3 == 0)
            {
                CreateSprite("Ceiling warning lamp // " + i.ToString("00"), new Vector3(x + 0.55f, 3.37f, 0f), new Vector2(1.1f, 0.055f), new Color(0.75f, 0.64f, 0.43f, 0.62f), -1, glow);
                CreateSprite("Cable // overhead", new Vector3(x + 1.2f, 3.91f, 0f), new Vector2(1.7f, 0.1f), shadow, 12, white);
            }
        }

        for (int i = 0; i < 7; i++)
        {
            float x = 3.2f + i * 5.2f;
            CreateSprite("Wall panel // " + i.ToString("00"), new Vector3(x, 0.5f, 0.2f), new Vector2(4.65f, 5.1f), wall, -9, white);
            CreateSprite("Structural rib // " + i.ToString("00"), new Vector3(x - 2.3f, 0.45f, 0f), new Vector2(0.15f, 5.3f), trim, 2, white);
            CreateSprite("Inspection window // " + i.ToString("00"), new Vector3(x, 0.6f, -0.2f), new Vector2(2.8f, 2.15f), new Color(0.035f, 0.058f, 0.06f, 0.92f), -7, white);
            CreateSprite("Glass edge // " + i.ToString("00"), new Vector3(x, -0.52f, -0.1f), new Vector2(2.85f, 0.035f), new Color(0.39f, 0.52f, 0.48f, 0.5f), -6, white);
        }

        CreateSprite("Foreground pipe // upper", new Vector3(18f, 2.9f, 0f), new Vector2(39f, 0.14f), new Color(0.025f, 0.035f, 0.038f), 20, white);
        CreateSprite("Foreground pipe // lower", new Vector3(28f, -1.35f, 0f), new Vector2(22f, 0.1f), new Color(0.025f, 0.035f, 0.038f), 18, white);
        for (int i = 0; i < 5; i++)
        {
            float x = 4f + i * 7.3f;
            CreateSprite("Emergency strip // " + i.ToString("00"), new Vector3(x, -1.93f, 0f), new Vector2(0.68f, 0.04f), new Color(0.84f, 0.31f, 0.16f, 0.55f), 5, glow);
        }

        CreateSprite("Observation glass // opaque", new Vector3(33.8f, 0f, 0f), new Vector2(5.8f, 5.4f), new Color(0.025f, 0.045f, 0.048f, 0.72f), 8, white);
        CreateSprite("Observation glass // frame", new Vector3(33.8f, 2.72f, 0f), new Vector2(6f, 0.16f), trim, 9, white);
        CreateSprite("Observation glass // lower rail", new Vector3(33.8f, -2.62f, 0f), new Vector2(6f, 0.16f), trim, 9, white);
    }

    private static EchoProtocolController2D BuildPlayer()
    {
        GameObject playerObject = new GameObject("Operator // 041");
        playerObject.transform.SetParent(world, true);
        playerObject.transform.position = new Vector3(2f, -1.75f, 0f);
        Rigidbody2D body = playerObject.AddComponent<Rigidbody2D>();
        body.gravityScale = 3.2f;
        CapsuleCollider2D collider = playerObject.AddComponent<CapsuleCollider2D>();
        collider.size = new Vector2(0.62f, 1.44f);

        GameObject visualObject = new GameObject("Visual");
        visualObject.transform.SetParent(playerObject.transform, false);
        visualObject.transform.localPosition = new Vector3(0f, 0.02f, 0f);
        visualObject.transform.localScale = new Vector3(0.96f, 1f, 1f);
        SpriteRenderer operatorRenderer = visualObject.AddComponent<SpriteRenderer>();
        operatorRenderer.sprite = operatorSprite;
        operatorRenderer.color = Color.white;
        operatorRenderer.sortingOrder = 10;

        GameObject shadowObject = new GameObject("Ground contact shadow");
        shadowObject.transform.SetParent(visualObject.transform, false);
        shadowObject.transform.localPosition = new Vector3(0f, -0.77f, 0f);
        shadowObject.transform.localScale = new Vector3(1.25f, 0.23f, 1f);
        SpriteRenderer shadow = shadowObject.AddComponent<SpriteRenderer>();
        shadow.sprite = glow;
        shadow.color = new Color(0f, 0f, 0f, 0.65f);
        shadow.sortingOrder = -2;

        EchoProtocolController2D controller = playerObject.AddComponent<EchoProtocolController2D>();
        controller.Configure(visualObject.transform);
        playerObject.AddComponent<EchoProtocolRecording2D>();
        return controller;
    }

    private static void BuildPuzzles()
    {
        CreateDoor("Bulkhead A // echo-only weight", 13.2f, new Color(0.2f, 0.24f, 0.22f), out EchoProtocolSecurityDoor2D firstDoor, out SpriteRenderer firstIndicator);
        GameObject plateObject = CreateSprite("Pressure plate // echo required", new Vector3(8.15f, -2.47f, 0f), new Vector2(2f, 0.12f), new Color(0.79f, 0.31f, 0.18f), 7, white).gameObject;
        BoxCollider2D plateCollider = plateObject.AddComponent<BoxCollider2D>();
        plateCollider.isTrigger = true;
        plateCollider.size = new Vector2(1.1f, 19f);
        plateCollider.offset = Vector2.zero;
        EchoProtocolPressurePlate2D plate = plateObject.AddComponent<EchoProtocolPressurePlate2D>();
        plate.Configure(firstDoor, plateObject.GetComponent<SpriteRenderer>(), true);
        CreateSprite("Plate // approach indicator", new Vector3(8.15f, -2.22f, 0f), new Vector2(1.1f, 0.035f), new Color(0.82f, 0.38f, 0.2f, 0.55f), 8, glow);

        CreateDoor("Bulkhead B // recorded credential", 25.5f, new Color(0.18f, 0.22f, 0.21f), out EchoProtocolSecurityDoor2D secondDoor, out SpriteRenderer secondIndicator);
        GameObject console = CreateSprite("Echo relay console // action target", new Vector3(18.55f, -1.82f, 0f), new Vector2(0.85f, 1.38f), new Color(0.16f, 0.2f, 0.19f), 7, white).gameObject;
        BoxCollider2D consoleCollider = console.AddComponent<BoxCollider2D>();
        consoleCollider.size = Vector2.one;
        EchoProtocolTimedRelay2D relay = console.AddComponent<EchoProtocolTimedRelay2D>();
        relay.Configure(secondDoor, secondIndicator, 18f);
        CreateSprite("Console // sealed display", new Vector3(18.55f, -1.55f, 0f), new Vector2(0.48f, 0.16f), new Color(0.57f, 0.75f, 0.71f, 0.52f), 8, glow);
        CreateSprite("Console // lower warning", new Vector3(18.55f, -2.19f, 0f), new Vector2(0.48f, 0.055f), new Color(0.85f, 0.33f, 0.2f, 0.8f), 8, white);

        GameObject laserObject = CreateSprite("Security laser // timing gate", new Vector3(22.9f, -0.38f, 0f), new Vector2(0.14f, 4.45f), new Color(0.8f, 0.2f, 0.12f, 0.3f), 11, glow).gameObject;
        BoxCollider2D laserCollider = laserObject.AddComponent<BoxCollider2D>();
        laserCollider.isTrigger = true;
        laserCollider.enabled = false;
        EchoProtocolTimedLaser2D laser = laserObject.AddComponent<EchoProtocolTimedLaser2D>();
        laser.Configure(laserCollider, laserObject.GetComponent<SpriteRenderer>(), 4.6f, 2.1f, 1.6f);
        CreateSprite("Laser emitter // west", new Vector3(22.9f, -2.3f, 0f), new Vector2(0.55f, 0.46f), new Color(0.19f, 0.22f, 0.21f), 6, white);
        CreateSprite("Laser emitter // east", new Vector3(22.9f, 1.55f, 0f), new Vector2(0.55f, 0.46f), new Color(0.19f, 0.22f, 0.21f), 6, white);

        // Optional upper maintenance route: it clears the laser beam but requires four precise jumps.
        CreateSolid("Maintenance route // step 01", new Vector3(16.2f, -0.98f, 0f), new Vector2(2.1f, 0.28f), new Color(0.17f, 0.2f, 0.19f), 3);
        CreateSolid("Maintenance route // step 02", new Vector3(18.35f, 0.23f, 0f), new Vector2(2.05f, 0.28f), new Color(0.17f, 0.2f, 0.19f), 3);
        CreateSolid("Maintenance route // step 03", new Vector3(20.52f, 1.48f, 0f), new Vector2(2.1f, 0.28f), new Color(0.17f, 0.2f, 0.19f), 3);
        CreateSolid("Maintenance route // above the beam", new Vector3(23.32f, 2.28f, 0f), new Vector2(2.55f, 0.28f), new Color(0.17f, 0.2f, 0.19f), 3);
        for (int i = 0; i < 4; i++)
            CreateSprite("Maintenance edge // " + i.ToString("00"), new Vector3(15.3f + i * 2.35f, -0.1f + i * 0.68f, 0f), new Vector2(0.62f, 0.035f), new Color(0.55f, 0.66f, 0.6f, 0.6f), 4, glow);
        CreateSecretCache(new Vector3(23.3f, 3.12f, 0f));
        CreateSolid("Raised archive threshold", new Vector3(29.7f, -2.12f, 0f), new Vector2(3.1f, 0.44f), new Color(0.19f, 0.22f, 0.2f), 1);
        CreateSprite("Threshold edge // warning", new Vector3(29.7f, -1.87f, 0f), new Vector2(2.9f, 0.04f), new Color(0.73f, 0.43f, 0.24f, 0.65f), 3, glow);
        CreateSprite("Archive access placard", new Vector3(30.5f, 1.92f, 0f), new Vector2(2.2f, 0.56f), new Color(0.13f, 0.17f, 0.16f), 5, white);
        CreateWorldText("ARCHIVE 04", new Vector3(29.62f, 1.84f, -0.1f), new Color(0.58f, 0.68f, 0.63f));
    }

    private static void CreateDoor(string name, float x, Color color, out EchoProtocolSecurityDoor2D door, out SpriteRenderer indicator)
    {
        GameObject doorObject = CreateSprite(name, new Vector3(x, -0.48f, 0f), new Vector2(1.38f, 4.25f), color, 12, white).gameObject;
        BoxCollider2D collider = doorObject.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
        door = doorObject.AddComponent<EchoProtocolSecurityDoor2D>();
        GameObject lamp = CreateSprite(name + " // status", new Vector3(x, 1.87f, 0f), new Vector2(0.58f, 0.08f), new Color(0.8f, 0.28f, 0.15f), 13, glow).gameObject;
        indicator = lamp.GetComponent<SpriteRenderer>();
        door.Configure(collider, indicator);
        CreateSprite(name + " // side rail", new Vector3(x - 0.82f, -0.48f, 0f), new Vector2(0.15f, 4.6f), new Color(0.25f, 0.29f, 0.27f), 11, white);
        CreateSprite(name + " // side rail", new Vector3(x + 0.82f, -0.48f, 0f), new Vector2(0.15f, 4.6f), new Color(0.25f, 0.29f, 0.27f), 11, white);
    }

    private static void CreateSecretCache(Vector3 position)
    {
        SpriteRenderer renderer = CreateSprite("Unindexed memory cache // optional", position, new Vector2(0.62f, 0.62f), new Color(0.75f, 0.77f, 0.63f, 0.88f), 10, glow);
        BoxCollider2D trigger = renderer.gameObject.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = Vector2.one;
        renderer.gameObject.AddComponent<EchoProtocolSecretCache2D>().Configure(renderer);
    }
    private static void BuildAnomaly()
    {
        GameObject anomaly = new GameObject("Unregistered subject // behind observation glass");
        anomaly.transform.SetParent(world, true);
        anomaly.transform.position = new Vector3(35.4f, -1.75f, -0.4f);
        GameObject visual = new GameObject("Signal silhouette");
        visual.transform.SetParent(anomaly.transform, false);
        visual.transform.localScale = new Vector3(0.96f, 1f, 1f);
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = operatorSprite;
        renderer.color = new Color(0.48f, 0.78f, 0.73f, 0.3f);
        renderer.sortingOrder = 9;
        EchoProtocolAnomaly2D component = anomaly.AddComponent<EchoProtocolAnomaly2D>();
        component.Configure(new[] { renderer });
    }

    private static void BuildNarrativeTriggers()
    {
        CreateWorldTrigger("Entry // welcome log", new Vector3(4.5f, -0.3f, 0f), new Vector2(1.1f, 5f), "SHIFT 041 // NO STAFF RESPONSE. MOTION ARCHIVE IS STILL RUNNING.");
        EchoProtocolAnomaly2D anomaly = Object.FindAnyObjectByType<EchoProtocolAnomaly2D>();
        CreateWorldTrigger("Observation bay // false movement", new Vector3(28.6f, -0.3f, 0f), new Vector2(0.8f, 5f), "CAMERA 09 // MOVEMENT LOGGED BEFORE YOUR ACCESS TIME.", anomaly);
        CreateWorldTrigger("Archive // final observation", new Vector3(32f, -0.2f, 0f), new Vector2(0.8f, 4.8f), "SUBJECT 041 // SECONDARY TRACE DETECTED. ORIGIN: YOUR POSITION.");
    }

    private static void CreateWorldTrigger(string name, Vector3 position, Vector2 size, string message, EchoProtocolAnomaly2D anomaly = null)
    {
        GameObject trigger = new GameObject(name);
        trigger.transform.SetParent(world, true);
        trigger.transform.position = position;
        BoxCollider2D collider = trigger.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = size;
        EchoProtocolWorldTrigger2D worldTrigger = trigger.AddComponent<EchoProtocolWorldTrigger2D>();
        worldTrigger.Configure(message, anomaly);
    }

    private static void BuildAtmosphere()
    {
        GameObject particles = new GameObject("Airborne dust // low visibility");
        particles.transform.SetParent(world, true);
        particles.AddComponent<EchoProtocolAmbientParticles2D>().Configure(40f, 8f);
        for (int i = 0; i < 9; i++)
        {
            float x = 1.4f + i * 4.2f;
            CreateSprite("Distant warning wash // " + i.ToString("00"), new Vector3(x, 3.08f, 0f), new Vector2(2.1f, 0.22f), new Color(0.59f, 0.36f, 0.23f, 0.11f), 3, glow);
        }
    }

    private static void BuildHud()
    {
        GameObject hud = new GameObject("ECHO PROTOCOL // field interface");
        EchoProtocolHUD2D ui = hud.AddComponent<EchoProtocolHUD2D>();
        ui.Configure(Object.FindAnyObjectByType<EchoProtocolRecording2D>());
    }

    private static void BuildAmbientAudio()
    {
        GameObject audio = new GameObject("Facility ambience // low signal drone");
        audio.AddComponent<EchoProtocolAmbientAudio2D>();
    }

    private static SpriteRenderer CreateSprite(string name, Vector3 position, Vector2 size, Color color, int order, Sprite sprite)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(world, true);
        go.transform.position = position;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = order;
        return renderer;
    }

    private static GameObject CreateSolid(string name, Vector3 position, Vector2 size, Color color, int order)
    {
        GameObject go = CreateSprite(name, position, size, color, order, white).gameObject;
        BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
        return go;
    }

    private static void CreateWorldText(string text, Vector3 position, Color color)
    {
        GameObject label = new GameObject("Text // " + text);
        label.transform.SetParent(world, true);
        label.transform.position = position;
        label.transform.localScale = Vector3.one * 0.2f;
        TextMesh mesh = label.AddComponent<TextMesh>();
        mesh.text = text;
        mesh.characterSize = 0.35f;
        mesh.fontSize = 32;
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.color = color;
        MeshRenderer renderer = label.GetComponent<MeshRenderer>();
        renderer.sortingOrder = 15;
    }
}
#endif





