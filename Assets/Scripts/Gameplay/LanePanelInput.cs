using UnityEngine;
using UnityEngine.EventSystems;

// Transparent UI panel for one lane. Receives EventSystem pointer events and
// forwards them to InputManager so one physical tap == one HitLane call.
// Attach this alongside an Image component on each lane panel RectTransform.
[RequireComponent(typeof(UnityEngine.UI.Image))]
public class LanePanelInput : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [HideInInspector] public int laneIndex = -1;

    public void OnPointerDown(PointerEventData eventData)
    {
        InputManager.Instance?.OnLanePanelDown(laneIndex, eventData.pointerId);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        InputManager.Instance?.OnLanePanelUp(laneIndex, eventData.pointerId);
    }

    // Finger slid off the panel edge. We do NOT release hold here — the hold
    // tracks the original down-panel until OnPointerUp fires on the same element,
    // matching how the raw touch path tracks by touchId rather than position.
    public void OnPointerExit(PointerEventData eventData)
    {
        InputManager.Instance?.OnLanePanelExit(laneIndex, eventData.pointerId);
    }
}
