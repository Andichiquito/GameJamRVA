using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Config")]
    public float turnDuration = 300f;  // 5 minutos

    // All machines in the scene, sorted by machineIndex
    [HideInInspector] public List<SlotMachine> allMachines      = new();
    // Subset that started broken (for task list)
    [HideInInspector] public List<SlotMachine> initialBroken    = new();
    // Subset still broken
    [HideInInspector] public List<SlotMachine> brokenMachines   = new();

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
        _timeLeft = turnDuration;

        allMachines.AddRange(FindObjectsByType<SlotMachine>(FindObjectsSortMode.None));
        allMachines.Sort((a, b) => a.machineIndex.CompareTo(b.machineIndex));

        BreakRandom();
        GameHUD.Instance?.RefreshTaskList();

        // Auto-create GerenteManager if not already in scene
        if (GerenteManager.Instance == null)
        {
            var go = new GameObject("GerenteManager");
            go.AddComponent<GerenteManager>();
        }
        GerenteManager.Instance.ShowIntro(OnIntroComplete);
    }

    void OnIntroComplete() => _gameStarted = true;

    void BreakRandom()
    {
        var pool = new List<SlotMachine>(allMachines);
        // Fisher-Yates shuffle
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }
        int count = Mathf.Clamp(Random.Range(2, 4), 0, pool.Count);
        for (int i = 0; i < count; i++)
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

    public void StartRepair(SlotMachine machine)
        => RepairMinigame.Instance?.Open(machine);

    public void NotifyRepaired(SlotMachine machine)
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
        if (GerenteManager.Instance != null)
        {
            if (_pendingWon) GerenteManager.Instance.ShowVictory();
            else             GerenteManager.Instance.ShowTimeGameOver();
        }
        else
        {
            GameHUD.Instance?.ShowEnd(_pendingWon);
        }
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
        Instance = null;
    }
}
