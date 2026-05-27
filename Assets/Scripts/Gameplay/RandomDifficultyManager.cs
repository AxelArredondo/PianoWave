using UnityEngine;
using System.Collections.Generic;

// Drives endless arcade difficulty: note types, double lanes, hold timing,
// lane-repetition avoidance, and sinusoidal speed fluctuation — all configurable
// from the Inspector per phase.
//
// Setup: attach this component to any GameObject in the game scene.
//        TileSpawner finds it via Instance and calls AdvanceBeat()/GetDecision() each beat.
//        Level Mode never touches this class.
public class RandomDifficultyManager : MonoBehaviour
{
    public static RandomDifficultyManager Instance;

    // ── Phase data ─────────────────────────────────────────────────────────────
    // One block per phase. All values are tweakable in the Inspector.

    [System.Serializable]
    public class PhaseData
    {
        [Tooltip("Beat number at which this phase becomes active.")]
        public int startBeat = 0;

        [Header("Density")]
        [Tooltip("Chance to skip spawning entirely this beat (musical rest).")]
        [Range(0f, 1f)] public float skipBeatChance = 0.25f;
        [Tooltip("Chance to spawn two lanes at once. Ignored for hold notes.")]
        [Range(0f, 1f)] public float doubleNoteChance = 0f;

        [Header("Note Type Weights  (remainder probability = tap)")]
        [Range(0f, 1f)] public float accentChance = 0.08f;
        [Range(0f, 1f)] public float quickChance  = 0f;
        [Range(0f, 1f)] public float holdChance   = 0f;

        [Header("Hold Duration (beats)")]
        public float holdDurationMin = 1.5f;
        public float holdDurationMax = 2.5f;

        [Header("Speed")]
        [Tooltip("Centre of the sinusoidal speed oscillation.")]
        public float speedBase = 1f;
        [Tooltip("Peak deviation added/subtracted from speedBase.")]
        public float speedOscAmp = 0.03f;
        [Tooltip("How many beats make one full oscillation cycle. Lower = faster wavering.")]
        public float speedOscPeriodBeats = 40f;
    }

    [Header("Phase 1 — Easy")]
    public PhaseData phase1 = new PhaseData
    {
        startBeat = 0,   skipBeatChance = 0.30f, doubleNoteChance = 0f,
        accentChance = 0.08f, quickChance = 0f,    holdChance = 0f,
        holdDurationMin = 1.5f, holdDurationMax = 2.5f,
        speedBase = 1.00f, speedOscAmp = 0.03f, speedOscPeriodBeats = 40f
    };

    [Header("Phase 2 — Doubles & Accents")]
    public PhaseData phase2 = new PhaseData
    {
        startBeat = 60,  skipBeatChance = 0.18f, doubleNoteChance = 0.15f,
        accentChance = 0.18f, quickChance = 0f,    holdChance = 0.06f,
        holdDurationMin = 1.5f, holdDurationMax = 3f,
        speedBase = 1.08f, speedOscAmp = 0.05f, speedOscPeriodBeats = 32f
    };

    [Header("Phase 3 — Quick Notes & Holds")]
    public PhaseData phase3 = new PhaseData
    {
        startBeat = 130, skipBeatChance = 0.12f, doubleNoteChance = 0.25f,
        accentChance = 0.20f, quickChance = 0.10f, holdChance = 0.12f,
        holdDurationMin = 1.5f, holdDurationMax = 3.5f,
        speedBase = 1.18f, speedOscAmp = 0.07f, speedOscPeriodBeats = 24f
    };

    [Header("Phase 4+ — Full Chaos (tuned to stay readable)")]
    public PhaseData phase4 = new PhaseData
    {
        startBeat = 220, skipBeatChance = 0.08f, doubleNoteChance = 0.32f,
        accentChance = 0.22f, quickChance = 0.18f, holdChance = 0.14f,
        holdDurationMin = 1.5f, holdDurationMax = 4f,
        speedBase = 1.28f, speedOscAmp = 0.10f, speedOscPeriodBeats = 20f
    };

    [Header("Lane Behaviour")]
    [Tooltip("How many recent lane choices to remember when weighting the next pick.")]
    [Range(1, 8)] public int laneRepetitionWindow = 3;
    [Tooltip("Weight subtracted from the most-recently-used lane. Decays for older picks.")]
    [Range(0f, 1f)] public float repetitionPenaltyBase = 0.70f;

    [Header("Hold Spacing")]
    [Tooltip("Same-lane notes must be at least this many beats after a hold ends. " +
             "Mirrors the Level Mode chart validation rule.")]
    [Range(0f, 2f)] public float holdSameLaneGapBeats = 0.5f;

    // ── Runtime ────────────────────────────────────────────────────────────────
    int beatsElapsed;
    int laneCount;
    readonly Queue<int> recentLanes = new Queue<int>();
    float[] holdEndBeat; // per-lane beat at which the active hold expires

    // ── Lifecycle ──────────────────────────────────────────────────────────────

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        bool isRandom = GameSettings.Instance == null
                     || GameSettings.Instance.Mode == GameMode.RandomMode;
        if (!isRandom) { enabled = false; return; }
    }

    // Called by TileSpawner once it knows how many lanes are in the scene.
    public void Initialize(int numLanes)
    {
        laneCount    = Mathf.Max(1, numLanes);
        holdEndBeat  = new float[laneCount];
        beatsElapsed = 0;
        recentLanes.Clear();
        ChartSpawner.SpeedMultiplier = 1f;
    }

    // ── Per-beat interface ─────────────────────────────────────────────────────

    // Advances the beat counter and updates the visual speed multiplier.
    // Call this BEFORE GetDecision each beat.
    public void AdvanceBeat()
    {
        beatsElapsed++;
        PhaseData p  = CurrentPhase;
        float osc    = Mathf.Sin((beatsElapsed / p.speedOscPeriodBeats) * Mathf.PI * 2f) * p.speedOscAmp;
        ChartSpawner.SpeedMultiplier = p.speedBase + osc;
    }

    // Returns what to spawn this beat. A Skip decision (lanes.Length == 0) means
    // no tile should be spawned — TileSpawner should return without instantiating.
    public SpawnDecision GetDecision()
    {
        if (holdEndBeat == null) return SpawnDecision.Skip;

        PhaseData p = CurrentPhase;

        if (Random.value < p.skipBeatChance)
            return SpawnDecision.Skip;

        string noteType   = PickNoteType(p);
        float  duration   = noteType == "hold"
            ? Random.Range(p.holdDurationMin, p.holdDurationMax) : 0f;

        // Hold doubles would require simultaneous input on two lanes while both are
        // moving downward — skip doubles for holds to keep it readable.
        int wantCount  = (noteType != "hold" && Random.value < p.doubleNoteChance) ? 2 : 1;
        int[] chosen   = PickLanes(wantCount);

        if (chosen.Length == 0)
            return SpawnDecision.Skip;

        if (noteType == "hold")
            foreach (int l in chosen)
                holdEndBeat[l] = beatsElapsed + duration;

        foreach (int l in chosen)
        {
            recentLanes.Enqueue(l);
            while (recentLanes.Count > laneRepetitionWindow + 2)
                recentLanes.Dequeue();
        }

        return new SpawnDecision { lanes = chosen, noteType = noteType, durationBeats = duration };
    }

    // ── Internal helpers ───────────────────────────────────────────────────────

    string PickNoteType(PhaseData p)
    {
        float r = Random.value;
        if (r < p.holdChance)   return "hold";
        r -= p.holdChance;
        if (r < p.quickChance)  return "quick";
        r -= p.quickChance;
        if (r < p.accentChance) return "accent";
        return "tap";
    }

    int[] PickLanes(int count)
    {
        float[] w      = BuildLaneWeights();
        var     result = new List<int>(count);
        for (int i = 0; i < count; i++)
        {
            int lane = WeightedRandom(w);
            if (lane < 0) break;
            result.Add(lane);
            w[lane] = 0f; // prevent same lane appearing twice in one beat
        }
        return result.ToArray();
    }

    float[] BuildLaneWeights()
    {
        var w = new float[laneCount];
        for (int i = 0; i < laneCount; i++) w[i] = 1f;

        // Apply decaying penalty to recently used lanes (most recent = heaviest).
        float pen = repetitionPenaltyBase;
        foreach (int lane in recentLanes)
        {
            if (lane >= 0 && lane < laneCount)
                w[lane] = Mathf.Max(0f, w[lane] - pen);
            pen = Mathf.Max(0.05f, pen * 0.55f);
        }

        // Block lanes still inside the post-hold spacing window.
        for (int i = 0; i < laneCount; i++)
            if (beatsElapsed < holdEndBeat[i] + holdSameLaneGapBeats)
                w[i] = 0f;

        return w;
    }

    int WeightedRandom(float[] weights)
    {
        float total = 0f;
        foreach (float wt in weights) total += wt;
        if (total <= 0f) return -1;
        float r = Random.value * total;
        for (int i = 0; i < weights.Length; i++)
        {
            r -= weights[i];
            if (r <= 0f) return i;
        }
        return -1;
    }

    PhaseData CurrentPhase
    {
        get
        {
            if (beatsElapsed >= phase4.startBeat) return phase4;
            if (beatsElapsed >= phase3.startBeat) return phase3;
            if (beatsElapsed >= phase2.startBeat) return phase2;
            return phase1;
        }
    }
}

// Returned by RandomDifficultyManager.GetDecision() each beat.
// lanes.Length == 0 signals TileSpawner to skip this beat.
public struct SpawnDecision
{
    public int[]  lanes;
    public string noteType;
    public float  durationBeats;

    public static SpawnDecision Skip =>
        new SpawnDecision { lanes = new int[0], noteType = "tap", durationBeats = 0f };
}
