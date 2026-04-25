using UnityEngine;

public class DayManager : MonoBehaviour
{
    public static DayManager Instance { get; private set; }

    const string PREF_KEY = "GameJam_CurrentDay";
    public const int MAX_DAYS = 5;

    public int  CurrentDay  { get; private set; }
    public bool IsFinalDay  => CurrentDay >= MAX_DAYS;

    public struct DayConfig
    {
        public int   caseritos;
        public int   machinesToBreak;
        public float turnDuration;
    }

    // Indexed by day-1
    static readonly DayConfig[] Configs =
    {
        new DayConfig { caseritos = 2, machinesToBreak = 3, turnDuration = 300f },
        new DayConfig { caseritos = 3, machinesToBreak = 4, turnDuration = 240f },
        new DayConfig { caseritos = 4, machinesToBreak = 4, turnDuration = 240f },
        new DayConfig { caseritos = 4, machinesToBreak = 5, turnDuration = 210f },
        new DayConfig { caseritos = 5, machinesToBreak = 6, turnDuration = 180f },
    };

    public DayConfig Config => Configs[CurrentDay - 1];

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance   = this;
        CurrentDay = Mathf.Clamp(PlayerPrefs.GetInt(PREF_KEY, 1), 1, MAX_DAYS);
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    public void AdvanceDay()
    {
        if (CurrentDay >= MAX_DAYS) return;
        CurrentDay++;
        PlayerPrefs.SetInt(PREF_KEY, CurrentDay);
        PlayerPrefs.Save();
    }

    public void ResetProgress()
    {
        CurrentDay = 1;
        PlayerPrefs.SetInt(PREF_KEY, 1);
        PlayerPrefs.Save();
    }
}
