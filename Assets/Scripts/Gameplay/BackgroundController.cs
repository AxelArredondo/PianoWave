using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BackgroundController : MonoBehaviour
{
    [Header("Fit To Screen")]
    public Camera cam;                // leave empty to auto-use Main Camera
    public bool coverScreen = true;   // true = fills screen (may crop), false = fully visible (may letterbox)
    public float zPosition = 0f;      // set behind gameplay if needed

    private SpriteRenderer sr;
    private int lastW, lastH;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (cam == null) cam = Camera.main;

        FitToCamera();
        CacheRes();
    }

    void LateUpdate()
    {
        if (cam == null) return;

        if (Screen.width != lastW ||
            Screen.height != lastH ||
            !Mathf.Approximately(cam.orthographicSize * 2f, sr.bounds.size.y))
        {
            FitToCamera();
            CacheRes();
        }
    }

    void CacheRes()
    {
        lastW = Screen.width;
        lastH = Screen.height;
    }

    void FitToCamera()
    {
        if (sr == null || sr.sprite == null || cam == null || !cam.orthographic)
            return;

        // Camera visible size in world units
        float worldHeight = cam.orthographicSize * 2f;
        float worldWidth = worldHeight * cam.aspect;

        // Sprite native size (at scale 1)
        Vector2 spriteSize = sr.sprite.bounds.size;

        // Scale independently in X and Y so it matches camera exactly
        float scaleX = worldWidth / spriteSize.x;
        float scaleY = worldHeight / spriteSize.y;

        // Exact match (no zoom crop)
        transform.localScale = new Vector3(scaleX, scaleY, 1f);

        // Always center it on camera
        Vector3 camPos = cam.transform.position;
        transform.position = new Vector3(camPos.x, camPos.y, zPosition);
    }

    void OnEnable()
    {
        BeatManager.OnBeat += Pulse;
    }

    void OnDisable()
    {
        BeatManager.OnBeat -= Pulse;
    }

    void Pulse()
    {
        if (GameManager.Instance.IsGameOver) return;

        sr.color = new Color(1f, 0.5f, 0.8f);
        Invoke(nameof(ResetColor), 0.1f);
    }

    void ResetColor()
    {
        sr.color = Color.white;
    }
}