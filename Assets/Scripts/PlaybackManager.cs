using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlaybackManager : MonoBehaviour
{

    public GameObject noteSpawnerObj; //prefab for note spawner class
    public GameObject ControllerRecorderObj;
    private NoteSpawner activeNoteSpawner;
    private ControllerRecorder ControllerRecorder;
    public GameObject drumManagerObj;
    private DrumManager drumManager;

    public string MIDIFilePath;

    public static PlaybackManager instance;

    public static bool rhythmLoaded = false;

    public long currentTimeInTicks;

    public static bool playing = false;
    private bool motionRecording = false;
    private bool motionRecorded = false;
    private bool savingPlaythrough = false;
    private Queue<(int,long,long,bool)> savedPlaythrough = new Queue<(int,long,long,bool)>();

    private void loadNewRhythm(string Path)
    {
        if (!rhythmLoaded)
        {
            MIDIFilePath = Path;
            activeNoteSpawner.Initialise(MIDIFilePath);
            activeNoteSpawner.StartedPlaying += OnMIDIStartedPlaying;
            activeNoteSpawner.FinishedPlaying += OnMIDIFinishedPlaying;
            rhythmLoaded = true;
        }
    }

    private void playRhythm()
    {
        if (rhythmLoaded)
        {
            activeNoteSpawner.Play();
        }
    }

    private void playWithRecord()
    {
        //reload and play with recording on
        loadNewRhythm(MIDIFilePath);
        playRhythm();
        ControllerRecorder.Record();
    }

    private void playRecorded()
    {
        if (motionRecorded && rhythmLoaded && (!motionRecording))
        {
            //play with recorded motion
            playRhythm();
            ControllerRecorder.Play();
        }
    }

    private void recordHit(int noteNumber, long timeHit, long closestNote, bool hitNote)
    {
        if (savingPlaythrough)
        {
            Debug.Log("Hit note: " + noteNumber + " Success: " + hitNote + " At: " + timeHit);
            savedPlaythrough.Enqueue((noteNumber, timeHit,closestNote,hitNote)); //store note hit at what time + closest note. allows calculation of offsets + playing back a run at the correct ticks
        }
    }

    private void subscribeToDrumHits()
    {
        foreach (var drum in drumManager.GetComponentsInChildren<DrumHit>())
        {
            drum.HitDrum += recordHit;
        }
    }

    private void Start()
    {
        activeNoteSpawner = Instantiate(noteSpawnerObj).GetComponent<NoteSpawner>();
        ControllerRecorder = ControllerRecorderObj.GetComponent<ControllerRecorder>();
        drumManager = drumManagerObj.GetComponent<DrumManager>();

        ControllerRecorder.StartedRecording += OnStartedRecording;
        ControllerRecorder.FinishedRecording += OnFinishedRecording;
        subscribeToDrumHits();
    }

    private void Update()
    {
        playing = activeNoteSpawner.playing;
        if (OVRInput.GetDown(OVRInput.RawButton.B))
        {
            if (!playing)
            {
                if (!motionRecorded)
                {
                    playWithRecord();
                }
                else
                {
                    playRecorded();
                }
            }
        }
        else if (OVRInput.GetDown(OVRInput.RawButton.Y))
        {
            if (!playing)
            {
                playWithRecord();
            }
        }

        if (playing)
        {
            currentTimeInTicks = activeNoteSpawner.GetCurrentOffsetMusicalTimeAsTicks(); //Update current time from active note spawner instance. unsure if necessary
        }

    }

    private void SavePlaythrough()
    {
        Debug.Log("Saving playthrough from tick " + currentTimeInTicks);
        savingPlaythrough = true;
    }

    private void OnMIDIStartedPlaying()
    {
        Debug.Log("(Playback Manager) MIDI Started");
        Debug.Log("Start logging accuracy here");

        SavePlaythrough();
    }

    private void OnMIDIFinishedPlaying()
    {
        Debug.Log("(Playback Manager) MIDI Finished");
        ControllerRecorder.StopRecording();
        savingPlaythrough = false;
        drumManager.clearNotes();

        int hitNotes = 0;
        int missedNotes;

        foreach (var dataPoint in savedPlaythrough)
        {
            (var note, var noteTime, var closestNote, var hitNote) = dataPoint;
            if (hitNote)
            {
                hitNotes++;
            }
        }

        missedNotes = activeNoteSpawner.totalNotes - hitNotes;

        Debug.Log("Missed Notes: " + missedNotes);

        double percentageMissed = (double)missedNotes / activeNoteSpawner.totalNotes * 100;

        Debug.Log("Percentage missed: "+percentageMissed+"%. Percentage hit: "+(100-percentageMissed)+"%.");
        
        

        //preliminary accuracy checker here


    }

    private void OnStartedRecording()
    {
        Debug.Log("(Playback Manager) Motion Recording");
        motionRecording = true;
    }

    private void OnFinishedRecording()
    {
        Debug.Log("(Playback Manager) Motion Recording Finished");
        motionRecorded = true;
        motionRecording = false;
    }

    //Should implement saving accuracy etc. using these signals + saving recorded motion to file



}
