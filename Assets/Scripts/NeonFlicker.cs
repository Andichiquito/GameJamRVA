using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class NeonFlicker : MonoBehaviour
{
    [Header("Colores neón")]
    public Color colorRojo  = new Color(0.9f, 0.05f, 0.05f, 1f);
    public Color colorDorado = new Color(0.95f, 0.78f, 0.05f, 1f);

    [Header("Tiempos (segundos)")]
    public float tiempoMinimoEstable = 0.8f;
    public float tiempoMaximoEstable = 3.5f;
    public float duracionParpadeo    = 0.08f;
    public float duracionTransicion  = 0.25f;

    private Text _texto;
    private Color _colorActual;

    void Start()
    {
        _texto = GetComponent<Text>();
        _colorActual = colorRojo;
        _texto.color = _colorActual;
        StartCoroutine(CicloParpadeo());
    }

    IEnumerator CicloParpadeo()
    {
        while (true)
        {
            // Espera aleatoria antes del próximo evento
            yield return new WaitForSeconds(Random.Range(tiempoMinimoEstable, tiempoMaximoEstable));

            // Decide si hace un parpadeo rápido o un cambio de color suave
            if (Random.value < 0.5f)
                yield return StartCoroutine(ParpadeoBrusco());
            else
                yield return StartCoroutine(CambioSuave());
        }
    }

    // Apaga y enciende 1-3 veces rápidamente
    IEnumerator ParpadeoBrusco()
    {
        int veces = Random.Range(1, 4);
        for (int i = 0; i < veces; i++)
        {
            yield return StartCoroutine(Fade(_colorActual, Color.black, duracionParpadeo * 0.5f));
            yield return StartCoroutine(Fade(Color.black, _colorActual, duracionParpadeo * 0.5f));
            yield return new WaitForSeconds(Random.Range(0.04f, 0.12f));
        }
    }

    // Transición suave entre rojo y dorado
    IEnumerator CambioSuave()
    {
        Color destino = (_colorActual == colorRojo) ? colorDorado : colorRojo;
        yield return StartCoroutine(Fade(_colorActual, destino, duracionTransicion));
        _colorActual = destino;
    }

    IEnumerator Fade(Color desde, Color hasta, float duracion)
    {
        float t = 0f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            _texto.color = Color.Lerp(desde, hasta, t / duracion);
            yield return null;
        }
        _texto.color = hasta;
    }
}
