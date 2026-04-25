using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameHUD : MonoBehaviour
{
    public static GameHUD Instance { get; private set; }

    [Header("Timer")]
    public Text timerText;

    [Header("Task Tablet")]
    public Transform taskListParent;

    [Header("Prompt")]
    public GameObject promptPanel;  // el panel completo — NO solo el texto
    public Text       promptText;

    [Header("End Screen")]
    public GameObject endPanel;
    public Text       endTitleText;
    public Text       endSubText;

    Transform _player;
    readonly List<(SlotMachine m, Text lbl)> _items = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        _player = GameObject.FindWithTag("Player")?.transform;
        if (endPanel)    endPanel.SetActive(false);
        if (promptPanel) promptPanel.SetActive(false);
    }

    void Update()
    {
        TickTimer();
        TickPrompt();
    }

    // ── Timer ────────────────────────────────────────────────────────────
    void TickTimer()
    {
        if (!timerText || !GameManager.Instance) return;
        float t   = GameManager.Instance.TimeLeft;
        int   min = (int)(t / 60f);
        int   sec = (int)(t % 60f);
        timerText.text  = $"{min:00}:{sec:00}";
        timerText.color = t < 60f
            ? new Color(1f, 0.18f, 0.08f)
            : new Color(0.92f, 0.80f, 0.08f);
    }

    // ── Prompt + E key ───────────────────────────────────────────────────
    void TickPrompt()
    {
        if (!promptPanel || !GameManager.Instance || !_player) return;

        bool miniOpen = RepairMinigame.Instance != null
                     && RepairMinigame.Instance.panel.activeSelf;

        if (miniOpen) { promptPanel.SetActive(false); return; }

        SlotMachine nearby = null;
        foreach (var m in GameManager.Instance.brokenMachines)
            if (m.IsPlayerNearby(_player, 3.5f)) { nearby = m; break; }

        promptPanel.SetActive(nearby != null);

        if (nearby != null && Input.GetKeyDown(KeyCode.E))
            GameManager.Instance.StartRepair(nearby);
    }

    // ── Task Tablet ──────────────────────────────────────────────────────
    public void RefreshTaskList()
    {
        if (!taskListParent || !GameManager.Instance) return;

        foreach (Transform ch in taskListParent) Destroy(ch.gameObject);
        _items.Clear();

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        foreach (var m in GameManager.Instance.initialBroken)
        {
            var go = new GameObject(m.machineName);
            go.transform.SetParent(taskListParent, false);
            var rt  = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(220f, 24f);
            var txt = go.AddComponent<Text>();
            txt.font      = font;
            txt.fontSize  = 15;
            txt.alignment = TextAnchor.MiddleLeft;
            SetTaskItem(txt, m);
            _items.Add((m, txt));
        }
    }

    void SetTaskItem(Text txt, SlotMachine m)
    {
        bool broken = m.state == MachineState.Broken;
        txt.text      = (broken ? "[ ] " : "[✓] ") + m.machineName;
        txt.color     = broken ? new Color(1f, 0.32f, 0.22f) : new Color(0.38f, 0.72f, 0.28f);
        txt.fontStyle = broken ? FontStyle.Normal : FontStyle.Italic;
    }

    // ── End Screen ───────────────────────────────────────────────────────
    public void ShowEnd(bool won, string customTitle = null, string customSub = null)
    {
        if (!endPanel) return;
        endPanel.SetActive(true);

        endTitleText.text = customTitle ?? (won ? "¡TURNO COMPLETADO!" : "DESPEDIDO");
        endSubText.text   = customSub   ?? (won
            ? "Todas las máquinas reparadas.\nBuen trabajo, empleado."
            : "Se acabó el tiempo.\nEl gerente no está satisfecho.");
        endTitleText.color = won
            ? new Color(0.92f, 0.80f, 0.08f)
            : new Color(0.92f, 0.12f, 0.08f);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    // ── Catch Flash ──────────────────────────────────────────────────────
    Image _catchFlashImg;

    public void TriggerCatchFlash()
    {
        if (_catchFlashImg == null)
        {
            var go = new GameObject("CatchFlash");
            go.transform.SetParent(transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            _catchFlashImg = go.AddComponent<Image>();
            _catchFlashImg.color = new Color(0.9f, 0f, 0f, 0f);
            go.transform.SetAsLastSibling();
        }
        StopCoroutine("CatchFlashRoutine");
        StartCoroutine(CatchFlashRoutine());
    }

    IEnumerator CatchFlashRoutine()
    {
        _catchFlashImg.color = new Color(0.9f, 0f, 0f, 0.85f);
        float dur = 0.55f;
        for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
        {
            float alpha = Mathf.Lerp(0.85f, 0f, t / dur);
            _catchFlashImg.color = new Color(0.9f, 0f, 0f, alpha);
            yield return null;
        }
        _catchFlashImg.color = new Color(0.9f, 0f, 0f, 0f);
    }
}
