using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class InputManager : MonoBehaviour
{
    // Per-lane held state, polled each frame by Tile.UpdateHold.
    // Index matches laneLayout.lanes[]. Cleared at the top of every Update.
    public static bool[] LaneHeld = new bool[4];

    [SerializeField] private LaneLayout laneLayout;

    // ── single-pointer (mouse) hold tracking ──────────────────────────────────
    private int pointerHeldLane = -1;

    // ── multi-touch hold tracking ─────────────────────────────────────────────
    // Maps each finger's touchId to the lane it pressed, kept alive until release.
    private readonly Dictionary<int, int> touchLaneMap = new Dictionary<int, int>();

    // ── lifecycle ──────────────────────────────────────────────────────────────

    void Start()
    {
        if (laneLayout == null)
            laneLayout = FindFirstObjectByType<LaneLayout>();

        if (laneLayout != null)
            LaneHeld = new bool[laneLayout.lanes.Length];
    }

    void Update()
    {
        System.Array.Clear(LaneHeld, 0, LaneHeld.Length);

        if (GameManager.Instance.IsGameOver || GameManager.Instance.IsPaused)
        {
            pointerHeldLane = -1;
            touchLaneMap.Clear();
            return;
        }

        // Use per-finger touch tracking on any device that has a touchscreen.
        // Fall back to single-pointer (mouse) when no touchscreen is present.
        if (Touchscreen.current != null)
            HandleTouchscreen();
        else
            HandlePointer();

        HandleKeyboard();
    }

    // ── touchscreen — multi-touch ──────────────────────────────────────────────
    //
    // Each finger has a unique touchId. On Began: detect lane, record mapping, hit.
    // While active (Began / Moved / Stationary): mark that lane as held.
    // On Ended / Canceled: remove the mapping.
    // This lets two fingers hold two lanes simultaneously.

    void HandleTouchscreen()
    {
        foreach (var touch in Touchscreen.current.touches)
        {
            UnityEngine.InputSystem.TouchPhase phase = touch.phase.ReadValue();
            if (phase == UnityEngine.InputSystem.TouchPhase.None) continue;

            int     touchId   = touch.touchId.ReadValue();
            Vector2 screenPos = touch.position.ReadValue();

            if (phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                int lane = ScreenPosToLane(screenPos);
                if (lane >= 0)
                {
                    touchLaneMap[touchId] = lane;
                    HitReceptorController.Instance?.PulseReceptor(lane);
                    HitLane(lane);
                }
            }

            // Keep the lane marked as held for every active phase of this finger.
            if (touchLaneMap.TryGetValue(touchId, out int heldLane)
                && heldLane >= 0 && heldLane < LaneHeld.Length)
            {
                LaneHeld[heldLane] = true;
            }

            if (phase == UnityEngine.InputSystem.TouchPhase.Ended
                || phase == UnityEngine.InputSystem.TouchPhase.Canceled)
                touchLaneMap.Remove(touchId);
        }
    }

    // ── pointer — mouse (single pointer, no touchscreen) ──────────────────────

    void HandlePointer()
    {
        if (Pointer.current == null) return;

        if (Pointer.current.press.wasPressedThisFrame)
        {
            Vector2 screenPos = Pointer.current.position.ReadValue();
            HandleMouseTap(screenPos);
            pointerHeldLane = ScreenPosToLane(screenPos);
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

        var kb   = Keyboard.current;
        var keys = new[] { kb.aKey, kb.sKey, kb.dKey, kb.fKey };

        for (int i = 0; i < keys.Length && i < laneLayout.lanes.Length; i++)
        {
            if (keys[i].wasPressedThisFrame)
            {
                HitReceptorController.Instance?.PulseReceptor(i);
                HitLane(i);
            }

            if (keys[i].isPressed) LaneHeld[i] = true;
        }
    }

    // ── hit helpers ────────────────────────────────────────────────────────────

    // Finds the nearest tile to the hitline within the given lane and calls Hit().
    // Used by keyboard and touch — works anywhere in the lane, not just on the collider.
    void HitLane(int laneIndex)
    {
        float laneX     = laneLayout.lanes[laneIndex].position.x;
        float threshold = laneLayout.LaneStepWorld * 0.5f;
        float hitY      = GameManager.Instance.hitLine != null
            ? GameManager.Instance.hitLine.position.y : 0f;

        Tile  bestTile = null;
        float bestDist = float.MaxValue;

        foreach (Tile tile in Tile.ActiveTiles)
        {
            if (Mathf.Abs(tile.transform.position.x - laneX) > threshold) continue;
            float dist = Mathf.Abs(tile.transform.position.y - hitY);
            if (dist < bestDist) { bestDist = dist; bestTile = tile; }
        }

        bestTile?.Hit();
    }

    // Mouse tap uses a raycast so clicking precisely on a tile collider is honoured.
    void HandleMouseTap(Vector2 screenPosition)
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
            hit.collider.GetComponent<Tile>()?.Hit();
    }

    // ── helpers ────────────────────────────────────────────────────────────────

    // Convert a screen position to a lane index, or -1 if outside all lanes.
    int ScreenPosToLane(Vector2 screenPos)
    {
        if (Camera.main == null) return -1;
        Vector3 world = Camera.main.ScreenToWorldPoint(
            new Vector3(screenPos.x, screenPos.y, 0f));
        return GetLaneIndexAtX(world.x);
    }

    int GetLaneIndexAtX(float worldX)
    {
        if (laneLayout == null || laneLayout.lanes == null) return -1;

        float halfStep = laneLayout.LaneStepWorld * 0.5f;
        int   best     = -1;
        float bestDist = float.MaxValue;

        for (int i = 0; i < laneLayout.lanes.Length; i++)
        {
            float dist = Mathf.Abs(laneLayout.lanes[i].position.x - worldX);
            if (dist <= halfStep && dist < bestDist) { bestDist = dist; best = i; }
        }

        return best;
    }
}
