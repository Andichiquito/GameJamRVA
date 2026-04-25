using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuBuilder
{
    [MenuItem("Casino/Rebuild MainMenu Scene")]
    public static void Execute()
    {
        if (EditorApplication.isPlaying)
            EditorApplication.isPlaying = false;

        // Abrir la escena MainMenu existente (conserva camera y light)
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);

        // Eliminar objetos previos de UI si existen
        var oldCanvas = GameObject.Find("Canvas");
        if (oldCanvas != null) Object.DestroyImmediate(oldCanvas);
        var oldMgr = GameObject.Find("MenuManager");
        if (oldMgr != null) Object.DestroyImmediate(oldMgr);
        var oldES = GameObject.Find("EventSystem");
        if (oldES != null) Object.DestroyImmediate(oldES);

        // Configurar cámara
        var camGO = GameObject.Find("Main Camera");
        if (camGO != null)
        {
            var cam = camGO.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.01f, 0.012f, 0.005f); // negro verdoso
        }

        // Apagar Directional Light (menú debe ser muy oscuro)
        var dl = GameObject.Find("Directional Light");
        if (dl != null) dl.SetActive(false);

        // ── EventSystem ──────────────────────────────────────────────────
        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // ── Canvas ───────────────────────────────────────────────────────
        var canvasGO = new GameObject("Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;
        var cs = canvasGO.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Fondo negro total ─────────────────────────────────────────────
        var bg = MakeImage("Background", canvasGO.transform,
            Vector2.zero, new Vector2(1920, 1080), new Color(0.01f, 0.012f, 0.005f, 1f));
        SetAnchors(bg, Vector2.zero, Vector2.one, Vector2.zero);

        // ── Panel central ─────────────────────────────────────────────────
        var panelGO = new GameObject("Panel");
        panelGO.transform.SetParent(canvasGO.transform, false);
        var panelRT = panelGO.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.3f, 0.1f);
        panelRT.anchorMax = new Vector2(0.7f, 0.9f);
        panelRT.offsetMin = panelRT.offsetMax = Vector2.zero;
        var panelImg = panelGO.AddComponent<Image>();
        panelImg.color = new Color(0.02f, 0.018f, 0.008f, 0.92f);

        // ── Línea roja superior ───────────────────────────────────────────
        MakeDivider("TopLine", panelGO.transform,
            new Vector2(0f, 0.92f), new Vector2(1f, 0.93f), new Color(0.85f, 0.05f, 0.05f));

        // ── Título ────────────────────────────────────────────────────────
        MakeLabel("Title", panelGO.transform,
            new Vector2(0.05f, 0.70f), new Vector2(0.95f, 0.90f),
            "CASINO\nNOCTURNO",
            new Color(0.92f, 0.80f, 0.08f), 72, FontStyle.Bold);

        // ── Subtítulo ─────────────────────────────────────────────────────
        MakeLabel("Subtitle", panelGO.transform,
            new Vector2(0.05f, 0.58f), new Vector2(0.95f, 0.70f),
            "UNA NOCHE QUE NUNCA TERMINA",
            new Color(0.55f, 0.10f, 0.10f), 22, FontStyle.Italic);

        // ── Línea divisoria ───────────────────────────────────────────────
        MakeDivider("MidLine", panelGO.transform,
            new Vector2(0.1f, 0.555f), new Vector2(0.9f, 0.562f), new Color(0.35f, 0.28f, 0.05f));

        // ── Botón INICIAR ─────────────────────────────────────────────────
        var btnStart = MakeButton("BtnStart", panelGO.transform,
            new Vector2(0.15f, 0.35f), new Vector2(0.85f, 0.50f),
            "INICIAR TURNO",
            new Color(0.55f, 0.04f, 0.04f), new Color(0.95f, 0.88f, 0.65f));

        // ── Botón SALIR ───────────────────────────────────────────────────
        var btnQuit = MakeButton("BtnQuit", panelGO.transform,
            new Vector2(0.25f, 0.18f), new Vector2(0.75f, 0.30f),
            "ABANDONAR",
            new Color(0.10f, 0.08f, 0.04f), new Color(0.40f, 0.35f, 0.20f));

        // ── Línea roja inferior ───────────────────────────────────────────
        MakeDivider("BotLine", panelGO.transform,
            new Vector2(0f, 0.07f), new Vector2(1f, 0.08f), new Color(0.85f, 0.05f, 0.05f));

        // ── Crédito ───────────────────────────────────────────────────────
        MakeLabel("Credit", panelGO.transform,
            new Vector2(0.05f, 0.01f), new Vector2(0.95f, 0.07f),
            "GameJam RVA  •  2026",
            new Color(0.22f, 0.18f, 0.08f), 16, FontStyle.Normal);

        // ── MenuManager ──────────────────────────────────────────────────
        var mgrGO = new GameObject("MenuManager");
        var mgr = mgrGO.AddComponent<MainMenuManager>();

        // Persistent listeners — se serializan correctamente en la escena
        var startBtn = btnStart.GetComponent<Button>();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(
            startBtn.onClick,
            mgr.OnStartGame);

        var quitBtn = btnQuit.GetComponent<Button>();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(
            quitBtn.onClick,
            mgr.OnQuitGame);

        // ── Guardar ───────────────────────────────────────────────────────
        EditorSceneManager.SaveScene(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene(),
            "Assets/Scenes/MainMenu.unity");

        Debug.Log("[MainMenuBuilder] MainMenu reconstruida con UI completa.");
    }

    // ─────────────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────────────

    static GameObject MakeImage(string name, Transform parent, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var img = go.AddComponent<Image>();
        img.color = color;
        return go;
    }

    static void SetAnchors(GameObject go, Vector2 min, Vector2 max, Vector2 pivot)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = min; rt.anchorMax = max;
        rt.pivot = pivot.magnitude > 0 ? pivot : new Vector2(0.5f, 0.5f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static void MakeDivider(string name, Transform parent, Vector2 ancMin, Vector2 ancMax, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = ancMin; rt.anchorMax = ancMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.AddComponent<Image>().color = color;
    }

    static void MakeLabel(string name, Transform parent,
        Vector2 ancMin, Vector2 ancMax, string text, Color color, int size, FontStyle style)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = ancMin; rt.anchorMax = ancMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var txt = go.AddComponent<Text>();
        txt.text = text;
        txt.color = color;
        txt.fontSize = size;
        txt.fontStyle = style;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.resizeTextForBestFit = false;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    static GameObject MakeButton(string name, Transform parent,
        Vector2 ancMin, Vector2 ancMax, string label, Color bgColor, Color textColor)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = ancMin; rt.anchorMax = ancMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var img = go.AddComponent<Image>();
        img.color = bgColor;

        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor      = bgColor;
        colors.highlightedColor = bgColor * 1.5f;
        colors.pressedColor     = bgColor * 0.7f;
        colors.selectedColor    = bgColor;
        btn.colors = colors;

        // Texto del botón
        var txtGO = new GameObject("Label");
        txtGO.transform.SetParent(go.transform, false);
        var txtRT = txtGO.AddComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = txtRT.offsetMax = Vector2.zero;
        var txt = txtGO.AddComponent<Text>();
        txt.text = label;
        txt.color = textColor;
        txt.fontSize = 32;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        return go;
    }
}
