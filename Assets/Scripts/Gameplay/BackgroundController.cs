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

    void Update()
    {
        if (Screen.width != lastW || Screen.height != lastH)
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
        if (sr == null || sr.sprite == null || cam == null || !cam.orthographic) return;

        float worldScreenHeight = cam.orthographicSize * 2f;
        float worldScreenWidth = worldScreenHeight * cam.aspect;

        Vector2 spriteSize = sr.sprite.bounds.size;

        float scaleX = worldScreenWidth / spriteSize.x;
        float scaleY = worldScreenHeight / spriteSize.y;

        float scale = coverScreen ? Mathf.Max(scaleX, scaleY) : Mathf.Min(scaleX, scaleY);

        transform.localScale = new Vector3(scale, scale, 1f);

        Vector3 p = transform.position;
        p.x = 0f;
        p.y = 0f;
        p.z = zPosition;
        transform.position = p;
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