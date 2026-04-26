using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;

// Menu: Casino → Rebuild GameScene (Grand Casino)
public class GameSceneFixer
{
    const float RW = 50f, RH = 5f, RD = 60f;

    [MenuItem("Casino/Rebuild GameScene (Grand Casino)")]
    public static void Execute()
    {
        if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity",  true),
            new EditorBuildSettingsScene("Assets/Scenes/GameScene.unity", true),
        };

        // ── Materials ─────────────────────────────────────────────────────
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");

        var floorMat      = Upsert("Assets/Materials/CasinoFloor.mat",     new Color(0.22f, 0.07f, 0.03f), 0.0f, 0.10f);
        var wallMat       = Upsert("Assets/Materials/CasinoWall.mat",      new Color(0.20f, 0.17f, 0.08f), 0.0f, 0.12f);
        var ceilMat       = Upsert("Assets/Materials/CasinoCeiling.mat",   new Color(0.04f, 0.03f, 0.02f), 0.0f, 0.08f);
        var slotBodyMat   = Upsert("Assets/Materials/SlotBody.mat",        new Color(0.08f, 0.05f, 0.12f), 0.6f, 0.55f);
        var slotTrimMat   = Upsert("Assets/Materials/SlotTrim.mat",        new Color(0.60f, 0.44f, 0.02f), 0.9f, 0.85f);
        var slotRedMat    = Upsert("Assets/Materials/SlotRedTrim.mat",     new Color(0.80f, 0.04f, 0.04f), 0.8f, 0.70f);
        var slotScreenMat = Upsert("Assets/Materials/SlotScreen.mat",      new Color(0.0f,  0.65f, 0.15f), 0.0f, 0.90f);
        SetEmission(slotScreenMat, new Color(0f, 1f, 0.2f) * 2.5f);
        SetEmission(slotRedMat,    new Color(1f, 0.04f, 0.04f) * 0.6f);

        var tableBodyMat  = Upsert("Assets/Materials/TableBody.mat",       new Color(0.15f, 0.07f, 0.03f), 0.5f, 0.30f);
        var rouletteFelt  = Upsert("Assets/Materials/RouletteFelt.mat",    new Color(0.04f, 0.28f, 0.07f), 0.0f, 0.15f);
        var cardFelt      = Upsert("Assets/Materials/CardFelt.mat",        new Color(0.04f, 0.10f, 0.28f), 0.0f, 0.15f);
        var wheelMat      = Upsert("Assets/Materials/RouletteWheel.mat",   new Color(0.55f, 0.45f, 0.10f), 0.8f, 0.70f);
        var columnMat     = Upsert("Assets/Materials/Column.mat",          new Color(0.55f, 0.45f, 0.10f), 0.7f, 0.60f);
        var slotZoneMat   = Upsert("Assets/Materials/SlotZonePad.mat",     new Color(0.30f, 0.05f, 0.04f), 0.0f, 0.05f);
        var roulZoneMat   = Upsert("Assets/Materials/RouletteZonePad.mat", new Color(0.04f, 0.18f, 0.04f), 0.0f, 0.05f);
        var cardZoneMat   = Upsert("Assets/Materials/CardZonePad.mat",     new Color(0.04f, 0.05f, 0.18f), 0.0f, 0.05f);
        var stripMat      = Upsert("Assets/Materials/LightStrip.mat",      new Color(0.75f, 0.82f, 0.50f), 0.0f, 1.0f);
        SetEmission(stripMat, new Color(0.55f, 0.72f, 0.28f) * 2.2f);
        AssetDatabase.SaveAssets();

        // ── New scene ─────────────────────────────────────────────────────
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        RenderSettings.ambientMode      = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight     = new Color(0.012f, 0.016f, 0.004f);
        RenderSettings.fog              = true;
        RenderSettings.fogColor         = new Color(0.010f, 0.014f, 0.003f);
        RenderSettings.fogMode          = FogMode.Linear;
        RenderSettings.fogStartDistance = 10f;
        RenderSettings.fogEndDistance   = 42f;

        // ── Room shell ────────────────────────────────────────────────────
        float hw = RW * 0.5f, hd = RD * 0.5f;
        Box("Floor",     new Vector3(0,            -0.5f, 0),          new Vector3(RW,        1f,        RD),        floorMat);
        Box("Ceiling",   new Vector3(0,   RH + 0.5f,     0),          new Vector3(RW,        1f,        RD),        ceilMat);
        Box("WallLeft",  new Vector3(-hw - 0.25f,  RH * 0.5f, 0),     new Vector3(0.5f,  RH + 1f,  RD + 1f),  wallMat);
        Box("WallRight", new Vector3( hw + 0.25f,  RH * 0.5f, 0),     new Vector3(0.5f,  RH + 1f,  RD + 1f),  wallMat);
        Box("WallBack",  new Vector3(0,   RH * 0.5f, -hd - 0.25f),    new Vector3(RW + 1f, RH + 1f, 0.5f),    wallMat);
        Box("WallFront", new Vector3(0,   RH * 0.5f,  hd + 0.25f),    new Vector3(RW + 1f, RH + 1f, 0.5f),    wallMat);

        // ── Zone floor pads (decorative, no collider) ─────────────────────
        // Slot zone  : center column  X=-10..10  Z=-29..9
        BoxDeco("SlotZonePad",     new Vector3(  0f, 0.01f, -10f), new Vector3(20f, 0.02f, 38f), slotZoneMat);
        // Roulette   : west          X=-25..-10  Z=-15..15
        BoxDeco("RouletteZonePad", new Vector3(-17f, 0.01f,   0f), new Vector3(15f, 0.02f, 30f), roulZoneMat);
        // Cards      : east          X=10..25    Z=-15..15
        BoxDeco("CardZonePad",     new Vector3( 17f, 0.01f,   0f), new Vector3(15f, 0.02f, 30f), cardZoneMat);

        // ── Columns at zone boundaries (X = ±10) ─────────────────────────
        float[] colZ = { -22f, -14f, -6f, 2f, 10f, 18f };
        foreach (float z in colZ)
        {
            Column("ColL" + (int)z, new Vector3(-10f, 0f, z), columnMat);
            Column("ColR" + (int)z, new Vector3( 10f, 0f, z), columnMat);
        }

        // ── 6 Slot Machines — 2 facing banks of 3 (indices 0-5) ──────────
        float[] slotZ = { -20f, -12f, -4f };
        int idx = 0;
        foreach (float z in slotZ)
        {
            SlotMachineGrouped("SlotL" + (int)z, idx++, new Vector3(-3.5f, 0f, z), true,
                slotBodyMat, slotTrimMat, slotScreenMat, slotRedMat);
            SlotMachineGrouped("SlotR" + (int)z, idx++, new Vector3( 3.5f, 0f, z), false,
                slotBodyMat, slotTrimMat, slotScreenMat, slotRedMat);
        }

        // ── 4 Roulette Tables — west zone (indices 6-9) ───────────────────
        float[] rtX = { -14f, -20f };
        float[] rtZ = {  -7f,   7f };
        foreach (float rx in rtX)
            foreach (float rz in rtZ)
                RouletteTableGrouped("Roulette_" + (int)rx + "_" + (int)rz, idx++,
                    new Vector3(rx, 0f, rz), tableBodyMat, rouletteFelt, wheelMat);

        // ── 4 Card Tables — east zone (indices 10-13) ─────────────────────
        float[] ctX = { 14f, 20f };
        float[] ctZ = { -7f,  7f };
        foreach (float cx in ctX)
            foreach (float cz in ctZ)
                CardTableGrouped("CardTable_" + (int)cx + "_" + (int)cz, idx++,
                    new Vector3(cx, 0f, cz), tableBodyMat, cardFelt);

        // ── Wall neons ────────────────────────────────────────────────────
        // Left wall  alternates red / amber
        // Right wall alternates pink / purple
        Color[] neonL = { new Color(1.0f, 0.06f, 0.06f), new Color(0.90f, 0.55f, 0.00f) };
        Color[] neonR = { new Color(0.85f, 0.08f, 0.75f), new Color(0.55f, 0.06f, 0.95f) };
        float[] wallNeonZ = { -26f, -18f, -10f, -2f, 6f, 14f, 22f };
        for (int i = 0; i < wallNeonZ.Length; i++)
        {
            float z = wallNeonZ[i];
            WallNeon("NeonL" + i, new Vector3(-hw + 0.3f, RH - 0.6f, z), neonL[i % 2], 1.8f, 7f);
            WallNeon("NeonR" + i, new Vector3( hw - 0.3f, RH - 0.6f, z), neonR[i % 2], 1.8f, 7f);
        }
        // Zone accent lights — mid-height green (roulette) and blue (cards)
        float[] accentZ = { -10f, 0f, 10f };
        for (int i = 0; i < accentZ.Length; i++)
        {
            WallNeon("AccentRL" + i, new Vector3(-hw + 0.3f, RH * 0.55f, accentZ[i]),
                new Color(0.08f, 0.92f, 0.18f), 1.4f, 5f);
            WallNeon("AccentCR" + i, new Vector3( hw - 0.3f, RH * 0.55f, accentZ[i]),
                new Color(0.10f, 0.28f, 1.00f), 1.4f, 5f);
        }

        // ── Ceiling light strips ──────────────────────────────────────────
        float[] stripZ = { -22f, -14f, -6f, 2f, 10f, 18f };
        foreach (float z in stripZ)
            CeilStrip("Strip" + (int)z, new Vector3(0f, RH - 0.05f, z), stripMat);

        // ── Player (starts near south entrance) ───────────────────────────
        var playerGO = new GameObject("Player");
        playerGO.tag = "Player";
        playerGO.transform.position = new Vector3(0f, 1f, 24f);

        var cc = playerGO.AddComponent<CharacterController>();
        cc.height = 2f; cc.center = Vector3.zero;
        cc.radius = 0.30f; cc.stepOffset = 0.3f; cc.slopeLimit = 45f;
        playerGO.AddComponent<FirstPersonController>();

        var camGO = new GameObject("MainCamera");
        camGO.tag = "MainCamera";
        camGO.transform.SetParent(playerGO.transform);
        camGO.transform.localPosition = new Vector3(0f, 0.7f, 0f);
        camGO.transform.localRotation = Quaternion.identity;
        var cam = camGO.AddComponent<Camera>();
        cam.nearClipPlane = 0.08f; cam.farClipPlane = 50f; cam.fieldOfView = 80f;
        camGO.AddComponent<AudioListener>();

        // ── HUD + Game scripts ────────────────────────────────────────────
        BuildHUD();

        // ── 5 Caseritos — GameManager activates only cfg.caseritos of them ──
        SpawnCaserito("Caserito_1", new Vector3(  0f, 0f,  12f));   // center aisle
        SpawnCaserito("Caserito_2", new Vector3( -5f, 0f,  -8f));   // slot zone
        SpawnCaserito("Caserito_3", new Vector3(-16f, 0f,   3f));   // roulette zone
        SpawnCaserito("Caserito_4", new Vector3( 16f, 0f,   3f));   // card zone
        SpawnCaserito("Caserito_5", new Vector3(  2f, 0f, -22f));   // back slot zone

        // ── Save ──────────────────────────────────────────────────────────
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/GameScene.unity");
        AssetDatabase.SaveAssets();
        Debug.Log("[GameSceneFixer] Grand Casino: 6 slots · 4 ruletas · 4 mesas de cartas · 5 caseritos.");
    }

    // ─────────────────────────────────────────────────────────────────────
    // SLOT MACHINE
    // ─────────────────────────────────────────────────────────────────────
    static void SlotMachineGrouped(string goName, int machineIdx, Vector3 basePos, bool facingPlusX,
        Material body, Material trim, Material screen, Material red)
    {
        float side = facingPlusX ? 1f : -1f;

        var parent = new GameObject(goName);
        parent.transform.position = basePos;

        var sm = parent.AddComponent<SlotMachine>();
        sm.machineName  = "Máquina " + (machineIdx + 1);
        sm.machineIndex = machineIdx;

        BoxChild(parent, "_Base",  new Vector3(0f, 0.10f, 0f),     new Vector3(0.90f, 0.20f, 0.58f), body);
        BoxChild(parent, "_Body",  new Vector3(0f, 1.20f, 0f),     new Vector3(0.80f, 2.00f, 0.50f), body);
        BoxChild(parent, "_Cap",   new Vector3(0f, 2.25f, 0f),     new Vector3(0.82f, 0.12f, 0.52f), trim);
        BoxChild(parent, "_TrimF", new Vector3(0f, 1.20f,  0.26f), new Vector3(0.82f, 2.02f, 0.02f), trim);
        BoxChild(parent, "_TrimB", new Vector3(0f, 1.20f, -0.26f), new Vector3(0.82f, 2.02f, 0.02f), trim);
        BoxChild(parent, "_Red",   new Vector3(0f, 0.30f,  0f),    new Vector3(0.81f, 0.08f, 0.51f), red);

        var scrOff = new Vector3(side * 0.41f, 1.3f, 0f);
        BoxChild(parent, "_Screen", scrOff, new Vector3(0.02f, 0.65f, 0.40f), screen);

        var slGO = new GameObject("ScreenLight");
        slGO.transform.SetParent(parent.transform);
        slGO.transform.localPosition = scrOff + new Vector3(side * 0.35f, 0f, 0f);
        var sl = slGO.AddComponent<Light>();
        sl.type = LightType.Point; sl.color = new Color(0f, 0.85f, 0.20f);
        sl.intensity = 0.8f; sl.range = 2.5f;
        sm.screenLight = sl;

        var neonGO = new GameObject("NeonSign");
        neonGO.transform.SetParent(parent.transform);
        neonGO.transform.localPosition = new Vector3(0f, RH - 0.5f, 0f);
        var nl = neonGO.AddComponent<Light>();
        nl.type = LightType.Point; nl.color = new Color(1f, 0.12f, 0f);
        nl.intensity = 4f; nl.range = 4f;
        neonGO.SetActive(false);
        sm.neonSign = nl;
    }

    // ─────────────────────────────────────────────────────────────────────
    // ROULETTE TABLE
    // ─────────────────────────────────────────────────────────────────────
    static void RouletteTableGrouped(string goName, int machineIdx, Vector3 basePos,
        Material body, Material felt, Material wheel)
    {
        var parent = new GameObject(goName);
        parent.transform.position = basePos;

        var rt = parent.AddComponent<RouletteTable>();
        rt.machineName  = "Ruleta " + (machineIdx - 5);
        rt.machineIndex = machineIdx;

        // Four legs
        int legN = 0;
        foreach (float lx in new[] { -0.50f, 0.50f })
            foreach (float lz in new[] { -1.00f, 1.00f })
                BoxChild(parent, "_Leg" + legN++, new Vector3(lx, 0.36f, lz), new Vector3(0.13f, 0.72f, 0.13f), body);

        BoxChild(parent, "_Apron", new Vector3(0f, 0.72f, 0f), new Vector3(1.30f, 0.08f, 2.50f), body);
        BoxChild(parent, "_Felt",  new Vector3(0f, 0.76f, 0f), new Vector3(1.20f, 0.04f, 2.40f), felt);

        // Decorative roulette wheel (flat cylinder)
        var wheelGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        wheelGO.name = goName + "_Wheel";
        wheelGO.transform.SetParent(parent.transform);
        wheelGO.transform.localPosition = new Vector3(0f, 0.82f, -0.90f);
        wheelGO.transform.localScale    = new Vector3(0.52f, 0.04f, 0.52f);
        Object.DestroyImmediate(wheelGO.GetComponent<CapsuleCollider>());
        wheelGO.GetComponent<MeshRenderer>().sharedMaterial = wheel;

        var slGO = new GameObject("ScreenLight");
        slGO.transform.SetParent(parent.transform);
        slGO.transform.localPosition = new Vector3(0f, 1.3f, 0f);
        var sl = slGO.AddComponent<Light>();
        sl.type = LightType.Point; sl.color = new Color(0.10f, 0.85f, 0.20f);
        sl.intensity = 0.7f; sl.range = 2.8f;
        rt.screenLight = sl;

        var neonGO = new GameObject("NeonSign");
        neonGO.transform.SetParent(parent.transform);
        neonGO.transform.localPosition = new Vector3(0f, RH - 0.5f, 0f);
        var nl = neonGO.AddComponent<Light>();
        nl.type = LightType.Point; nl.color = new Color(0.08f, 1f, 0.15f);
        nl.intensity = 4f; nl.range = 4f;
        neonGO.SetActive(false);
        rt.neonSign = nl;
    }

    // ─────────────────────────────────────────────────────────────────────
    // CARD TABLE
    // ─────────────────────────────────────────────────────────────────────
    static void CardTableGrouped(string goName, int machineIdx, Vector3 basePos,
        Material body, Material felt)
    {
        var parent = new GameObject(goName);
        parent.transform.position = basePos;

        var ct = parent.AddComponent<CardTable>();
        ct.machineName  = "Mesa de Cartas " + (machineIdx - 9);
        ct.machineIndex = machineIdx;

        // Four legs
        int legN = 0;
        foreach (float lx in new[] { -0.65f, 0.65f })
            foreach (float lz in new[] { -0.90f, 0.90f })
                BoxChild(parent, "_Leg" + legN++, new Vector3(lx, 0.36f, lz), new Vector3(0.13f, 0.72f, 0.13f), body);

        BoxChild(parent, "_Apron", new Vector3(0f, 0.72f, 0f), new Vector3(1.55f, 0.08f, 2.10f), body);
        BoxChild(parent, "_Felt",  new Vector3(0f, 0.76f, 0f), new Vector3(1.45f, 0.04f, 2.00f), felt);

        // Decorative card deck
        var deckMat = new Material(Shader.Find("Standard"));
        deckMat.color = new Color(0.95f, 0.93f, 0.88f);
        var deckGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        deckGO.name = goName + "_Deck";
        deckGO.transform.SetParent(parent.transform);
        deckGO.transform.localPosition = new Vector3(0.30f, 0.82f, 0.55f);
        deckGO.transform.localScale    = new Vector3(0.10f, 0.06f, 0.14f);
        Object.DestroyImmediate(deckGO.GetComponent<BoxCollider>());
        deckGO.GetComponent<MeshRenderer>().sharedMaterial = deckMat;

        var slGO = new GameObject("ScreenLight");
        slGO.transform.SetParent(parent.transform);
        slGO.transform.localPosition = new Vector3(0f, 1.3f, 0f);
        var sl = slGO.AddComponent<Light>();
        sl.type = LightType.Point; sl.color = new Color(0.15f, 0.30f, 0.95f);
        sl.intensity = 0.7f; sl.range = 2.8f;
        ct.screenLight = sl;

        var neonGO = new GameObject("NeonSign");
        neonGO.transform.SetParent(parent.transform);
        neonGO.transform.localPosition = new Vector3(0f, RH - 0.5f, 0f);
        var nl = neonGO.AddComponent<Light>();
        nl.type = LightType.Point; nl.color = new Color(0.10f, 0.20f, 1f);
        nl.intensity = 4f; nl.range = 4f;
        neonGO.SetActive(false);
        ct.neonSign = nl;
    }

    // ─────────────────────────────────────────────────────────────────────
    // COLUMN
    // ─────────────────────────────────────────────────────────────────────
    static void Column(string n, Vector3 basePos, Material mat)
    {
        Box(n + "_Shaft",   basePos + new Vector3(0f, RH * 0.5f, 0f), new Vector3(0.50f, RH,   0.50f), mat);
        Box(n + "_Capital", basePos + new Vector3(0f, RH - 0.1f, 0f), new Vector3(0.70f, 0.3f, 0.70f), mat);
        Box(n + "_Base",    basePos + new Vector3(0f, 0.15f,     0f), new Vector3(0.70f, 0.3f, 0.70f), mat);

        var lg = new GameObject(n + "_Light");
        lg.transform.position = basePos + new Vector3(0f, RH + 0.3f, 0f);
        var l = lg.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(0.85f, 0.72f, 0.25f);
        l.intensity = 1.2f; l.range = 5f;
    }

    // ─────────────────────────────────────────────────────────────────────
    // HUD CANVAS + GAME SCRIPTS
    // ─────────────────────────────────────────────────────────────────────
    static void BuildHUD()
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // ── Canvas ────────────────────────────────────────────────────────
        var cGO = new GameObject("HUDCanvas");
        var canvas = cGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var cs = cGO.AddComponent<CanvasScaler>();
        cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight  = 0.5f;
        cGO.AddComponent<GraphicRaycaster>();

        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // Timer panel — top center
        var timerBg  = Panel("TimerBg", cGO.transform,
            new Vector2(0.38f, 0.93f), new Vector2(0.62f, 1.00f),
            new Color(0.04f, 0.03f, 0.02f, 0.88f));
        var timerTxt = Label("TimerText", timerBg.transform,
            Vector2.zero, Vector2.one, "05:00",
            new Color(0.92f, 0.80f, 0.08f), 44, FontStyle.Bold, font);

        // Tablet panel — bottom right
        var tabletBezel  = Panel("Tablet", cGO.transform,
            new Vector2(0.75f, 0.01f), new Vector2(0.99f, 0.32f),
            new Color(0.12f, 0.10f, 0.06f, 0.96f));
        var tabletScreen = Panel("TabletScreen", tabletBezel.transform,
            new Vector2(0.04f, 0.03f), new Vector2(0.96f, 0.97f),
            new Color(0.02f, 0.06f, 0.02f, 1f));
        Label("TabletHeader", tabletScreen.transform,
            new Vector2(0f, 0.84f), new Vector2(1f, 1f),
            "TABLETA DE TURNO",
            new Color(0.38f, 0.70f, 0.28f), 13, FontStyle.Bold, font);
        Panel("Divider", tabletScreen.transform,
            new Vector2(0.02f, 0.82f), new Vector2(0.98f, 0.835f),
            new Color(0.25f, 0.50f, 0.18f, 0.8f));

        var taskGO = new GameObject("TaskList");
        taskGO.transform.SetParent(tabletScreen.transform, false);
        var taskRT = taskGO.AddComponent<RectTransform>();
        taskRT.anchorMin = new Vector2(0.04f, 0.02f);
        taskRT.anchorMax = new Vector2(0.96f, 0.80f);
        taskRT.offsetMin = taskRT.offsetMax = Vector2.zero;
        var vlg = taskGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4f;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(4, 4, 4, 4);

        // Interaction prompt — center bottom
        var promptGO  = Panel("PromptPanel", cGO.transform,
            new Vector2(0.28f, 0.08f), new Vector2(0.72f, 0.16f),
            new Color(0.02f, 0.02f, 0.01f, 0.80f));
        var promptTxt = Label("PromptText", promptGO.transform,
            Vector2.zero, Vector2.one, "[E]  Reparar",
            new Color(0.85f, 0.78f, 0.20f), 24, FontStyle.Bold, font);
        promptGO.SetActive(false);

        // RepairMinigame container — EnsureUI() builds the rest on first Open()
        var repairPanel = new GameObject("RepairPanel");
        repairPanel.transform.SetParent(cGO.transform, false);
        FullScreen(repairPanel);
        repairPanel.SetActive(false);

        // End panel — full screen
        var endPanel = new GameObject("EndPanel");
        endPanel.transform.SetParent(cGO.transform, false);
        FullScreen(endPanel);
        Panel("Darkener", endPanel.transform, Vector2.zero, Vector2.one,
            new Color(0f, 0f, 0f, 0.88f));
        var endWin    = Panel("EndWindow", endPanel.transform,
            new Vector2(0.25f, 0.28f), new Vector2(0.75f, 0.72f),
            new Color(0.05f, 0.04f, 0.02f, 0.97f));
        var endTitle  = Label("EndTitle", endWin.transform,
            new Vector2(0f, 0.68f), new Vector2(1f, 1f),
            "TURNO COMPLETADO",
            new Color(0.92f, 0.80f, 0.08f), 40, FontStyle.Bold, font);
        var endSub    = Label("EndSub", endWin.transform,
            new Vector2(0.05f, 0.30f), new Vector2(0.95f, 0.65f),
            "...", new Color(0.70f, 0.60f, 0.40f), 20, FontStyle.Normal, font);
        var menuBtn   = MakeButton("MenuBtn", endWin.transform,
            new Vector2(0.25f, 0.06f), new Vector2(0.75f, 0.24f),
            "VOLVER AL MENÚ",
            new Color(0.40f, 0.12f, 0.04f), new Color(0.92f, 0.80f, 0.20f), font, 22);
        endPanel.SetActive(false);

        // Runtime scripts
        new GameObject("GameManager").AddComponent<GameManager>();

        var rm  = cGO.AddComponent<RepairMinigame>();
        rm.panel = repairPanel;

        var hud = cGO.AddComponent<GameHUD>();
        hud.timerText      = timerTxt;
        hud.taskListParent = taskGO.transform;
        hud.promptPanel    = promptGO;
        hud.promptText     = promptTxt;
        hud.endPanel       = endPanel;
        hud.endTitleText   = endTitle;
        hud.endSubText     = endSub;

        UnityEditor.Events.UnityEventTools.AddPersistentListener(menuBtn.onClick, hud.ReturnToMenu);
    }

    // ─────────────────────────────────────────────────────────────────────
    // CASERITO
    // ─────────────────────────────────────────────────────────────────────
    static void SpawnCaserito(string goName, Vector3 position)
    {
        var root = new GameObject(goName);
        root.transform.position = position;

        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = new Vector3(0f, 1f, 0f);
        Object.DestroyImmediate(body.GetComponent<CapsuleCollider>());
        var bodyMat = new Material(Shader.Find("Standard"));
        bodyMat.color = new Color(0.28f, 0.02f, 0.02f);
        bodyMat.SetFloat("_Metallic",   0.72f);
        bodyMat.SetFloat("_Glossiness", 0.55f);
        body.GetComponent<MeshRenderer>().sharedMaterial = bodyMat;

        var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eye.name = "Eye";
        eye.transform.SetParent(root.transform, false);
        eye.transform.localPosition = new Vector3(0f, 1.55f, 0.53f);
        eye.transform.localScale    = new Vector3(0.26f, 0.26f, 0.26f);
        Object.DestroyImmediate(eye.GetComponent<SphereCollider>());
        var eyeMat = new Material(Shader.Find("Standard"));
        eyeMat.color = new Color(0.12f, 0f, 0f);
        eyeMat.EnableKeyword("_EMISSION");
        eyeMat.SetColor("_EmissionColor", Color.black);
        eye.GetComponent<MeshRenderer>().sharedMaterial = eyeMat;

        var cap = root.AddComponent<CapsuleCollider>();
        cap.center = new Vector3(0f, 1f, 0f); cap.radius = 0.45f; cap.height = 2f;

        var trig = root.AddComponent<SphereCollider>();
        trig.center = new Vector3(0f, 1f, 0f); trig.radius = 1.1f; trig.isTrigger = true;

        var rb = root.AddComponent<Rigidbody>();
        rb.mass = 80f; rb.linearDamping = 8f; rb.angularDamping = 999f;
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        root.AddComponent<Caserito>();
    }

    // ─────────────────────────────────────────────────────────────────────
    // UI HELPERS
    // ─────────────────────────────────────────────────────────────────────
    static GameObject Panel(string name, Transform parent, Vector2 ancMin, Vector2 ancMax, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Anchor(go, ancMin, ancMax);
        go.AddComponent<Image>().color = color;
        return go;
    }

    static Text Label(string name, Transform parent,
        Vector2 ancMin, Vector2 ancMax, string text,
        Color color, int size, FontStyle style, Font font)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Anchor(go, ancMin, ancMax);
        var txt = go.AddComponent<Text>();
        txt.font      = font; txt.fontSize = size; txt.fontStyle = style;
        txt.color     = color; txt.text = text;
        txt.alignment = TextAnchor.MiddleCenter;
        return txt;
    }

    static Button MakeButton(string name, Transform parent,
        Vector2 ancMin, Vector2 ancMax, string label,
        Color bgColor, Color textColor, Font font, int fontSize)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Anchor(go, ancMin, ancMax);
        var img = go.AddComponent<Image>();
        img.color = bgColor;
        var btn = go.AddComponent<Button>();
        var c = btn.colors;
        c.normalColor      = bgColor;
        c.highlightedColor = bgColor * 1.4f;
        c.pressedColor     = bgColor * 0.7f;
        btn.colors = c;

        var lgo = new GameObject("Label");
        lgo.transform.SetParent(go.transform, false);
        Anchor(lgo, Vector2.zero, Vector2.one);
        var txt = lgo.AddComponent<Text>();
        txt.font      = font; txt.fontSize = fontSize; txt.fontStyle = FontStyle.Bold;
        txt.color     = textColor; txt.text = label;
        txt.alignment = TextAnchor.MiddleCenter;
        return btn;
    }

    static void Anchor(GameObject go, Vector2 min, Vector2 max)
    {
        var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
        rt.anchorMin = min; rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static void FullScreen(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    // ─────────────────────────────────────────────────────────────────────
    // SCENE HELPERS
    // ─────────────────────────────────────────────────────────────────────
    static void WallNeon(string n, Vector3 pos, Color color, float intensity, float range)
    {
        var go = new GameObject(n);
        go.transform.position = pos;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point; l.color = color;
        l.intensity = intensity; l.range = range;
        var f = go.AddComponent<LightFlicker>();
        f.normalIntensity = intensity;
        f.minStableTime   = 0.5f; f.maxStableTime = 3.5f;
        f.flickerSpeed    = 0.03f; f.maxFlickers   = 5;
    }

    static void CeilStrip(string n, Vector3 pos, Material mat)
    {
        var strip = Box(n + "_Geo", pos, new Vector3(0.12f, 0.05f, 1.8f), mat);
        Object.DestroyImmediate(strip.GetComponent<BoxCollider>());

        foreach (float off in new[] { -0.7f, 0.7f })
        {
            var lg = new GameObject(n + "_Light" + (int)(off * 10));
            lg.transform.position = pos + new Vector3(0f, -0.10f, off);
            var l = lg.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0.60f, 0.72f, 0.30f);
            l.intensity = 2.0f; l.range = 10f;
            var f = lg.AddComponent<LightFlicker>();
            f.normalIntensity = 2.0f; f.dimIntensity  = 0.05f;
            f.minStableTime   = 0.5f; f.maxStableTime = 5f;
            f.flickerSpeed    = 0.04f; f.maxFlickers   = 4;
        }
    }

    static Material Upsert(string path, Color color, float metallic, float smoothness)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Standard"))
                { name = System.IO.Path.GetFileNameWithoutExtension(path) };
            AssetDatabase.CreateAsset(m, path);
        }
        m.color = color;
        m.SetFloat("_Metallic",   metallic);
        m.SetFloat("_Glossiness", smoothness);
        EditorUtility.SetDirty(m);
        return m;
    }

    static void SetEmission(Material m, Color emission)
    {
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", emission);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
    }

    static GameObject Box(string n, Vector3 pos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = n;
        go.transform.position   = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    static GameObject BoxDeco(string n, Vector3 pos, Vector3 scale, Material mat)
    {
        var go = Box(n, pos, scale, mat);
        Object.DestroyImmediate(go.GetComponent<BoxCollider>());
        return go;
    }

    static GameObject BoxChild(GameObject parent, string suffix, Vector3 localPos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = parent.name + suffix;
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = localPos;
        go.transform.localScale    = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }
}
