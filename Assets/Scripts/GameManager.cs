using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Config")]
    public float turnDuration = 300f;  // 5 minutos

    // All machines in the scene, sorted by machineIndex
    [HideInInspector] public List<CasinoMachine> allMachines      = new();
    // Subset that started broken (for task list)
    [HideInInspector] public List<CasinoMachine> initialBroken    = new();
    // Subset still broken
    [HideInInspector] public List<CasinoMachine> brokenMachines   = new();

    float _timeLeft;
    bool  _ended;
    bool  _gameStarted;

    public float TimeLeft      => _timeLeft;
    public bool  IsGameOver    => _ended;
    public bool  IsGameStarted => _gameStarted;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        // Auto-create DayManager if absent
        if (DayManager.Instance == null)
            new GameObject("DayManager").AddComponent<DayManager>();

        var cfg   = DayManager.Instance.Config;
        _timeLeft = cfg.turnDuration;

        allMachines.AddRange(FindObjectsByType<CasinoMachine>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None));
        allMachines.Sort((a, b) => a.machineIndex.CompareTo(b.machineIndex));

        SetupCaseritos(cfg.caseritos);
        BreakRandom(cfg.machinesToBreak);
        GameHUD.Instance?.RefreshTaskList();

        // Auto-create GerenteManager if absent
        if (GerenteManager.Instance == null)
            new GameObject("GerenteManager").AddComponent<GerenteManager>();

        GerenteManager.Instance.ShowDayIntro(DayManager.Instance.CurrentDay, OnIntroComplete);
    }

    void OnIntroComplete() => _gameStarted = true;

    // Enable only the first `count` caseritos found in the scene; disable the rest
    void SetupCaseritos(int count)
    {
        var all = FindObjectsByType<Caserito>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
            all[i].gameObject.SetActive(i < count);
    }

    void BreakRandom(int count)
    {
        var pool = new List<CasinoMachine>(allMachines);
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }
        int n = Mathf.Clamp(count, 0, pool.Count);
        for (int i = 0; i < n; i++)
        {
            pool[i].SetBroken();
            brokenMachines.Add(pool[i]);
            initialBroken.Add(pool[i]);
        }
    }

    void Update()
    {
        if (!_gameStarted || _ended) return;
        _timeLeft = Mathf.Max(0f, _timeLeft - Time.deltaTime);
        if (_timeLeft <= 0f) TriggerEnd(won: false);
    }

    public void StartRepair(CasinoMachine machine)
    {
        if (machine is RouletteTable)
        {
            if (RouletteMinigame.Instance == null)
                new GameObject("RouletteMinigame").AddComponent<RouletteMinigame>();
            RouletteMinigame.Instance.Open(machine);
        }
        else if (machine is CardTable)
        {
            if (CardMinigame.Instance == null)
                new GameObject("CardMinigame").AddComponent<CardMinigame>();
            CardMinigame.Instance.Open(machine);
        }
        else
        {
            RepairMinigame.Instance?.Open(machine);
        }
    }

    public void NotifyRepaired(CasinoMachine machine)
    {
        brokenMachines.Remove(machine);
        GameHUD.Instance?.RefreshTaskList();
        if (brokenMachines.Count == 0 && !_ended)
            TriggerEnd(won: true);
    }

    // Called when a caserito catches the player
    public void TriggerCaseritoGameOver()
    {
        if (_ended) return;
        _ended = true;
        RepairMinigame.Instance?.Cancel();
        RouletteMinigame.Instance?.Cancel();
        CardMinigame.Instance?.Cancel();
        Invoke(nameof(ShowCaught), 0.3f);
    }
    void ShowCaught()
    {
        Time.timeScale = 0f;
        if (GerenteManager.Instance != null)
            GerenteManager.Instance.ShowCaseritoGameOver();
        else
            GameHUD.Instance?.ShowEnd(false,
                "¡TE ATRAPARON!", "Un caserito te alcanzó.\nEl casino ya no es seguro.");
    }

    void TriggerEnd(bool won)
    {
        _ended = true;
        Invoke(nameof(FreezeAndShow), 0.5f);
        _pendingWon = won;
    }
    bool _pendingWon;
    void FreezeAndShow()
    {
        Time.timeScale = 0f;
        if (GerenteManager.Instance == null) { GameHUD.Instance?.ShowEnd(_pendingWon); return; }

        if (!_pendingWon)
        {
            GerenteManager.Instance.ShowTimeGameOver();
            return;
        }

        int  repaired = initialBroken.Count - brokenMachines.Count;
        int  total    = initialBroken.Count;
        int  day      = DayManager.Instance?.CurrentDay ?? 1;
        bool isFinal  = DayManager.Instance?.IsFinalDay ?? false;

        if (isFinal)
            GerenteManager.Instance.ShowFinalVictory();
        else
            GerenteManager.Instance.ShowDayComplete(day, _timeLeft, repaired, total, OnDayComplete);
    }

    void OnDayComplete()
    {
        DayManager.Instance?.AdvanceDay();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
        Instance = null;
    }
}
