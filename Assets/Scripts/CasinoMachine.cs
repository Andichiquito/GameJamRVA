using UnityEngine;

public enum MachineState { Working, Broken }

// Base class shared by SlotMachine, RouletteTable and CardTable.
public abstract class CasinoMachine : MonoBehaviour
{
    [Header("Info")]
    public string machineName;
    public int    machineIndex;

    [Header("Lights")]
    public Light screenLight;
    public Light neonSign;

    [HideInInspector] public MachineState state = MachineState.Working;

    protected Color _origScreenColor;
    protected float _origScreenIntensity;
    float _flicker;

    protected virtual void Start()
    {
        if (screenLight)
        {
            _origScreenColor     = screenLight.color;
            _origScreenIntensity = screenLight.intensity;
        }
    }

    protected virtual void Update()
    {
        if (state == MachineState.Broken) DoFlicker();
    }

    void DoFlicker()
    {
        _flicker += Time.deltaTime;
        float t = Mathf.Abs(Mathf.Sin(_flicker * 20f) * Mathf.Sin(_flicker * 6.3f));

        if (screenLight)
        {
            screenLight.color     = Color.red;
            screenLight.intensity = t * 2.2f;
        }
        if (neonSign && neonSign.gameObject.activeSelf)
        {
            neonSign.color     = new Color(1f, 0.12f, 0f);
            neonSign.intensity = t * 4f + 0.4f;
        }
    }

    public virtual void SetBroken()
    {
        state    = MachineState.Broken;
        _flicker = Random.Range(0f, 10f);
        if (neonSign) neonSign.gameObject.SetActive(true);
    }

    public virtual void Repair()
    {
        state = MachineState.Working;
        if (screenLight)
        {
            screenLight.color     = _origScreenColor;
            screenLight.intensity = _origScreenIntensity;
        }
        if (neonSign) neonSign.gameObject.SetActive(false);
    }

    public virtual void SetMoreBroken()
    {
        _flicker = Random.Range(0f, 10f);
        if (screenLight) screenLight.intensity = 0f;
    }

    // Subclasses override to show a custom interaction hint
    public virtual string InteractPrompt => "[E]  Reparar";

    public bool IsPlayerNearby(Transform player, float range = 3.5f)
    {
        float dx = transform.position.x - player.position.x;
        float dz = transform.position.z - player.position.z;
        return dx * dx + dz * dz < range * range;
    }
}
