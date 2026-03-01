using UnityEngine;

public static class TileSizing
{
    // The most recently computed tile width in WORLD units.
    // Guides/Hitline will read this.
    public static float CurrentTileWidthWorld = 1f;

    // Optional: if you want to drive boundaries from lane step instead:
    public static float CurrentLaneStepWorld = 1f;
}