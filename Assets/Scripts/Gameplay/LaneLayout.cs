using UnityEngine;

public class LaneLayout : MonoBehaviour
{
    public Transform[] lanes;
    public float yPosition = 4f;

    [Header("Fixed spacing")]
    public float laneStepWorld = 1.2f; // distance between lane centers (constant)
    public float maxTotalSpanPercentOfScreen = 0.95f; // shrink if screen too narrow

    public float LaneStepWorld { get; private set; }

    Camera cam;

    void Start()
    {
        cam = Camera.main;
        ApplyLayout();
    }

    void Update()
    {
        ApplyLayout();
    }

    void ApplyLayout()
    {
        if (cam == null || lanes == null || lanes.Length == 0) return;

        float halfW = cam.orthographicSize * cam.aspect;
        float screenW = halfW * 2f;

        float desiredSpan = laneStepWorld * (lanes.Length - 1);
        float maxSpan = screenW * maxTotalSpanPercentOfScreen;

        float span = Mathf.Min(desiredSpan, maxSpan);
        float step = (lanes.Length == 1) ? 0f : span / (lanes.Length - 1);

        LaneStepWorld = step;

        float startX = -span / 2f;

        for (int i = 0; i < lanes.Length; i++)
        {
            Vector3 p = lanes[i].position;
            p.x = startX + step * i;
            p.y = yPosition;
            lanes[i].position = p;
        }
    }
}