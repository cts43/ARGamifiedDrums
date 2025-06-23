using Melanchall.DryWetMidi.Interaction;
using Midi;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class MidiEventCatcher : MonoBehaviour
{

    public Boolean enableDebugActions;
    public Boolean acceptInputs = true;
    public int RTriggerMIDINote;
    public DrumManager drumManager;

    public AudioClip drumSound;
    private AudioSource sound;

    private Dictionary<int, Dictionary<int, AudioClip>> drumClips = new Dictionary<int, Dictionary<int, AudioClip>>();


    private static SynchronizationContext context; //for running on main thread as these midi calls are not

    private void LoadDrumSamples()
    {
        int[] notes = { 35, 38, 42 }; //kick, snare, hat

        foreach (int note in notes)
        {
            var velocityDict = new Dictionary<int, AudioClip>();

            string drumName;
            
            for (int i = 1; i < 33; i++)
            {
                //for every note in the samples folder (files are labelled 1-32 and then some name)

                if (note == 35)
                {
                    drumName = "Kick";
                }
                else if (note == 38)
                {
                    drumName = "Snare";
                }
                else
                {
                    drumName = "Hat";
                } //just an else here as a fallback

                var path = Path.Combine("Audio/Drum Samples", drumName , i.ToString()); 
                //this would be folder+some file that starts with the right number

                var audioClip = Resources.Load<AudioClip>(path);
                Debug.Log(path);
                Debug.Log(audioClip);
                velocityDict.Add(i, audioClip);
            }

            drumClips.Add(note,velocityDict);
        }
    }

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

    private void Awake()
    {
        sound = gameObject.AddComponent<AudioSource>();

        LoadDrumSamples();

        context = SynchronizationContext.Current;
    }

    public void checkForDrum(int note, int velocity)
    {
        DrumHit[] drums = drumManager.GetComponentsInChildren<DrumHit>();

        //Checks whether the MIDI note hit exists as a drum in DrumManager, and runs relevant function if so

        foreach (DrumHit drum in drums)
        {
            if (note == drum.note)
            {
                //Debug.Log("Found drum!");
                drum.OnDrumHit(velocity);
            }
        }
    }

    private void NoteOn(int note, int velocity)
    {
        if (acceptInputs)
        {
            Debug.Log("Note " + note + " on, velocity " + velocity);
            context.Post(_ => { playSound(note,velocity); checkForDrum(note, velocity); }, null); //queue to run on the main thread
            //sound should play here as above waits a frame before playing
        }
        else
        {
            Debug.Log("Recieved MIDI input but acceptInputs = False");
        }
        
    }

    private void playSound(int note, int velocity)
    {
        Debug.Log("Note is" + note);

        int scaledVelocity = (velocity * 31 / 128) + 1; //scale velocity to be within 1-32

        Debug.Log("Velocity for file load: "+scaledVelocity);
        scaledVelocity = Math.Clamp(scaledVelocity, 1, 32); //make sure resulting number is definitely within the range
        Debug.Log("Velocity after clamp: " + scaledVelocity);
        var soundToPlay = drumClips[note][scaledVelocity];

        sound.PlayOneShot(soundToPlay);
    }

    private void NoteOff(int note)
    {
        Debug.Log("Note " + note + " off");
    }

    private void Update()
    {
        if (enableDebugActions && acceptInputs) //Use Right trigger to activate given drum, for outside of headset debug purposes
        {
            if (OVRInput.GetDown(OVRInput.RawButton.RIndexTrigger))
            {
                //Debug.Log("pressed right trigger");
                checkForDrum(RTriggerMIDINote, 127);
                playSound(RTriggerMIDINote, 1);
            }
        }
    }


}
