using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class TimelineController : MonoBehaviour
{
    public PlayableDirector director;

    [Serializable]
    class Signal
    {
        public string sigName;
        public float sigTime;
    }

    [SerializeField] List<Signal> signals = new();
    private Dictionary<string, float> signalLookup;

    private void Awake()
    {
        // Dictionary Ä³½Ì
        signalLookup = new Dictionary<string, float>();

        foreach (var s in signals)
        {
            if (!signalLookup.ContainsKey(s.sigName))
            {
                signalLookup.Add(s.sigName, s.sigTime);
            }
            else
            {
                Debug.LogWarning($"Duplicate signal name detected: {s.sigName}");
            }
        }
    }

    [SerializeField]
    public void PlayFromMarker(string sigName)
    {
        if (signalLookup.TryGetValue(sigName, out float time))
        {
            director.time = time;
            director.Play();
        }
        else
        {
            Debug.LogWarning($"Signal not found: {sigName}");
        }
    }
}
