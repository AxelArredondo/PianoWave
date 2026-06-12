using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class InputManager : MonoBehaviour
{
    // Per-lane held state, polled each frame by Tile.UpdateHold.
    // Index matches laneLayout.lanes[]. Cleared at the top of every Update.
    public static bool[] LaneHeld = new bool[4];

    [SerializeField] private LaneLayout laneLayout;

    // ── touch settings ─────────────────────────────────────────────────────────
    [Header("Touch Settings")]
    [Tooltip("Snap to nearest lane up to this many lane-steps away. Higher = more forgiving edge taps.")]
    [SerializeField] private float touchForgivenessMultiplier = 1.5f;

    // ── tap debug ──────────────────────────────────────────────────────────────
    [Header("Tap Debug")]
    [SerializeField] private bool showTapDebug = false;
    [SerializeField] private float tapMarkerDuration = 1.5f;
    private Sprite _debugCircleSprite;

    // Last touch coords — set on TouchPhase.Began, read by BuildAndRecordTapContext.
    private Vector2 _lastTouchScreenPos;
    private Vector3 _lastTouchWorldPos;

    // ── singleton guard ────────────────────────────────────────────────────────
    private static InputManager _instance;
    public static InputManager Instance => _instance;

    // ── per-tap deduplication ─────────────────────────────────────────────────
    // _tapCounter increments on every unique physical tap (one per Began event).
    // _currentTapId is the id assigned to the tap currently being processed.
    // _laneHitFrame guards against the same lane being hit more than once per frame.
    // _lastTapTileHitId lets us detect if a tapId consumed more than one tile.
    private int   _tapCounter        = 0;
    private int   _currentTapId      = 0;
    private int   _lastTapTileHitId  = -1;
    private int[] _laneHitFrame;    // indexed by lane; value = frame number of last hit
    private int[] _laneHitTapId;    // indexed by lane; value = tapId of last hit

    // ── single-pointer (mouse) hold tracking ──────────────────────────────────
    private int pointerHeldLane = -1;

    // ── multi-touch hold tracking ─────────────────────────────────────────────
    // Maps each finger's touchId to the lane it pressed, kept alive until release.
    private readonly Dictionary<int, int> touchLaneMap = new Dictionary<int, int>();

    // ── UI lane panel hold tracking ────────────────────────────────────────────
    // Maps EventSystem pointerId → lane index while a finger holds a lane panel.
    // Populated by OnLanePanelDown, removed by OnLanePanelUp.
    private readonly Dictionary<int, int> _panelPointerLaneMap = new Dictionary<int, int>();

    // ── lifecycle ──────────────────────────────────────────────────────────────

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Debug.LogError(
                $"[InputManager] Duplicate InputManager detected on '{gameObject.name}'. " +
                $"Only one InputManager may exist. Destroying this one.");
            Destroy(this);
            return;
        }
        _instance = this;

        if (showTapDebug)
            _debugCircleSprite = BuildCircleSprite(32);
    }

    void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    void Start()
    {
        if (laneLayout == null)
            laneLayout = FindFirstObjectByType<LaneLayout>();

        int n = laneLayout != null ? laneLayout.lanes.Length : 4;
        LaneHeld      = new bool[n];
        _laneHitFrame = new int[n];
        _laneHitTapId = new int[n];
        for (int i = 0; i < n; i++) { _laneHitFrame[i] = -1; _laneHitTapId[i] = -1; }
    }

    void Update()
    {
        System.Array.Clear(LaneHeld, 0, LaneHeld.Length);

        if (GameManager.Instance.IsGameOver || GameManager.Instance.IsPaused)
        {
            pointerHeldLane = -1;
            touchLaneMap.Clear();
            _panelPointerLaneMap.Clear();
            return;
        }

        // Re-apply held state from UI lane panels still active this frame.
        foreach (var kvp in _panelPointerLaneMap)
            if (kvp.Value >= 0 && kvp.Value < LaneHeld.Length)
                LaneHeld[kvp.Value] = true;

        // On Android at runtime, EventSystem lane panels handle all touch input.
        // Raw Touchscreen.current is bypassed to prevent double-hits.
        // The Editor and non-Android platforms always use raw touch/pointer as before.
#if UNITY_ANDROID && !UNITY_EDITOR
        if (Touchscreen.current != null)
        {
            if (LanePanelSetup.PanelsCreated)
            {
                if (showTapDebug) ConfirmRawTouchBypassed();
            }
            else
            {
                HandleTouchscreen();  // safety net: panels missing, fall back to raw touch
            }
        }
        else
            HandlePointer();
#else
        if (Touchscreen.current != null)
            HandleTouchscreen();
        else
            HandlePointer();
#endif

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
                // Guard: reject if this touchId already has an active entry.
                // Some Android drivers re-fire Began for the same finger contact.
                if (touchLaneMap.ContainsKey(touchId))
                {
                    Debug.LogWarning(
                        $"[InputDuplicate] touchId={touchId} fired Began again " +
                        $"(frame={Time.frameCount}) — duplicate suppressed");
                }
                else
                {
                    _currentTapId = ++_tapCounter;

                    int lane = ScreenPosToLaneTouchForgiving(screenPos);

                    _lastTouchScreenPos = screenPos;
                    _lastTouchWorldPos  = Camera.main != null
                        ? Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f))
                        : Vector3.zero;

                    if (showTapDebug)
                        Debug.Log(
                            $"[TapEvent] tapId={_currentTapId} frame={Time.frameCount} " +
                            $"time={Time.time:F3} src=Touch " +
                            $"screen=({screenPos.x:F0},{screenPos.y:F0}) " +
                            $"world=({_lastTouchWorldPos.x:F3},{_lastTouchWorldPos.y:F3}) " +
                            $"lane={lane}");

                    if (showTapDebug)
                        SpawnTapMarker(_lastTouchWorldPos, lane);

                    if (lane >= 0)
                    {
                        touchLaneMap[touchId] = lane;
                        HitReceptorController.Instance?.PulseReceptor(lane);
                        HitLane(lane, showTapDebug, isTouch: true);
                    }
                    else if (TapDebugMode.Instance != null && TapDebugMode.Instance.debugMode)
                    {
                        float hl = GameManager.Instance?.hitLine != null
                            ? GameManager.Instance.hitLine.position.y : 0f;
                        TapDebugMode.Instance.RecordTapContext(new TapDebugMode.TapContext
                        {
                            screenPos = _lastTouchScreenPos,
                            worldPos  = _lastTouchWorldPos,
                            lane      = -1,
                            tileFound = false,
                            hitLineY  = hl,
                            result    = "NoLane",
                            reason    = $"worldX={_lastTouchWorldPos.x:F3} outside all lanes (forgivenessMultiplier={touchForgivenessMultiplier})"
                        });
                    }
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
                _currentTapId = ++_tapCounter;
                if (showTapDebug)
                    Debug.Log($"[TapEvent] tapId={_currentTapId} frame={Time.frameCount} time={Time.time:F3} src=Keyboard lane={i}");
                HitReceptorController.Instance?.PulseReceptor(i);
                HitLane(i);
            }

            if (keys[i].isPressed) LaneHeld[i] = true;
        }
    }

    // ── hit helpers ────────────────────────────────────────────────────────────

    // Finds the nearest tile to the hitline within the given lane and calls Hit().
    // Used by keyboard and touch — works anywhere in the lane, not just on the collider.
    void HitLane(int laneIndex, bool debugLog = false, bool isTouch = false)
    {
        // ── per-frame, per-lane duplicate guard ───────────────────────────────
        // Prevents a second hit on the same lane within the same frame regardless
        // of how many times this method is invoked (duplicate touches, etc.).
        if (_laneHitFrame != null && laneIndex >= 0 && laneIndex < _laneHitFrame.Length)
        {
            if (_laneHitFrame[laneIndex] == Time.frameCount)
            {
                Debug.LogWarning(
                    $"[InputDuplicate] tapId={_currentTapId} lane={laneIndex} " +
                    $"frame={Time.frameCount} — lane already processed this frame, ignoring");
                return;
            }
            _laneHitFrame[laneIndex] = Time.frameCount;
            _laneHitTapId[laneIndex] = _currentTapId;
        }

        float laneX     = laneLayout.lanes[laneIndex].position.x;
        float threshold = laneLayout.LaneStepWorld * 0.5f;
        float hitY      = GameManager.Instance.hitLine != null
            ? GameManager.Instance.hitLine.position.y : 0f;

        Tile  bestTile   = null;
        float bestDist   = float.MaxValue;
        int   candidates = 0;

        foreach (Tile tile in Tile.ActiveTiles)
        {
            if (Mathf.Abs(tile.transform.position.x - laneX) > threshold) continue;
            candidates++;
            float dist = Mathf.Abs(tile.transform.position.y - hitY);
            if (dist < bestDist) { bestDist = dist; bestTile = tile; }
        }

        if (candidates > 1)
            Debug.LogWarning(
                $"[TapDebug] tapId={_currentTapId} lane={laneIndex}: " +
                $"{candidates} candidate tiles found — selecting closest ({bestTile?.name ?? "none"})");

        if (debugLog)
        {
            if (bestTile != null)
                Debug.Log($"[TapDebug] HitLane({laneIndex}): tile at Y={bestTile.transform.position.y:F3}  hitY={hitY:F3}  dist={bestDist:F3} → Hit() called");
            else
                Debug.Log($"[TapDebug] HitLane({laneIndex}): no tile found  ActiveTiles={Tile.ActiveTiles.Count}  hitY={hitY:F3}");
        }

        if (bestTile != null)
        {
            bool firstHit = _currentTapId != _lastTapTileHitId;
            _lastTapTileHitId = _currentTapId;
            if (showTapDebug)
                Debug.Log(
                    $"[TileHit] tapId={_currentTapId} tile={bestTile.name} " +
                    $"lane={laneIndex} frame={Time.frameCount} " +
                    $"dist={bestDist:F3} time={Time.time:F3} firstHit={firstHit}");
            if (!firstHit)
                Debug.LogWarning(
                    $"[TapDuplicate] tapId={_currentTapId} is consuming a 2nd tile " +
                    $"(lane={laneIndex}) — duplicate input reached Hit()!");
        }

        if (isTouch && TapDebugMode.Instance != null && TapDebugMode.Instance.debugMode)
            BuildAndRecordTapContext(laneIndex, bestTile);

        bestTile?.Hit();
    }

    // Replicates Tile.Hit()'s exact evaluation to predict the outcome without
    // changing any gameplay logic. Called only from the touch path in debug mode.
    void BuildAndRecordTapContext(int laneIndex, Tile tile)
    {
        float hitLineY = GameManager.Instance?.hitLine != null
            ? GameManager.Instance.hitLine.position.y : 0f;

        var ctx = new TapDebugMode.TapContext
        {
            screenPos = _lastTouchScreenPos,
            worldPos  = _lastTouchWorldPos,
            lane      = laneIndex,
            hitLineY  = hitLineY
        };

        if (tile == null)
        {
            ctx.tileFound = false;
            ctx.result    = "NoTile";
            ctx.reason    = $"No tile in lane {laneIndex} within halfStep of hitY={hitLineY:F3} (ActiveTiles={Tile.ActiveTiles.Count})";
        }
        else
        {
            ctx.tileFound         = true;
            ctx.tilePos           = tile.transform.position;
            ctx.distTileToHitLine = tile.transform.position.y - hitLineY;

            SpriteRenderer sr = tile.tileRenderer;
            if (sr == null)
            {
                ctx.result = "Miss";
                ctx.reason = "tile.tileRenderer is null";
            }
            else
            {
                float topY = sr.bounds.max.y;
                float botY = sr.bounds.min.y;
                ctx.tileTopY = topY;
                ctx.tileBotY = botY;

                if (hitLineY < botY)
                {
                    ctx.result = "Miss";
                    ctx.reason = $"hitY({hitLineY:F3}) is {botY - hitLineY:F3} below tile bottom({botY:F3}) — tile not yet reached";
                }
                else if (hitLineY > topY)
                {
                    ctx.result = "Miss";
                    ctx.reason = $"hitY({hitLineY:F3}) is {hitLineY - topY:F3} above tile top({topY:F3}) — tile already passed";
                }
                else
                {
                    float tileH   = Mathf.Max(0.0001f, topY - botY);
                    float perfWin = tileH * tile.perfectPercentOfTileHeight;
                    float distCtr = Mathf.Abs(sr.bounds.center.y - hitLineY);

                    if (distCtr <= perfWin)
                    {
                        ctx.result = "Perfect";
                        ctx.reason = $"distToCenter({distCtr:F3}) <= perfectWindow({perfWin:F3})";
                    }
                    else
                    {
                        ctx.result = "Good";
                        ctx.reason = $"in bounds — distToCenter({distCtr:F3}) > perfectWindow({perfWin:F3})";
                    }
                }
            }
        }

        TapDebugMode.Instance.RecordTapContext(ctx);
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

    // Touch variant: snaps to the nearest lane within touchForgivenessMultiplier lane steps.
    // Handles taps at screen edges that would fall outside the strict halfStep boundary.
    int ScreenPosToLaneTouchForgiving(Vector2 screenPos)
    {
        if (Camera.main == null || laneLayout == null || laneLayout.lanes == null) return -1;
        Vector3 world = Camera.main.ScreenToWorldPoint(
            new Vector3(screenPos.x, screenPos.y, 0f));
        return GetLaneIndexAtXForgiving(world.x);
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

    // Nearest-lane detection with a generous acceptance radius.
    // Returns -1 only if the tap is more than touchForgivenessMultiplier lane steps from every lane.
    int GetLaneIndexAtXForgiving(float worldX)
    {
        if (laneLayout == null || laneLayout.lanes == null) return -1;

        int   best     = -1;
        float bestDist = float.MaxValue;

        for (int i = 0; i < laneLayout.lanes.Length; i++)
        {
            float dist = Mathf.Abs(laneLayout.lanes[i].position.x - worldX);
            if (dist < bestDist) { bestDist = dist; best = i; }
        }

        return bestDist <= laneLayout.LaneStepWorld * touchForgivenessMultiplier ? best : -1;
    }

    // ── debug helpers ──────────────────────────────────────────────────────────

    // Spawns a short-lived circle at the world position of the tap.
    // Green = lane detected, red = no lane.
    void SpawnTapMarker(Vector3 worldPos, int lane)
    {
        if (_debugCircleSprite == null) return;
        var go = new GameObject("_TapDebug");
        go.transform.position = new Vector3(worldPos.x, worldPos.y, 0f);
        go.transform.localScale = Vector3.one * 0.3f;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = _debugCircleSprite;
        sr.color        = lane >= 0 ? new Color(0f, 1f, 0.2f, 0.85f) : new Color(1f, 0.1f, 0.1f, 0.85f);
        sr.sortingOrder = 20;
        Destroy(go, tapMarkerDuration);
    }

    // Builds a filled-circle sprite procedurally — no texture asset required.
    static Sprite BuildCircleSprite(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        tex.filterMode = FilterMode.Bilinear;
        var    pixels = new Color[size * size];
        float  r      = size * 0.5f - 0.5f;
        Vector2 c     = new Vector2(r, r);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                pixels[y * size + x] = Vector2.Distance(new Vector2(x, y), c) <= r
                    ? Color.white : Color.clear;
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    // ── UI lane panel callbacks ────────────────────────────────────────────────
    // Called by LanePanelInput. Each pointerId is tracked independently so one
    // finger release never cancels another finger's held lane.

    public void OnLanePanelDown(int laneIndex, int pointerId)
    {
        if (GameManager.Instance.IsGameOver || GameManager.Instance.IsPaused) return;

        // Guard: same pointer fired Down again without an intervening Up.
        if (_panelPointerLaneMap.ContainsKey(pointerId))
        {
            Debug.LogWarning(
                $"[PanelInput] pointerId={pointerId} Down again (lane={laneIndex} " +
                $"frame={Time.frameCount}) — duplicate suppressed");
            return;
        }

        _currentTapId = ++_tapCounter;
        _panelPointerLaneMap[pointerId] = laneIndex;

        if (showTapDebug)
            Debug.Log(
                $"[TapEvent] tapId={_currentTapId} frame={Time.frameCount} time={Time.time:F3} " +
                $"src=UIPanel pointerId={pointerId} lane={laneIndex}");

        if (laneIndex < 0 || laneIndex >= LaneHeld.Length) return;

        HitReceptorController.Instance?.PulseReceptor(laneIndex);
        HitLane(laneIndex, showTapDebug);
    }

    public void OnLanePanelUp(int laneIndex, int pointerId)
    {
        // Remove regardless of which lane the pointer mapped to (handles slide-off).
        if (_panelPointerLaneMap.Remove(pointerId) && showTapDebug)
            Debug.Log($"[PanelInput] pointerId={pointerId} up (lane={laneIndex} frame={Time.frameCount})");
    }

    // Debug-only: log that a finger exited a panel without releasing.
    // Hold continues — the touch tracks the original panel until OnPointerUp.
    public void OnLanePanelExit(int laneIndex, int pointerId)
    {
        if (showTapDebug)
            Debug.Log($"[PanelInput] pointerId={pointerId} exited panel lane={laneIndex} frame={Time.frameCount} (hold continues)");
    }

    // ── focus / pause safety ───────────────────────────────────────────────────
    // Any of these events clears stale held state so no touch remains "stuck".

    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) ClearAllHeldState();
    }

    void OnApplicationPause(bool paused)
    {
        if (paused) ClearAllHeldState();
    }

    void ClearAllHeldState()
    {
        touchLaneMap.Clear();
        _panelPointerLaneMap.Clear();
        pointerHeldLane = -1;
        System.Array.Clear(LaneHeld, 0, LaneHeld.Length);
        if (showTapDebug)
            Debug.Log("[InputManager] Focus/pause lost — cleared all held input state.");
    }

    // Logs any raw touch events reaching InputManager on Android while panels are active,
    // confirming that they are being bypassed and not reaching HitLane.
    void ConfirmRawTouchBypassed()
    {
        foreach (var touch in Touchscreen.current.touches)
        {
            var phase = touch.phase.ReadValue();
            if (phase == UnityEngine.InputSystem.TouchPhase.None) continue;
            Debug.LogWarning(
                $"[RawTouch] BYPASSED on Android: touchId={touch.touchId.ReadValue()} " +
                $"phase={phase} frame={Time.frameCount} — UI panels are active, raw touch ignored.");
        }
    }
}
