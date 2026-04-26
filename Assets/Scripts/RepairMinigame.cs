using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class RepairMinigame : MonoBehaviour
{
    public static RepairMinigame Instance { get; private set; }

    [Header("UI Root (full-screen container) — assign in Inspector")]
    public GameObject panel;

    [Header("Sounds (optional)")]
    public AudioClip connectSound;
    public AudioClip failSound;
    public AudioClip successSound;

    // Built at runtime
    RectTransform _window;          // the centered sub-window
    RectTransform _leftColumn, _rightColumn;
    Image         _timerBar, _dragLine, _flashOverlay;
    Text          _timerLabel, _statusText;
    GameObject    _completedBanner;
    Canvas        _canvas;
    bool          _uiBuilt;

    // Caseritos read this to narrow their watching FOV during repair
    public static bool IsActive { get; private set; }

    // State
    CasinoMachine _target;
    CablePlug     _dragging;
    CablePlug[]   _plugs;
    CableSocket[] _sockets;
    float _timeLeft;
    bool  _active, _completed;
    float _origFov;

    AudioSource _audio;

    static readonly Color[] CableColors = {
        new Color(0.93f, 0.10f, 0.10f),
        new Color(0.97f, 0.84f, 0.00f),
        new Color(0.10f, 0.42f, 0.97f),
        new Color(0.10f, 0.84f, 0.22f),
        new Color(0.87f, 0.28f, 0.93f),
    };
    static readonly string[] CableNames = { "ROJO", "AMARILLO", "AZUL", "VERDE", "MORADO" };
    const float TIME_LIMIT = 30f;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        _audio = gameObject.AddComponent<AudioSource>();
        _audio.playOnAwake = false;
        panel.SetActive(false);
    }

    // ─── UI CONSTRUCTION ──────────────────────────────────────────────────
    void EnsureUI()
    {
        if (_uiBuilt) return;
        _uiBuilt = true;

        _canvas = panel.GetComponentInParent<Canvas>();

        // Clear old children (old button-based system)
        for (int i = panel.transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(panel.transform.GetChild(i).gameObject);

        // panel acts as full-screen darkener
        var darkener = panel.GetComponent<Image>() ?? panel.AddComponent<Image>();
        darkener.color = new Color(0f, 0f, 0f, 0.80f);
        darkener.raycastTarget = true;

        // Centered window child — all UI lives here
        var winGo = new GameObject("Window");
        winGo.transform.SetParent(panel.transform, false);
        _window = winGo.AddComponent<RectTransform>();
        _window.anchorMin = new Vector2(0.18f, 0.10f);
        _window.anchorMax = new Vector2(0.82f, 0.90f);
        _window.offsetMin = Vector2.zero;
        _window.offsetMax = Vector2.zero;

        // Window background — dark metal
        var winImg = winGo.AddComponent<Image>();
        winImg.color = new Color(0.07f, 0.07f, 0.09f, 0.98f);
        winImg.raycastTarget = true;

        Transform W = _window; // shorthand

        // Gold neon border (behind everything in window)
        var bdr = Img("Border", W, new Color(0.97f, 0.76f, 0.05f));
        SA(bdr.rectTransform, 0,0,1,1, -4,-4,4,4);
        bdr.transform.SetAsFirstSibling();

        // Title
        var title = Txt("Title", W,
            "⚡  REPARACIÓN DE EMERGENCIA  ⚡",
            new Color(1f, 0.12f, 0.08f), 19, TextAnchor.MiddleCenter, FontStyle.Bold);
        SA(title.rectTransform, 0,1,1,1, 8,-50,-8,-5);

        // Timer label
        _timerLabel = Txt("TimerLabel", W, "30",
            new Color(0.97f, 0.84f, 0.06f), 15, TextAnchor.MiddleCenter, FontStyle.Bold);
        SA(_timerLabel.rectTransform, 0,1,1,1, 8,-70,-8,-52);

        // Timer bar background
        var tbg = Img("TimerBg", W, new Color(0.10f, 0.06f, 0.01f));
        SA(tbg.rectTransform, 0,1,1,1, 14,-86,-14,-73);

        // Timer bar fill
        _timerBar = Img("TimerFill", tbg.transform, new Color(0.97f, 0.76f, 0.05f));
        var tfrt = _timerBar.rectTransform;
        tfrt.anchorMin = Vector2.zero; tfrt.anchorMax = Vector2.one;
        tfrt.offsetMin = new Vector2(1,1); tfrt.offsetMax = new Vector2(-1,-1);
        _timerBar.type       = Image.Type.Filled;
        _timerBar.fillMethod = Image.FillMethod.Horizontal;
        _timerBar.fillAmount = 1f;

        // Column headers
        var lHdr = Txt("LHdr", W, "━  CABLES  ━",
            new Color(0.55f,0.55f,0.55f), 11, TextAnchor.MiddleCenter, FontStyle.Normal);
        SA(lHdr.rectTransform, 0,1,0.47f,1, 6,-106,-2,-89);

        var rHdr = Txt("RHdr", W, "━  CONECTORES  ━",
            new Color(0.55f,0.55f,0.55f), 11, TextAnchor.MiddleCenter, FontStyle.Normal);
        SA(rHdr.rectTransform, 0.53f,1,1,1, 2,-106,-6,-89);

        // Left column container
        var lcGo = new GameObject("LeftCol");
        lcGo.transform.SetParent(W, false);
        _leftColumn = lcGo.AddComponent<RectTransform>();
        SA(_leftColumn, 0,0,0.47f,1, 6,10,-2,-109);

        // Right column container
        var rcGo = new GameObject("RightCol");
        rcGo.transform.SetParent(W, false);
        _rightColumn = rcGo.AddComponent<RectTransform>();
        SA(_rightColumn, 0.53f,0,1,1, 2,10,-6,-109);

        // Center divider
        var div = Img("Divider", W, new Color(0.97f,0.76f,0.05f,0.30f));
        SA(div.rectTransform, 0.5f,0,0.5f,1, -1,10,1,-89);

        // Status text
        _statusText = Txt("Status", W, "",
            new Color(0.55f,0.55f,0.55f), 12, TextAnchor.MiddleCenter, FontStyle.Normal);
        SA(_statusText.rectTransform, 0,0,1,0, 8,4,-8,22);

        // Drag line (initially hidden)
        var dlGo = new GameObject("DragLine");
        dlGo.transform.SetParent(W, false);
        _dragLine = dlGo.AddComponent<Image>();
        _dragLine.rectTransform.pivot = new Vector2(0f, 0.5f);
        _dragLine.raycastTarget = false;
        dlGo.SetActive(false);

        // Flash overlay (over entire window)
        _flashOverlay = Img("Flash", W, new Color(1,1,1,0));
        var frt = _flashOverlay.rectTransform;
        frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
        frt.offsetMin = Vector2.zero; frt.offsetMax = Vector2.zero;
        _flashOverlay.raycastTarget = false;

        // Completed banner
        _completedBanner = new GameObject("Banner");
        _completedBanner.transform.SetParent(W, false);
        var crt = _completedBanner.AddComponent<RectTransform>();
        SA(crt, 0.1f,0.3f,0.9f,0.7f, 0,0,0,0);
        _completedBanner.AddComponent<Image>().color = new Color(0.04f,0.04f,0.04f,0.94f);
        var ctxt = Txt("BannerTxt", _completedBanner.transform,
            "✓  ¡REPARADO!", new Color(0.97f,0.79f,0.05f), 24, TextAnchor.MiddleCenter, FontStyle.Bold);
        ctxt.rectTransform.anchorMin = Vector2.zero;
        ctxt.rectTransform.anchorMax = Vector2.one;
        ctxt.rectTransform.offsetMin = Vector2.zero;
        ctxt.rectTransform.offsetMax = Vector2.zero;
        _completedBanner.SetActive(false);

        // ESC hint
        var esc = Txt("Esc", W, "[ESC] Cancelar",
            new Color(0.28f,0.28f,0.28f), 10, TextAnchor.LowerRight, FontStyle.Normal);
        SA(esc.rectTransform, 0.5f,0,1,0, 0,2,-6,14);
    }

    // ─── OPEN ─────────────────────────────────────────────────────────────
    public void Open(CasinoMachine machine)
    {
        EnsureUI();
        _target    = machine;
        _completed = false;
        _dragging  = null;
        _timeLeft  = TIME_LIMIT;
        _active    = true;

        int idx = machine.machineIndex;
        int cableCount; bool hasFake;
        if      (idx <= 2) { cableCount = 3; hasFake = false; }
        else if (idx <= 4) { cableCount = 4; hasFake = false; }
        else               { cableCount = 5; hasFake = true;  }

        _completedBanner.SetActive(false);
        _flashOverlay.color  = new Color(1,1,1,0);
        _dragLine.gameObject.SetActive(false);
        _timerBar.fillAmount = 1f;
        _timerBar.color      = new Color(0.97f,0.76f,0.05f);
        _timerLabel.text     = TIME_LIMIT.ToString("0");
        _timerLabel.color    = new Color(0.97f,0.84f,0.06f);
        _statusText.text     = "Arrastra cada cable a su conector correcto";
        _statusText.color    = new Color(0.50f,0.50f,0.50f);

        BuildCables(cableCount, hasFake);

        // Narrow FOV — caseritos gain ground while you repair
        if (Camera.main != null)
        {
            _origFov = Camera.main.fieldOfView;
            Camera.main.fieldOfView = 45f;
        }
        IsActive = true;

        panel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
        var fpc = FindAnyObjectByType<FirstPersonController>();
        if (fpc) fpc.enabled = false;
    }

    void BuildCables(int total, bool hasFake)
    {
        for (int i = _leftColumn.childCount  - 1; i >= 0; i--) DestroyImmediate(_leftColumn.GetChild(i).gameObject);
        for (int i = _rightColumn.childCount - 1; i >= 0; i--) DestroyImmediate(_rightColumn.GetChild(i).gameObject);

        int realCount = hasFake ? total - 1 : total;

        var pool = new List<int> { 0, 1, 2, 3 };
        Shuffle(pool);
        var chosen = pool.GetRange(0, Mathf.Min(realCount, 4));

        var plugOrder = new List<int>(chosen);
        if (hasFake) plugOrder.Add(4);
        Shuffle(plugOrder);

        var sockOrder = new List<int>(chosen);
        Shuffle(sockOrder);

        _plugs   = new CablePlug[total];
        _sockets = new CableSocket[realCount];

        const float H = 52f, GAP = 7f;

        // ── Plugs (left side) ──
        for (int i = 0; i < total; i++)
        {
            int ci = plugOrder[i];
            var go = new GameObject($"Plug_{i}");
            go.transform.SetParent(_leftColumn, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0,1); rt.anchorMax = new Vector2(1,1);
            rt.pivot     = new Vector2(0.5f,1f);
            float top    = -(i * (H + GAP));
            rt.offsetMin = new Vector2(4, top - H);
            rt.offsetMax = new Vector2(-4, top);

            var img = go.AddComponent<Image>();
            img.color = CableColors[ci];

            var outline = go.AddComponent<Outline>();
            outline.effectColor    = new Color(CableColors[ci].r, CableColors[ci].g, CableColors[ci].b, 0.85f);
            outline.effectDistance = new Vector2(3,3);

            var lbl = Txt("Lbl", go.transform, CableNames[ci],
                Color.white, 13, TextAnchor.MiddleCenter, FontStyle.Bold);
            lbl.rectTransform.anchorMin = Vector2.zero; lbl.rectTransform.anchorMax = Vector2.one;
            lbl.rectTransform.offsetMin = Vector2.zero; lbl.rectTransform.offsetMax = Vector2.zero;

            var dot = Txt("Dot", go.transform, "●",
                new Color(1f,1f,1f,0.35f), 22, TextAnchor.MiddleRight, FontStyle.Normal);
            dot.rectTransform.anchorMin = Vector2.zero; dot.rectTransform.anchorMax = Vector2.one;
            dot.rectTransform.offsetMin = new Vector2(0,0); dot.rectTransform.offsetMax = new Vector2(-5,0);

            var plug = go.AddComponent<CablePlug>();
            plug.colorIndex = ci;
            plug.connected  = false;
            _plugs[i] = plug;
        }

        // ── Sockets (right side) ──
        for (int i = 0; i < realCount; i++)
        {
            int ci = sockOrder[i];
            var go = new GameObject($"Sock_{i}");
            go.transform.SetParent(_rightColumn, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0,1); rt.anchorMax = new Vector2(1,1);
            rt.pivot     = new Vector2(0.5f,1f);
            float top    = -(i * (H + GAP));
            rt.offsetMin = new Vector2(4, top - H);
            rt.offsetMax = new Vector2(-4, top);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.10f,0.07f,0.02f);

            var outline = go.AddComponent<Outline>();
            outline.effectColor    = new Color(CableColors[ci].r*0.55f, CableColors[ci].g*0.55f, CableColors[ci].b*0.55f, 0.55f);
            outline.effectDistance = new Vector2(2,2);

            var lbl = Txt("Lbl", go.transform, "○",
                new Color(CableColors[ci].r*0.65f, CableColors[ci].g*0.65f, CableColors[ci].b*0.65f),
                24, TextAnchor.MiddleCenter, FontStyle.Normal);
            lbl.rectTransform.anchorMin = Vector2.zero; lbl.rectTransform.anchorMax = Vector2.one;
            lbl.rectTransform.offsetMin = Vector2.zero; lbl.rectTransform.offsetMax = Vector2.zero;

            var sock = go.AddComponent<CableSocket>();
            sock.expectedColorIndex = ci;
            sock.occupied           = false;
            _sockets[i] = sock;
        }
    }

    // ─── DRAG API (called by CablePlug) ───────────────────────────────────
    public void OnPlugBeginDrag(CablePlug plug, PointerEventData e)
    {
        _dragging = plug;
        _dragLine.gameObject.SetActive(true);
        UpdateDragLine(e.position, e.pressEventCamera);
    }

    public void OnPlugDrag(CablePlug plug, PointerEventData e)
    {
        if (_dragging != plug) return;
        UpdateDragLine(e.position, e.pressEventCamera);
        if (_sockets == null) return;
        foreach (var s in _sockets)
        {
            bool hit = RectTransformUtility.RectangleContainsScreenPoint(
                s.GetComponent<RectTransform>(), e.position, e.pressEventCamera);
            s.SetHovered(hit);
        }
    }

    public void OnPlugEndDrag(CablePlug plug, CableSocket target)
    {
        _dragging = null;
        _dragLine.gameObject.SetActive(false);
        if (_sockets != null)
            foreach (var s in _sockets) s.SetHovered(false);

        if (target == null) return;

        if (target.expectedColorIndex == plug.colorIndex)
        {
            plug.connected = true;
            plug.GetComponent<Image>().color = CableColors[plug.colorIndex] * new Color(0.75f,0.75f,0.75f,1f);
            target.MarkOccupied(CableColors[plug.colorIndex]);
            Play(connectSound);
            StartCoroutine(Flash(new Color(0.97f,0.82f,0.08f,0.42f)));
            _statusText.text  = "✓  ¡Cable conectado!";
            _statusText.color = new Color(0.97f,0.82f,0.08f);
            CheckDone();
        }
        else
        {
            Play(failSound);
            StartCoroutine(Flash(new Color(1f,0.05f,0.05f,0.42f)));
            StartCoroutine(Shake(0.25f,5f));
            _statusText.text  = "✗  Cable incorrecto — inténtalo de nuevo";
            _statusText.color = new Color(1f,0.15f,0.10f);
        }
    }

    void UpdateDragLine(Vector2 screenPos, Camera eventCam)
    {
        if (_dragging == null) return;
        Camera cam = (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? _canvas.worldCamera : null;

        Vector2 plugScreen = RectTransformUtility.WorldToScreenPoint(cam, _dragging.GetComponent<RectTransform>().position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_window, plugScreen, cam, out Vector2 lStart);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_window, screenPos,  cam, out Vector2 lEnd);

        var dir = lEnd - lStart;
        _dragLine.rectTransform.localPosition    = new Vector3(lStart.x, lStart.y, 0f);
        _dragLine.rectTransform.sizeDelta        = new Vector2(dir.magnitude, 7f);
        _dragLine.rectTransform.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        _dragLine.color = new Color(
            CableColors[_dragging.colorIndex].r,
            CableColors[_dragging.colorIndex].g,
            CableColors[_dragging.colorIndex].b, 0.92f);
    }

    void CheckDone()
    {
        if (_sockets == null) return;
        foreach (var s in _sockets) if (!s.occupied) return;
        _completed = true;
        _active    = false;
        StartCoroutine(CompletionSequence());
    }

    IEnumerator CompletionSequence()
    {
        _completedBanner.SetActive(true);
        Play(successSound);
        for (int i = 0; i < 5; i++)
        {
            yield return StartCoroutine(Flash(new Color(0.97f,0.82f,0.08f,0.55f)));
            yield return new WaitForSecondsRealtime(0.13f);
        }
        yield return new WaitForSecondsRealtime(1.0f);
        DoFinish();
    }

    void DoFinish()
    {
        IsActive = false;
        RestoreFov();
        panel.SetActive(false);
        _target.Repair();
        GameManager.Instance.NotifyRepaired(_target);
        RestorePlayer();
    }

    // ─── TIMER ────────────────────────────────────────────────────────────
    void Update()
    {
        if (panel.activeSelf && !_completed)
        {
            // ESC or Q — quick exit to face the caseritos
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Q))
                Cancel();
        }

        if (!_active || _completed) return;

        _timeLeft -= Time.deltaTime;
        float ratio = Mathf.Clamp01(_timeLeft / TIME_LIMIT);

        _timerBar.fillAmount = ratio;
        _timerLabel.text     = Mathf.CeilToInt(Mathf.Max(0f, _timeLeft)).ToString();
        _timerBar.color      = ratio > 0.5f
            ? Color.Lerp(new Color(0.97f,0.76f,0.05f), new Color(0.10f,0.92f,0.22f), (ratio-0.5f)*2f)
            : Color.Lerp(new Color(1f,0.10f,0.05f),   new Color(0.97f,0.76f,0.05f), ratio*2f);
        _timerLabel.color    = ratio < 0.33f ? new Color(1f,0.12f,0.08f) : new Color(0.97f,0.84f,0.06f);

        if (_timeLeft <= 0f) StartCoroutine(OnTimeUp());
    }

    IEnumerator OnTimeUp()
    {
        _active = false;
        IsActive = false;
        RestoreFov();
        _statusText.text  = "⚠  ¡TIEMPO AGOTADO!";
        _statusText.color = new Color(1f,0.05f,0.05f);
        StartCoroutine(Flash(new Color(1f,0.05f,0.05f,0.65f)));
        StartCoroutine(Shake(0.50f,10f));
        Play(failSound);
        yield return new WaitForSecondsRealtime(1.5f);
        panel.SetActive(false);
        _target.SetMoreBroken();
        RestorePlayer();
    }

    // ─── CANCEL ───────────────────────────────────────────────────────────
    public void Cancel()
    {
        _active   = false;
        IsActive  = false;
        RestoreFov();
        _dragging = null;
        if (_dragLine) _dragLine.gameObject.SetActive(false);
        panel.SetActive(false);
        RestorePlayer();
    }

    void RestoreFov()
    {
        if (Camera.main != null && _origFov > 0f)
            Camera.main.fieldOfView = _origFov;
    }

    void RestorePlayer()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
        var fpc = FindAnyObjectByType<FirstPersonController>();
        if (fpc) fpc.enabled = true;
    }

    // ─── EFFECTS ──────────────────────────────────────────────────────────
    IEnumerator Flash(Color col)
    {
        _flashOverlay.color = col;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime * 6f)
        {
            _flashOverlay.color = new Color(col.r, col.g, col.b, Mathf.Lerp(col.a, 0f, t));
            yield return null;
        }
        _flashOverlay.color = new Color(col.r, col.g, col.b, 0f);
    }

    IEnumerator Shake(float dur, float mag)
    {
        var cam = Camera.main?.transform;
        if (!cam) yield break;
        var orig = cam.localPosition;
        for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
        {
            float s = mag * (1f - t / dur) * 0.01f;
            cam.localPosition = orig + new Vector3(Random.Range(-s,s), Random.Range(-s,s), 0f);
            yield return null;
        }
        cam.localPosition = orig;
    }

    void Play(AudioClip clip) { if (clip && _audio) _audio.PlayOneShot(clip); }

    // ─── HELPERS ──────────────────────────────────────────────────────────
    static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    // SetAnchors: anchorMin(ax,ay) anchorMax(bx,by) offsetMin(ol,ob) offsetMax(or,ot)
    static void SA(RectTransform rt, float ax, float ay, float bx, float by,
                   float ol, float ob, float or_, float ot)
    {
        rt.anchorMin = new Vector2(ax, ay);
        rt.anchorMax = new Vector2(bx, by);
        rt.offsetMin = new Vector2(ol, ob);
        rt.offsetMax = new Vector2(or_, ot);
    }

    Image Img(string name, Transform parent, Color col)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = col;
        img.raycastTarget = false;
        return img;
    }

    Text Txt(string name, Transform parent, string content,
             Color col, int size, TextAnchor anchor, FontStyle style)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text      = content;
        t.color     = col;
        t.fontSize  = size;
        t.alignment = anchor;
        t.fontStyle = style;
        t.raycastTarget = false;
        return t;
    }
}
