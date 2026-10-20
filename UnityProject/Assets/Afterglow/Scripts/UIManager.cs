using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Afterglow
{
    public class UIManager : MonoBehaviour
    {
        public static bool IsPaused { get; private set; }
        private static readonly Color Accent = new Color(.75f, .59f, .36f);
        private static readonly Color AccentBright = new Color(.86f, .75f, .53f);
        private static readonly Color MutedText = new Color(.67f, .65f, .59f);
        private static readonly Color Danger = new Color(.66f, .24f, .2f);
        private Canvas canvas;
        private Font font;
        private GameObject hudRoot;
        private GameObject menuRoot;
        private GameObject settingsRoot;
        private GameObject creditsRoot;
        private GameObject pauseRoot;
        private GameObject gameOverRoot;
        private GameObject stageCompleteRoot;
        private GameObject bossBarRoot;
        private GameObject quickMenuRoot;
        private Text notificationText;
        private Text healthText;
        private Text chainText;
        private Text targetText;
        private Text controlText;
        private Text ultimateText;
        private Text quickMenuTitle;
        private Text quickMenuBody;
        private Text timerText;
        private Text shakeButtonText;
        private Slider bossSlider;
        private AfterglowPlayer player;
        private AfterglowCameraRig cameraRig;
        private int stageIndex;
        private float noticeTimer;
        private float timerRefresh;
        private bool menuScene;
        private bool settingsFromPause;
        private bool interactionNear;
        private bool interactionIsShortcut;
        private string markedLabel;
        private bool perfectPrompt;
        private string quickMode;

        public void BuildMenu()
        {
            AfterglowSession.Load();
            IsPaused = false;
            Time.timeScale = 1f;
            menuScene = true;
            EnsureCanvas();
            var wash = Panel(canvas.transform, "Ash veil", new Vector2(1920, 1080), Vector2.zero, new Color(.012f, .016f, .025f, .18f));
            var washImage = wash.GetComponent<Image>();
            washImage.sprite = AfterglowArt.Pixel("pixel");
            washImage.type = Image.Type.Simple;
            washImage.raycastTarget = false;
            wash.transform.SetAsFirstSibling();
            menuRoot = NewPanel("Main menu", new Vector2(650, 920), new Vector2(72f, 0f), new Color(.012f, .015f, .025f, .91f));
            var menuRect = menuRoot.GetComponent<RectTransform>();
            menuRect.anchorMin = menuRect.anchorMax = new Vector2(0f, .5f);
            menuRect.pivot = new Vector2(0f, .5f);
            AddText(menuRoot.transform, "AFTERGLOW", 62, FontStyle.Bold, new Color(.9f, .84f, .7f), new Vector2(560, 108), new Vector2(0, 314));
            AddText(menuRoot.transform, "THE ROOTBOUND BELL", 18, FontStyle.Bold, Accent, new Vector2(560, 38), new Vector2(0, 265));
            AddText(menuRoot.transform, "HET RIJK VERDWEEN. DE WORTELS BEWAREN ZIJN LAATSTE LIED.", 15, FontStyle.Bold, new Color(.8f, .76f, .67f), new Vector2(560, 48), new Vector2(0, 218));
            AddText(menuRoot.transform, "Volg Elins belslag door een oerwoud dat een vergeten rijk heeft opgeslokt.", 16, FontStyle.Normal, new Color(.82f, .78f, .7f), new Vector2(560, 46), new Vector2(0, 179));
            Button(menuRoot.transform, "NIEUW AVONTUUR", () => { AfterglowSession.StartRun(true); LoadStage(0); }, true, new Vector2(500, 58), 17, new Vector2(0, 105));
            Button(menuRoot.transform, "VERDER GAAN", () => LoadStage(AfterglowSession.Profile.currentStage), false, new Vector2(500, 58), 17, new Vector2(0, 32));
            Button(menuRoot.transform, "HOOFDSTUKKEN", ShowLevelSelect, false, new Vector2(500, 58), 17, new Vector2(0, -41));
            Button(menuRoot.transform, "INSTELLINGEN", ShowSettings, false, new Vector2(500, 58), 17, new Vector2(0, -114));
            Button(menuRoot.transform, "CREDITS", ShowCredits, false, new Vector2(500, 58), 17, new Vector2(0, -187));
            AddText(menuRoot.transform, "A/D BEWEGEN  •  SPACE SPRINGEN  •  LMB VUUR / LADEN  •  RMB RELIEKSTOOT  •  Q MERK  •  SHIFT DASH",
                11, FontStyle.Normal, new Color(.72f, .69f, .61f), new Vector2(600, 30), new Vector2(0, -235));
            AddText(menuRoot.transform, "Made by Jaiden Asruf", 16, FontStyle.Bold, AccentBright, new Vector2(560, 34), new Vector2(0, -275));
            BuildSettingsOverlay();
            BuildCreditsOverlay();
            BuildLevelSelectOverlay();
        }

        public void ConfigureGame(int stage, AfterglowPlayer hero, AfterglowCameraRig rig)
        {
            AfterglowSession.Load();
            IsPaused = false;
            Time.timeScale = 1f;
            stageIndex = stage;
            player = hero;
            cameraRig = rig;
            EnsureCanvas();
            AddHud();
            BuildSettingsOverlay();
            BuildPauseOverlay();
            BuildGameOverOverlay();
            BuildStageCompleteOverlay();
            BuildBossBar();
            BuildQuickMenu();
        }

        protected virtual void Update()
        {
            if (menuScene)
            {
                if (KeyboardEscapePressed() && settingsRoot != null && settingsRoot.activeSelf) HideSettings();
                return;
            }
            Keyboard keyboard = Keyboard.current;
            if (quickMenuRoot != null && quickMenuRoot.activeSelf)
            {
                if (KeyboardEscapePressed()) { HideQuickMenu(); return; }
                if (keyboard != null)
                {
                    if (keyboard.tabKey.wasPressedThisFrame) ShowQuickMenu("MAP");
                    else if (keyboard.mKey.wasPressedThisFrame) ShowQuickMenu("WORLD");
                    else if (keyboard.cKey.wasPressedThisFrame) ShowQuickMenu("CHARACTER");
                }
                return;
            }
            if (KeyboardEscapePressed() && IsPaused && pauseRoot != null && pauseRoot.activeSelf) TogglePause(false);
            else if (KeyboardEscapePressed() && !IsPaused && gameOverRoot != null && !gameOverRoot.activeSelf && stageCompleteRoot != null && !stageCompleteRoot.activeSelf) TogglePause(true);
            if (IsPaused) return;
            if (keyboard != null)
            {
                if (keyboard.tabKey.wasPressedThisFrame) ShowQuickMenu("MAP");
                else if (keyboard.mKey.wasPressedThisFrame) ShowQuickMenu("WORLD");
                else if (keyboard.cKey.wasPressedThisFrame) ShowQuickMenu("CHARACTER");
            }
            timerRefresh -= Time.unscaledDeltaTime;
            if (timerText != null && timerRefresh <= 0f)
            {
                timerText.text = FormatTime(AfterglowSession.RunSeconds);
                timerRefresh = 1f;
            }
            if (noticeTimer > 0f)
            {
                noticeTimer -= Time.unscaledDeltaTime;
                if (noticeTimer <= 0f && notificationText != null) notificationText.gameObject.SetActive(false);
            }
        }

        private static bool KeyboardEscapePressed() => UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame;

        private void EnsureCanvas()
        {
            if (canvas != null) return;
            canvas = GetComponent<Canvas>();
            if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = GetComponent<CanvasScaler>() ?? gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();
            font = Resources.Load<Font>("Fonts/Inter-Variable");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (EventSystem.current == null)
            {
                var events = new GameObject("Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
        }

        private void AddHud()
        {
            hudRoot = new GameObject("In-game HUD", typeof(RectTransform));
            hudRoot.transform.SetParent(canvas.transform, false);
            var hudRect = hudRoot.GetComponent<RectTransform>();
            hudRect.anchorMin = Vector2.zero; hudRect.anchorMax = Vector2.one; hudRect.offsetMin = Vector2.zero; hudRect.offsetMax = Vector2.zero;
            var left = Panel(hudRoot.transform, "Hero status", new Vector2(330, 86), new Vector2(-775, 475), new Color(.025f, .026f, .027f, .9f));
            healthText = AddText(left.transform, "VITALITEIT  04 / 04", 18, FontStyle.Bold, new Color(.82f, .79f, .71f), new Vector2(300, 32), new Vector2(0, 18), TextAnchor.MiddleLeft, new Vector2(18, 0));
            ultimateText = AddText(left.transform, "WORTELHART  00%  •  R", 13, FontStyle.Bold, AccentBright, new Vector2(300, 28), new Vector2(0, -19), TextAnchor.MiddleLeft, new Vector2(18, 0));

            var objective = Panel(hudRoot.transform, "Chapter label", new Vector2(480, 82), new Vector2(0, 476), new Color(.025f, .026f, .027f, .86f));
            AddText(objective.transform, "HOOFDSTUK 0" + (stageIndex + 1), 17, FontStyle.Bold, Accent, new Vector2(440, 28), new Vector2(0, 18));
            AddText(objective.transform, StageTitle(stageIndex), 22, FontStyle.Bold, Color.white, new Vector2(440, 34), new Vector2(0, -15));

            var combo = Panel(hudRoot.transform, "Rebound chain", new Vector2(330, 78), new Vector2(0, 392), new Color(.025f, .026f, .027f, .88f));
            chainText = AddText(combo.transform, "KETEN  0", 20, FontStyle.Bold, AccentBright, new Vector2(300, 50), Vector2.zero);

            var right = Panel(hudRoot.transform, "Run details", new Vector2(310, 82), new Vector2(780, 478), new Color(.025f, .026f, .027f, .9f));
            timerText = AddText(right.transform, "00:00", 17, FontStyle.Bold, new Color(.78f, .76f, .69f), new Vector2(100, 42), new Vector2(-72, 0));
            Button(right.transform, "II  PAUZE", () => TogglePause(true), false, new Vector2(130, 48), 14, new Vector2(78, 0));
            var targetPanel = Panel(hudRoot.transform, "Oathroot controls", new Vector2(1060, 76), new Vector2(0, -465), new Color(.025f, .026f, .027f, .9f));
            controlText = AddText(targetPanel.transform, "LMB VUUR  •  HOUD VAST OM TE LADEN     RMB RELIEKSTOOT     Q PULSMERK     SHIFT DASH", 13, FontStyle.Bold, new Color(.86f,.83f,.75f), new Vector2(1020, 28), new Vector2(0, 17));
            targetText = AddText(targetPanel.transform, "A/D BEWEGEN     SPACE SPRINGEN     E INTERACT     TAB KAART     M WERELDKAART     C PERSONAGE     R ULTIMAAT", 12, FontStyle.Normal, MutedText, new Vector2(1020, 28), new Vector2(0, -16));
            player.HealthChanged += UpdateHealth;
            player.ChainChanged += UpdateChain;
            UpdateHealth(player.Health, player.MaximumHealth);
            UpdateUltimate(player.UltimatePercent);
        }

        private void BuildBossBar()
        {
            bossBarRoot = new GameObject("Boss mastery bar");
            bossBarRoot.transform.SetParent(canvas.transform, false);
            var panel = Panel(bossBarRoot.transform, "Boss mastery plate", new Vector2(700, 84), new Vector2(0, 300), new Color(.025f, .026f, .027f, .96f));
            AddText(panel.transform, "", 17, FontStyle.Bold, Color.white, new Vector2(650, 28), new Vector2(0, 19), TextAnchor.MiddleCenter);
            bossSlider = Slider(panel.transform, new Vector2(630, 18), new Vector2(0, -18), Accent);
            bossBarRoot.SetActive(false);
        }

        private void BuildQuickMenu()
        {
            quickMenuRoot = NewPanel("Field journal", new Vector2(850, 650), Vector2.zero, new Color(.025f, .03f, .04f, .985f));
            quickMenuRoot.SetActive(false);
            quickMenuTitle = AddText(quickMenuRoot.transform, "VELDJOURNAAL", 32, FontStyle.Bold, AccentBright, new Vector2(760, 56), new Vector2(0, 270));
            quickMenuBody = AddText(quickMenuRoot.transform, "", 20, FontStyle.Normal, new Color(.81f,.8f,.74f), new Vector2(720, 350), new Vector2(0, 8), TextAnchor.MiddleLeft);
            Button(quickMenuRoot.transform, "VERDER SPELEN", HideQuickMenu, true, new Vector2(460, 58), 16, new Vector2(0, -260));
        }

        private void ShowQuickMenu(string mode)
        {
            if (quickMenuRoot == null) return;
            if (quickMenuRoot.activeSelf && quickMode == mode) { HideQuickMenu(); return; }
            quickMode = mode;
            int memories = AfterglowSession.RecoveredMemoryCount;
            if (mode == "MAP")
            {
                quickMenuTitle.text = "ROUTEKAART  •  " + StageTitle(stageIndex);
                quickMenuBody.text = "HUIDIGE ROUTE\n\n" + (stageIndex > 0 ? "✓  DE VERGROEIDE SCHRIJN  —  de bel in de woudreus" : "●  DE VERGROEIDE SCHRIJN  —  de bel in de woudreus") +
                    "\n" + (stageIndex > 1 ? "✓  DE VERZONKEN ARK  —  het gevluchte volk" : stageIndex == 1 ? "●  DE VERZONKEN ARK  —  het gevluchte volk" : "○  DE VERZONKEN ARK  —  nog verborgen") +
                    "\n" + (stageIndex > 2 ? "✓  HET WORTELHART  —  onder de oude tempel" : stageIndex == 2 ? "●  HET WORTELHART  —  onder de oude tempel" : "○  HET WORTELHART  —  nog verborgen") +
                    "\n\nHerinneringen gevonden: " + memories + " / 3\nKijk naar beschadigde muurschriften en licht onder de wortels. Geheimen liggen buiten het hoofdpad.";
            }
            else if (mode == "WORLD")
            {
                quickMenuTitle.text = "WERELDKAART  •  HET VERLOREN RIJK";
                quickMenuBody.text = "DE WORTELRAND\nEen eeuwenoude woudreus heeft het belfort omsloten. De Wachters hielden hier ooit de seizoensklok gaande.\n\nDE VERZONKEN ARK\nEen tempelboot ligt vast in het overstroomde ravijn. De evacuatie eindigde hier; de vuurplaatsen zijn nog koud.\n\nHET WORTELHART\nOnder de kathedraal groeit de machine die het bos wakker maakte. Rode bloei markeert waar zij het steen heeft gebroken.\n\nHerstelde getuigenissen: " + memories + " / 3";
            }
            else
            {
                quickMenuTitle.text = "MARA VALE  •  KLOKBEWAARDER";
                quickMenuBody.text = "WAPEN\nOATHROOT — een familiekanon van zwart ijzer, messing en een levende blauwe kristalkern. LMB vuurt; vasthouden laadt een doorborend reliekschot.\n\nTECHNIEKEN\nRMB Reliekstoot: nabije vijanden onderbreken. Q Pulsmerk: bindt één rebound-doel. Shift dash naar een markering of in je bewegingsrichting. Perfect rebound door Shift op de inslag.\n\nWORTELHART\nLading " + (player != null ? player.UltimatePercent : 0) + "%  •  R ontketent de ultieme puls.\n\nHAAR SPOOR\nMara zoekt Elin, haar zus, via drie laatste herinneringen in de ruïnes.";
            }
            quickMenuRoot.SetActive(true);
            IsPaused = true;
            Time.timeScale = 0f;
        }

        private void HideQuickMenu()
        {
            if (quickMenuRoot != null) quickMenuRoot.SetActive(false);
            quickMode = null;
            IsPaused = false;
            Time.timeScale = 1f;
        }

        public void ShowBossMastery(string bossName, int completed, int required, int chainRequired)
        {
            if (bossBarRoot == null) return;
            bossBarRoot.SetActive(true);
            var label = bossBarRoot.GetComponentInChildren<Text>();
            if (label != null) label.text = bossName + "  •  ZEGELS " + completed + " / " + required + "  •  KETEN " + chainRequired + " NODIG";
            UpdateBossMastery(completed, required);
        }
        public void UpdateBossMastery(int completed, int required)
        {
            if (bossSlider != null) bossSlider.value = required <= 0 ? 0 : Mathf.Clamp01(completed / (float)required) * 100f;
            if (bossBarRoot != null)
            {
                var label = bossBarRoot.GetComponentInChildren<Text>();
                if (label != null)
                {
                    string[] parts = label.text.Split('•');
                    string name = parts.Length > 0 ? parts[0].Trim() : "WACHTER";
                    string chainRequirement = parts.Length > 2 ? parts[2].Trim() : "KETEN NODIG";
                    label.text = name + "  •  ZEGELS " + completed + " / " + required + "  •  " + chainRequirement;
                }
            }
        }
        public void HideBossMastery() { if (bossBarRoot != null) bossBarRoot.SetActive(false); }

        public void ShowMarkedTarget(string label)
        {
            markedLabel = label;
            perfectPrompt = false;
            RefreshControlHint();
        }

        public void ClearMarkedTarget()
        {
            markedLabel = null;
            perfectPrompt = false;
            RefreshControlHint();
        }

        public void PromptPerfectRebound()
        {
            perfectPrompt = true;
            RefreshControlHint();
        }

        public void SetInteractionHint(bool nearby, bool shortcut = false)
        {
            interactionNear = nearby;
            interactionIsShortcut = nearby && shortcut;
            RefreshControlHint();
        }

        public void UpdateUltimate(int amount)
        {
            if (ultimateText == null) return;
            ultimateText.text = amount >= 100 ? "WORTELHART VOL  •  R VUUR" : "WORTELHART  " + Mathf.Clamp(amount, 0, 100).ToString("00") + "%  •  R";
            ultimateText.color = amount >= 100 ? new Color(.97f,.79f,.46f) : AccentBright;
        }

        public void UpdateWeaponCharge(float amount)
        {
            if (controlText == null) return;
            if (amount > 0f) controlText.text = "OATHROOT LADEN  " + Mathf.RoundToInt(amount * 100f) + "%  •  LAAT LMB LOS VOOR DE RELIEKSCHOT";
            else controlText.text = "LMB VUUR  •  HOUD VAST OM TE LADEN     RMB RELIEKSTOOT     Q PULSMERK     SHIFT DASH";
        }

        private void RefreshControlHint()
        {
            if (targetText == null) return;
            if (perfectPrompt) targetText.text = "NU!  •  DRUK SHIFT OP DE INSLAANDE BELSLAG VOOR PERFECTE PULSE REBOUND";
            else if (!string.IsNullOrEmpty(markedLabel)) targetText.text = "Q GEMERKT: " + markedLabel + "  •  SHIFT ERNAARTOE  •  SHIFT BIJ IMPACT = REBOUND";
            else if (interactionNear) targetText.text = interactionIsShortcut ? "E  •  GEBRUIK DE VERBORGEN WORTELDOORGANG" : "E  •  ONDERZOEK ELINS HERINNERING";
            else targetText.text = "A/D BEWEGEN     SPACE SPRINGEN     E INTERACT     TAB KAART     M WERELDKAART     C PERSONAGE     R ULTIMAAT";
        }

        public void UpdateChain(int chain, bool perfect)
        {
            if (chainText == null) return;
            chainText.text = chain <= 0 ? "KETEN  0" : "KETEN  " + chain.ToString("00") + (perfect ? "  •  PERFECT" : "");
            chainText.color = perfect ? new Color(.9f, .79f, .52f) : AccentBright;
        }

        private void BuildSettingsOverlay()
        {
            if (settingsRoot != null) return;
            settingsRoot = NewPanel("Settings overlay", new Vector2(720, 680), Vector2.zero, new Color(.035f, .033f, .031f, .985f));
            settingsRoot.SetActive(false);
            AddText(settingsRoot.transform, "INSTELLINGEN", 35, FontStyle.Bold, Color.white, new Vector2(640, 64), new Vector2(0, 270));
            AddText(settingsRoot.transform, "GELUID", 15, FontStyle.Bold, Accent, new Vector2(590, 30), new Vector2(0, 216));
            AddSettingSlider(settingsRoot.transform, "Totaal volume", 162, AfterglowSession.Profile.masterVolume, value => { AfterglowSession.SetAudio(value, AfterglowSession.Profile.musicVolume, AfterglowSession.Profile.effectsVolume); AfterglowAudio.Instance?.ApplyVolumes(); });
            AddSettingSlider(settingsRoot.transform, "Muziek", 88, AfterglowSession.Profile.musicVolume, value => { AfterglowSession.SetAudio(AfterglowSession.Profile.masterVolume, value, AfterglowSession.Profile.effectsVolume); AfterglowAudio.Instance?.ApplyVolumes(); });
            AddSettingSlider(settingsRoot.transform, "Geluidseffecten", 14, AfterglowSession.Profile.effectsVolume, value => { AfterglowSession.SetAudio(AfterglowSession.Profile.masterVolume, AfterglowSession.Profile.musicVolume, value); AfterglowAudio.Instance?.ApplyVolumes(); });
            AddText(settingsRoot.transform, "BESTURING", 15, FontStyle.Bold, Accent, new Vector2(590, 30), new Vector2(0, -72));
            AddText(settingsRoot.transform, "A/D bewegen • SPACE springen • LMB vuur (vasthouden = laden) • RMB reliekstoot • SHIFT dash • Q pulsmerk", 14, FontStyle.Normal, MutedText, new Vector2(630, 46), new Vector2(0, -112));
            AddText(settingsRoot.transform, "E interact • R ultiem (na opladen) • ESC pauze • TAB kaart • M wereldkaart • C personage", 13, FontStyle.Normal, MutedText, new Vector2(630, 42), new Vector2(0, -152));
            shakeButtonText = Button(settingsRoot.transform, "SCHERMBEWEGING: " + (AfterglowSession.Profile.screenShake ? "AAN" : "UIT"), () =>
            {
                AfterglowSession.SetScreenShake(!AfterglowSession.Profile.screenShake);
                if (shakeButtonText != null) shakeButtonText.text = "SCHERMBEWEGING: " + (AfterglowSession.Profile.screenShake ? "AAN" : "UIT");
            }, false, new Vector2(480, 54), 15, new Vector2(0, -215));
            Button(settingsRoot.transform, "TERUG", HideSettings, true, new Vector2(460, 58), 16, new Vector2(0, -285));
        }

        private void AddSettingSlider(Transform parent, string label, float y, float initial, Action<float> changed)
        {
            AddText(parent, label, 16, FontStyle.Normal, new Color(.83f, .8f, .73f), new Vector2(190, 36), new Vector2(-215, y), TextAnchor.MiddleLeft);
            var slider = Slider(parent, new Vector2(300, 16), new Vector2(120, y), Accent);
            slider.maxValue = 1f;
            slider.value = initial;
            slider.onValueChanged.AddListener(v => changed(v));
        }

        private void BuildCreditsOverlay()
        {
            if (creditsRoot != null) return;
            creditsRoot = NewPanel("Credits", new Vector2(720, 520), Vector2.zero, new Color(.025f, .029f, .034f, .99f));
            creditsRoot.SetActive(false);
            AddText(creditsRoot.transform, "CREDITS", 20, FontStyle.Bold, new Color(.68f, .48f, .3f), new Vector2(650, 42), new Vector2(0, 180));
            AddText(creditsRoot.transform, "AFTERGLOW: THE ROOTBOUND BELL", 30, FontStyle.Bold, new Color(.87f, .82f, .69f), new Vector2(650, 60), new Vector2(0, 112));
            AddText(creditsRoot.transform, "EEN VERHAAL OVER EEN VERGETEN RIJK EN HET LIED DAT HET BEWAART", 13, FontStyle.Normal, new Color(.58f, .59f, .57f), new Vector2(630, 34), new Vector2(0, 70));
            AddText(creditsRoot.transform, "Made by Jaiden Asruf", 26, FontStyle.Bold, AccentBright, new Vector2(650, 54), new Vector2(0, -15));
            Button(creditsRoot.transform, "TERUG", () => { creditsRoot.SetActive(false); if (menuRoot != null) menuRoot.SetActive(true); }, true, new Vector2(460, 58), 16, new Vector2(0, -175));
        }

        private void ShowCredits()
        {
            if (menuRoot != null) menuRoot.SetActive(false);
            if (creditsRoot != null) creditsRoot.SetActive(true);
        }

        private void BuildLevelSelectOverlay()
        {
            var levelRoot = NewPanel("Chapter select", new Vector2(720, 680), Vector2.zero, new Color(.035f, .033f, .031f, .985f));
            levelRoot.SetActive(false);
            AddText(levelRoot.transform, "KIES JE HOOFDSTUK", 34, FontStyle.Bold, Color.white, new Vector2(650, 70), new Vector2(0, 270));
            string[] titles = { "01  —  DE VERGROEIDE SCHRIJN", "02  —  DE VERZONKEN ARK", "03  —  HET WORTELHART" };
            string[] scenes = { "Afterglow_01_Rooftops", "Afterglow_02_Market", "Afterglow_03_Core" };
            for (int i = 0; i < 3; i++)
            {
                int stage = i;
                Button(levelRoot.transform, titles[i], () =>
                {
                    if (stage + 1 > AfterglowSession.Profile.bestUnlockedStage) { Notify("DIT HOOFDSTUK IS NOG GESLOTEN", new Color(1f, .65f, .42f)); return; }
                    AfterglowSession.SetCurrentStage(stage);
                    LoadStage(stage);
                }, false, new Vector2(510, 60), 17, new Vector2(0, 115 - i * 80));
            }
            Button(levelRoot.transform, "TERUG", () => { levelRoot.SetActive(false); menuRoot.SetActive(true); }, true, new Vector2(460, 58), 16, new Vector2(0, -210));
            levelRoot.name = "Chapter select";
        }

        private void ShowLevelSelect()
        {
            var level = transform.Find("Chapter select");
            if (level != null) { menuRoot.SetActive(false); level.gameObject.SetActive(true); }
        }

        private void BuildPauseOverlay()
        {
            pauseRoot = NewPanel("Pause overlay", new Vector2(620, 690), Vector2.zero, new Color(.035f, .033f, .031f, .985f));
            pauseRoot.SetActive(false);
            AddText(pauseRoot.transform, "PAUZE", 43, FontStyle.Bold, Color.white, new Vector2(560, 70), new Vector2(0, 270));
            AddText(pauseRoot.transform, "Je voortgang wordt automatisch bewaard.", 15, FontStyle.Normal, MutedText, new Vector2(560, 34), new Vector2(0, 222));
            Button(pauseRoot.transform, "VERDER SPELEN", () => TogglePause(false), true, new Vector2(430, 58), 16, new Vector2(0, 130));
            Button(pauseRoot.transform, "INSTELLINGEN", () => { settingsFromPause = true; settingsRoot.SetActive(true); pauseRoot.SetActive(false); }, false, new Vector2(430, 58), 16, new Vector2(0, 48));
            Button(pauseRoot.transform, "VANAF CHECKPOINT", RestartFromCheckpoint, false, new Vector2(430, 58), 16, new Vector2(0, -34));
            Button(pauseRoot.transform, "NAAR HOOFDMENU", () => { TogglePause(false); LoadMenu(); }, false, new Vector2(430, 58), 16, new Vector2(0, -116));
            AddText(pauseRoot.transform, "Huidige tijd  " + FormatTime(AfterglowSession.RunSeconds), 15, FontStyle.Normal, MutedText, new Vector2(500, 30), new Vector2(0, -225));
        }

        private void BuildGameOverOverlay()
        {
            gameOverRoot = NewPanel("Game over overlay", new Vector2(620, 570), Vector2.zero, new Color(.08f, .035f, .03f, .985f));
            gameOverRoot.SetActive(false);
            AddText(gameOverRoot.transform, "KETEN GEBROKEN", 46, FontStyle.Bold, new Color(.78f, .43f, .38f), new Vector2(560, 80), new Vector2(0, 205));
            AddText(gameOverRoot.transform, "Probeer het reboundpad opnieuw vanaf je herinnering.", 18, FontStyle.Normal, MutedText, new Vector2(560, 42), new Vector2(0, 143));
            Button(gameOverRoot.transform, "OPNIEUW VANAF CHECKPOINT", RestartFromCheckpoint, true, new Vector2(470, 64), 17, new Vector2(0, 46));
            Button(gameOverRoot.transform, "HOOFDMENU", LoadMenu, false, new Vector2(470, 58), 16, new Vector2(0, -42));
        }

        private void BuildStageCompleteOverlay()
        {
            stageCompleteRoot = NewPanel("Chapter complete overlay", new Vector2(690, 610), Vector2.zero, new Color(.035f, .033f, .031f, .985f));
            stageCompleteRoot.SetActive(false);
        }

        private void ShowStageCompleteInternal(int stage)
        {
            if (stageCompleteRoot == null) return;
            stageCompleteRoot.SetActive(true);
            foreach (Transform child in stageCompleteRoot.transform) Destroy(child.gameObject);
            bool final = stage >= 2;
            bool ending = final;
            int recoveredMemories = AfterglowSession.RecoveredMemoryCount;
            string endingTitle = final && recoveredMemories == 3 ? "DE MELODIE KEERT TERUG" : final ? "HET LAATSTE LICHT" : "ROUTE HERSTELD";
            AddText(stageCompleteRoot.transform, endingTitle, 36, FontStyle.Bold, new Color(.76f, .62f, .39f), new Vector2(630, 88), new Vector2(0, 215));
            string[] chapterEndings = {
                "De oude schrijn antwoordt. Elin leeft; haar vierde slag wijst naar de tempelark in het overstroomde ravijn.",
                "Het arkregister onthult waarom Elin achterbleef: zij sloot het Wortelhart zodat de vluchtelingen konden ontsnappen. Haar spoor loopt onder de kathedraalwortels.",
                recoveredMemories == 3
                    ? "De drie herinneringen brengen Elins melodie terug. Samen luiden Mara en Elin het oerwoud wakker; de godmachine komt tot rust en de wortels wijken."
                    : "Mara vindt Elin in de verzegelde tempelkamer. Het Wortelhart komt tot rust, maar hun familielied blijft onvolledig. Boven wacht het herboren woud."
            };
            AddText(stageCompleteRoot.transform, chapterEndings[Mathf.Clamp(stage, 0, 2)], 18, FontStyle.Normal, new Color(.78f, .75f, .67f), new Vector2(590, 92), new Vector2(0, 142));
            AddText(stageCompleteRoot.transform, "TIJD  " + FormatTime(AfterglowSession.RunSeconds) + "    BESTE KETEN  " + (player != null ? player.BestChain : 0) + "    HERINNERINGEN  " + AfterglowSession.RecoveredMemoryCount + " / 3",
                16, FontStyle.Bold, AccentBright, new Vector2(600, 42), new Vector2(0, 66));
            if (ending) AddText(stageCompleteRoot.transform, "Made by Jaiden Asruf", 18, FontStyle.Bold, AccentBright, new Vector2(590, 36), new Vector2(0, 8));
            Button(stageCompleteRoot.transform, ending ? "TERUG NAAR HOOFDMENU" : "VOLGEND HOOFDSTUK", () =>
            {
                Time.timeScale = 1f; IsPaused = false;
                if (ending) LoadMenu(); else LoadStage(stage + 1);
            }, true, new Vector2(500, 64), 17, new Vector2(0, -18));
            Button(stageCompleteRoot.transform, "HOOFDMENU", () => { Time.timeScale = 1f; IsPaused = false; LoadMenu(); }, false, new Vector2(500, 58), 16, new Vector2(0, -104));
            Time.timeScale = 0f;
            IsPaused = true;
        }

        public void ShowGameOver()
        {
            if (gameOverRoot != null) gameOverRoot.SetActive(true);
            IsPaused = true;
            Time.timeScale = 0f;
        }

        public void ShowStageComplete(int stage) => ShowStageCompleteInternal(stage);
        public void ShowChapterIntro(int stage)
        {
            string[] chapter = { "DE VERGROEIDE SCHRIJN", "DE VERZONKEN ARK", "HET WORTELHART" };
            string[] story = {
                "Mara volgt Elins vierde belslag naar de schrijn in de woudreus.",
                "Het register in de tempelark vertelt waarom Elin terugkeerde naar het verzonken rijk.",
                "Onder de kathedraalwortels wacht Elin bij de godmachine."
            };
            int index = Mathf.Clamp(stage, 0, 2);
            Notify(chapter[index] + "\n" + story[index], AccentBright, 4f);
        }
        public void BossIntroduction(string name, string subtitle)
        {
            Notify(name + "   •   " + subtitle, AccentBright, 3.3f);
            AfterglowAudio.Instance?.Play("boss");
        }
        public void BeginBossCinematic() => cameraRig?.BeginBossCinematic();
        public void PlayBossAttackImpact(Vector3 position, Color color)
        {
            PlayImpactEffect(position, color);
            cameraRig?.Shake(.2f, .18f);
        }
        public void BossPhaseChange(string title) => Notify(title, Danger, 2f);
        public void BossDefeated(string name) { Notify(name + " VERSLAGEN", AccentBright, 3f); AfterglowAudio.Instance?.Play("win"); }

        public void ShowMemory(int index, string title, int recovered)
        {
            string[] fragments =
            {
                "‘Ik blijf tot de laatste bel. Breng iedereen naar de tempelark.’ — Elin",
                "Elin gaf haar plaats in de ark aan de laatste bewoner en keerde terug naar het Wortelhart.",
                "‘Drie tonen. Zoek me bij de wortelklok waar we leerden tellen.’ — Elin"
            };
            string passage = fragments[Mathf.Clamp(index, 0, fragments.Length - 1)];
            Notify("HERINNERING HERSTELD  " + recovered + " / 3  •  " + title + "\n" + passage, AccentBright, 5f);
            if (notificationText != null) notificationText.fontSize = 17;
        }

        public void PlayMemoryEffect(Vector3 position)
        {
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI * 2f / 8f;
                Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * .34f;
                WorldEffect(position + offset, new Color(.91f, .72f, .43f, .88f), .58f, .75f);
            }
            cameraRig?.Shake(.12f, .16f);
            AfterglowAudio.Instance?.Play("win", 1.22f);
        }

        public void Notify(string message, Color color, float duration = 2.2f)
        {
            if (notificationText == null)
            {
                notificationText = AddText(canvas.transform, "", 20, FontStyle.Bold, Color.white, new Vector2(850, 70), new Vector2(0, -385));
                notificationText.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, .7f);
            }
            notificationText.text = message;
            notificationText.fontSize = 20;
            notificationText.color = color;
            notificationText.gameObject.SetActive(true);
            noticeTimer = duration;
        }

        public void TogglePause(bool show)
        {
            if (pauseRoot == null) return;
            IsPaused = show;
            pauseRoot.SetActive(show);
            Time.timeScale = show ? 0f : 1f;
        }

        private void ShowSettings()
        {
            if (menuRoot != null) menuRoot.SetActive(false);
            settingsFromPause = false;
            settingsRoot.SetActive(true);
        }

        private void HideSettings()
        {
            if (settingsRoot != null) settingsRoot.SetActive(false);
            if (!menuScene && settingsFromPause) { pauseRoot.SetActive(true); IsPaused = true; Time.timeScale = 0f; }
            else if (menuRoot != null) menuRoot.SetActive(true);
        }

        private void RestartFromCheckpoint()
        {
            Time.timeScale = 1f;
            IsPaused = false;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void LoadMenu()
        {
            Time.timeScale = 1f;
            IsPaused = false;
            SceneManager.LoadScene("AfterglowMenu");
        }

        private void LoadStage(int stage)
        {
            Time.timeScale = 1f;
            IsPaused = false;
            string[] scenes = { "Afterglow_01_Rooftops", "Afterglow_02_Market", "Afterglow_03_Core" };
            AfterglowSession.SetCurrentStage(stage);
            SceneManager.LoadScene(scenes[Mathf.Clamp(stage, 0, scenes.Length - 1)]);
        }

        private void UpdateHealth(int current, int max)
        {
            if (healthText == null) return;
            healthText.text = "VITALITEIT   " + current.ToString("00") + " / " + max.ToString("00");
            healthText.color = current <= 1 ? new Color(.78f, .35f, .29f) : new Color(.82f, .79f, .71f);
        }

        public void PlayJumpSound() => AfterglowAudio.Instance?.Play("jump", 1f + UnityEngine.Random.Range(-.06f, .06f));
        public void PlayDashEffect(Vector3 pos, int facing)
        {
            AfterglowAudio.Instance?.Play("dash");
            for (int i = 0; i < 4; i++) WorldEffect(pos + new Vector3(-facing * i * .35f, .25f, 0f), new Color(.74f, .58f, .36f, .72f), .22f, .6f);
            cameraRig?.Shake(.08f, .1f);
        }
        public void PlayReboundEffect(Vector2 pos, Vector2 direction, int chain, bool perfect)
        {
            Color color = perfect ? new Color(.96f, .79f, .47f, .98f) : new Color(.7f, .62f, .47f, .94f);
            WorldEffect(pos, color, .28f, perfect ? 1.9f : 1.45f);
            for (int i = 1; i <= Mathf.Min(chain, 5); i++)
                WorldEffect(pos - direction * (.24f * i), new Color(color.r, color.g, color.b, .7f - i * .08f), .2f, .58f);
            cameraRig?.Shake(perfect ? .16f : .09f, perfect ? .17f : .1f);
            AfterglowAudio.Instance?.Play(perfect ? "perfect" : "rebound", perfect ? 1.05f + chain * .04f : .9f + chain * .03f);
        }
        public void PlayDamageEffect(Vector3 pos)
        {
            WorldEffect(pos, new Color(.68f, .2f, .18f, .94f), .35f, 2f);
            cameraRig?.Shake(.27f, .22f);
            AfterglowAudio.Instance?.Play("hurt");
        }
        public void PlayCheckpointEffect(Vector3 pos) { WorldEffect(pos, new Color(.8f, .68f, .43f, .95f), .42f, 1.25f); AfterglowAudio.Instance?.Play("win", 1f + UnityEngine.Random.Range(-.05f, .1f)); }
        public void PlayImpactEffect(Vector3 pos, Color color) { WorldEffect(pos, color, .24f, .8f); cameraRig?.Shake(.045f, .075f); AfterglowAudio.Instance?.Play("impact", UnityEngine.Random.Range(.9f, 1.15f)); }
        public void PlayEnemyBurst(Vector3 pos)
        {
            for (int i = 0; i < 6; i++)
            {
                float angle = i * Mathf.PI * 2f / 6f;
                WorldEffect(pos + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * .25f, new Color(.78f, .62f, .39f, .94f), .42f, .62f);
            }
            cameraRig?.Shake(.13f, .18f);
        }
        public void PlayWeaponMuzzle(Vector2 position, Vector2 direction, bool charged)
        {
            Color glow = charged ? new Color(.55f,.91f,1f,.96f) : new Color(.59f,.82f,.87f,.92f);
            WorldEffect(position, glow, charged ? .22f : .12f, charged ? 1.05f : .55f);
            if (charged) cameraRig?.Shake(.1f, .1f);
        }
        public void PlayPulseWave(Vector2 position, float radius, Color color)
        {
            WorldEffect(position, new Color(color.r,color.g,color.b,.62f), .38f, radius * 2f);
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI * 2f / 8f;
                Vector3 point = position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius * .58f;
                WorldEffect(point, new Color(color.r,color.g,color.b,.83f), .2f, .48f);
            }
            cameraRig?.Shake(radius > 7f ? .35f : .19f, radius > 7f ? .25f : .14f);
        }
        public void PlayLandingEffect(Vector3 pos) { WorldEffect(pos, new Color(.69f, .61f, .48f, .73f), .18f, 1.05f); cameraRig?.Shake(.055f, .075f); }
        public void PlayEnemyTelegraph(Vector3 pos) { WorldEffect(pos, new Color(.72f, .25f, .2f, .76f), .48f, 1.1f); }
        public void WarnFloor(float left, float right)
        {
            var go = new GameObject("Telegraphed attack line");
            go.transform.position = new Vector3((left + right) * .5f, .18f, 0f);
            go.transform.localScale = new Vector3(Mathf.Abs(right - left), .075f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AfterglowArt.Pixel("pixel"); sr.color = new Color(.62f, .19f, .17f, .84f); sr.sortingOrder = 14;
            if (AfterglowArt.SpriteMaterial != null) sr.sharedMaterial = AfterglowArt.SpriteMaterial;
            Destroy(go, 1.05f);
        }

        private void WorldEffect(Vector3 pos, Color color, float duration, float size)
        {
            var go = new GameObject("Afterglow feedback");
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.transform.localScale = Vector3.one * .25f;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AfterglowArt.Disc("fx-disc", Color.white, Color.white);
            sr.color = color;
            sr.sortingOrder = 16;
            if (AfterglowArt.SpriteMaterial != null) sr.sharedMaterial = AfterglowArt.SpriteMaterial;
            go.AddComponent<AfterglowEffectFade>().Configure(duration, color, size);
        }

        private GameObject NewPanel(string name, Vector2 size, Vector2 position, Color color)
        {
            var go = Panel(canvas.transform, name, size, position, color);
            return go;
        }

        private GameObject Panel(Transform parent, string name, Vector2 size, Vector2 position, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
            rt.pivot = new Vector2(.5f, .5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.sprite = AfterglowArt.Panel("ui-panel", new Color(.035f, .033f, .031f), new Color(.2f, .17f, .13f), new Color(.54f, .4f, .24f));
            image.type = Image.Type.Sliced;
            image.color = color;
            return go;
        }

        private Text AddText(Transform parent, string content, int size, FontStyle style, Color color, Vector2 dimensions, Vector2 position,
            TextAnchor alignment = TextAnchor.MiddleCenter, Vector2 offset = default)
        {
            var go = new GameObject("Text " + content, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
            rt.pivot = new Vector2(.5f, .5f);
            rt.anchoredPosition = position + offset;
            rt.sizeDelta = dimensions;
            var text = go.GetComponent<Text>();
            text.text = content;
            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.supportRichText = false;
            return text;
        }

        private Text Button(Transform parent, string label, Action action, bool primary, Vector2 size = default, int fontSize = 16, Vector2 position = default)
        {
            if (size == default) size = new Vector2(460, 62);
            var go = new GameObject("Button " + label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(AfterglowMenuHover));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
            rt.pivot = new Vector2(.5f, .5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.sprite = AfterglowArt.Pixel("pixel");
            image.type = Image.Type.Simple;
            image.color = primary ? new Color(.28f, .2f, .12f, .99f) : new Color(.13f, .12f, .105f, .99f);
            var outline = go.AddComponent<Outline>();
            outline.effectColor = primary ? new Color(.78f, .58f, .34f, .94f) : new Color(.38f, .31f, .22f, .88f);
            outline.effectDistance = new Vector2(1f, -1f);
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            var cb = button.colors;
            cb.normalColor = primary ? Color.white : new Color(.83f, .8f, .73f);
            cb.highlightedColor = new Color(.91f, .77f, .52f);
            cb.pressedColor = new Color(.61f, .45f, .27f);
            cb.selectedColor = cb.highlightedColor;
            cb.fadeDuration = .1f;
            button.colors = cb;
            button.onClick.AddListener(() => { AfterglowAudio.Instance?.Play("menu"); action?.Invoke(); });
            var text = AddText(go.transform, label, fontSize, FontStyle.Bold, primary ? Color.white : new Color(.82f, .79f, .72f), size - new Vector2(24, 6), Vector2.zero);
            return text;
        }

        private Slider Slider(Transform parent, Vector2 size, Vector2 position, Color fillColor)
        {
            var root = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            root.transform.SetParent(parent, false);
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
            rt.pivot = new Vector2(.5f, .5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            var background = MakeImage(root.transform, "Track", size, new Color(.09f, .08f, .07f, 1f));
            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(root.transform, false);
            var fa = fillArea.GetComponent<RectTransform>(); fa.anchorMin = Vector2.zero; fa.anchorMax = Vector2.one; fa.offsetMin = new Vector2(2, 2); fa.offsetMax = new Vector2(-2, -2);
            var fill = MakeImage(fillArea.transform, "Fill", Vector2.zero, fillColor);
            var fr = fill.rectTransform; fr.anchorMin = Vector2.zero; fr.anchorMax = Vector2.one; fr.offsetMin = Vector2.zero; fr.offsetMax = Vector2.zero;
            var slider = root.GetComponent<Slider>();
            slider.fillRect = fr; slider.targetGraphic = background; slider.minValue = 0f; slider.maxValue = 100f; slider.wholeNumbers = false;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            var handle = MakeImage(root.transform, "Handle", new Vector2(18, 28), AccentBright);
            slider.handleRect = handle.rectTransform;
            var handleRect = handle.rectTransform; handleRect.anchorMin = new Vector2(0, .5f); handleRect.anchorMax = new Vector2(0, .5f); handleRect.sizeDelta = new Vector2(18, 28);
            slider.value = 100f;
            return slider;
        }

        private Image MakeImage(Transform parent, string name, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = AfterglowArt.Panel("ui-panel", Color.white, Color.white, Color.white);
            image.type = Image.Type.Sliced;
            image.color = color;
            if (size != Vector2.zero) go.GetComponent<RectTransform>().sizeDelta = size;
            return image;
        }

        private static string StageTitle(int stage) => stage == 0 ? "DE VERGROEIDE SCHRIJN" : stage == 1 ? "DE VERZONKEN ARK" : "HET WORTELHART";
        private static string FormatTime(float seconds)
        {
            int minutes = Mathf.FloorToInt(seconds / 60f);
            int remainder = Mathf.FloorToInt(seconds % 60f);
            return minutes.ToString("00") + ":" + remainder.ToString("00");
        }
    }

}
