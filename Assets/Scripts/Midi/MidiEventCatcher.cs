using Midi;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class MidiEventCatcher : MonoBehaviour
{

    public Boolean enableDebugActions;
    public int RTriggerMIDINote;

    public DrumManager drumManager;

    private void OnEnable()
    {
        MidiEventHandler.OnNoteOn += NoteOn; //Subscribe local functions to MidiEventHandler Events
        MidiEventHandler.OnNoteOff += NoteOff;
    }

    private void OnDisable()
    {
        MidiEventHandler.OnNoteOn -= NoteOn;
        MidiEventHandler.OnNoteOff -= NoteOff;
    }

    private void checkForDrum(int note, int velocity)
    {
        DrumHit[] drums = drumManager.GetComponentsInChildren<DrumHit>();

        //Checks whether the MIDI note hit exists as a drum in DrumManager, and runs relevant function if so

        foreach (DrumHit drum in drums)
        {
            if (note == drum.note)
            {
                Debug.Log("Found drum!");
                drum.OnDrumHit();
            }
        }
    }

    private void NoteOn(int note, int velocity)
    {
        Debug.Log("Note " + note + " on, velocity " + velocity);
        checkForDrum(note, velocity);
    }

    private void NoteOff(int note)
    {
        Debug.Log("Note " + note + " off");
    }

    private void Update()
    {
        if (enableDebugActions) //Use Right trigger to activate given drum, for outside of headset debug purposes
        {
            if (OVRInput.GetDown(OVRInput.RawButton.RIndexTrigger))
            {
                Debug.Log("pressed right trigger");
                checkForDrum(RTriggerMIDINote, 127);
            }
        }
    }


}
