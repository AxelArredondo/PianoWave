using UnityEngine;

public static class TileSizing
{
    /// <summary>World-space tile width, updated by LaneLayout every frame and by TileSpawner on spawn.</summary>
    public static float CurrentTileWidthWorld  = 1f;

    /// <summary>World-space tile height = CurrentTileWidthWorld * TileAspectRatio.</summary>
    public static float CurrentTileHeightWorld = 1.4f;

    /// <summary>World-space lane step (centre-to-centre spacing).</summary>
    public static float CurrentLaneStepWorld   = 1f;

    /// <summary>height / width ratio. Set by TileSpawner; read by LaneLayout and HitLineFitter.</summary>
    public static float TileAspectRatio = 1.4f;
}
