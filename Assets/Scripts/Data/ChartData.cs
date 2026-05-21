using System;

// Top-level chart file. One JSON file = one song chart.
// Add more songs by creating more JSON files under Resources/Charts/.
[Serializable]
public class ChartData
{
    public string songName;         // display name
    public string audioClipName;    // name of the AudioClip in AudioManager.musicLibrary
    public float bpm;               // used for beat->second conversion and tile fall speed
    public float songOffsetBeats;   // shift all note beats by this amount (positive = notes arrive later)
    public ChartNote[] notes;       // sorted ascending by beat
}
