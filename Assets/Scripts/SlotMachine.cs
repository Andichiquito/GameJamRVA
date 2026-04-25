using UnityEngine;

public enum MachineState { Working, Broken }

public class SlotMachine : MonoBehaviour
{
    [Header("Info")]
    public string machineName;
    public int machineIndex;

    [Header("Lights")]
    public Light screenLight;
    public Light neonSign;

    [HideInInspector] public MachineState state = MachineState.Working;

    Color _origScreenColor;
    float _origScreenIntensity;
    float _flicker;

    void Start()
    {
        if (screenLight)
        {
            _origScreenColor     = screenLight.color;
            _origScreenIntensity = screenLight.intensity;
        }
    }

    void Update()
    {
        if (state == MachineState.Broken)
            DoFlicker();
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

    public void SetBroken()
    {
        state   = MachineState.Broken;
        _flicker = Random.Range(0f, 10f);
        if (neonSign) neonSign.gameObject.SetActive(true);
    }

    public void Repair()
    {
        state = MachineState.Working;
        if (screenLight)
        {
            screenLight.color     = _origScreenColor;
            screenLight.intensity = _origScreenIntensity;
        }
        if (neonSign) neonSign.gameObject.SetActive(false);
    }

    // Called when the repair timer runs out — machine gets worse (screen goes dark)
    public void SetMoreBroken()
    {
        _flicker = Random.Range(0f, 10f);
        if (screenLight) screenLight.intensity = 0f;
    }

    // XZ-plane distance so height doesn't affect proximity
    public bool IsPlayerNearby(Transform player, float range = 3.5f)
    {
        var a = new Vector3(transform.position.x, 0f, transform.position.z);
        var b = new Vector3(player.position.x,    0f, player.position.z);
        return Vector3.Distance(a, b) < range;
    }
}
