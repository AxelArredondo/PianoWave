using UnityEngine;
using System;

public class BeatManager : MonoBehaviour
{
    public static BeatManager Instance;

    public float bpm = 120f;
    private float timer;

    public float SecondsPerBeat => 60f / bpm;

    public static event Action OnBeat;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= SecondsPerBeat)
        {
            timer -= SecondsPerBeat;
            OnBeat?.Invoke();
        }
    }

    public void IncreaseBPM(float amount)
    {
        bpm += amount;
    }
}
