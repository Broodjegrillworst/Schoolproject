using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class VesperGameUI
{
    private static Text messageText;
    private static Text promptText;
    private static Text headerText;
    private static Text shardsText;
    private static Text healthText;
    private static Text energyText;
    private static Image energyFill;
    private static Text bossLabel;
    private static Image bossFill;
    private static GameObject pausePanel;
    private static GameObject mapPanel;
    private static GameObject inventoryPanel;
    private static GameObject skillPanel;
    private static GameObject settingsPanel;
    private static Text mapContent;
    private static Text inventoryContent;
    private static Text skillContent;
    private static readonly List<Image> regionMapMarkers = new List<Image>();
    private static readonly List<Image> roomMapMarkers = new List<Image>();
    private static float messageUntil;
    private static string currentMessage;
    private static int currentMode;
    private static readonly Dictionary<int, string> AreaNames = new Dictionary<int, string>
    {
        {1,"01  //  HOLLOW GROVE"}, {2,"02  //  DROWNED ARCHIVE"}, {3,"03  //  EMBER VEINS"},
        {4,"04  //  MOONLIT CANOPY"}, {5,"05  //  RIFT HEART"}
    };
    private static readonly Color Ink = new Color(0.025f, 0.045f, 0.065f, 0.94f);
    private static readonly Color Mint = new Color(0.48f, 0.93f, 0.81f);
    private static readonly Color Pale = new Color(0.83f, 0.89f, 0.84f);
    private static readonly Vector2[] RegionMapPositions =
    {
        new Vector2(0.12f, 0.54f), new Vector2(0.31f, 0.72f), new Vector2(0.5f, 0.52f),
        new Vector2(0.69f, 0.72f), new Vector2(0.88f, 0.54f)
    };

    public static void BuildMenu()
    {
        Reset();
        Canvas c = CreateCanvas("VESPER // MENU");
        RectTransform back = Panel(c.transform, new Color(0.018f, 0.03f, 0.048f, 0.78f), Vector2.zero, Vector2.one);
        Panel(back, new Color(0.025f, 0.045f, 0.065f, 0.94f), new Vector2(0.04f, 0.05f), new Vector2(0.51f, 0.95f));
        Text kicker = Label(back, "AN ORIGINAL 2D METROIDVANIA", 18, Mint, TextAnchor.MiddleLeft, new Vector2(0.09f, 0.84f), new Vector2(0.48f, 0.9f));
        kicker.fontStyle = FontStyle.Bold;
        Text title = Label(back, "VESPER", 84, Pale, TextAnchor.MiddleLeft, new Vector2(0.085f, 0.69f), new Vector2(0.5f, 0.85f));
        title.fontStyle = FontStyle.Bold;
        Label(back, "ECHOES BELOW", 25, new Color(0.52f, 0.69f, 0.72f), TextAnchor.MiddleLeft, new Vector2(0.095f, 0.65f), new Vector2(0.48f, 0.73f));
        Panel(back, new Color(0.48f, 0.93f, 0.81f, 0.75f), new Vector2(0.095f, 0.615f), new Vector2(0.27f, 0.619f));
        Label(back, "A vanished world remembers you. Find the five roots,\nawaken what was buried, and return the light below.", 18, Pale, TextAnchor.UpperLeft, new Vector2(0.095f, 0.52f), new Vector2(0.49f, 0.61f));
        Button(back, "BEGIN NEW DESCENT", new Vector2(0.095f, 0.42f), new Vector2(0.35f, 0.06f), () => { VesperProgress.NewGame(); SceneManager.LoadScene("Region01"); }, true);
        Button(back, "CONTINUE DESCENT", new Vector2(0.095f, 0.345f), new Vector2(0.35f, 0.06f), ContinueSaved, false);
        Button(back, "WORLD MAP", new Vector2(0.095f, 0.265f), new Vector2(0.17f, 0.06f), ShowMenuMap, false);
        Button(back, "SETTINGS", new Vector2(0.275f, 0.265f), new Vector2(0.17f, 0.06f), ShowMenuSettings, false);
        Label(back, "A / D MOVE     SPACE JUMP     SHIFT DASH\nJ STRIKE     L ECHO WAVE     E INTERACT", 14, new Color(0.54f, 0.67f, 0.7f), TextAnchor.UpperLeft, new Vector2(0.095f, 0.08f), new Vector2(0.47f, 0.16f));
        Button(back, "QUIT", new Vector2(0.095f, 0.18f), new Vector2(0.35f, 0.055f), Application.Quit, false);
        Label(c.transform, "MADE BY JAIDEN ASRUF", 14, new Color(0.67f, 0.75f, 0.71f), TextAnchor.LowerRight, new Vector2(0.7f, 0.055f), new Vector2(0.96f, 0.09f));
        settingsPanel = BuildSettingsOverlay(c.transform, false);
        mapPanel = BuildMapOverlay(c.transform, false);
    }

    public static void BuildHud(int region)
    {
        Reset();
        Canvas canvas = CreateCanvas("VESPER // HUD");
        headerText = Label(canvas.transform, "", 18, Pale, TextAnchor.UpperLeft, new Vector2(0.025f, 0.91f), new Vector2(0.45f, 0.97f));
        shardsText = Label(canvas.transform, "MEMORY  00", 18, Mint, TextAnchor.UpperLeft, new Vector2(0.025f, 0.865f), new Vector2(0.3f, 0.92f));
        healthText = Label(canvas.transform, "VITALS  ◆ ◆ ◆ ◆ ◆", 17, new Color(1f, 0.68f, 0.62f), TextAnchor.UpperLeft, new Vector2(0.025f, 0.82f), new Vector2(0.35f, 0.875f));
        energyText = Label(canvas.transform, "ECHO  100%", 15, Mint, TextAnchor.UpperRight, new Vector2(0.76f, 0.895f), new Vector2(0.97f, 0.94f));
        energyFill = ImageBar(canvas.transform, new Vector2(0.77f, 0.865f), new Vector2(0.20f, 0.012f), Mint);
        bossLabel = Label(canvas.transform, "", 16, new Color(1f, 0.78f, 0.72f), TextAnchor.MiddleCenter, new Vector2(0.38f, 0.90f), new Vector2(0.62f, 0.94f));
        bossFill = ImageBar(canvas.transform, new Vector2(0.38f, 0.88f), new Vector2(0.24f, 0.012f), new Color(0.95f, 0.39f, 0.47f));
        bossLabel.gameObject.SetActive(false); bossFill.transform.parent.gameObject.SetActive(false);
        Label(canvas.transform, "M  MAP     I  INVENTORY     TAB  SKILLS     ESC  PAUSE", 12, new Color(0.62f, 0.71f, 0.72f), TextAnchor.UpperRight, new Vector2(0.57f, 0.82f), new Vector2(0.97f, 0.87f));
        messageText = Label(canvas.transform, "", 19, Mint, TextAnchor.MiddleCenter, new Vector2(0.2f, 0.075f), new Vector2(0.8f, 0.13f));
        promptText = Label(canvas.transform, "", 15, Pale, TextAnchor.LowerCenter, new Vector2(0.25f, 0.03f), new Vector2(0.75f, 0.075f));
        pausePanel = BuildPauseOverlay(canvas.transform);
        mapPanel = BuildMapOverlay(canvas.transform, false);
        inventoryPanel = BuildInventoryOverlay(canvas.transform, false);
        skillPanel = BuildSkillOverlay(canvas.transform, false);
        settingsPanel = BuildSettingsOverlay(canvas.transform, false);
        SetRegion(region);
        RefreshProgress();
    }

    public static void SetRegion(int region)
    {
        if (headerText != null) headerText.text = AreaNames.TryGetValue(region, out string name) ? name : "VESPER // BELOW";
    }
    public static void SetVitals(int health, int maximum)
    {
        if (healthText != null) healthText.text = "VITALS  " + new string('◆', Mathf.Clamp(health, 0, maximum)) + new string('◇', Mathf.Clamp(maximum - health, 0, maximum));
    }
    public static void SetEnergy(float value, float maximum)
    {
        if (energyText != null) energyText.text = "ECHO  " + Mathf.RoundToInt(value / Mathf.Max(1f, maximum) * 100f) + "%";
        if (energyFill != null) energyFill.fillAmount = Mathf.Clamp01(value / Mathf.Max(1f, maximum));
    }
    public static void SetBoss(string title, float fraction)
    {
        if (bossLabel == null || bossFill == null) return;
        bool show = fraction > 0.001f;
        bossLabel.gameObject.SetActive(show); bossFill.transform.parent.gameObject.SetActive(show);
        bossLabel.text = "WARDEN // " + title;
        bossFill.fillAmount = Mathf.Clamp01(fraction);
    }
    public static void FlashMessage(string message, float seconds)
    {
        currentMessage = message; messageUntil = Time.unscaledTime + seconds;
        if (messageText != null) messageText.text = message;
        RefreshProgress();
    }
    public static void FlashPrompt(string message) { if (promptText != null) promptText.text = message; }
    public static void ClearPrompt() { if (promptText != null) promptText.text = ""; }
    public static void ToggleMap() { TogglePanel(mapPanel, 1); }
    public static void ToggleInventory() { TogglePanel(inventoryPanel, 2); }
    public static void ToggleSkillTree() { TogglePanel(skillPanel, 3); }
    public static void TogglePause() { if (pausePanel == null) return; bool open = !pausePanel.activeSelf; CloseAll(); pausePanel.SetActive(open); Time.timeScale = open ? 0f : 1f; currentMode = open ? 4 : 0; }

    public static void Tick()
    {
        if (messageText != null && Time.unscaledTime > messageUntil && messageText.text == currentMessage) messageText.text = "";
    }
    public static void RefreshState() => RefreshProgress();
    private static void TogglePanel(GameObject panel, int mode)
    {
        if (panel == null) return;
        bool open = !panel.activeSelf || currentMode != mode;
        CloseAll();
        panel.SetActive(open);
        Time.timeScale = open ? 0f : 1f;
        currentMode = open ? mode : 0;
        RefreshProgress();
    }
    private static void CloseAll()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (mapPanel != null) mapPanel.SetActive(false);
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        if (skillPanel != null) skillPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        Time.timeScale = 1f;
        currentMode = 0;
    }

    private static void RefreshProgress()
    {
        if (shardsText != null) shardsText.text = "MEMORY  " + VesperProgress.Shards.ToString("00");
        if (mapPanel != null)
        {
            if (mapContent != null) mapContent.text = RegionMapText();
            for (int i = 0; i < regionMapMarkers.Count && i < 5; i++)
            {
                bool discovered = VesperProgress.IsDiscovered(i + 1);
                bool defeated = VesperProgress.HasDefeatedBoss(i + 1);
                regionMapMarkers[i].color = defeated ? Mint : i + 1 == VesperProgress.CurrentRegion ? Pale : discovered ? new Color(0.37f, 0.75f, 0.72f) : new Color(0.22f, 0.3f, 0.34f);
            }
            for (int i = 0; i < roomMapMarkers.Count && i < 7; i++)
                roomMapMarkers[i].color = VesperProgress.IsRoomDiscovered(VesperProgress.CurrentRegion, i) ? Mint : new Color(0.22f, 0.3f, 0.34f);
        }
        if (inventoryPanel != null)
        {
            if (inventoryContent != null) inventoryContent.text = AbilitiesText() + "\nMEMORY SHARDS   " + VesperProgress.Shards + "\nVITALS   " + (5 + VesperProgress.VitalityTier * 2) + "\nECHO RESERVOIR   " + (100 + VesperProgress.ReservoirTier * 25);
        }
        if (skillPanel != null)
        {
            if (skillContent != null) skillContent.text = "Choose a path. Each tier costs more memory.\nSTRIKE " + VesperProgress.SkillTier + "/3  ·  RESERVOIR " + VesperProgress.ReservoirTier + "/3  ·  VITALITY " + VesperProgress.VitalityTier + "/3\nMEMORY SHARDS   " + VesperProgress.Shards;
        }
    }

    private static string RegionMapText()
    {
        int mapped = 0;
        for (int i = 0; i < 7; i++) if (VesperProgress.IsRoomDiscovered(VesperProgress.CurrentRegion, i)) mapped++;
        return "CURRENT REGION   " + AreaNames[VesperProgress.CurrentRegion] + "\nCHARTED CHAMBERS   " + mapped + " / 7     ·     MINT = WARDEN DEFEATED     ·     PALE = CURRENT REGION";
    }
    private static string AbilitiesText()
    {
        string result = "";
        VesperAbility[] values = { VesperAbility.DoubleJump, VesperAbility.WallJump, VesperAbility.AirDash, VesperAbility.SuperDash, VesperAbility.EnergyWave };
        foreach (VesperAbility a in values) result += (VesperProgress.Has(a) ? "●  " : "○  ") + a.ToString() + "\n";
        return result;
    }
    private static void ShowMenuMap() { if (mapPanel != null) mapPanel.SetActive(!mapPanel.activeSelf); RefreshProgress(); }
    private static void ShowMenuSettings() { if (settingsPanel != null) settingsPanel.SetActive(!settingsPanel.activeSelf); }
    private static void ContinueSaved()
    {
        VesperProgress.Load();
        SceneManager.LoadScene("Region" + Mathf.Clamp(VesperProgress.CurrentRegion, 1, 5).ToString("00"));
    }

    private static Canvas CreateCanvas(string objectName)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas c = go.GetComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = 20;
        CanvasScaler scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;
        if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
        return c;
    }
    private static Font DefaultFont() => Resources.GetBuiltinResource<Font>("Arial.ttf");
    private static RectTransform Panel(Transform parent, Color color, Vector2 min, Vector2 max)
    {
        GameObject go = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>(); rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = color; return rt;
    }
    private static Text Label(Transform parent, string text, int size, Color color, TextAnchor align, Vector2 min, Vector2 max)
    {
        GameObject go = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)); go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>(); rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        Text t = go.GetComponent<Text>(); t.text = text; t.font = DefaultFont(); t.fontSize = size; t.color = color; t.alignment = align; t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow; t.lineSpacing = 1.1f;
        return t;
    }
    private static Button Button(Transform parent, string text, Vector2 min, Vector2 size, UnityAction action, bool primary)
    {
        GameObject go = new GameObject("Button // " + text, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(VesperButtonMotion)); go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>(); rt.anchorMin = min; rt.anchorMax = min; rt.pivot = new Vector2(0, 0); rt.sizeDelta = size * new Vector2(1920, 1080); rt.anchoredPosition = Vector2.zero;
        Image image = go.GetComponent<Image>(); image.color = primary ? new Color(0.28f, 0.73f, 0.64f, 0.96f) : new Color(0.12f, 0.2f, 0.24f, 0.96f);
        Button b = go.GetComponent<Button>(); b.targetGraphic = image; b.onClick.AddListener(action); ColorBlock cb = b.colors; cb.normalColor = image.color; cb.highlightedColor = new Color(0.58f, 0.94f, 0.82f); cb.pressedColor = new Color(0.18f, 0.42f, 0.4f); b.colors = cb;
        Text label = Label(go.transform, text, 17, primary ? new Color(0.025f, 0.06f, 0.065f) : Pale, TextAnchor.MiddleLeft, new Vector2(0.08f, 0f), new Vector2(0.95f, 1f)); label.fontStyle = FontStyle.Bold;
        return b;
    }
    private static Image ImageBar(Transform parent, Vector2 min, Vector2 size, Color fill)
    {
        RectTransform bg = Panel(parent, new Color(0.13f, 0.2f, 0.23f, 0.9f), min, min + size);
        RectTransform fr = Panel(bg, fill, Vector2.zero, Vector2.one); Image image = fr.GetComponent<Image>(); image.type = Image.Type.Filled; image.fillMethod = Image.FillMethod.Horizontal; return image;
    }
    private static GameObject BuildPauseOverlay(Transform parent)
    {
        RectTransform panel = Panel(parent, new Color(0.01f, 0.02f, 0.035f, 0.86f), new Vector2(0.32f, 0.18f), new Vector2(0.68f, 0.82f));
        Label(panel, "PAUSED // MEMORY HELD", 27, Pale, TextAnchor.MiddleCenter, new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.95f));
        Button(panel, "RESUME", new Vector2(0.15f, 0.64f), new Vector2(0.7f, 0.1f), TogglePause, true);
        Button(panel, "SETTINGS", new Vector2(0.15f, 0.49f), new Vector2(0.7f, 0.1f), () => { settingsPanel.SetActive(true); }, false);
        Button(panel, "RETURN TO TITLE", new Vector2(0.15f, 0.34f), new Vector2(0.7f, 0.1f), () => { Time.timeScale = 1f; SceneManager.LoadScene("VesperMenu"); }, false);
        Button(panel, "QUIT", new Vector2(0.15f, 0.19f), new Vector2(0.7f, 0.1f), Application.Quit, false);
        panel.gameObject.SetActive(false); return panel.gameObject;
    }
    private static GameObject BuildMapOverlay(Transform parent, bool show)
    {
        RectTransform panel = Panel(parent, Ink, new Vector2(0.29f, 0.18f), new Vector2(0.71f, 0.82f));
        regionMapMarkers.Clear(); roomMapMarkers.Clear();
        Label(panel, "WORLD ATLAS  //  ROOT NETWORK", 24, Mint, TextAnchor.UpperLeft, new Vector2(0.08f, 0.84f), new Vector2(0.92f, 0.96f));
        Label(panel, "Five regions share a root network. Gold paths are ability-gated shortcuts.", 14, Pale, TextAnchor.UpperLeft, new Vector2(0.08f, 0.77f), new Vector2(0.92f, 0.84f));
        for (int i = 0; i < RegionMapPositions.Length - 1; i++)
            MapLink(panel, RegionMapPositions[i], RegionMapPositions[i + 1], new Color(0.34f, 0.47f, 0.48f, 0.8f), 4f);
        MapLink(panel, RegionMapPositions[0], RegionMapPositions[3], new Color(0.76f, 0.61f, 0.37f, 0.62f), 2f);
        MapLink(panel, RegionMapPositions[1], RegionMapPositions[4], new Color(0.76f, 0.61f, 0.37f, 0.62f), 2f);
        string[] regions = { "HOLLOW GROVE", "DROWNED ARCHIVE", "EMBER VEINS", "MOONLIT CANOPY", "RIFT HEART" };
        for (int i = 0; i < RegionMapPositions.Length; i++)
        {
            regionMapMarkers.Add(MapMarker(panel, RegionMapPositions[i], 28f, new Color(0.22f, 0.3f, 0.34f)));
            Vector2 p = RegionMapPositions[i];
            Label(panel, regions[i], 11, Pale, TextAnchor.MiddleCenter, new Vector2(p.x - 0.1f, p.y - 0.12f), new Vector2(p.x + 0.1f, p.y - 0.075f));
        }
        Label(panel, "LOCAL CHAMBERS  //  MEMORY TRACE", 12, Mint, TextAnchor.MiddleLeft, new Vector2(0.08f, 0.39f), new Vector2(0.65f, 0.44f));
        Vector2[] localPositions =
        {
            new Vector2(0.12f, 0.28f), new Vector2(0.25f, 0.33f), new Vector2(0.38f, 0.28f),
            new Vector2(0.51f, 0.34f), new Vector2(0.64f, 0.28f), new Vector2(0.77f, 0.33f)
        };
        for (int i = 0; i < localPositions.Length - 1; i++) MapLink(panel, localPositions[i], localPositions[i + 1], new Color(0.25f, 0.35f, 0.38f, 0.7f), 2f);
        for (int i = 0; i < localPositions.Length; i++)
        {
            roomMapMarkers.Add(MapMarker(panel, localPositions[i], 13f, new Color(0.22f, 0.3f, 0.34f)));
            Label(panel, (i + 1).ToString("00"), 11, new Color(0.62f, 0.71f, 0.72f), TextAnchor.MiddleCenter,
                new Vector2(localPositions[i].x - 0.045f, localPositions[i].y - 0.075f), new Vector2(localPositions[i].x + 0.045f, localPositions[i].y - 0.04f));
        }
        // The vault is a real branch off the fourth chamber, not a free map marker.
        Vector2 vaultPosition = new Vector2(0.53f, 0.48f);
        MapLink(panel, localPositions[3], vaultPosition, new Color(0.68f, 0.53f, 0.36f, 0.6f), 2f);
        roomMapMarkers.Add(MapMarker(panel, vaultPosition, 17f, new Color(0.22f, 0.3f, 0.34f)));
        Label(panel, "VAULT", 11, new Color(0.9f, 0.72f, 0.46f), TextAnchor.MiddleCenter, new Vector2(0.49f, 0.425f), new Vector2(0.57f, 0.46f));
        mapContent = Label(panel, "", 12, Pale, TextAnchor.MiddleLeft, new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.2f));
        Button(panel, "CLOSE", new Vector2(0.69f, 0.06f), new Vector2(0.23f, 0.1f), () => CloseOverlay(panel.gameObject), false);
        panel.gameObject.SetActive(show); return panel.gameObject;
    }

    private static Image MapMarker(Transform parent, Vector2 anchor, float diameter, Color color)
    {
        GameObject go = new GameObject("Atlas marker", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>(); rect.anchorMin = anchor; rect.anchorMax = anchor; rect.pivot = new Vector2(0.5f, 0.5f); rect.sizeDelta = Vector2.one * diameter;
        Image image = go.GetComponent<Image>(); image.sprite = VesperArtFactory.Orb; image.color = color; return image;
    }

    private static void MapLink(Transform parent, Vector2 from, Vector2 to, Color color, float thickness)
    {
        RectTransform parentRect = parent.GetComponent<RectTransform>();
        GameObject go = new GameObject("Atlas route", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        Vector2 mid = (from + to) * 0.5f;
        rect.anchorMin = mid; rect.anchorMax = mid; rect.pivot = new Vector2(0.5f, 0.5f);
        Vector2 delta = Vector2.Scale(to - from, parentRect.rect.size);
        rect.sizeDelta = new Vector2(delta.magnitude, thickness);
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        go.GetComponent<Image>().color = color;
        go.transform.SetAsFirstSibling();
    }
    private static GameObject BuildInventoryOverlay(Transform parent, bool show)
    {
        RectTransform panel = Panel(parent, Ink, new Vector2(0.31f, 0.16f), new Vector2(0.69f, 0.84f));
        Label(panel, "RELICS // MEMORY", 27, Mint, TextAnchor.UpperLeft, new Vector2(0.08f, 0.82f), new Vector2(0.9f, 0.96f));
        inventoryContent = Label(panel, "", 18, Pale, TextAnchor.UpperLeft, new Vector2(0.08f, 0.2f), new Vector2(0.9f, 0.78f));
        Button(panel, "CLOSE", new Vector2(0.69f, 0.06f), new Vector2(0.23f, 0.1f), () => CloseOverlay(panel.gameObject), false);
        panel.gameObject.SetActive(show); return panel.gameObject;
    }
    private static GameObject BuildSkillOverlay(Transform parent, bool show)
    {
        RectTransform panel = Panel(parent, Ink, new Vector2(0.31f, 0.16f), new Vector2(0.69f, 0.84f));
        Label(panel, "SKILL TREE // MEMORY WEAVE", 27, Mint, TextAnchor.UpperLeft, new Vector2(0.08f, 0.82f), new Vector2(0.9f, 0.96f));
        skillContent = Label(panel, "", 17, Pale, TextAnchor.UpperLeft, new Vector2(0.08f, 0.66f), new Vector2(0.9f, 0.80f));
        Button(panel, "STRIKE  //  STRENGTHEN MELEE", new Vector2(0.08f, 0.52f), new Vector2(0.84f, 0.09f), () => PurchaseUpgrade(VesperSkillPath.Strike), true);
        Button(panel, "RESERVOIR  //  MORE ECHO ENERGY", new Vector2(0.08f, 0.39f), new Vector2(0.84f, 0.09f), () => PurchaseUpgrade(VesperSkillPath.Reservoir), false);
        Button(panel, "VITALITY  //  MORE HEALTH", new Vector2(0.08f, 0.26f), new Vector2(0.84f, 0.09f), () => PurchaseUpgrade(VesperSkillPath.Vitality), false);
        Button(panel, "CLOSE", new Vector2(0.69f, 0.06f), new Vector2(0.23f, 0.1f), () => CloseOverlay(panel.gameObject), false);
        panel.gameObject.SetActive(show); return panel.gameObject;
    }
    private static GameObject BuildSettingsOverlay(Transform parent, bool show)
    {
        RectTransform panel = Panel(parent, Ink, new Vector2(0.32f, 0.2f), new Vector2(0.68f, 0.8f));
        Label(panel, "SETTINGS", 27, Mint, TextAnchor.UpperLeft, new Vector2(0.08f, 0.82f), new Vector2(0.9f, 0.96f));
        Label(panel, "MASTER VOLUME", 16, Pale, TextAnchor.MiddleLeft, new Vector2(0.08f, 0.65f), new Vector2(0.45f, 0.76f));
        GameObject sliderGo = new GameObject("Master Volume", typeof(RectTransform), typeof(Slider)); sliderGo.transform.SetParent(panel, false);
        RectTransform srt = sliderGo.GetComponent<RectTransform>(); srt.anchorMin = new Vector2(0.08f, 0.54f); srt.anchorMax = new Vector2(0.92f, 0.63f); srt.offsetMin = Vector2.zero; srt.offsetMax = Vector2.zero;
        Slider slider = sliderGo.GetComponent<Slider>(); slider.minValue = 0f; slider.maxValue = 1f; slider.value = PlayerPrefs.GetFloat("VESPER_Volume", 0.8f); slider.onValueChanged.AddListener(v => { AudioListener.volume = v; PlayerPrefs.SetFloat("VESPER_Volume", v); });
        RectTransform track = Panel(sliderGo.transform, new Color(0.18f, 0.27f, 0.3f), new Vector2(0, 0.38f), new Vector2(1, 0.62f));
        RectTransform fill = Panel(track, Mint, Vector2.zero, Vector2.one); slider.fillRect = fill;
        RectTransform handle = Panel(sliderGo.transform, Pale, new Vector2(0, 0), new Vector2(0.05f, 1)); slider.handleRect = handle; slider.targetGraphic = handle.GetComponent<Image>();
        Button(panel, Screen.fullScreen ? "FULLSCREEN // ON" : "FULLSCREEN // OFF", new Vector2(0.08f, 0.38f), new Vector2(0.84f, 0.1f), () => { Screen.fullScreen = !Screen.fullScreen; }, false);
        Button(panel, "CLOSE", new Vector2(0.69f, 0.08f), new Vector2(0.23f, 0.1f), () => panel.gameObject.SetActive(false), true);
        panel.gameObject.SetActive(show); return panel.gameObject;
    }
    private static void Reset()
    {
        regionMapMarkers.Clear(); roomMapMarkers.Clear();
        Time.timeScale = 1f;
        messageText = null; promptText = null; headerText = null; shardsText = null; healthText = null; energyText = null; energyFill = null; bossLabel = null; bossFill = null;
        pausePanel = null; mapPanel = null; inventoryPanel = null; skillPanel = null; settingsPanel = null; currentMode = 0; currentMessage = "";
        mapContent = null; inventoryContent = null; skillContent = null;
    }

    private static void PurchaseUpgrade(VesperSkillPath path)
    {
        if (!VesperProgress.BuyUpgrade(path)) FlashMessage("NOT ENOUGH MEMORY / PATH COMPLETE", 1.2f);
        else
        {
            VesperPlayerController2D player = Object.FindAnyObjectByType<VesperPlayerController2D>();
            if (player != null) player.ApplyProgressionChange();
            FlashMessage(path.ToString().ToUpperInvariant() + " WEAVE FORGED", 1.2f);
        }
        RefreshProgress();
    }

    private static void CloseOverlay(GameObject panel)
    {
        if (panel != null) panel.SetActive(false);
        currentMode = 0;
        Time.timeScale = 1f;
    }
}

public sealed class VesperUIUpdater : MonoBehaviour
{
    private void Update() => VesperGameUI.Tick();
}

public sealed class VesperButtonMotion : MonoBehaviour, UnityEngine.EventSystems.IPointerEnterHandler, UnityEngine.EventSystems.IPointerExitHandler
{
    private Vector3 target = Vector3.one;
    private void Update() { transform.localScale = Vector3.Lerp(transform.localScale, target, Time.unscaledDeltaTime * 12f); }
    public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData) { target = Vector3.one * 1.035f; }
    public void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData) { target = Vector3.one; }
}
