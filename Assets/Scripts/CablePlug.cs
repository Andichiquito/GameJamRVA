using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class CablePlug : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [HideInInspector] public int  colorIndex;
    [HideInInspector] public bool connected;

    public void OnBeginDrag(PointerEventData e)
    {
        if (connected) return;
        RepairMinigame.Instance.OnPlugBeginDrag(this, e);
    }

    public void OnDrag(PointerEventData e)
    {
        if (connected) return;
        RepairMinigame.Instance.OnPlugDrag(this, e);
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (connected) return;

        // Find socket under cursor manually (drag events capture input)
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(e, results);

        CableSocket target = null;
        foreach (var r in results)
        {
            var s = r.gameObject.GetComponent<CableSocket>();
            if (s != null && !s.occupied) { target = s; break; }
        }

        RepairMinigame.Instance.OnPlugEndDrag(this, target);
    }
}
