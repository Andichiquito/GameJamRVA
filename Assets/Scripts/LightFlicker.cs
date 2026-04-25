using System.Collections;
using UnityEngine;

public class LightFlicker : MonoBehaviour
{
    [Header("Timing")]
    public float minStableTime  = 1.2f;
    public float maxStableTime  = 6f;
    public float flickerSpeed   = 0.05f;
    public int   maxFlickers    = 4;

    [Header("Intensity")]
    public float normalIntensity = 1.8f;
    public float dimIntensity    = 0.05f;

    private Light _light;

    void Start()
    {
        _light = GetComponent<Light>();
        _light.intensity = normalIntensity;
        StartCoroutine(FlickerLoop());
    }

    IEnumerator FlickerLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minStableTime, maxStableTime));

            int count = Random.Range(1, maxFlickers + 1);
            for (int i = 0; i < count; i++)
            {
                _light.intensity = dimIntensity;
                yield return new WaitForSeconds(flickerSpeed * Random.Range(0.5f, 2f));
                _light.intensity = normalIntensity;
                yield return new WaitForSeconds(flickerSpeed * Random.Range(0.3f, 1.2f));
            }
        }
    }
}
