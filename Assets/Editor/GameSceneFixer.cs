using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;

public class GameSceneFixer
{
    const float W = 9f, H = 3.2f, D = 28f;

    [MenuItem("Casino/Rebuild GameScene (Night of Consumers)")]
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

        var floorMat   = Upsert("Assets/Materials/CasinoFloor.mat",   new Color(0.22f, 0.07f, 0.03f), 0.0f, 0.10f);
        var wallMat    = Upsert("Assets/Materials/CasinoWall.mat",    new Color(0.20f, 0.17f, 0.08f), 0.0f, 0.12f);
        var ceilMat    = Upsert("Assets/Materials/CasinoCeiling.mat", new Color(0.04f, 0.03f, 0.02f), 0.0f, 0.08f);
        var bodyMat    = Upsert("Assets/Materials/SlotBody.mat",      new Color(0.08f, 0.05f, 0.12f), 0.6f, 0.55f);
        var trimMat    = Upsert("Assets/Materials/SlotTrim.mat",      new Color(0.60f, 0.44f, 0.02f), 0.9f, 0.85f);
        var redMat     = Upsert("Assets/Materials/SlotRedTrim.mat",   new Color(0.80f, 0.04f, 0.04f), 0.8f, 0.70f);
        var screenMat  = Upsert("Assets/Materials/SlotScreen.mat",    new Color(0.0f,  0.65f, 0.15f), 0.0f, 0.90f);
        SetEmission(screenMat, new Color(0f, 1f, 0.2f) * 2.5f);
        SetEmission(redMat,    new Color(1f, 0.04f, 0.04f) * 0.6f);
        var stripMat = Upsert("Assets/Materials/LightStrip.mat", new Color(0.75f, 0.82f, 0.50f), 0.0f, 1.0f);
        SetEmission(stripMat, new Color(0.55f, 0.72f, 0.28f) * 2.2f);
        AssetDatabase.SaveAssets();

        // ── New scene ─────────────────────────────────────────────────────
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        RenderSettings.ambientMode      = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight     = new Color(0.010f, 0.014f, 0.003f);
        RenderSettings.fog              = true;
        RenderSettings.fogColor         = new Color(0.012f, 0.018f, 0.004f);
        RenderSettings.fogMode          = FogMode.Linear;
        RenderSettings.fogStartDistance = 3f;
        RenderSettings.fogEndDistance   = 12f;

        // ── Room ──────────────────────────────────────────────────────────
        Box("Floor",     new Vector3(0,  -0.5f,     0), new Vector3(W,   1f,   D),   floorMat);
        Box("Ceiling",   new Vector3(0,  H + 0.5f,  0), new Vector3(W,   1f,   D),   ceilMat);
        Box("WallLeft",  new Vector3(-W/2f-0.25f, H/2f, 0), new Vector3(0.5f, H+1f, D+1f), wallMat);
        Box("WallRight", new Vector3( W/2f+0.25f, H/2f, 0), new Vector3(0.5f, H+1f, D+1f), wallMat);
        Box("WallBack",  new Vector3(0, H/2f, -D/2f-0.25f), new Vector3(W+1f, H+1f, 0.5f), wallMat);
        Box("WallFront", new Vector3(0, H/2f,  D/2f+0.25f), new Vector3(W+1f, H+1f, 0.5f), wallMat);

        // ── 6 Slot Machines (3 pairs) ─────────────────────────────────────
        float[] mZ = { -8f, 0f, 8f };
        int     idx = 0;
        foreach (float z in mZ)
        {
            SlotMachineGrouped("Machine_L" + (int)z, idx++, new Vector3(-3f, 0f, z), true,
                bodyMat, trimMat, screenMat, redMat);
            SlotMachineGrouped("Machine_R" + (int)z, idx++, new Vector3( 3f, 0f, z), false,
                bodyMat, trimMat, screenMat, redMat);
        }

        // ── Wall neons ────────────────────────────────────────────────────
        Color[] neons = {
            new Color(1.0f, 0.06f, 0.06f),
            new Color(0.90f, 0.55f, 0.0f),
            new Color(0.85f, 0.08f, 0.75f),
        };
        float[] nZ = { -10f, -4f, 4f, 10f };
        for (int i = 0; i < nZ.Length; i++)
        {
            WallNeon("NeonL" + i, new Vector3(-W/2f+0.3f, H-0.5f, nZ[i]), neons[i%3],       1.6f, 5f);
            WallNeon("NeonR" + i, new Vector3( W/2f-0.3f, H-0.5f, nZ[i]), neons[(i+1)%3], 1.6f, 5f);
        }

        // ── Ceiling strips ────────────────────────────────────────────────
        float[] cZ = { -10f, -4f, 2f, 8f };
        foreach (float z in cZ)
            CeilStrip("Strip_" + (int)z, new Vector3(0f, H-0.05f, z), stripMat);

        // ── Player ────────────────────────────────────────────────────────
        var playerGO = new GameObject("Player");
        playerGO.tag = "Player";
        playerGO.transform.position = new Vector3(0f, 1f, -12f);

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
        cam.nearClipPlane = 0.08f; cam.farClipPlane = 30f; cam.fieldOfView = 80f;
        camGO.AddComponent<AudioListener>();

        // ── HUD + Game scripts ────────────────────────────────────────────
        BuildHUD();

        // ── Caseritos (animatrónicos enemigos) ────────────────────────────
        // Player starts at (0,1,-12). Machines at z=-8,0,8 x=±3.
        // Place caseritos in the aisles — they activate after 4 seconds.
        SpawnCaserito("Caserito_1", new Vector3( 0.5f, 0f, -5f));
        SpawnCaserito("Caserito_2", new Vector3(-1.5f, 0f,  3f));
        SpawnCaserito("Caserito_3", new Vector3( 1.0f, 0f, 11f));

        // ── Save ──────────────────────────────────────────────────────────
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/GameScene.unity");
        AssetDatabase.SaveAssets();
        Debug.Log("[GameSceneFixer] GameScene reconstruida con sistema de reparación.");
    }

    // ─────────────────────────────────────────────────────────────────────
    // SLOT MACHINE — grouped under a parent with SlotMachine component
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

        // Parts as children
        BoxChild(parent, "_Base",  new Vector3(0f, 0.10f, 0f),    new Vector3(0.90f, 0.20f, 0.58f), body);
        BoxChild(parent, "_Body",  new Vector3(0f, 1.20f, 0f),    new Vector3(0.80f, 2.00f, 0.50f), body);
        BoxChild(parent, "_Cap",   new Vector3(0f, 2.25f, 0f),    new Vector3(0.82f, 0.12f, 0.52f), trim);
        BoxChild(parent, "_TrimF", new Vector3(0f, 1.20f,  0.26f), new Vector3(0.82f, 2.02f, 0.02f), trim);
        BoxChild(parent, "_TrimB", new Vector3(0f, 1.20f, -0.26f), new Vector3(0.82f, 2.02f, 0.02f), trim);
        BoxChild(parent, "_Red",   new Vector3(0f, 0.30f,  0f),   new Vector3(0.81f, 0.08f, 0.51f), red);

        var scrOff = new Vector3(side * 0.41f, 1.3f, 0f);
        BoxChild(parent, "_Screen", scrOff, new Vector3(0.02f, 0.65f, 0.40f), screen);

        // Screen light (green when working)
        var slGO = new GameObject("ScreenLight");
        slGO.transform.SetParent(parent.transform);
        slGO.transform.localPosition = scrOff + new Vector3(side * 0.35f, 0f, 0f);
        var sl = slGO.AddComponent<Light>();
        sl.type = LightType.Point; sl.color = new Color(0f, 0.85f, 0.20f);
        sl.intensity = 0.8f; sl.range = 2.5f;
        sm.screenLight = sl;

        // Neon sign above machine — starts disabled, enabled when broken
        var neonGO = new GameObject("NeonSign");
        neonGO.transform.SetParent(parent.transform);
        neonGO.transform.position = basePos + new Vector3(0f, H - 0.35f, 0f);
        var nl = neonGO.AddComponent<Light>();
        nl.type = LightType.Point; nl.color = new Color(1f, 0.12f, 0f);
        nl.intensity = 4f; nl.range = 3.5f;
        neonGO.SetActive(false);
        sm.neonSign = nl;
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
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var cs = cGO.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight = 0.5f;
        cGO.AddComponent<GraphicRaycaster>();

        // EventSystem
        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // ── Timer panel (top center) ──────────────────────────────────────
        var timerBg = Panel("TimerBg", cGO.transform,
            new Vector2(0.38f, 0.93f), new Vector2(0.62f, 1.0f),
            new Color(0.04f, 0.03f, 0.02f, 0.88f));
        var timerTxt = Label("TimerText", timerBg.transform,
            Vector2.zero, Vector2.one, "05:00",
            new Color(0.92f, 0.80f, 0.08f), 44, FontStyle.Bold, font);
        timerBg.GetComponent<Image>().color = new Color(0.04f, 0.03f, 0.02f, 0.88f);

        // ── Tablet panel (bottom right) ───────────────────────────────────
        var tabletBezel = Panel("Tablet", cGO.transform,
            new Vector2(0.75f, 0.01f), new Vector2(0.99f, 0.32f),
            new Color(0.12f, 0.10f, 0.06f, 0.96f));
        // Inner screen
        var tabletScreen = Panel("TabletScreen", tabletBezel.transform,
            new Vector2(0.04f, 0.03f), new Vector2(0.96f, 0.97f),
            new Color(0.02f, 0.06f, 0.02f, 1f));
        Label("TabletHeader", tabletScreen.transform,
            new Vector2(0f, 0.84f), new Vector2(1f, 1f),
            "TABLETA DE TURNO",
            new Color(0.38f, 0.70f, 0.28f), 13, FontStyle.Bold, font);
        var divider = Panel("Divider", tabletScreen.transform,
            new Vector2(0.02f, 0.82f), new Vector2(0.98f, 0.835f),
            new Color(0.25f, 0.50f, 0.18f, 0.8f));
        // Task list with VerticalLayoutGroup
        var taskGO = new GameObject("TaskList");
        taskGO.transform.SetParent(tabletScreen.transform, false);
        var taskRT = taskGO.AddComponent<RectTransform>();
        taskRT.anchorMin = new Vector2(0.04f, 0.02f);
        taskRT.anchorMax = new Vector2(0.96f, 0.80f);
        taskRT.offsetMin = taskRT.offsetMax = Vector2.zero;
        var vlg = taskGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4f; vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(4, 4, 4, 4);

        // ── Prompt (center bottom) ─────────────────────────────────────────
        var promptGO = Panel("PromptPanel", cGO.transform,
            new Vector2(0.28f, 0.08f), new Vector2(0.72f, 0.16f),
            new Color(0.02f, 0.02f, 0.01f, 0.80f));
        var promptTxt = Label("PromptText", promptGO.transform,
            Vector2.zero, Vector2.one,
            "[E]  Reparar máquina",
            new Color(0.85f, 0.78f, 0.20f), 24, FontStyle.Bold, font);
        promptGO.SetActive(false);

        // ── Repair Panel (full screen overlay) ────────────────────────────
        var repairPanel = new GameObject("RepairPanel");
        repairPanel.transform.SetParent(cGO.transform, false);
        FullScreen(repairPanel);

        // Dark overlay
        var darkener = Panel("Darkener", repairPanel.transform,
            Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.82f));

        // Repair window
        var repairWin = Panel("RepairWindow", repairPanel.transform,
            new Vector2(0.22f, 0.22f), new Vector2(0.78f, 0.78f),
            new Color(0.06f, 0.05f, 0.02f, 0.97f));

        Label("RepairTitle", repairWin.transform,
            new Vector2(0f, 0.82f), new Vector2(1f, 1f),
            "PANEL DE REPARACIÓN",
            new Color(0.92f, 0.80f, 0.08f), 28, FontStyle.Bold, font);

        // Cables row
        var cablesRow = new GameObject("CablesRow");
        cablesRow.transform.SetParent(repairWin.transform, false);
        Anchor(cablesRow, new Vector2(0.05f, 0.45f), new Vector2(0.95f, 0.78f));
        var hlg = cablesRow.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 16f; hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;
        hlg.padding = new RectOffset(8, 8, 4, 4);

        var cableBtns = new Button[3];
        for (int i = 0; i < 3; i++)
        {
            var btnGO = new GameObject("CableBtn" + i);
            btnGO.transform.SetParent(cablesRow.transform, false);
            var btnImg = btnGO.AddComponent<Image>();
            btnImg.color = Color.grey; // set at runtime by RepairMinigame
            var btn = btnGO.AddComponent<Button>();
            var col = btn.colors;
            col.pressedColor = new Color(0.6f, 0.6f, 0.6f); btn.colors = col;

            var lblGO = new GameObject("Label");
            lblGO.transform.SetParent(btnGO.transform, false);
            Anchor(lblGO, Vector2.zero, Vector2.one);
            var lbl = lblGO.AddComponent<Text>();
            lbl.font = font; lbl.fontSize = 20; lbl.fontStyle = FontStyle.Bold;
            lbl.alignment = TextAnchor.MiddleCenter;
            lbl.color = Color.white;
            lbl.text = "---";

            cableBtns[i] = btn;
        }

        var progressTxt = Label("ProgressText", repairWin.transform,
            new Vector2(0f, 0.28f), new Vector2(1f, 0.44f),
            "Conecta en orden: ROJO → AMARILLO → AZUL",
            new Color(0.72f, 0.88f, 0.32f), 16, FontStyle.Normal, font);

        var cancelBtn = MakeButton("CancelBtn", repairWin.transform,
            new Vector2(0.30f, 0.05f), new Vector2(0.70f, 0.22f),
            "CANCELAR  [ESC]",
            new Color(0.30f, 0.08f, 0.04f), new Color(0.6f, 0.5f, 0.3f), font, 18);

        repairPanel.SetActive(false);

        // ── End Panel (full screen) ────────────────────────────────────────
        var endPanel = new GameObject("EndPanel");
        endPanel.transform.SetParent(cGO.transform, false);
        FullScreen(endPanel);

        Panel("Darkener", endPanel.transform, Vector2.zero, Vector2.one,
            new Color(0f, 0f, 0f, 0.88f));

        var endWin = Panel("EndWindow", endPanel.transform,
            new Vector2(0.25f, 0.28f), new Vector2(0.75f, 0.72f),
            new Color(0.05f, 0.04f, 0.02f, 0.97f));

        var endTitle = Label("EndTitle", endWin.transform,
            new Vector2(0f, 0.68f), new Vector2(1f, 1f),
            "TURNO COMPLETADO",
            new Color(0.92f, 0.80f, 0.08f), 40, FontStyle.Bold, font);

        var endSub = Label("EndSub", endWin.transform,
            new Vector2(0.05f, 0.30f), new Vector2(0.95f, 0.65f),
            "...",
            new Color(0.70f, 0.60f, 0.40f), 20, FontStyle.Normal, font);

        var menuBtn = MakeButton("MenuBtn", endWin.transform,
            new Vector2(0.25f, 0.06f), new Vector2(0.75f, 0.24f),
            "VOLVER AL MENÚ",
            new Color(0.40f, 0.12f, 0.04f), new Color(0.92f, 0.80f, 0.20f), font, 22);

        endPanel.SetActive(false);

        // ── Runtime scripts ───────────────────────────────────────────────

        // GameManager
        var gmGO = new GameObject("GameManager");
        var gm = gmGO.AddComponent<GameManager>();
        gm.turnDuration = 300f;

        // RepairMinigame va en HUDCanvas (siempre activo) para que Awake() corra.
        // repairPanel empieza inactivo pero la referencia rm.panel lo activa al abrir.
        var rm = cGO.AddComponent<RepairMinigame>();
        rm.panel = repairPanel;

        // Connect Cancel button
        UnityEditor.Events.UnityEventTools.AddPersistentListener(
            cancelBtn.onClick, rm.Cancel);

        // GameHUD (on canvas)
        var hud = cGO.AddComponent<GameHUD>();
        hud.timerText      = timerTxt;
        hud.taskListParent = taskGO.transform;
        hud.promptPanel    = promptGO;   // panel completo — el hijo Text lo muestra
        hud.promptText     = promptTxt;
        hud.endPanel       = endPanel;
        hud.endTitleText   = endTitle;
        hud.endSubText     = endSub;

        // Connect Menu button
        UnityEditor.Events.UnityEventTools.AddPersistentListener(
            menuBtn.onClick, hud.ReturnToMenu);
    }

    // ─────────────────────────────────────────────────────────────────────
    // CASERITO SPAWN  — builds full visual hierarchy at edit-time so models
    // are visible in Scene view and in Play mode without runtime creation
    // ─────────────────────────────────────────────────────────────────────
    static void SpawnCaserito(string goName, Vector3 position)
    {
        // ── Root ──────────────────────────────────────────────────────────
        var root = new GameObject(goName);
        root.transform.position = position;

        // ── Body capsule ──────────────────────────────────────────────────
        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = new Vector3(0f, 1f, 0f);
        Object.DestroyImmediate(body.GetComponent<CapsuleCollider>());   // root owns colliders

        var bodyMat = new Material(Shader.Find("Standard"));
        bodyMat.color = new Color(0.28f, 0.02f, 0.02f);
        bodyMat.SetFloat("_Metallic",    0.72f);
        bodyMat.SetFloat("_Glossiness",  0.55f);
        body.GetComponent<MeshRenderer>().sharedMaterial = bodyMat;

        // ── Eye sphere ────────────────────────────────────────────────────
        var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eye.name = "Eye";
        eye.transform.SetParent(root.transform, false);
        eye.transform.localPosition = new Vector3(0f, 1.55f, 0.53f);  // past capsule radius (0.5)
        eye.transform.localScale    = new Vector3(0.26f, 0.26f, 0.26f);
        Object.DestroyImmediate(eye.GetComponent<SphereCollider>());

        var eyeMat = new Material(Shader.Find("Standard"));
        eyeMat.color = new Color(0.12f, 0.0f, 0.0f);
        eyeMat.EnableKeyword("_EMISSION");
        eyeMat.SetColor("_EmissionColor", Color.black);   // off at start
        eye.GetComponent<MeshRenderer>().sharedMaterial = eyeMat;

        // ── Root colliders ────────────────────────────────────────────────
        var cap = root.AddComponent<CapsuleCollider>();
        cap.center = new Vector3(0f, 1f, 0f);
        cap.radius = 0.45f;
        cap.height = 2f;

        var trig = root.AddComponent<SphereCollider>();
        trig.center    = new Vector3(0f, 1f, 0f);
        trig.radius    = 1.1f;
        trig.isTrigger = true;

        // ── Rigidbody — physics handles wall collisions ───────────────────
        var rb = root.AddComponent<Rigidbody>();
        rb.mass                   = 80f;
        rb.linearDamping          = 8f;
        rb.angularDamping         = 999f;
        rb.useGravity             = true;
        rb.constraints            = RigidbodyConstraints.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // ── Behavior script ───────────────────────────────────────────────
        // [RequireComponent(Rigidbody)] already satisfied above
        root.AddComponent<Caserito>();
    }

    // ─────────────────────────────────────────────────────────────────────
    // UI HELPERS
    // ─────────────────────────────────────────────────────────────────────

    static GameObject Panel(string name, Transform parent, Vector2 ancMin, Vector2 ancMax, Color color)
    {
        var go  = new GameObject(name);
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
        txt.font = font; txt.fontSize = size; txt.fontStyle = style;
        txt.color = color; txt.text = text;
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
        c.normalColor = bgColor; c.highlightedColor = bgColor * 1.4f;
        c.pressedColor = bgColor * 0.7f; btn.colors = c;

        var lblGO = new GameObject("Label");
        lblGO.transform.SetParent(go.transform, false);
        Anchor(lblGO, Vector2.zero, Vector2.one);
        var txt = lblGO.AddComponent<Text>();
        txt.font = font; txt.fontSize = fontSize; txt.fontStyle = FontStyle.Bold;
        txt.color = textColor; txt.text = label;
        txt.alignment = TextAnchor.MiddleCenter;
        return btn;
    }

    static void Anchor(GameObject go, Vector2 min, Vector2 max)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = min; rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static void FullScreen(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
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
        f.minStableTime = 0.5f; f.maxStableTime = 3.5f;
        f.flickerSpeed = 0.03f; f.maxFlickers = 5;
    }

    static void CeilStrip(string n, Vector3 pos, Material mat)
    {
        var strip = Box(n + "_Geo", pos, new Vector3(0.12f, 0.05f, 1.8f), mat);
        Object.DestroyImmediate(strip.GetComponent<BoxCollider>());

        foreach (float off in new[] { -0.7f, 0.7f })
        {
            var lg = new GameObject(n + "_Light" + (int)(off * 10));
            lg.transform.position = pos - new Vector3(0f, 0.10f, -off);
            var l = lg.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0.60f, 0.72f, 0.30f);
            l.intensity = 2.0f; l.range = 8f;
            var f = lg.AddComponent<LightFlicker>();
            f.normalIntensity = 2.0f; f.dimIntensity = 0.05f;
            f.minStableTime = 0.5f; f.maxStableTime = 5f;
            f.flickerSpeed = 0.04f; f.maxFlickers = 4;
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
