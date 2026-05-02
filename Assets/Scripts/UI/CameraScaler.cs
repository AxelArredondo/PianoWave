using UnityEngine;
using System.Collections;

[DefaultExecutionOrder(-10)]
[RequireComponent(typeof(Camera))]
public class CameraScaler : MonoBehaviour
{
    [Header("Reference (what your game was designed for)")]
    public float referenceWidthPixels = 1080f;
    public float referenceHeightPixels = 1920f;

    [Header("Sprite Import Settings")]
    public float pixelsPerUnit = 100f;

    [Header("Safety clamps (tune these to make things bigger/smaller)")]
    public float minOrthoSize = 6f;
    public float maxOrthoSize = 12f; // LOWER = zoom in more (tiles look bigger)

    private Camera cam;
    private int lastW, lastH;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    IEnumerator Start()
    {
        // Wait until Game view has final size/aspect
        yield return new WaitForEndOfFrame();
        ApplyScale();
        Cache();
    }

    void Update()
    {
        if (Screen.width != lastW || Screen.height != lastH)
        {
            ApplyScale();
            Cache();
        }
    }

    void Cache()
    {
        lastW = Screen.width;
        lastH = Screen.height;
    }

    void ApplyScale()
    {
        if (cam == null || !cam.orthographic) return;

        float orthoByHeight = (referenceHeightPixels / pixelsPerUnit) * 0.5f;
        float orthoByWidth = (referenceWidthPixels / pixelsPerUnit) * 0.5f / cam.aspect;

        float ortho = Mathf.Max(orthoByHeight, orthoByWidth);
        cam.orthographicSize = Mathf.Clamp(ortho, minOrthoSize, maxOrthoSize);
    }
}