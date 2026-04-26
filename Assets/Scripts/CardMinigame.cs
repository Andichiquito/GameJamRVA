using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Puzzle: click 5 shuffled cards in ascending order (1 → 2 → 3 → 4 → 5).
// Wrong pick flashes red and resets progress. 25 s timer.
public class CardMinigame : MonoBehaviour
{
    public static CardMinigame Instance { get; private set; }
    public static bool IsActive { get; private set; }

    const float TIME_LIMIT = 25f;
    const int   CARD_COUNT = 5;

    Canvas      _canvas;
    GameObject  _root;
    Text        _timerLabel, _statusText;
    Image       _timerBar, _flashOverlay;
    GameObject  _completedBanner;

    CasinoMachine _target;
    float _timeLeft;
    bool  _active, _completed;
    float _origFov;

    int    _nextExpected;
    int[]  _cardValues;
    bool[] _cardDone;
    Image[] _cardBgs;
    Text[]  _cardLabels;

    AudioSource _audio;

    static readonly Color ColDefault = new Color(0.10f, 0.22f, 0.45f);
    static readonly Color ColDone    = new Color(0.06f, 0.38f, 0.10f);
    static readonly Color ColWrong   = new Color(0.60f, 0.04f, 0.04f);

    // ── Lifecycle ──────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        _audio = gameObject.AddComponent<AudioSource>();
        _audio.playOnAwake = false;
        BuildCanvas();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    // ── Canvas ─────────────────────────────────────────────────────────────
    void BuildCanvas()
    {
        var cGO = new GameObject("CardMinigameCanvas");
        cGO.transform.SetParent(transform, false);
        _canvas = cGO.AddComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 50;
        var cs = cGO.AddComponent<CanvasScaler>();
        cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight  = 0.5f;
        cGO.AddComponent<GraphicRaycaster>();

        // Full-screen root / darkener
        _root = new GameObject("Root");
        _root.transform.SetParent(cGO.transform, false);
        FullRT(_root);
        var dark = _root.AddComponent<Image>();
        dark.color = new Color(0f, 0f, 0f, 0.82f);
        dark.raycastTarget = true;

        // Centered window
        var winGO = new GameObject("Window");
        winGO.transform.SetParent(_root.transform, false);
        var wrt = winGO.AddComponent<RectTransform>();
        SA(wrt, 0.15f, 0.08f, 0.85f, 0.92f, 0, 0, 0, 0);
        var winImg = winGO.AddComponent<Image>();
        winImg.color = new Color(0.06f, 0.05f, 0.10f, 0.98f);
        winImg.raycastTarget = true;
        Transform W = winGO.transform;

        // Gold border
        var bdr = Img("Border", W, new Color(0.97f, 0.76f, 0.05f));
        SA(bdr.rectTransform, 0, 0, 1, 1, -4, -4, 4, 4);
        bdr.transform.SetAsFirstSibling();

        // Title
        var title = Txt("Title", W, "♠  RESTAURACIÓN DE MESA  ♠",
            new Color(1f, 0.12f, 0.08f), 19, TextAnchor.MiddleCenter, FontStyle.Bold);
        SA(title.rectTransform, 0, 1, 1, 1, 8, -50, -8, -5);

        // Timer label
        _timerLabel = Txt("TimerLbl", W, "25",
            new Color(0.97f, 0.84f, 0.06f), 15, TextAnchor.MiddleCenter, FontStyle.Bold);
        SA(_timerLabel.rectTransform, 0, 1, 1, 1, 8, -70, -8, -52);

        // Timer bar bg
        var tbg = Img("TimerBg", W, new Color(0.10f, 0.06f, 0.01f));
        SA(tbg.rectTransform, 0, 1, 1, 1, 14, -86, -14, -73);

        // Timer bar fill
        _timerBar = Img("TimerFill", tbg.transform, new Color(0.97f, 0.76f, 0.05f));
        var tfrt = _timerBar.rectTransform;
        tfrt.anchorMin = Vector2.zero; tfrt.anchorMax = Vector2.one;
        tfrt.offsetMin = new Vector2(1, 1); tfrt.offsetMax = new Vector2(-1, -1);
        _timerBar.type       = Image.Type.Filled;
        _timerBar.fillMethod = Image.FillMethod.Horizontal;
        _timerBar.fillAmount = 1f;

        // Instruction
        var inst = Txt("Inst", W, "Toca las cartas en orden:  1 → 2 → 3 → 4 → 5",
            new Color(0.62f, 0.62f, 0.62f), 13, TextAnchor.MiddleCenter, FontStyle.Normal);
        SA(inst.rectTransform, 0, 1, 1, 1, 8, -106, -8, -89);

        // Card row
        var rowGO = new GameObject("CardRow");
        rowGO.transform.SetParent(W, false);
        var rrt = rowGO.AddComponent<RectTransform>();
        SA(rrt, 0.04f, 0.18f, 0.96f, 0.72f, 0, 0, 0, 0);
        var hlg = rowGO.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 14f;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;
        hlg.padding = new RectOffset(8, 8, 8, 8);

        _cardBgs    = new Image[CARD_COUNT];
        _cardLabels = new Text[CARD_COUNT];

        for (int i = 0; i < CARD_COUNT; i++)
        {
            var cardGO = new GameObject("Card" + i);
            cardGO.transform.SetParent(rowGO.transform, false);
            var bg = cardGO.AddComponent<Image>();
            bg.color = ColDefault;
            _cardBgs[i] = bg;

            var ol = cardGO.AddComponent<Outline>();
            ol.effectColor    = new Color(0.97f, 0.76f, 0.05f, 0.5f);
            ol.effectDistance = new Vector2(3, 3);

            var lbl = Txt("Num", cardGO.transform, "?",
                Color.white, 42, TextAnchor.MiddleCenter, FontStyle.Bold);
            lbl.rectTransform.anchorMin = Vector2.zero;
            lbl.rectTransform.anchorMax = Vector2.one;
            lbl.rectTransform.offsetMin = Vector2.zero;
            lbl.rectTransform.offsetMax = Vector2.zero;
            _cardLabels[i] = lbl;

            int idx = i;
            var btn = cardGO.AddComponent<Button>();
            var cols = btn.colors;
            cols.normalColor      = Color.white;
            cols.highlightedColor = new Color(1.3f, 1.3f, 1.3f);
            cols.pressedColor     = new Color(0.7f, 0.7f, 0.7f);
            btn.colors = cols;
            btn.onClick.AddListener(() => OnCardClicked(idx));
        }

        // Status text
        _statusText = Txt("Status", W, "",
            new Color(0.55f, 0.55f, 0.55f), 13, TextAnchor.MiddleCenter, FontStyle.Normal);
        SA(_statusText.rectTransform, 0, 0, 1, 0, 8, 6, -8, 24);

        // Flash overlay
        _flashOverlay = Img("Flash", W, new Color(1, 1, 1, 0));
        var frt = _flashOverlay.rectTransform;
        frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
        frt.offsetMin = Vector2.zero; frt.offsetMax = Vector2.zero;
        _flashOverlay.raycastTarget = false;

        // Completed banner
        _completedBanner = new GameObject("Banner");
        _completedBanner.transform.SetParent(W, false);
        var crt = _completedBanner.AddComponent<RectTransform>();
        SA(crt, 0.1f, 0.3f, 0.9f, 0.7f, 0, 0, 0, 0);
        _completedBanner.AddComponent<Image>().color = new Color(0.04f, 0.04f, 0.04f, 0.94f);
        var ctxt = Txt("BannerTxt", _completedBanner.transform,
            "✓  ¡MESA RESTAURADA!", new Color(0.97f, 0.79f, 0.05f), 24,
            TextAnchor.MiddleCenter, FontStyle.Bold);
        ctxt.rectTransform.anchorMin = Vector2.zero;
        ctxt.rectTransform.anchorMax = Vector2.one;
        ctxt.rectTransform.offsetMin = Vector2.zero;
        ctxt.rectTransform.offsetMax = Vector2.zero;
        _completedBanner.SetActive(false);

        // ESC hint
        var esc = Txt("Esc", W, "[ESC] Cancelar",
            new Color(0.28f, 0.28f, 0.28f), 10, TextAnchor.LowerRight, FontStyle.Normal);
        SA(esc.rectTransform, 0.5f, 0, 1, 0, 0, 2, -6, 14);

        _root.SetActive(false);
    }

    // ── Open ───────────────────────────────────────────────────────────────
    public void Open(CasinoMachine machine)
    {
        _target       = machine;
        _completed    = false;
        _timeLeft     = TIME_LIMIT;
        _active       = true;
        _nextExpected = 1;

        // Shuffle card values 1-5
        _cardValues = new int[CARD_COUNT];
        var pool = new List<int> { 1, 2, 3, 4, 5 };
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }
        for (int i = 0; i < CARD_COUNT; i++) _cardValues[i] = pool[i];
        _cardDone = new bool[CARD_COUNT];

        // Reset visuals
        _completedBanner.SetActive(false);
        _flashOverlay.color  = new Color(1, 1, 1, 0);
        _timerBar.fillAmount = 1f;
        _timerBar.color      = new Color(0.97f, 0.76f, 0.05f);
        _timerLabel.text     = TIME_LIMIT.ToString("0");
        _timerLabel.color    = new Color(0.97f, 0.84f, 0.06f);
        _statusText.text     = "Toca las cartas en orden correcto";
        _statusText.color    = new Color(0.50f, 0.50f, 0.50f);

        for (int i = 0; i < CARD_COUNT; i++)
        {
            _cardBgs[i].color   = ColDefault;
            _cardLabels[i].text  = _cardValues[i].ToString();
            _cardLabels[i].color = Color.white;
        }

        if (Camera.main != null) { _origFov = Camera.main.fieldOfView; Camera.main.fieldOfView = 45f; }
        IsActive = true;
        _root.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
        var fpc = FindAnyObjectByType<FirstPersonController>();
        if (fpc) fpc.enabled = false;
    }

    // ── Card interaction ───────────────────────────────────────────────────
    void OnCardClicked(int cardIndex)
    {
        if (!_active || _completed) return;
        int val = _cardValues[cardIndex];

        if (val == _nextExpected)
        {
            _cardDone[cardIndex]       = true;
            _cardBgs[cardIndex].color  = ColDone;
            _cardLabels[cardIndex].color = new Color(0.80f, 1f, 0.82f);
            _nextExpected++;

            string nextHint = _nextExpected <= CARD_COUNT ? _nextExpected.ToString() : "—";
            _statusText.text  = $"✓  ¡Carta {val} correcta!   Siguiente: {nextHint}";
            _statusText.color = new Color(0.35f, 0.82f, 0.28f);
            StartCoroutine(Flash(new Color(0.15f, 0.90f, 0.20f, 0.28f)));
            PlayTone(440f + val * 80f, 0.12f);

            if (_nextExpected > CARD_COUNT)
            {
                _completed = true;
                _active    = false;
                StartCoroutine(WinSequence());
            }
        }
        else
        {
            StartCoroutine(WrongFlash(cardIndex, val));
        }
    }

    IEnumerator WrongFlash(int cardIndex, int tappedVal)
    {
        _cardBgs[cardIndex].color = ColWrong;
        _statusText.text  = "✗  ¡Orden incorrecto! — Empieza de nuevo";
        _statusText.color = new Color(1f, 0.15f, 0.10f);
        StartCoroutine(Flash(new Color(1f, 0.05f, 0.05f, 0.40f)));
        PlayTone(180f, 0.25f);
        yield return new WaitForSecondsRealtime(0.5f);

        if (!_active) yield break;  // cancelled or timed-out during delay

        // Reset all progress
        _nextExpected = 1;
        for (int i = 0; i < CARD_COUNT; i++)
        {
            _cardDone[i]         = false;
            _cardBgs[i].color    = ColDefault;
            _cardLabels[i].color = Color.white;
        }
        _statusText.text  = "Toca las cartas en orden correcto";
        _statusText.color = new Color(0.50f, 0.50f, 0.50f);
    }

    IEnumerator WinSequence()
    {
        _completedBanner.SetActive(true);
        PlayTone(660f, 0.5f);
        for (int i = 0; i < 5; i++)
        {
            yield return StartCoroutine(Flash(new Color(0.97f, 0.82f, 0.08f, 0.55f)));
            yield return new WaitForSecondsRealtime(0.13f);
        }
        yield return new WaitForSecondsRealtime(1.0f);
        DoFinish();
    }

    void DoFinish()
    {
        IsActive = false;
        RestoreFov();
        _root.SetActive(false);
        _target.Repair();
        GameManager.Instance.NotifyRepaired(_target);
        RestorePlayer();
    }

    // ── Timer ──────────────────────────────────────────────────────────────
    void Update()
    {
        if (_root != null && _root.activeSelf && !_completed)
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Q))
                Cancel();

        if (!_active || _completed) return;

        _timeLeft -= Time.deltaTime;
        float ratio = Mathf.Clamp01(_timeLeft / TIME_LIMIT);

        _timerBar.fillAmount = ratio;
        _timerLabel.text     = Mathf.CeilToInt(Mathf.Max(0f, _timeLeft)).ToString();
        _timerBar.color      = ratio > 0.5f
            ? Color.Lerp(new Color(0.97f, 0.76f, 0.05f), new Color(0.10f, 0.92f, 0.22f), (ratio - 0.5f) * 2f)
            : Color.Lerp(new Color(1f, 0.10f, 0.05f),    new Color(0.97f, 0.76f, 0.05f),  ratio * 2f);
        _timerLabel.color = ratio < 0.33f ? new Color(1f, 0.12f, 0.08f) : new Color(0.97f, 0.84f, 0.06f);

        if (_timeLeft <= 0f)
        {
            _active = false;
            StartCoroutine(OnTimeUp());
        }
    }

    IEnumerator OnTimeUp()
    {
        IsActive = false;
        RestoreFov();
        _statusText.text  = "⚠  ¡TIEMPO AGOTADO!";
        _statusText.color = new Color(1f, 0.05f, 0.05f);
        StartCoroutine(Flash(new Color(1f, 0.05f, 0.05f, 0.65f)));
        PlayTone(120f, 0.6f);
        yield return new WaitForSecondsRealtime(1.5f);
        _root.SetActive(false);
        _target.SetMoreBroken();
        RestorePlayer();
    }

    public void Cancel()
    {
        _active  = false;
        IsActive = false;
        RestoreFov();
        if (_root) _root.SetActive(false);
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

    // ── Effects ────────────────────────────────────────────────────────────
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

    void PlayTone(float freq, float dur)
    {
        int n = (int)(44100 * dur);
        var data = new float[n];
        for (int i = 0; i < n; i++)
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * i / 44100f) * Mathf.Exp(-3f * i / n);
        var clip = AudioClip.Create("tone", n, 1, 44100, false);
        clip.SetData(data, 0);
        _audio.PlayOneShot(clip);
    }

    // ── Helpers ────────────────────────────────────────────────────────────
    static void SA(RectTransform rt, float ax, float ay, float bx, float by,
                   float ol, float ob, float or_, float ot)
    {
        rt.anchorMin = new Vector2(ax, ay); rt.anchorMax = new Vector2(bx, by);
        rt.offsetMin = new Vector2(ol, ob); rt.offsetMax = new Vector2(or_, ot);
    }

    static void FullRT(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static Image Img(string name, Transform parent, Color col)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>(); img.color = col; img.raycastTarget = false;
        return img;
    }

    static Text Txt(string name, Transform parent, string content,
                    Color col, int size, TextAnchor anchor, FontStyle style)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var t = go.AddComponent<Text>();
        t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text      = content; t.color = col;
        t.fontSize  = size;   t.alignment = anchor; t.fontStyle = style;
        t.raycastTarget = false;
        return t;
    }
}
