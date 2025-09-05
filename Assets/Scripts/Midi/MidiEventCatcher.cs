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

    public bool enableDebugActions;
    public bool acceptInputs = true;
    public int RTriggerMIDINote;
    public DrumManager drumManager;
    private AudioSource sound;

    public int kickVolumeMultiplier = 3;

    private Dictionary<int, Dictionary<int, AudioClip>> drumClips = new Dictionary<int, Dictionary<int, AudioClip>>();

    public bool playDrumSounds = true;


    private static SynchronizationContext context; //for running on main thread as these midi calls are not

    private void LoadDrumSamples()
    {
        int[] notes = { 36, 38, 46 }; //kick, snare, hat

        foreach (int note in notes)
        {
            var velocityDict = new Dictionary<int, AudioClip>();

            string drumName;

            for (int i = 1; i < 33; i++)
            {
                //for every note in the samples folder (files are labelled 1-32 and then some name)

                if (note == 36)
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

                var path = Path.Combine("Audio/Drum Samples", drumName, i.ToString());
                //this would be folder+some file that starts with the right number

                var audioClip = Resources.Load<AudioClip>(path);
                velocityDict.Add(i, audioClip);
            }

            drumClips.Add(note, velocityDict);
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
                playSound(note, velocity);
            }
        }
    }

    private void NoteOn(int note, int velocity)
    {
        if (acceptInputs)
        {
            //Debug.Log("Note " + note + " on, velocity " + velocity);
            context.Post(_ => { checkForDrum(note, velocity); }, null); //queue to run on the main thread
        }
        else
        {
            Debug.Log("Recieved MIDI input but acceptInputs = False");
        }

    }

    private void playSound(int note, int velocity)
    {
        if (!playDrumSounds)
        {
            return;
        }

        int scaledVelocity = (velocity * 31 / 128) + 1; //scale velocity to be within 1-32

        if (note == 36)
        {
            scaledVelocity *= kickVolumeMultiplier; //Just for user study, make kick louder to ensure participants can hear properly
        }

        scaledVelocity = Math.Clamp(scaledVelocity, 1, 32); //make sure resulting number is definitely within the range
        var soundToPlay = drumClips[note][scaledVelocity];

        sound.PlayOneShot(soundToPlay);
    }

    private void NoteOff(int note)
    {
        //Debug.Log("Note " + note + " off");
    }

    private void Update()
    {

        if (PlaybackManager.Instance.IsPlayingRecordedInputs())
        {
            acceptInputs = false;
        }
        else
        {
            acceptInputs = true;
        }

        if (enableDebugActions && acceptInputs) //Use Right trigger to activate given drum, for outside of headset debug purposes
        {
            if (OVRInput.GetDown(OVRInput.RawButton.RIndexTrigger))
            {
                //Debug.Log("pressed right trigger");
                checkForDrum(RTriggerMIDINote, 127);
                //playSound(RTriggerMIDINote, 10);
            }
        }
    }

    public void ToggleDrumSounds()
    {
        playDrumSounds = !playDrumSounds;
    }


}