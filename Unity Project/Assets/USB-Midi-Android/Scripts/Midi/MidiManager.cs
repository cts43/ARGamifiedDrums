using System;
using UnityEngine;

public class MidiManager : MonoBehaviour
{
    public static MidiManager Instance = null;
    private UnityMidiAndroid _midiAndroid;
    private void Awake()
    {
        Instance = this;
    }

    public void RegisterEventHandler(IMidiEventHandler eventHandler)
    {
        _midiAndroid = new UnityMidiAndroid(eventHandler);
    }
}