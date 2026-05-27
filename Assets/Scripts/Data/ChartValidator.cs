using UnityEngine;

// Validates a loaded ChartData for hold-note readability issues.
// Only hold notes impose spacing rules — tap/quick chains are unrestricted.
// Call Validate() once after loading a chart. Logs warnings; never throws.
public static class ChartValidator
{
    const int MaxLanes = 8;

    // holdGapBeats: minimum beat gap required between a hold and any adjacent same-lane note.
    // Tweak this value to loosen or tighten hold breathing room. Default: 0.5 beats.
    public static void Validate(ChartData chart, float holdGapBeats = 0.5f)
    {
        if (chart?.notes == null || chart.notes.Length == 0) return;

        // holdEndBeat[lane]  — beat when the last hold in this lane finishes.
        // lastNoteBeat[lane] — beat of the most recent note in this lane (any type).
        // lastNoteType[lane] — type of that most recent note.
        float[]  holdEndBeat  = new float[MaxLanes];
        float[]  lastNoteBeat = new float[MaxLanes];
        string[] lastNoteType = new string[MaxLanes];

        for (int i = 0; i < MaxLanes; i++)
        {
            holdEndBeat[i]  = float.NegativeInfinity;
            lastNoteBeat[i] = float.NegativeInfinity;
            lastNoteType[i] = null;
        }

        foreach (var note in chart.notes)
        {
            if (note?.lanes == null) continue;

            foreach (int lane in note.lanes)
            {
                if (lane < 0 || lane >= MaxLanes) continue;

                // Rule: after a hold ends, the next same-lane note needs holdGapBeats of space.
                // Covers: tap-after-hold and quick-after-hold in the same lane.
                if (!float.IsNegativeInfinity(holdEndBeat[lane])
                    && note.beat < holdEndBeat[lane] + holdGapBeats)
                {
                    Debug.LogWarning(
                        $"Chart warning: {note.noteType} too close after hold end in lane {lane} " +
                        $"(beat {note.beat:F2} vs hold end {holdEndBeat[lane]:F2}, " +
                        $"gap {note.beat - holdEndBeat[lane]:F2} < {holdGapBeats} beats).");
                }

                // Rule: a hold note should not start within holdGapBeats of the previous same-lane note.
                // Covers: hold-after-tap and hold-after-quick in the same lane.
                if (note.noteType == "hold"
                    && !float.IsNegativeInfinity(lastNoteBeat[lane])
                    && note.beat - lastNoteBeat[lane] < holdGapBeats)
                {
                    Debug.LogWarning(
                        $"Chart warning: hold note too close after {lastNoteType[lane]} in lane {lane} " +
                        $"(hold at beat {note.beat:F2}, previous at beat {lastNoteBeat[lane]:F2}, " +
                        $"gap {note.beat - lastNoteBeat[lane]:F2} < {holdGapBeats} beats).");
                }

                // Track the end beat of this hold so the next note in this lane can be checked.
                if (note.noteType == "hold" && note.durationBeats > 0f)
                {
                    float endBeat = note.beat + note.durationBeats;
                    if (endBeat > holdEndBeat[lane])
                        holdEndBeat[lane] = endBeat;
                }

                lastNoteBeat[lane] = note.beat;
                lastNoteType[lane] = note.noteType;
            }
        }
    }
}
