using UnityEngine;

namespace Afterglow
{
    public class LevelManager : MonoBehaviour
    {
        [SerializeField] private bool menuScene;
        [SerializeField, Range(0, 2)] private int stageIndex;
        private float worldEnd;
        private AfterglowPlayer player;
        private AfterglowUI ui;
        private AfterglowCameraRig cameraRig;
        private AfterglowExit stageExit;

        public void ConfigureScene(bool isMenu, int stage)
        {
            menuScene = isMenu;
            stageIndex = Mathf.Clamp(stage, 0, 2);
        }

        protected virtual void Awake()
        {
            AfterglowSession.Load();
            Time.timeScale = 1f;
            Physics2D.gravity = new Vector2(0f, -9.81f);
            Physics2D.defaultContactOffset = .015f;
            Physics2D.queriesHitTriggers = false;
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 1;
        }

        protected virtual void Start()
        {
            if (menuScene) BuildMenu();
            else BuildLevel();
        }

        private void BuildMenu()
        {
            SetupCamera(new Vector3(0f, 1.2f, -10f));
            var back = SpriteObject("Illustrated title scene", new Vector3(0f, 1.1f, 0f),
                AfterglowArt.AuthoredSprite("Menu_Cover", 100f), Color.white, -100);
            back.transform.localScale = new Vector3(1.02f, 1.02f, 1f);
            var rootBridge = SpriteObject("Ancient living root bridge", new Vector3(4.25f, -.46f, 0f),
                AfterglowArt.Polygon("menu-root-bridge", new Color(.075f,.085f,.08f), new Color(.39f,.32f,.22f),
                    new Vector2(0f,.65f),new Vector2(.12f,.52f),new Vector2(.27f,.61f),new Vector2(.42f,.47f),new Vector2(.59f,.58f),new Vector2(.78f,.49f),new Vector2(1f,.62f),new Vector2(1f,0f),new Vector2(0f,0f)), Color.white, 1);
            rootBridge.transform.localScale = new Vector3(7.7f, 1.18f, 1f);
            var rootMoss = SpriteObject("Moss along the root", new Vector3(4.25f, -.12f, 0f), AfterglowArt.Pixel("pixel"), new Color(.36f,.42f,.25f,.92f), 2);
            rootMoss.transform.localScale = new Vector3(7.1f, .055f, 1f);
            Transform hero = AfterglowCharacterArt.Hero(transform);
            hero.position = new Vector3(4.25f, 1.22f, 0f);
            hero.localScale = new Vector3(1.35f, 1.35f, 1f);
            var audio = new GameObject("Afterglow Audio").AddComponent<AfterglowAudio>();
            audio.PlayStage(0);
            ui = new GameObject("Afterglow User Interface").AddComponent<AfterglowUI>();
            ui.BuildMenu();
        }

        private void BuildLevel()
        {
            worldEnd = stageIndex == 0 ? 105f : stageIndex == 1 ? 110f : 116f;
            float playerX = 3f;
            if (AfterglowSession.Profile.currentStage == stageIndex && AfterglowSession.Profile.checkpointStage == stageIndex && AfterglowSession.Profile.checkpointX > 1f && AfterglowSession.Profile.checkpointX < worldEnd - 15f)
                playerX = AfterglowSession.Profile.checkpointX;

            SetupCamera(new Vector3(9f, 2.7f, -10f));
            BuildBackdrop(stageIndex, worldEnd);

            var playerObject = new GameObject("Mara Vale • Keeper of the Last Bell");
            playerObject.transform.position = new Vector3(playerX, .06f, 0f);
            var body = playerObject.AddComponent<Rigidbody2D>();
            body.mass = 1.2f;
            body.linearDamping = 0f;
            var capsule = playerObject.AddComponent<CapsuleCollider2D>();
            capsule.direction = CapsuleDirection2D.Vertical;
            player = playerObject.AddComponent<AfterglowPlayer>();
            Transform visuals = AfterglowCharacterArt.Hero(playerObject.transform);
            visuals.localPosition = new Vector3(0f, 1f, 0f);
            cameraRig = Camera.main.gameObject.AddComponent<AfterglowCameraRig>();
            cameraRig.Follow(playerObject.transform, worldEnd);

            ui = new GameObject("Afterglow User Interface").AddComponent<AfterglowUI>();
            ui.ConfigureGame(stageIndex, player, cameraRig);
            player.Configure(ui, visuals, stageIndex == 0);
            var audio = new GameObject("Afterglow Audio").AddComponent<AfterglowAudio>();
            audio.PlayStage(stageIndex);

            BuildLevelGeometry(stageIndex);
            BuildStageActors(stageIndex);
            CreateExitAndBoss(stageIndex);
            ui.ShowChapterIntro(stageIndex);
        }

        private void SetupCamera(Vector3 position)
        {
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = position;
            var cam = cameraObject.GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.15f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(.025f, .035f, .085f);
        }

        private void BuildBackdrop(int stage, float length)
        {
            string[] sceneArt = { "World_Rooftops", "World_Market", "World_Heart" };
            Sprite mural = AfterglowArt.AuthoredSprite(sceneArt[Mathf.Clamp(stage, 0, 2)], 50f);
            SpriteObject("Layered jungle ruin panorama", new Vector3(length * .5f, 5f, 0f), mural, Color.white, -100);
            CreateAtmosphere(stage, length);
        }

        private void CreateAtmosphere(int stage, float length)
        {
            var root = new GameObject("Atmospheric particles");
            var system = root.AddComponent<ParticleSystem>();
            var main = system.main;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 220;
            main.startLifetime = stage == 1 ? new ParticleSystem.MinMaxCurve(4f, 7f) : new ParticleSystem.MinMaxCurve(3f, 8f);
            main.startSpeed = stage == 1 ? new ParticleSystem.MinMaxCurve(.14f, .62f) : new ParticleSystem.MinMaxCurve(.08f, .45f);
            main.startSize = stage == 1 ? new ParticleSystem.MinMaxCurve(.04f, .11f) : new ParticleSystem.MinMaxCurve(.035f, .1f);
            main.startColor = stage == 1 ? new Color(.48f, .73f, .72f, .38f) : stage == 2 ? new Color(.63f, .3f, .35f, .34f) : new Color(.68f, .59f, .36f, .4f);
            main.gravityModifier = -.025f;
            main.playOnAwake = true;
            var emission = system.emission;
            emission.rateOverTime = stage == 1 ? 22f : stage == 2 ? 20f : 16f;
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(length + 15f, 11f, 1f);
            system.transform.position = new Vector3(length * .5f, 5f, 0f);
            var particleRenderer = root.GetComponent<ParticleSystemRenderer>();
            if (AfterglowArt.SpriteMaterial != null) particleRenderer.sharedMaterial = AfterglowArt.SpriteMaterial;
            particleRenderer.sortingOrder = -60;
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        }



        private void BuildLevelGeometry(int stage)
        {
            Color accent = stage == 0 ? new Color(.57f, .47f, .66f) : stage == 1 ? new Color(.68f, .52f, .31f) : new Color(.68f, .25f, .3f);
            FloorWithGaps(worldEnd * .5f, worldEnd + 8f, stage, accent);
            WorldWall(-4f, stage, accent);
            WorldWall(worldEnd + 4f, stage, accent);
            CreateStoryLandmark(stage);
            if (stage == 0)
            {
                Platform(13f, 3.1f, 5f, .65f, stage, accent); Platform(20.5f, 4.45f, 4.5f, .6f, stage, accent);
                Platform(27.4f, 5.75f, 4.8f, .58f, stage, accent); Platform(36.5f, 2.8f, 4.5f, .6f, stage, accent);
                Platform(47f, 4.1f, 5f, .62f, stage, accent); Platform(56f, 5.55f, 4.2f, .6f, stage, accent);
                Platform(67f, 3.1f, 5.4f, .62f, stage, accent); Platform(76f, 4.65f, 4.6f, .6f, stage, accent);
                Spikes(18.1f, .35f, 1.8f, .9f, stage); Spikes(55f, .35f, 1.5f, .9f, stage); Spikes(72.4f, .35f, 1.7f, .9f, stage);
                Checkpoint(35.5f, 1);
                ReboundCeiling(51f, 9.35f, 7f, stage, accent);
                CreateBellSeal(32f, 5.1f, 39f, stage);
            }
            else if (stage == 1)
            {
                Platform(11f, 3f, 4.6f, .6f, stage, accent); Platform(20f, 4.5f, 4.3f, .6f, stage, accent);
                Platform(37f, 4.1f, 5f, .6f, stage, accent);
                Platform(53f, 3f, 4.8f, .6f, stage, accent); Platform(60f, 4.7f, 4.5f, .6f, stage, accent);
                Platform(72f, 3.2f, 5.2f, .6f, stage, accent); Platform(81f, 5f, 4.6f, .62f, stage, accent);
                MovingPlatform(29f, 2.4f, new Vector2(0f, 2.1f), 2.4f, stage, accent);
                CollapsingPlatform(39.5f, 2.9f, stage, accent); CollapsingPlatform(44f, 3.8f, stage, accent);
                Spikes(17.2f, .34f, 1.9f, .9f, stage); Laser(49.4f, 1.75f, 3.5f, stage); Laser(76.1f, 2f, 4f, stage);
                Checkpoint(55f, 2);
                ReboundCeiling(71f, 9.25f, 9f, stage, accent);
                CreateBellSeal(38f, 4.65f, 45f, stage);
            }
            else
            {
                Platform(12f, 3.3f, 4.5f, .6f, stage, accent); Platform(22f, 5f, 4.5f, .6f, stage, accent);
                Platform(38f, 4.2f, 5f, .6f, stage, accent);
                Platform(51f, 3.1f, 4.5f, .6f, stage, accent); Platform(59f, 5f, 4.8f, .62f, stage, accent);
                Platform(71f, 3.3f, 5.1f, .6f, stage, accent); Platform(80f, 5.2f, 4.3f, .62f, stage, accent);
                Platform(91f, 3.4f, 4.8f, .6f, stage, accent); Platform(99f, 5.2f, 4.2f, .62f, stage, accent);
                MovingPlatform(32f, 2.1f, new Vector2(0f, 2.3f), 1.8f, stage, accent);
                MovingPlatform(64f, 2.2f, new Vector2(0f, 2.8f), 2.2f, stage, accent);
                CollapsingPlatform(43f, 3f, stage, accent); CollapsingPlatform(48f, 4.2f, stage, accent);
                Spikes(18.5f, .34f, 1.7f, .92f, stage); Spikes(56f, .34f, 1.7f, .92f, stage); Laser(73f, 1.8f, 3.6f, stage);
                RiftSpinner(85f, 2.7f, stage);
                Checkpoint(69f, 3);
                ReboundCeiling(62f, 9.25f, 8f, stage, accent);
                CreateBellSeal(41f, 4.7f, 48f, stage);
            }
            CreateMemoryRoom(stage, accent);
            // Pulse relays open the rebound gate; platforms, walls, hazards and enemies remain valid chain targets.
        }

        private void CreateMemoryRoom(int stage, Color accent)
        {
            float[] ledgeX = { 60f, 63.5f, 82f };
            float[] ledgeTop = { 7.4f, 7.1f, 7.4f };
            float[] wallX = { 57.1f, 61f, 79.5f };
            float[] memoryX = { 63f, 66.2f, 85.3f };
            string[] wallArt = { "SecretWall_Rooftop", "SecretWall_Market", "SecretWall_Core" };
            string[] memoryArt = { "Memory_Rooftop", "Memory_Market", "Memory_Core" };
            string[] wallNames = { "SCHRIJNVERZEGELING", "ARKREGISTER", "OSSUARIUMMUUR" };
            string[] memoryNames = { "ELINS LAATSTE BEL", "HET LEGE ARKTICKET", "DE DRIEDUBBELE KLEPEL" };

            var ledge = Platform(ledgeX[stage], ledgeTop[stage], 5.6f, .52f, stage, accent);
            ledge.name = "Hidden memory room • " + memoryNames[stage];

            var memory = SpriteObject("Unheard memory • " + memoryNames[stage],
                new Vector3(memoryX[stage], ledgeTop[stage] + .92f, 0f),
                AfterglowArt.AuthoredSprite(memoryArt[stage], 190f), Color.white, 18);
            var memoryCollider = memory.AddComponent<CircleCollider2D>();
            memoryCollider.isTrigger = true;
            memoryCollider.radius = .52f;
            memory.AddComponent<AfterglowMemory>().Configure(stage, memoryNames[stage], ui);

            var seal = SpriteObject("Cracked hidden seal • " + wallNames[stage],
                new Vector3(wallX[stage], ledgeTop[stage] + .96f, 0f),
                AfterglowArt.AuthoredSprite(wallArt[stage], 190f), Color.white, 17);
            var sealCollider = seal.AddComponent<BoxCollider2D>();
            sealCollider.size = new Vector2(.94f, 1.92f);
            seal.AddComponent<AfterglowReboundTarget>().Configure(ReboundTargetKind.Relay, wallNames[stage], Vector2.left);
            seal.AddComponent<AfterglowBreakableWall>().Configure(memory, ui, wallNames[stage]);

            var shortcut = new GameObject("Hidden rootway • " + memoryNames[stage]);
            shortcut.transform.position = new Vector3(memoryX[stage] + 1.25f, ledgeTop[stage] + .88f, 0f);
            var glyph = shortcut.AddComponent<SpriteRenderer>();
            glyph.sprite = AfterglowArt.Disc("rootway-glyph", new Color(.09f,.12f,.11f,.78f), new Color(.72f,.67f,.43f,.86f));
            glyph.sortingOrder = 19;
            if (AfterglowArt.SpriteMaterial != null) glyph.sharedMaterial = AfterglowArt.SpriteMaterial;
            glyph.transform.localScale = new Vector3(.7f, 1.2f, 1f);
            var shortcutTrigger = shortcut.AddComponent<CircleCollider2D>();
            shortcutTrigger.radius = .72f;
            float[] shortcutX = { 83f, 88f, 94f };
            shortcut.AddComponent<AfterglowShortcut>().Configure(stage, new Vector2(shortcutX[stage], .12f), ui);
        }

        private void CreateStoryLandmark(int stage)
        {
            string art = stage == 0 ? "Story_RooftopShrine" : stage == 1 ? "Story_MarketArk" : "Story_HeartReliquary";
            float ppu = stage == 0 ? 120f : stage == 1 ? 140f : 150f;
            float x = stage == 0 ? 35f : stage == 1 ? 44f : 68f;
            float baseOffset = stage == 0 ? 3.08f : stage == 1 ? 2f : 2.73f;
            Sprite illustration = AfterglowArt.AuthoredSprite(art, ppu);
            SpriteObject("Ancient world landmark", new Vector3(x, baseOffset, 0f), illustration, Color.white, -18);
        }

        private void BuildStageActors(int stage)
        {
            if (stage == 0)
            {
                Enemy(11f, EnemyKind.Stalker); Enemy(23f, EnemyKind.RiftHound); Enemy(40f, EnemyKind.HexCaster);
                Enemy(53f, EnemyKind.Breaker); Enemy(71f, EnemyKind.RiftHound); Enemy(80f, EnemyKind.HexCaster);
            }
            else if (stage == 1)
            {
                Enemy(9f, EnemyKind.Stalker); Enemy(21f, EnemyKind.HexCaster); Enemy(35f, EnemyKind.RiftHound);
                Enemy(48f, EnemyKind.Breaker); Enemy(69f, EnemyKind.HexCaster); Enemy(76f, EnemyKind.RiftHound); Enemy(89f, EnemyKind.Breaker);
            }
            else
            {
                Enemy(10f, EnemyKind.RiftHound); Enemy(22f, EnemyKind.HexCaster); Enemy(38f, EnemyKind.Breaker);
                Enemy(50f, EnemyKind.RiftHound); Enemy(62f, EnemyKind.HexCaster); Enemy(76f, EnemyKind.Breaker);
                Enemy(89f, EnemyKind.RiftHound); Enemy(96f, EnemyKind.HexCaster);
            }
        }

        private void CreateExitAndBoss(int stage)
        {
            float bossX = stage == 0 ? 88f : stage == 1 ? 94f : 100f;
            float exitX = worldEnd - 2.5f;
            stageExit = Exit(exitX, stage);
            var wall = new GameObject("Boss arena seal");
            wall.transform.position = new Vector3(bossX + 5.5f, 7.25f, 0f);
            var barrier = wall.AddComponent<BoxCollider2D>();
            barrier.size = new Vector2(.6f, 15f);
            barrier.isTrigger = false;
            var seal = wall.AddComponent<SpriteRenderer>();
            seal.sprite = AfterglowArt.Panel("arena-seal", new Color(.12f, .035f, .03f, .74f), new Color(.46f, .16f, .12f, .84f), new Color(.68f, .31f, .2f, .68f));
            if (AfterglowArt.SpriteMaterial != null) seal.sharedMaterial = AfterglowArt.SpriteMaterial;
            seal.drawMode = SpriteDrawMode.Sliced;
            seal.size = new Vector2(.6f, 15f);
            seal.color = new Color(.58f, .2f, .15f, .48f);
            seal.sortingOrder = 4;
            stageExit.Configure(stage, ui, false);
            stageExit.SetBarrier(wall);
            wall.AddComponent<AfterglowReboundTarget>().Configure(ReboundTargetKind.Surface, "ARENA SEAL");

            BossStyle style = stage == 0 ? BossStyle.Vandal : stage == 1 ? BossStyle.CinderMatriarch : BossStyle.Eclipse;
            var bossRoot = AfterglowCharacterArt.Boss(transform, style);
            bossRoot.position = new Vector3(bossX, 2.3f, 0f);
            var body = bossRoot.gameObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            var box = bossRoot.gameObject.AddComponent<BoxCollider2D>();
            box.size = new Vector2(2.5f, 3.6f);
            var boss = bossRoot.gameObject.AddComponent<AfterglowBoss>();
            boss.Configure(style, player, ui, stageExit, stage);
        }

        private void Floor(float centerX, float width, int stage, Color accent)
        {
            float height = 2.35f;
            var foundation = SpriteObject("Buried stone foundation", new Vector3(centerX, -height * .5f, 0f), AfterglowArt.Pixel("pixel"), new Color(.075f, .08f, .08f), -2);
            foundation.transform.localScale = new Vector3(width, height, 1f);
            var floor = SpriteObject("Broken district surface", new Vector3(centerX, -height * .5f, 0f), AfterglowArt.AuthoredSprite(SurfaceArtName(stage), 256f), Color.white, 0);
            var sr = floor.GetComponent<SpriteRenderer>(); sr.drawMode = SpriteDrawMode.Tiled; sr.size = new Vector2(width, height);
            var collider = floor.AddComponent<BoxCollider2D>(); collider.size = new Vector2(width, height);
            floor.AddComponent<AfterglowReboundTarget>().Configure(ReboundTargetKind.Surface, "GROUND");
            var trim = SpriteObject("Worn stone edge", new Vector3(centerX, .105f, 0f), AfterglowArt.Pixel("pixel"), accent, 3);
            trim.transform.localScale = new Vector3(width, .055f, 1f);
            var deck = SpriteObject("Flagstone lip", new Vector3(centerX, -.16f, 0f), AfterglowArt.Pixel("pixel"), new Color(.18f, .17f, .15f), 2);
            deck.transform.localScale = new Vector3(width, .09f, 1f);
            var underside = SpriteObject("Ground shadow", new Vector3(centerX, -1.32f, 0f), AfterglowArt.Pixel("pixel"), new Color(.035f, .04f, .09f), -1);
            underside.transform.localScale = new Vector3(width, .055f, 1f);
        }

        private void FloorWithGaps(float centerX, float width, int stage, Color accent)
        {
            float left = centerX - width * .5f;
            float right = centerX + width * .5f;
            float[][] gaps = stage == 0
                ? new[] { new[] { 22.8f, 25.2f }, new[] { 62.8f, 65.2f } }
                : stage == 1
                    ? new[] { new[] { 27.8f, 30.2f }, new[] { 58.8f, 61.2f }, new[] { 83.8f, 86.2f } }
                    : new[] { new[] { 30.8f, 33.2f }, new[] { 62.8f, 65.2f }, new[] { 73.8f, 76.2f }, new[] { 92.8f, 95.2f } };
            float cursor = left;
            foreach (float[] gap in gaps)
            {
                float start = Mathf.Clamp(gap[0], left, right);
                float end = Mathf.Clamp(gap[1], left, right);
                if (start > cursor + .1f) Floor((cursor + start) * .5f, start - cursor, stage, accent);
                cursor = Mathf.Max(cursor, end);
            }
            if (cursor < right - .1f) Floor((cursor + right) * .5f, right - cursor, stage, accent);
        }

        private GameObject Platform(float x, float top, float width, float height, int stage, Color accent)
        {
            var root = SpriteObject("Carved district ledge", new Vector3(x, top - height * .5f, 0f), AfterglowArt.AuthoredSprite(SurfaceArtName(stage), 256f), Color.white, 1);
            var sr = root.GetComponent<SpriteRenderer>(); sr.drawMode = SpriteDrawMode.Tiled; sr.size = new Vector2(width, height);
            var collider = root.AddComponent<BoxCollider2D>(); collider.size = new Vector2(width, height);
            root.AddComponent<AfterglowReboundTarget>().Configure(ReboundTargetKind.Surface, "PLATFORM");
            var topGlow = SpriteObject("Platform edge", new Vector3(x, top - .04f, 0f), AfterglowArt.Pixel("pixel"), accent, 3);
            topGlow.transform.localScale = new Vector3(width - .1f, .07f, 1f);
            topGlow.transform.SetParent(root.transform, true);
            return root;
        }

        private void MovingPlatform(float x, float top, Vector2 travel, float seconds, int stage, Color accent)
        {
            GameObject platform = Platform(x, top, 4f, .58f, stage, accent);
            var rb = platform.GetComponent<Rigidbody2D>() ?? platform.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            var mover = platform.AddComponent<AfterglowMovingPlatform>();
            mover.Configure(travel, seconds);
        }

        private void CollapsingPlatform(float x, float top, int stage, Color accent)
        {
            GameObject platform = Platform(x, top, 4.3f, .55f, stage, accent);
            platform.AddComponent<AfterglowCollapsingPlatform>();
        }

        private void WorldWall(float x, int stage, Color accent)
        {
            var wall = SpriteObject("Boundary of the ruin", new Vector3(x, 6.5f, 0f), AfterglowArt.Panel("world-wall-" + stage, new Color(.075f, .08f, .085f), new Color(.2f, .17f, .14f), accent), Color.white, 1);
            var sr = wall.GetComponent<SpriteRenderer>(); sr.drawMode = SpriteDrawMode.Sliced; sr.size = new Vector2(.75f, 15f);
            var box = wall.AddComponent<BoxCollider2D>(); box.size = new Vector2(.75f, 15f);
            wall.AddComponent<AfterglowReboundTarget>().Configure(ReboundTargetKind.Surface, "WALL");
        }

        private void ReboundCeiling(float x, float y, float width, int stage, Color accent)
        {
            var ceiling = SpriteObject("Rebound lintel", new Vector3(x, y, 0f), AfterglowArt.Panel("rebound-ceiling-" + stage, new Color(.075f, .08f, .085f), new Color(.24f, .2f, .16f), accent), Color.white, 2);
            var sr = ceiling.GetComponent<SpriteRenderer>(); sr.drawMode = SpriteDrawMode.Sliced; sr.size = new Vector2(width, .48f);
            var collider = ceiling.AddComponent<BoxCollider2D>(); collider.size = new Vector2(width, .48f);
            ceiling.AddComponent<AfterglowReboundTarget>().Configure(ReboundTargetKind.Surface, "CEILING");
        }

        private void CreateBellSeal(float x, float y, float gateX, int stage)
        {
            var gate = SpriteObject("Sealed reliquary gate", new Vector3(gateX, 4.2f, 0f), AfterglowArt.Panel("rebound-gate-" + stage, new Color(.12f, .08f, .075f), new Color(.42f, .27f, .18f), new Color(.66f, .45f, .27f)), new Color(.66f, .48f, .3f, .68f), 5);
            var gateSprite = gate.GetComponent<SpriteRenderer>(); gateSprite.drawMode = SpriteDrawMode.Sliced; gateSprite.size = new Vector2(.72f, 9.6f);
            var gateCollider = gate.AddComponent<BoxCollider2D>(); gateCollider.size = new Vector2(.72f, 9.6f);
            gate.AddComponent<AfterglowReboundTarget>().Configure(ReboundTargetKind.Surface, "GATE");

            var relay = SpriteObject("Last-bell relic", new Vector3(x, y, 0f), AfterglowArt.Polygon("last-bell-relic", new Color(.22f, .14f, .085f), new Color(.78f, .6f, .31f), new Vector2(.5f, 1f), new Vector2(.78f, .28f), new Vector2(.66f, .04f), new Vector2(.34f, .04f), new Vector2(.22f, .28f)), Color.white, 12);
            relay.transform.localScale = new Vector3(.78f, 1.1f, 1f);
            var collider = relay.AddComponent<CircleCollider2D>(); collider.isTrigger = true; collider.radius = .58f;
            var rings = SpriteObject("Bell inscription", relay.transform.position, AfterglowArt.Disc("pulse-relay-ring", new Color(.26f, .16f, .09f, .04f), new Color(.75f, .55f, .28f, .72f)), new Color(.68f, .51f, .29f, .62f), 11);
            rings.transform.SetParent(relay.transform, true); rings.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
            relay.AddComponent<AfterglowReboundTarget>().Configure(ReboundTargetKind.Relay, "BELL RELIC", new Vector2(.28f, 1f));
            relay.AddComponent<AfterglowPulseRelay>().Configure(gate, ui, new Vector2(.28f, 1f), stage + 1);
        }

        private void Spikes(float x, float y, float width, float height, int stage)
        {
            var root = new GameObject("Living root snare");
            root.transform.position = new Vector3(x, y, 0f);
            var trigger = root.AddComponent<BoxCollider2D>(); trigger.isTrigger = true; trigger.size = new Vector2(width, height);
            root.AddComponent<AfterglowHazard>().Configure(HazardKind.Spikes);
            int count = Mathf.Max(2, Mathf.RoundToInt(width / .33f));
            for (int i = 0; i < count; i++)
            {
                float px = -width * .5f + (i + .5f) * width / count;
                float thornHeight = height * (.78f + .22f * Mathf.Abs(Mathf.Sin(i * 2.17f)));
                var spike = SpriteObject("Petrified thorn", root.transform.position + new Vector3(px, 0f, 0f), AfterglowArt.Polygon("spike-" + stage, new Color(.23f, .095f, .09f), new Color(.58f, .29f, .2f), new Vector2(.5f, 1f), new Vector2(.82f, .18f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(.18f, .18f)), new Color(.62f, .37f, .25f), 4);
                spike.transform.SetParent(root.transform, true);
                spike.transform.localScale = new Vector3(width / count, thornHeight, 1f);
            }
        }

        private void Laser(float x, float y, float height, int stage)
        {
            var root = new GameObject("Buried rune sear");
            root.transform.position = new Vector3(x, y, 0f);
            var trigger = root.AddComponent<BoxCollider2D>(); trigger.size = new Vector2(.22f, height); trigger.isTrigger = true;
            var rail = SpriteObject("Rune channel", root.transform.position, AfterglowArt.Panel("laser-rail", new Color(.08f, .085f, .08f), new Color(.31f, .24f, .17f), new Color(.42f, .32f, .21f)), Color.white, 5);
            rail.transform.SetParent(root.transform, true); rail.transform.localScale = new Vector3(.16f, height, 1f);
            var beam = SpriteObject("Sealing ember", root.transform.position, AfterglowArt.Pixel("pixel"), new Color(.54f, .16f, .13f, .76f), 6);
            beam.transform.SetParent(root.transform, true); beam.transform.localScale = new Vector3(.045f, height, 1f);
            foreach (float offset in new[] { -.72f, .72f })
            {
                var rune = SpriteObject("Runestone pylon", root.transform.position + Vector3.right * offset, AfterglowArt.Panel("rune-pylon", new Color(.12f, .11f, .1f), new Color(.48f, .34f, .21f), new Color(.62f, .43f, .26f)), Color.white, 7);
                rune.transform.SetParent(root.transform, true); rune.transform.localScale = new Vector3(.44f, .86f, 1f);
                var mark = SpriteObject("Carved rune", root.transform.position + Vector3.right * offset, AfterglowArt.Polygon("rune-mark", new Color(.45f, .2f, .13f), new Color(.73f, .5f, .29f), new Vector2(.5f, 1f), new Vector2(.82f, .5f), new Vector2(.5f, 0f), new Vector2(.18f, .5f)), new Color(.64f, .39f, .22f), 8);
                mark.transform.SetParent(root.transform, true); mark.transform.localScale = new Vector3(.13f, .25f, 1f);
            }
            root.AddComponent<AfterglowHazard>().Configure(HazardKind.PulseLaser, x * .17f);
        }

        private void RiftSpinner(float x, float y, int stage)
        {
            var root = new GameObject("Ossuary pendulum");
            root.transform.position = new Vector3(x, y, 0f);
            var trigger = root.AddComponent<CircleCollider2D>(); trigger.radius = .86f; trigger.isTrigger = true;
            var spin = new GameObject("Hanging censer hub").transform;
            spin.SetParent(root.transform, false);
            var disc = SpriteObject("Iron censer", root.transform.position, AfterglowArt.Disc("rift-hub", new Color(.16f, .13f, .12f), new Color(.58f, .39f, .24f)), Color.white, 8);
            disc.transform.SetParent(spin, true); disc.transform.localScale = new Vector3(.46f, .46f, 1f);
            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f;
                var blade = SpriteObject("Censer chain weight", root.transform.position, AfterglowArt.Polygon("rift-blade", new Color(.19f, .15f, .13f), new Color(.62f, .43f, .27f), new Vector2(.5f, 1f), new Vector2(.7f, .43f), new Vector2(.5f, 0f), new Vector2(.3f, .43f)), new Color(.53f, .37f, .24f), 7);
                blade.transform.SetParent(spin, true);
                blade.transform.localPosition = Quaternion.Euler(0f, 0f, angle) * Vector3.up * .72f;
                blade.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
                blade.transform.localScale = new Vector3(.34f, .77f, 1f);
            }
            spin.gameObject.AddComponent<AfterglowSpinner>().Configure(stage == 2 ? 125f : 80f);
            root.AddComponent<AfterglowHazard>().Configure(HazardKind.RiftField);
        }

        private void Checkpoint(float x, int id)
        {
            var root = new GameObject("Pilgrim memory shrine"); root.transform.position = new Vector3(x, .62f, 0f);
            var col = root.AddComponent<BoxCollider2D>(); col.isTrigger = true; col.size = new Vector2(1.25f, 1.7f);
            var pole = SpriteObject("Name stone", root.transform.position + Vector3.up * .38f, AfterglowArt.Polygon("checkpoint", new Color(.22f, .18f, .13f), new Color(.61f, .48f, .3f), new Vector2(.5f, 1f), new Vector2(.82f, .82f), new Vector2(.68f, 0f), new Vector2(.32f, 0f), new Vector2(.18f, .82f)), new Color(.6f, .48f, .3f), 6);
            pole.transform.SetParent(root.transform, true); pole.transform.localScale = new Vector3(.38f, 1.1f, 1f);
            var glow = SpriteObject("Votive ember", root.transform.position + Vector3.up * .88f, AfterglowArt.Disc("checkpoint-glow", new Color(.48f, .3f, .15f, .24f), new Color(.8f, .59f, .32f, .72f)), new Color(.72f, .52f, .28f, .68f), 7);
            glow.transform.SetParent(root.transform, true); glow.transform.localScale = new Vector3(.5f, .5f, 1f);
            root.AddComponent<AfterglowCheckpoint>().Configure(id, ui);
        }

        private AfterglowExit Exit(float x, int stage)
        {
            var root = new GameObject("Gothic passage"); root.transform.position = new Vector3(x, 1.75f, 0f);
            var trigger = root.AddComponent<CircleCollider2D>(); trigger.radius = .9f; trigger.isTrigger = true;
            var rim = SpriteObject("Carved stone arch", root.transform.position, AfterglowArt.Disc("portal-rim", new Color(.12f, .11f, .1f, .3f), new Color(.55f, .42f, .27f, .88f)), new Color(.55f, .43f, .28f, .7f), 7);
            rim.transform.SetParent(root.transform, true); rim.transform.localScale = new Vector3(1.55f, 2.3f, 1f);
            var core = SpriteObject("Dark doorway", root.transform.position, AfterglowArt.Disc("portal-core", new Color(.025f, .025f, .03f, .84f), new Color(.38f, .25f, .15f, .45f)), new Color(.32f, .23f, .16f, .45f), 6);
            core.transform.SetParent(root.transform, true); core.transform.localScale = new Vector3(.72f, 1.55f, 1f);
            return root.AddComponent<AfterglowExit>();
        }

        private void Enemy(float x, EnemyKind kind)
        {
            var root = AfterglowCharacterArt.Enemy(transform, kind);
            root.position = new Vector3(x, kind == EnemyKind.RiftHound ? .7f : kind == EnemyKind.Breaker ? .98f : .82f, 0f);
            var body = root.gameObject.AddComponent<Rigidbody2D>(); body.mass = kind == EnemyKind.Breaker ? 1.7f : 1f;
            Collider2D collider;
            if (kind == EnemyKind.RiftHound)
            {
                var box = root.gameObject.AddComponent<BoxCollider2D>(); box.size = new Vector2(1.1f, .72f); collider = box;
            }
            else
            {
                var capsule = root.gameObject.AddComponent<CapsuleCollider2D>(); capsule.size = kind == EnemyKind.Breaker ? new Vector2(1.25f, 1.85f) : new Vector2(.82f, 1.58f); collider = capsule;
            }
            var enemy = root.gameObject.AddComponent<AfterglowEnemy>();
            enemy.Configure(kind, player, ui);
        }





        private GameObject SpriteObject(string name, Vector3 position, Sprite sprite, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.color = color; sr.sortingOrder = order;
            if (AfterglowArt.SpriteMaterial != null) sr.sharedMaterial = AfterglowArt.SpriteMaterial;
            return go;
        }

        private static string StageTitle(int stage) => stage == 0 ? "DE VERGROEIDE SCHRIJN" : stage == 1 ? "DE VERZONKEN ARK" : "HET WORTELHART";
        private static string SurfaceArtName(int stage) => stage == 0 ? "Surface_Rooftops" : stage == 1 ? "Surface_Market" : "Surface_Heart";
    }

}
