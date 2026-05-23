using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    RectTransform _rt;
    Rect _lastSafeArea;

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        Apply();
    }

    void Update()
    {
        if (Screen.safeArea != _lastSafeArea)
            Apply();
    }

    void Apply()
    {
        _lastSafeArea = Screen.safeArea;
        Vector2 screen = new Vector2(Screen.width, Screen.height);
        _rt.anchorMin = Screen.safeArea.position / screen;
        _rt.anchorMax = (Screen.safeArea.position + Screen.safeArea.size) / screen;
        _rt.offsetMin = Vector2.zero;
        _rt.offsetMax = Vector2.zero;
    }
}
