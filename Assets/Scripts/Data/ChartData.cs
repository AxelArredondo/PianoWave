using System;

// Top-level chart file. One JSON file = one song chart.
// Add more songs by creating more JSON files under Resources/Charts/.
[Serializable]
public class ChartData
{
    public string songName;              // display name
    public string audioClipName;         // name of the AudioClip in AudioManager.musicLibrary
    public float bpm;                    // base bpm — used for beat->second conversion
    public float songOffsetBeats;        // shift all note beats by this amount (positive = notes arrive later)
    public ChartNote[] notes;            // sorted ascending by beat
    public SpeedEvent[] speedEvents;     // optional — change visual scroll speed at a beat (sorted ascending)
    public BackgroundEvent[] backgroundEvents; // optional — change background theme at a beat (sorted ascending)
}
