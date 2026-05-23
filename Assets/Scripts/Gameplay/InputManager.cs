using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    // Per-lane held state, polled each frame by HoldTile.UpdateHold.
    // Index matches laneLayout.lanes[].
    // Cleared at the top of every Update so it only reflects the current frame.
    public static bool[] LaneHeld = new bool[4];

    [SerializeField] private LaneLayout laneLayout;

    // Which lane index the pointer was pressed on — tracked for hold notes.
    private int pointerHeldLane = -1;

    void Start()
    {
        if (laneLayout == null)
            laneLayout = FindFirstObjectByType<LaneLayout>();

        if (laneLayout != null)
            LaneHeld = new bool[laneLayout.lanes.Length];
    }

    void Update()
    {
        // Always clear first so stale held-states don't persist across frames.
        System.Array.Clear(LaneHeld, 0, LaneHeld.Length);

        if (GameManager.Instance.IsGameOver || GameManager.Instance.IsPaused)
        {
            pointerHeldLane = -1;
            return;
        }

        HandlePointer();
        HandleKeyboard();
    }

    // ── pointer (mouse / touch) ────────────────────────────────────────────────

    void HandlePointer()
    {
        if (Pointer.current == null) return;

        if (Pointer.current.press.wasPressedThisFrame)
        {
            Vector2 screenPos = Pointer.current.position.ReadValue();
            HandleTap(screenPos);

            // Record the lane so we can mark it as held in subsequent frames.
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(
                new Vector3(screenPos.x, screenPos.y, 0f));
            pointerHeldLane = GetLaneIndexAtX(worldPos.x);
        }

        if (Pointer.current.press.isPressed
            && pointerHeldLane >= 0
            && pointerHeldLane < LaneHeld.Length)
        {
            LaneHeld[pointerHeldLane] = true;
        }

        if (Pointer.current.press.wasReleasedThisFrame)
            pointerHeldLane = -1;
    }

    // ── keyboard ───────────────────────────────────────────────────────────────

    void HandleKeyboard()
    {
        if (laneLayout == null || Keyboard.current == null) return;

        var kb = Keyboard.current;
        var keys = new[]
        {
            kb.aKey, kb.sKey, kb.dKey, kb.fKey
        };

        for (int i = 0; i < keys.Length && i < laneLayout.lanes.Length; i++)
        {
            if (keys[i].wasPressedThisFrame)
            {
                HitReceptorController.Instance?.PulseReceptor(i);
                HitLane(i);
            }

            if (keys[i].isPressed)
                LaneHeld[i] = true;
        }
    }

    // ── lane hit logic ─────────────────────────────────────────────────────────

    void HitLane(int laneIndex)
    {
        float laneX     = laneLayout.lanes[laneIndex].position.x;
        float threshold = laneLayout.LaneStepWorld * 0.5f;
        float hitY      = GameManager.Instance.hitLine != null
            ? GameManager.Instance.hitLine.position.y : 0f;

        Tile bestTile  = null;
        float bestDist = float.MaxValue;

        foreach (Tile tile in Tile.ActiveTiles)
        {
            if (Mathf.Abs(tile.transform.position.x - laneX) > threshold) continue;

            float dist = Mathf.Abs(tile.transform.position.y - hitY);
            if (dist < bestDist) { bestDist = dist; bestTile = tile; }
        }

        bestTile?.Hit();
    }

    void HandleTap(Vector2 screenPosition)
    {
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(screenPosition.x, screenPosition.y, 0f));

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
        int   best     = -1;
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
