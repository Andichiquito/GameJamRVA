using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class CableSocket : MonoBehaviour
{
    [HideInInspector] public int  expectedColorIndex;
    [HideInInspector] public bool occupied;

    Image _bg;
    void Awake() => _bg = GetComponent<Image>();

    public void SetHovered(bool hovered)
    {
        if (!occupied)
            _bg.color = hovered
                ? new Color(0.22f, 0.17f, 0.07f)
                : new Color(0.07f, 0.05f, 0.03f);
    }

    public void MarkOccupied(Color col)
    {
        occupied = true;
        _bg.color = col * new Color(1, 1, 1, 0.5f);
    }
}
