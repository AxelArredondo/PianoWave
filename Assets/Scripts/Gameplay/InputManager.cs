using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    [SerializeField] private LaneLayout laneLayout;

    void Start()
    {
        if (laneLayout == null)
            laneLayout = FindFirstObjectByType<LaneLayout>();
    }

    void Update()
    {
        if (GameManager.Instance.IsGameOver || GameManager.Instance.IsPaused)
            return;

        if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
        {
            HandleTap(Pointer.current.position.ReadValue());
        }

        if (laneLayout != null && Keyboard.current != null)
        {
            var kb = Keyboard.current;
            UnityEngine.InputSystem.Controls.KeyControl[] keys = { kb.aKey, kb.sKey, kb.dKey, kb.fKey };
            for (int i = 0; i < keys.Length && i < laneLayout.lanes.Length; i++)
            {
                if (keys[i].wasPressedThisFrame)
                {
                    HitReceptorController.Instance?.PulseReceptor(i);
                    HitLane(i);
                }
            }
        }
    }

    void HitLane(int laneIndex)
    {
        float laneX    = laneLayout.lanes[laneIndex].position.x;
        float threshold = laneLayout.LaneStepWorld * 0.5f;
        float hitY     = GameManager.Instance.hitLine != null ? GameManager.Instance.hitLine.position.y : 0f;

        Tile bestTile  = null;
        float bestDist = float.MaxValue;

        foreach (Tile tile in Tile.ActiveTiles)
        {
            if (Mathf.Abs(tile.transform.position.x - laneX) > threshold) continue;

            float dist = Mathf.Abs(tile.transform.position.y - hitY);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestTile = tile;
            }
        }

        bestTile?.Hit();
    }

    void HandleTap(Vector2 screenPosition)
    {
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(screenPosition.x, screenPosition.y, 0f)
        );

        // Pulse the receptor for whichever lane the tap falls in.
        if (laneLayout != null)
        {
            int tapLane = GetLaneIndexAtX(worldPos.x);
            if (tapLane >= 0)
                HitReceptorController.Instance?.PulseReceptor(tapLane);
        }

        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
        if (hit.collider != null)
        {
            Tile tile = hit.collider.GetComponent<Tile>();
            tile?.Hit();
        }
    }

    // Returns the lane index whose centre is closest to worldX, or -1 if outside all lanes.
    int GetLaneIndexAtX(float worldX)
    {
        if (laneLayout == null || laneLayout.lanes == null) return -1;

        float halfStep = laneLayout.LaneStepWorld * 0.5f;
        int best = -1;
        float bestDist = float.MaxValue;

        for (int i = 0; i < laneLayout.lanes.Length; i++)
        {
            float dist = Mathf.Abs(laneLayout.lanes[i].position.x - worldX);
            if (dist <= halfStep && dist < bestDist)
            {
                bestDist = dist;
                best = i;
            }
        }

        return best;
    }
}
