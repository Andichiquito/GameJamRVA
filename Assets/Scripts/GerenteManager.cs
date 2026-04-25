using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GerenteManager : MonoBehaviour
{
    public static GerenteManager Instance { get; private set; }

    // Canvas dimensions at reference resolution (1920×1080)
    const float HW = 960f;
    const float HH = 540f;

    Canvas      _canvas;
    AudioSource _audio;
    AudioClip   _jackpotClip;
    AudioClip   _dismissClip;
    Coroutine   _typewriterRoutine;

    readonly List<ConfettiPiece> _confetti = new();

    struct ConfettiPiece
    {
        public RectTransform rt;
        public float speed, drift, rotSpeed;
    }

    // ─── Lifecycle ────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        _audio = gameObject.AddComponent<AudioSource>();
        _audio.playOnAwake = false;
        _jackpotClip = BuildJackpot();
        _dismissClip = BuildDismiss();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    // ─── Canvas setup ─────────────────────────────────────────────────────────
    Canvas EnsureCanvas()
    {
        if (_canvas) return _canvas;

        var go = new GameObject("GerenteCanvas");
        go.transform.SetParent(transform);
        _canvas = go.AddComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 200;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;

        go.AddComponent<GraphicRaycaster>();

        if (FindAnyObjectByType<EventSystem>() == null)
        {
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();
        }
        return _canvas;
    }

    // ─── Public API ───────────────────────────────────────────────────────────
    public void ShowIntro(Action onComplete)
        => StartCoroutine(IntroRoutine(onComplete));

    public void ShowCaseritoGameOver()
        => StartCoroutine(EndRoutine(
            won:      false,
            title:    "¡TE ATRAPARON!",
            body:     "Un cliente te atrapó. Definitivamente no estás\nlisto para este trabajo.",
            footer:   "DESPEDIDO.",
            titleCol: new Color(0.90f, 0.08f, 0.08f)));

    public void ShowTimeGameOver()
        => StartCoroutine(EndRoutine(
            won:      false,
            title:    "TIEMPO AGOTADO",
            body:     "Se acabó tu turno de prueba.\nNo fuiste suficientemente rápido.",
            footer:   "DESPEDIDO.",
            titleCol: new Color(0.90f, 0.08f, 0.08f)));

    public void ShowVictory()
        => StartCoroutine(EndRoutine(
            won:      true,
            title:    "¡TURNO COMPLETADO!",
            body:     "...Impresionante. Sobreviviste tu turno de prueba.\nBienvenido al equipo. Aunque...\ndudo que quieras volver mañana.",
            footer:   "",
            titleCol: new Color(0.95f, 0.80f, 0.10f)));

    // ─── Intro routine ────────────────────────────────────────────────────────
    IEnumerator IntroRoutine(Action onComplete)
    {
        var fpc = FindAnyObjectByType<FirstPersonController>();
        if (fpc) fpc.enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        var canvas = EnsureCanvas();

        // Full-screen backdrop
        var root   = Go("IntroPanel", canvas.transform);
        var rootRt = root.AddComponent<RectTransform>();
        Stretch(rootRt);
        var rootImg = root.AddComponent<Image>();
        rootImg.color        = new Color(0.02f, 0.01f, 0.01f, 1f);
        rootImg.raycastTarget = true;

        // Subtle red vignette
        var vig = Img("Vignette", root.transform, new Color(0.22f, 0f, 0f, 0.22f));
        Stretch(vig.rectTransform);

        BuildManagerFigure(root.transform, new Vector2(0f, 100f), ManagerMood.Neutral);

        // Dialogue box
        var dbox = Img("DBox", root.transform, new Color(0f, 0f, 0f, 0.82f));
        SA(dbox.rectTransform, 0.06f, 0.04f, 0.94f, 0.37f, 0, 0, 0, 0);

        Img("DBorder", dbox.transform, new Color(0.85f, 0.65f, 0.05f, 0.55f))
            .rectTransform.anchorMin = Vector2.zero;
        var dbBorder = dbox.transform.Find("DBorder").GetComponent<RectTransform>();
        SA(dbBorder, 0, 0, 1, 1, -2, -2, 2, 2);
        dbox.transform.Find("DBorder").SetAsFirstSibling();

        var speakerLbl = Txt("Speaker", dbox.transform,
            "EL GERENTE", new Color(0.92f, 0.72f, 0.08f), 12, TextAnchor.UpperLeft, FontStyle.Bold);
        SA(speakerLbl.rectTransform, 0, 1, 0.5f, 1, 14, 2, 0, 16);

        var dialogTxt = Txt("Dialog", dbox.transform,
            "", new Color(0.95f, 0.92f, 0.85f), 17, TextAnchor.UpperLeft);
        SA(dialogTxt.rectTransform, 0, 0, 1, 1, 14, 10, -14, -10);
        dialogTxt.lineSpacing = 1.35f;

        var skipHint = Txt("Skip", root.transform,
            "[ESPACIO / ENTER]  Continuar", new Color(0.40f, 0.40f, 0.40f), 12, TextAnchor.LowerRight);
        SA(skipHint.rectTransform, 0, 0, 1, 0, 0, 8, -14, 22);

        const string dialogue =
            "Bienvenido a tu turno de prueba. Tienes 5 minutos.\n" +
            "Arregla las máquinas averiadas antes de que acabe tu turno.\n" +
            "Y... ten cuidado con los clientes. Son... especiales.";

        // Typewriter — can be skipped
        bool typewriterDone = false;
        _typewriterRoutine = StartCoroutine(
            Typewriter(dialogue, dialogTxt, 0.038f, () => typewriterDone = true));

        while (!typewriterDone)
        {
            if (ConfirmKey())
            {
                StopCoroutine(_typewriterRoutine);
                dialogTxt.text = dialogue;
                break;
            }
            yield return null;
        }

        // Wait for a second confirm press (blinking hint)
        float blink = 0f;
        while (!ConfirmKey())
        {
            blink += Time.unscaledDeltaTime * 3f;
            skipHint.color = new Color(0.6f, 0.6f, 0.6f,
                0.5f + 0.5f * Mathf.Sin(blink * Mathf.PI * 2f));
            yield return null;
        }

        yield return StartCoroutine(FadeImg(rootImg, rootImg.color, Color.clear, 0.40f));
        Destroy(root);

        if (fpc) fpc.enabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        onComplete?.Invoke();
    }

    // ─── End-screen routine ───────────────────────────────────────────────────
    IEnumerator EndRoutine(bool won, string title, string body, string footer, Color titleCol)
    {
        var canvas = EnsureCanvas();

        var root   = Go("EndPanel", canvas.transform);
        var rootRt = root.AddComponent<RectTransform>();
        Stretch(rootRt);
        var rootImg = root.AddComponent<Image>();
        rootImg.color        = Color.clear;
        rootImg.raycastTarget = true;

        yield return StartCoroutine(FadeImg(rootImg,
            Color.clear, new Color(0.02f, 0.01f, 0.01f, 0.96f), 0.55f));

        BuildManagerFigure(root.transform, new Vector2(0f, 85f),
            won ? ManagerMood.Happy : ManagerMood.Disappointed);

        // Content box
        var cbox = Img("CBox", root.transform, new Color(0f, 0f, 0f, 0.84f));
        SA(cbox.rectTransform, 0.07f, 0.04f, 0.93f, 0.41f, 0, 0, 0, 0);

        var bdrCol = new Color(titleCol.r * 0.55f, titleCol.g * 0.55f, titleCol.b * 0.55f, 0.65f);
        var cbdr = Img("CBdr", cbox.transform, bdrCol);
        SA(cbdr.rectTransform, 0, 0, 1, 1, -2, -2, 2, 2);
        cbox.transform.Find("CBdr").SetAsFirstSibling();

        var titleTxt = Txt("Title", cbox.transform,
            title, titleCol, 22, TextAnchor.MiddleCenter, FontStyle.Bold);
        SA(titleTxt.rectTransform, 0, 0.68f, 1, 1, 10, 0, -10, -4);

        var bodyTxt = Txt("Body", cbox.transform,
            "", new Color(0.90f, 0.88f, 0.82f), 15, TextAnchor.UpperCenter);
        bodyTxt.lineSpacing = 1.35f;
        SA(bodyTxt.rectTransform,
            0, string.IsNullOrEmpty(footer) ? 0.12f : 0.30f, 1, 0.70f, 12, 0, -12, 0);

        if (!string.IsNullOrEmpty(footer))
        {
            var footerTxt = Txt("Footer", cbox.transform,
                footer, titleCol, 20, TextAnchor.MiddleCenter, FontStyle.Bold);
            SA(footerTxt.rectTransform, 0, 0.02f, 1, 0.30f, 10, 0, -10, 0);
        }

        bool done = false;
        _typewriterRoutine = StartCoroutine(Typewriter(body, bodyTxt, 0.04f, () => done = true));
        while (!done) yield return null;

        if (won)
        {
            _audio.PlayOneShot(_jackpotClip, 0.90f);
            StartCoroutine(SpawnConfetti(root.transform));
        }
        else
        {
            _audio.PlayOneShot(_dismissClip, 0.80f);
        }

        yield return new WaitForSecondsRealtime(0.60f);

        // Buttons
        var retry = MakeButton("BtnRetry", cbox.transform,
            "↺  Reintentar",
            new Color(0.12f, 0.10f, 0.03f), new Color(0.92f, 0.76f, 0.08f),
            () => { Time.timeScale = 1f; SceneManager.LoadScene(SceneManager.GetActiveScene().name); });
        SA(retry.GetComponent<RectTransform>(), 0.06f, 0.02f, 0.47f, 0.17f, 0, 0, -4, 0);

        var menu = MakeButton("BtnMenu", cbox.transform,
            "⌂  Menú Principal",
            new Color(0.10f, 0.05f, 0.05f), new Color(0.75f, 0.22f, 0.18f),
            () => { Time.timeScale = 1f; SceneManager.LoadScene("MainMenu"); });
        SA(menu.GetComponent<RectTransform>(), 0.53f, 0.02f, 0.94f, 0.17f, 4, 0, 0, 0);
    }

    // ─── Manager silhouette ───────────────────────────────────────────────────
    enum ManagerMood { Neutral, Happy, Disappointed }

    void BuildManagerFigure(Transform parent, Vector2 center, ManagerMood mood)
    {
        var fig = Go("Manager", parent);
        var rt  = fig.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = center;
        rt.sizeDelta        = Vector2.zero;

        Color body = new Color(0.04f, 0.03f, 0.03f, 1f);
        Color suit = new Color(0.08f, 0.06f, 0.06f, 1f);
        Color tie  = new Color(0.42f, 0.03f, 0.03f, 1f);
        Color eye  = mood == ManagerMood.Happy
            ? new Color(1f, 0.72f, 0.08f)
            : new Color(1f, 0.05f, 0.05f);

        // Ambient glow — red or golden depending on mood
        Part(fig.transform, "Glow",
            mood == ManagerMood.Happy
                ? new Color(0.30f, 0.22f, 0f, 0.18f)
                : new Color(0.22f, 0f, 0f, 0.20f),
            0, 30, 230, 320);

        // Legs
        Part(fig.transform, "LegL",  body, -26, -108, 40, 88);
        Part(fig.transform, "LegR",  body,  26, -108, 40, 88);

        // Torso
        Part(fig.transform, "Torso", body,   0,   10, 110, 125);

        // Lapels (slightly lighter strip for jacket detail)
        Part(fig.transform, "LapL",  suit, -22,  48,  20, 52);
        Part(fig.transform, "LapR",  suit,  22,  48,  20, 52);

        // Tie
        Part(fig.transform, "Tie",   tie,    0,  25,  13, 58);

        // Arms
        Part(fig.transform, "ArmL",  body, -72,  10,  30, 105);
        Part(fig.transform, "ArmR",  body,  72,  10,  30, 105);

        // Shoulders
        Part(fig.transform, "Shld",  body,   0,  73, 168,  28);

        // Neck
        Part(fig.transform, "Neck",  body,   0, 102,  24,  26);

        // Head
        Part(fig.transform, "Head",  body,   0, 148,  82,  82);

        // Eyes (drawn over head)
        var eyeL = Part(fig.transform, "EyeL", eye, -16, 152, 14, 10);
        var eyeR = Part(fig.transform, "EyeR", eye,  16, 152, 14, 10);

        // Hat
        Part(fig.transform, "Brim",  body,   0, 197, 128, 14);
        Part(fig.transform, "Crown", body,   0, 226,  76, 50);

        // Pocket square (only when happy)
        if (mood == ManagerMood.Happy)
            Part(fig.transform, "Pocket",
                new Color(0.92f, 0.74f, 0.08f), 36, 42, 14, 12);

        StartCoroutine(PulseEyes(
            eyeL.GetComponent<Image>(),
            eyeR.GetComponent<Image>(), eye, mood));
    }

    IEnumerator PulseEyes(Image a, Image b, Color col, ManagerMood mood)
    {
        float speed = mood == ManagerMood.Disappointed ? 1.5f : 0.55f;
        while (a != null && b != null)
        {
            float t = (Mathf.Sin(Time.unscaledTime * speed * Mathf.PI * 2f) + 1f) * 0.5f;
            var c   = new Color(col.r, col.g, col.b, Mathf.Lerp(0.50f, 1f, t));
            a.color = c;
            b.color = c;
            yield return null;
        }
    }

    // ─── Confetti ─────────────────────────────────────────────────────────────
    IEnumerator SpawnConfetti(Transform parent)
    {
        Color[] palette =
        {
            new Color(0.97f, 0.80f, 0.05f),
            new Color(1.00f, 1.00f, 0.90f),
            new Color(0.95f, 0.15f, 0.10f),
            new Color(0.10f, 0.80f, 0.25f),
            new Color(0.20f, 0.55f, 1.00f),
        };

        for (int i = 0; i < 60; i++)
        {
            var go = Go($"C{i}", parent);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(
                UnityEngine.Random.Range(-HW, HW),
                HH + UnityEngine.Random.Range(20f, 130f));
            rt.sizeDelta = new Vector2(
                UnityEngine.Random.Range(10f, 22f),
                UnityEngine.Random.Range( 6f, 14f));

            var img = go.AddComponent<Image>();
            img.color = palette[UnityEngine.Random.Range(0, palette.Length)];
            img.raycastTarget = false;

            _confetti.Add(new ConfettiPiece
            {
                rt       = rt,
                speed    = UnityEngine.Random.Range(210f, 430f),
                drift    = UnityEngine.Random.Range(-85f,  85f),
                rotSpeed = UnityEngine.Random.Range(-250f, 250f),
            });

            yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(0.02f, 0.06f));
        }
    }

    void LateUpdate()
    {
        if (_confetti.Count == 0) return;
        float dt    = Time.unscaledDeltaTime;
        float floor = -(HH + 160f);

        for (int i = _confetti.Count - 1; i >= 0; i--)
        {
            var p = _confetti[i];
            if (!p.rt) { _confetti.RemoveAt(i); continue; }

            var pos = p.rt.anchoredPosition;
            pos.y -= p.speed * dt;
            pos.x += p.drift * dt;
            p.rt.anchoredPosition        = pos;
            p.rt.localEulerAngles       += new Vector3(0f, 0f, p.rotSpeed * dt);

            if (pos.y < floor)
            {
                Destroy(p.rt.gameObject);
                _confetti.RemoveAt(i);
            }
        }
    }

    // ─── Typewriter ───────────────────────────────────────────────────────────
    IEnumerator Typewriter(string text, Text target, float delay, Action onDone)
    {
        target.text = "";
        foreach (char c in text)
        {
            target.text += c;
            if (c != ' ' && c != '\n')
                yield return new WaitForSecondsRealtime(delay);
        }
        onDone?.Invoke();
    }

    // ─── Procedural audio ─────────────────────────────────────────────────────
    static AudioClip BuildJackpot()
    {
        const int sr = 44100;
        int n = (int)(sr * 2.0f);
        var d = new float[n];
        // Ascending fanfare: C5 E5 G5 C6 E6
        float[] freqs = { 523.25f, 659.25f, 783.99f, 1046.50f, 1318.51f };
        for (int note = 0; note < freqs.Length; note++)
        {
            int s0 = (int)(note * 0.20f * sr);
            for (int i = s0; i < n && i < s0 + (int)(0.60f * sr); i++)
            {
                float t = (float)(i - s0) / sr;
                d[i] = Mathf.Clamp(d[i] +
                    Mathf.Sin(Mathf.PI * 2f * freqs[note] * t) *
                    Mathf.Exp(-t * 2.2f) * 0.50f, -1f, 1f);
            }
        }
        var clip = AudioClip.Create("jackpot", n, 1, sr, false);
        clip.SetData(d, 0);
        return clip;
    }

    static AudioClip BuildDismiss()
    {
        const int sr = 44100;
        int n = (int)(sr * 1.0f);
        var d = new float[n];
        // Descending minor phrase: G4 F4 Eb4 C4
        float[] freqs = { 392f, 349.23f, 311.13f, 261.63f };
        for (int note = 0; note < freqs.Length; note++)
        {
            int s0 = (int)(note * 0.16f * sr);
            for (int i = s0; i < n && i < s0 + (int)(0.42f * sr); i++)
            {
                float t = (float)(i - s0) / sr;
                d[i] = Mathf.Clamp(d[i] +
                    Mathf.Sin(Mathf.PI * 2f * freqs[note] * t) *
                    Mathf.Exp(-t * 3.0f) * 0.55f, -1f, 1f);
            }
        }
        var clip = AudioClip.Create("dismiss", n, 1, sr, false);
        clip.SetData(d, 0);
        return clip;
    }

    // ─── UI helpers ───────────────────────────────────────────────────────────
    // Silhouette rectangle part (center-anchored)
    GameObject Part(Transform parent, string name, Color col, float x, float y, float w, float h)
    {
        var go  = Go(name, parent);
        var rt  = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta        = new Vector2(w, h);
        var img = go.AddComponent<Image>();
        img.color = col;
        img.raycastTarget = false;
        return go;
    }

    static Image Img(string name, Transform parent, Color col)
    {
        var go  = Go(name, parent);
        go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.color = col;
        img.raycastTarget = false;
        return img;
    }

    static Text Txt(string name, Transform parent, string content,
        Color col, int size, TextAnchor anchor, FontStyle style = FontStyle.Normal)
    {
        var go = Go(name, parent);
        go.AddComponent<RectTransform>();
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

    static GameObject MakeButton(string name, Transform parent,
        string label, Color bgCol, Color textCol, Action onClick)
    {
        var go = Go(name, parent);
        go.AddComponent<RectTransform>();

        var img = go.AddComponent<Image>();
        img.color = bgCol;

        var btn   = go.AddComponent<Button>();
        var cols  = btn.colors;
        cols.normalColor      = bgCol;
        cols.highlightedColor = bgCol * new Color(1.5f, 1.5f, 1.5f, 1f);
        cols.pressedColor     = bgCol * new Color(0.7f, 0.7f, 0.7f, 1f);
        btn.colors = cols;
        btn.onClick.AddListener(() => onClick?.Invoke());

        // Border highlight
        var bdrGo = Go("Bdr", go.transform);
        var brt   = bdrGo.AddComponent<RectTransform>();
        brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
        brt.offsetMin = new Vector2(-1, -1); brt.offsetMax = new Vector2(1, 1);
        var bImg = bdrGo.AddComponent<Image>();
        bImg.color = new Color(textCol.r * 0.55f, textCol.g * 0.55f, textCol.b * 0.55f, 0.65f);
        bImg.raycastTarget = false;
        bdrGo.transform.SetAsFirstSibling();

        var lbl = Txt("Lbl", go.transform, label, textCol, 15, TextAnchor.MiddleCenter, FontStyle.Bold);
        lbl.rectTransform.anchorMin = Vector2.zero;
        lbl.rectTransform.anchorMax = Vector2.one;
        lbl.rectTransform.offsetMin = Vector2.zero;
        lbl.rectTransform.offsetMax = Vector2.zero;

        return go;
    }

    IEnumerator FadeImg(Image img, Color from, Color to, float dur)
    {
        for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
        {
            img.color = Color.Lerp(from, to, t / dur);
            yield return null;
        }
        img.color = to;
    }

    static void SA(RectTransform rt, float ax, float ay, float bx, float by,
                   float ol, float ob, float or_, float ot)
    {
        rt.anchorMin = new Vector2(ax, ay);
        rt.anchorMax = new Vector2(bx, by);
        rt.offsetMin = new Vector2(ol, ob);
        rt.offsetMax = new Vector2(or_, ot);
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static bool ConfirmKey()
        => Input.GetKeyDown(KeyCode.Space)
        || Input.GetKeyDown(KeyCode.Return)
        || Input.GetKeyDown(KeyCode.Escape);

    static GameObject Go(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go;
    }
}
