using System;

// One note event in a chart. lanes[] supports single notes, double notes, and future multi-lane patterns.
[Serializable]
public class ChartNote
{
    public float beat;          // beat number when this note should reach the hitline
    public int[] lanes;         // which lanes (0-3) to spawn tiles in
    public string noteType;     // "tap" | "hold" | "quick" — used for future variant visuals
    public float durationBeats; // hold notes only: how long the hold lasts in beats
}
