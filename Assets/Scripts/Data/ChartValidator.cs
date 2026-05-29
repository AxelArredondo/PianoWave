using UnityEngine;

// Validates a loaded ChartData for readability issues.
// Call Validate() once after loading a chart. Logs warnings; never throws.
//
// Spacing rules enforced:
//   holdGapBeats  — minimum gap between a hold note and any adjacent same-lane note (default 0.5).
//   quickGapBeats — minimum gap between a quick note and any tap/accent/hold in the same lane (default 0.5).
//
// quick → quick  in the same lane: always allowed (no minimum enforced).
// tap   → tap    in the same lane: always allowed (no minimum enforced).
// Different lanes are never checked.
public static class ChartValidator
{
    const int MaxLanes = 8;

    public static void Validate(ChartData chart, float holdGapBeats = 0.5f, float quickGapBeats = 0.5f)
    {
        if (chart?.notes == null || chart.notes.Length == 0) return;

        // Per-lane tracking state.
        float[]  holdEndBeat   = new float[MaxLanes];   // beat when the last hold in this lane ends
        float[]  lastNoteBeat  = new float[MaxLanes];   // beat of the most recent note (any type)
        float[]  lastQuickBeat = new float[MaxLanes];   // beat of the most recent quick note
        string[] lastNoteType  = new string[MaxLanes];  // type of the most recent note

        for (int i = 0; i < MaxLanes; i++)
        {
            holdEndBeat[i]   = float.NegativeInfinity;
            lastNoteBeat[i]  = float.NegativeInfinity;
            lastQuickBeat[i] = float.NegativeInfinity;
            lastNoteType[i]  = null;
        }

        foreach (var note in chart.notes)
        {
            if (note?.lanes == null) continue;

            foreach (int lane in note.lanes)
            {
                if (lane < 0 || lane >= MaxLanes) continue;

                bool isQuick    = note.noteType == "quick";
                bool isNonQuick = note.noteType == "tap"
                               || note.noteType == "accent"
                               || note.noteType == "hold";

                // ── Hold end spacing ──────────────────────────────────────────────────
                // Any note (tap, quick, accent, hold) must be at least holdGapBeats after
                // the end of the previous hold in this lane.
                if (!float.IsNegativeInfinity(holdEndBeat[lane])
                    && note.beat < holdEndBeat[lane] + holdGapBeats)
                {
                    Debug.LogWarning(
                        $"Chart warning: {note.noteType} too close after hold end in lane {lane} " +
                        $"(beat {note.beat:F2} vs hold end {holdEndBeat[lane]:F2}, " +
                        $"gap {note.beat - holdEndBeat[lane]:F2} < {holdGapBeats} beats).");
                }

                // ── Hold start spacing ────────────────────────────────────────────────
                // A hold must start at least holdGapBeats after the previous same-lane note
                // (covers hold-after-tap and hold-after-quick).
                if (note.noteType == "hold"
                    && !float.IsNegativeInfinity(lastNoteBeat[lane])
                    && note.beat - lastNoteBeat[lane] < holdGapBeats)
                {
                    Debug.LogWarning(
                        $"Chart warning: hold note too close after {lastNoteType[lane]} in lane {lane} " +
                        $"(hold at beat {note.beat:F2}, previous at beat {lastNoteBeat[lane]:F2}, " +
                        $"gap {note.beat - lastNoteBeat[lane]:F2} < {holdGapBeats} beats).");
                }

                // ── Quick-after-nonQuick spacing ──────────────────────────────────────
                // A quick note must be at least quickGapBeats after the most recent
                // tap/accent/hold in the same lane.
                if (isQuick
                    && !float.IsNegativeInfinity(lastNoteBeat[lane])
                    && lastNoteType[lane] != "quick"
                    && note.beat - lastNoteBeat[lane] < quickGapBeats)
                {
                    Debug.LogWarning(
                        $"Chart warning: quick note too close after {lastNoteType[lane]} in lane {lane} " +
                        $"(quick at beat {note.beat:F2}, previous at beat {lastNoteBeat[lane]:F2}, " +
                        $"gap {note.beat - lastNoteBeat[lane]:F2} < {quickGapBeats} beats). " +
                        $"Consider shifting quick to a different lane or beat.");
                }

                // ── NonQuick-after-quick spacing ──────────────────────────────────────
                // A tap, accent, or hold must be at least quickGapBeats after the most
                // recent quick note in the same lane.
                if (isNonQuick
                    && !float.IsNegativeInfinity(lastQuickBeat[lane])
                    && note.beat - lastQuickBeat[lane] < quickGapBeats)
                {
                    Debug.LogWarning(
                        $"Chart warning: {note.noteType} too close after quick in lane {lane} " +
                        $"(beat {note.beat:F2}, previous quick at beat {lastQuickBeat[lane]:F2}, " +
                        $"gap {note.beat - lastQuickBeat[lane]:F2} < {quickGapBeats} beats). " +
                        $"Consider shifting quick to a different lane or beat.");
                }

                // ── Update tracking state ─────────────────────────────────────────────

                if (note.noteType == "hold" && note.durationBeats > 0f)
                {
                    float endBeat = note.beat + note.durationBeats;
                    if (endBeat > holdEndBeat[lane])
                        holdEndBeat[lane] = endBeat;
                }

                if (isQuick)
                    lastQuickBeat[lane] = note.beat;

                lastNoteBeat[lane] = note.beat;
                lastNoteType[lane] = note.noteType;
            }
        }
    }
}
