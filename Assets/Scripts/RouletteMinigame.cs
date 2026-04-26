using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Puzzle: calibrate 3 dials (0-9) to match the target combination.
public class RouletteMinigame : MonoBehaviour
{
    public static RouletteMinigame Instance { get; private set; }
    public static bool IsActive { get; private set; }

    const int   DIALS      = 3;
    const float TIME_LIMIT = 30f;

    // ── Runtime state ─────────────────────────────────────────────────────
    Canvas       _canvas;
    GameObject   _panel;
    Text         _timerLabel;
    Image        _timerBar;
    Text         _statusText;
    Text[]       _currentLabels = new Text[DIALS];
    Text[]       _targetLabels  = new Text[DIALS];
    Image        _flashOverlay;
    bool         _uiBuilt;

    int[]  _target  = new int[DIALS];
    int[]  _current = new int[DIALS];
    float  _timeLeft;
    bool   _active, _completed;
    float  _origFov;

    CasinoMachine _machine;
    AudioSource   _audio;
    AudioClip     _clickClip, _errorClip, _successClip;

    // ── Lifecycle ─────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        _audio = gameObject.AddComponent<AudioSource>();
        _audio.playOnAwake = false;
        _clickClip   = MakeClick();
        _errorClip   = MakeError();
        _successClip = MakeSuccess();

        BuildCanvas();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    // ── Canvas + Panel ─────────────────────────────────────────────────────
    void BuildCanvas()
    {
        var cGO = new GameObject("RouletteCanvas");
        cGO.transform.SetParent(transform);
        _canvas = cGO.AddComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 50;
        var scaler = cGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        cGO.AddComponent<GraphicRaycaster>();
    }

    // ── Open ──────────────────────────────────────────────────────────────
    public void Open(CasinoMachine machine)
    {
        _machine   = machine;
        _timeLeft  = TIME_LIMIT;
        _active    = true;
        _completed = false;

        // Random target & different starting values
        for (int i = 0; i < DIALS; i++)
        {
            _target[i]  = Random.Range(0, 10);
            _current[i] = (_target[i] + Random.Range(1, 9)) % 10;
        }

        if (!_uiBuilt) BuildUI();
        RefreshDials();
        UpdateStatus("Ajusta los diales para alcanzar el objetivo", new Color(0.55f, 0.55f, 0.55f));
        _timerBar.fillAmount = 1f;
        _timerBar.color      = new Color(0.97f, 0.76f, 0.05f);
        _timerLabel.text     = TIME_LIMIT.ToString("0");
        _flashOverlay.color  = new Color(1, 1, 1, 0);

        if (Camera.main != null) { _origFov = Camera.main.fieldOfView; Camera.main.fieldOfView = 45f; }
        IsActive = true;
        _panel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
        FindAnyObjectByType<FirstPersonController>()?.gameObject.SetActive(false);
    }

    // ── Update ────────────────────────────────────────────────────────────
    void Update()
    {
        if (!_panel || !_panel.activeSelf) return;

        if ((Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Q)) && !_completed)
            Cancel();

        if (!_active || _completed) return;

        _timeLeft -= Time.deltaTime;
        float ratio = Mathf.Clamp01(_timeLeft / TIME_LIMIT);
        _timerBar.fillAmount = ratio;
        _timerBar.color      = ratio > 0.5f
            ? Color.Lerp(new Color(0.97f, 0.76f, 0.05f), new Color(0.10f, 0.92f, 0.22f), (ratio - 0.5f) * 2f)
            : Color.Lerp(new Color(1f, 0.10f, 0.05f), new Color(0.97f, 0.76f, 0.05f), ratio * 2f);
        _timerLabel.text  = Mathf.CeilToInt(Mathf.Max(0, _timeLeft)).ToString();
        _timerLabel.color = ratio < 0.33f ? new Color(1f, 0.12f, 0.08f) : new Color(0.97f, 0.84f, 0.06f);

        if (_timeLeft <= 0f) StartCoroutine(OnTimeUp());
    }

    // ── Dial interaction ──────────────────────────────────────────────────
    void Increment(int dial)
    {
        if (!_active || _completed) return;
        _current[dial] = (_current[dial] + 1) % 10;
        Play(_clickClip);
        RefreshDials();
        CheckWin();
    }

    void Decrement(int dial)
    {
        if (!_active || _completed) return;
        _current[dial] = (_current[dial] + 9) % 10;
        Play(_clickClip);
        RefreshDials();
        CheckWin();
    }

    void RefreshDials()
    {
        for (int i = 0; i < DIALS; i++)
        {
            if (_currentLabels[i]) _currentLabels[i].text = _current[i].ToString();
            if (_targetLabels[i])  _targetLabels[i].text  = _target[i].ToString();

            bool matched = _current[i] == _target[i];
            if (_currentLabels[i])
                _currentLabels[i].color = matched
                    ? new Color(0.10f, 0.92f, 0.22f)
                    : new Color(0.95f, 0.92f, 0.85f);
        }
    }

    void CheckWin()
    {
        for (int i = 0; i < DIALS; i++)
            if (_current[i] != _target[i]) return;

        _completed = true;
        _active    = false;
        StartCoroutine(WinSequence());
    }

    IEnumerator WinSequence()
    {
        UpdateStatus("✓  ¡Calibración completa!", new Color(0.10f, 0.92f, 0.22f));
        Play(_successClip);
        for (int i = 0; i < 4; i++)
        {
            yield return StartCoroutine(Flash(new Color(0.10f, 0.92f, 0.22f, 0.5f)));
            yield return new WaitForSecondsRealtime(0.12f);
        }
        yield return new WaitForSecondsRealtime(0.8f);
        Finish(success: true);
    }

    IEnumerator OnTimeUp()
    {
        _active = false;
        IsActive = false;
        RestoreFov();
        UpdateStatus("⚠  ¡TIEMPO AGOTADO!", new Color(1f, 0.05f, 0.05f));
        StartCoroutine(Flash(new Color(1f, 0.05f, 0.05f, 0.65f)));
        Play(_errorClip);
        yield return new WaitForSecondsRealtime(1.5f);
        _panel.SetActive(false);
        _machine.SetMoreBroken();
        RestorePlayer();
    }

    void Finish(bool success)
    {
        IsActive = false;
        RestoreFov();
        _panel.SetActive(false);
        if (success)
        {
            _machine.Repair();
            GameManager.Instance.NotifyRepaired(_machine);
        }
        RestorePlayer();
    }

    public void Cancel()
    {
        _active  = false;
        IsActive = false;
        RestoreFov();
        _panel.SetActive(false);
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
        FindAnyObjectByType<FirstPersonController>()?.gameObject.SetActive(true);
    }

    // ── UI construction ───────────────────────────────────────────────────
    void BuildUI()
    {
        _uiBuilt = true;

        _panel = new GameObject("RoulettePanel");
        _panel.transform.SetParent(_canvas.transform, false);

        var rootRt  = _panel.AddComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero; rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = rootRt.offsetMax = Vector2.zero;

        var darkener = _panel.AddComponent<Image>();
        darkener.color = new Color(0f, 0f, 0f, 0.82f);
        darkener.raycastTarget = true;

        // Window
        var win = Img("Win", _panel.transform, new Color(0.06f, 0.05f, 0.02f, 0.98f));
        SA(win.rectTransform, 0.18f, 0.12f, 0.82f, 0.88f, 0, 0, 0, 0);
        win.raycastTarget = true;
        Transform W = win.transform;

        // Gold border
        var bdr = Img("Bdr", W, new Color(0.88f, 0.66f, 0.04f, 0.6f));
        SA(bdr.rectTransform, 0, 0, 1, 1, -3, -3, 3, 3);
        bdr.transform.SetAsFirstSibling();

        // Title
        Txt("Title", W, "⚙  CALIBRACIÓN DE RULETA  ⚙",
            new Color(0.97f, 0.76f, 0.05f), 18, TextAnchor.MiddleCenter, FontStyle.Bold,
            0, 1, 1, 1, 8, -48, -8, -4);

        // Timer label + bar
        _timerLabel = Txt("TimerLbl", W, "30", new Color(0.97f, 0.84f, 0.06f), 14,
            TextAnchor.MiddleCenter, FontStyle.Bold, 0, 1, 1, 1, 8, -68, -8, -50);

        var tbg = Img("TBg", W, new Color(0.10f, 0.06f, 0.01f));
        SA(tbg.rectTransform, 0, 1, 1, 1, 14, -84, -14, -71);
        _timerBar = Img("TFill", tbg.transform, new Color(0.97f, 0.76f, 0.05f));
        _timerBar.type       = Image.Type.Filled;
        _timerBar.fillMethod = Image.FillMethod.Horizontal;
        _timerBar.fillAmount = 1f;
        _timerBar.rectTransform.anchorMin = Vector2.zero;
        _timerBar.rectTransform.anchorMax = Vector2.one;
        _timerBar.rectTransform.offsetMin = new Vector2(1, 1);
        _timerBar.rectTransform.offsetMax = new Vector2(-1, -1);

        // "OBJETIVO:" header
        Txt("ObjHdr", W, "OBJETIVO", new Color(0.50f, 0.50f, 0.50f), 11,
            TextAnchor.MiddleCenter, FontStyle.Normal, 0, 1, 1, 1, 8, -110, -8, -87);

        // Target dial boxes
        for (int i = 0; i < DIALS; i++)
        {
            float cx   = (i - 1) * 0.28f + 0.5f;
            float half = 0.10f;
            var tbox = Img($"Tbox{i}", W, new Color(0.04f, 0.08f, 0.04f));
            SA(tbox.rectTransform, cx - half, 1, cx + half, 1, 0, -148, 0, -112);
            _targetLabels[i] = Txt($"Tlbl{i}", tbox.transform, "0",
                new Color(0.10f, 0.92f, 0.22f), 22, TextAnchor.MiddleCenter, FontStyle.Bold,
                0, 0, 1, 1, 0, 0, 0, 0);
        }

        // Divider
        var div = Img("Div", W, new Color(0.88f, 0.66f, 0.04f, 0.25f));
        SA(div.rectTransform, 0.08f, 1, 0.92f, 1, 0, -156, 0, -149);

        // "ACTUAL:" header
        Txt("CurHdr", W, "ACTUAL", new Color(0.50f, 0.50f, 0.50f), 11,
            TextAnchor.MiddleCenter, FontStyle.Normal, 0, 1, 1, 1, 8, -182, -8, -159);

        // Current dial groups (▲ number ▼)
        for (int i = 0; i < DIALS; i++)
        {
            int idx = i;  // capture for lambda
            float cx   = (i - 1) * 0.28f + 0.5f;
            float half = 0.10f;

            // Up button
            var up = Btn($"Up{i}", W, "▲", new Color(0.15f, 0.12f, 0.04f),
                new Color(0.88f, 0.76f, 0.06f), 18,
                cx - half, 1, cx + half, 1, 0, -218, 0, -184,
                () => Increment(idx));

            // Current value box
            var cbox = Img($"Cbox{i}", W, new Color(0.10f, 0.08f, 0.04f));
            SA(cbox.rectTransform, cx - half, 1, cx + half, 1, 2, -258, -2, -222);
            _currentLabels[i] = Txt($"Clbl{i}", cbox.transform, "0",
                new Color(0.95f, 0.92f, 0.85f), 28, TextAnchor.MiddleCenter, FontStyle.Bold,
                0, 0, 1, 1, 0, 0, 0, 0);

            // Down button
            var dn = Btn($"Dn{i}", W, "▼", new Color(0.15f, 0.12f, 0.04f),
                new Color(0.88f, 0.76f, 0.06f), 18,
                cx - half, 1, cx + half, 1, 0, -296, 0, -262,
                () => Decrement(idx));
        }

        // Status text
        _statusText = Txt("Status", W, "",
            new Color(0.55f, 0.55f, 0.55f), 12, TextAnchor.MiddleCenter, FontStyle.Normal,
            0, 0, 1, 0, 8, 22, -8, 42);

        // ESC hint
        Txt("Esc", W, "[ESC] Cancelar", new Color(0.28f, 0.28f, 0.28f), 10,
            TextAnchor.LowerRight, FontStyle.Normal, 0.5f, 0, 1, 0, 0, 2, -6, 14);

        // Flash overlay
        _flashOverlay = Img("Flash", W, new Color(1, 1, 1, 0));
        _flashOverlay.rectTransform.anchorMin = Vector2.zero;
        _flashOverlay.rectTransform.anchorMax = Vector2.one;
        _flashOverlay.rectTransform.offsetMin = Vector2.zero;
        _flashOverlay.rectTransform.offsetMax = Vector2.zero;
        _flashOverlay.raycastTarget = false;

        _panel.SetActive(false);
    }

    void UpdateStatus(string msg, Color col)
    {
        if (_statusText) { _statusText.text = msg; _statusText.color = col; }
    }

    IEnumerator Flash(Color col)
    {
        _flashOverlay.color = col;
        for (float t = 0; t < 1f; t += Time.unscaledDeltaTime * 6f)
        {
            _flashOverlay.color = new Color(col.r, col.g, col.b, Mathf.Lerp(col.a, 0, t));
            yield return null;
        }
        _flashOverlay.color = new Color(col.r, col.g, col.b, 0);
    }

    // ── Audio ─────────────────────────────────────────────────────────────
    static AudioClip MakeClick()
    {
        const int sr = 44100; int n = (int)(sr * 0.08f);
        float[] d = new float[n];
        for (int i = 0; i < n; i++) { float t = (float)i/sr; d[i] = Mathf.Sin(Mathf.PI*2*1200*t)*Mathf.Exp(-t*80)*0.7f; }
        var c = AudioClip.Create("c",n,1,sr,false); c.SetData(d,0); return c;
    }

    static AudioClip MakeError()
    {
        const int sr = 44100; int n = (int)(sr * 0.35f);
        float[] d = new float[n];
        var rng = new System.Random(3);
        for (int i = 0; i < n; i++) { float t = (float)i/sr; d[i] = Mathf.Clamp(Mathf.Sin(Mathf.PI*2*180*t)*Mathf.Exp(-t*6)*0.7f + (float)(rng.NextDouble()*2-1)*0.2f*Mathf.Exp(-t*8),-1,1); }
        var c = AudioClip.Create("e",n,1,sr,false); c.SetData(d,0); return c;
    }

    static AudioClip MakeSuccess()
    {
        const int sr = 44100; float dur = 0.6f; int n = (int)(sr*dur);
        float[] d = new float[n];
        float[] freqs = {523.25f,659.25f,783.99f};
        foreach (var f in freqs)
            for (int i=0;i<n;i++) { float t=(float)i/sr; d[i]=Mathf.Clamp(d[i]+Mathf.Sin(Mathf.PI*2*f*t)*Mathf.Exp(-t*3)*0.35f,-1,1); }
        var c = AudioClip.Create("s",n,1,sr,false); c.SetData(d,0); return c;
    }

    void Play(AudioClip clip) { if (clip && _audio) _audio.PlayOneShot(clip); }

    // ── UI helpers ────────────────────────────────────────────────────────
    static Image Img(string name, Transform parent, Color col)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>(); img.color = col; img.raycastTarget = false;
        return img;
    }

    static Text Txt(string name, Transform parent, string text, Color col, int size,
        TextAnchor anchor, FontStyle style,
        float ax, float ay, float bx, float by, float ol, float ob, float or_, float ot)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        SA(rt, ax, ay, bx, by, ol, ob, or_, ot);
        var t  = go.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text = text; t.color = col; t.fontSize = size; t.alignment = anchor; t.fontStyle = style;
        t.raycastTarget = false;
        return t;
    }

    static Text Txt(string name, Transform parent, string text, Color col, int size,
        TextAnchor anchor, FontStyle style)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var t  = go.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text = text; t.color = col; t.fontSize = size; t.alignment = anchor; t.fontStyle = style;
        t.raycastTarget = false;
        return t;
    }

    static void SA(RectTransform rt, float ax, float ay, float bx, float by,
                   float ol, float ob, float or_, float ot)
    {
        rt.anchorMin = new Vector2(ax, ay); rt.anchorMax = new Vector2(bx, by);
        rt.offsetMin = new Vector2(ol, ob); rt.offsetMax = new Vector2(or_, ot);
    }

    static UnityEngine.UI.Button Btn(string name, Transform parent, string label,
        Color bg, Color tc, int fs,
        float ax, float ay, float bx, float by, float ol, float ob, float or_, float ot,
        System.Action onClick)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>(); SA(rt, ax, ay, bx, by, ol, ob, or_, ot);
        var img = go.AddComponent<Image>(); img.color = bg;
        var btn = go.AddComponent<UnityEngine.UI.Button>();
        var cols = btn.colors;
        cols.normalColor = bg; cols.highlightedColor = bg * new Color(1.5f,1.5f,1.5f,1); btn.colors = cols;
        btn.onClick.AddListener(() => onClick?.Invoke());
        var lgo = new GameObject("L"); lgo.transform.SetParent(go.transform, false);
        var lrt = lgo.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        var lt = lgo.AddComponent<Text>();
        lt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        lt.text = label; lt.color = tc; lt.fontSize = fs; lt.alignment = TextAnchor.MiddleCenter; lt.fontStyle = FontStyle.Bold;
        lt.raycastTarget = false;
        return btn;
    }
}
