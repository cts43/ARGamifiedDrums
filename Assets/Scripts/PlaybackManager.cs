using System;
using System.Collections.Generic;
using System.IO;
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
    private Queue<playthroughFrame> savedPlaythrough = new Queue<playthroughFrame>();
    private bool hasSavedPlaythrough = false;

    //Serialisable classes for saving playthrough to file -- needed for plotting graphs etc.
    [Serializable]
    private class playthroughFrame
    {
        [SerializeField] private int note;
        [SerializeField] private int velocity;
        [SerializeField] private long hitTime;
        [SerializeField] private long closestNoteTime;
        [SerializeField] private bool hitSuccessfully;
        //Constructor
        public playthroughFrame(int note, int velocity, long hitTime, long closestNoteTime, bool hitSuccessfully)
        {
            this.note = note;
            this.velocity = velocity;
            this.hitTime = hitTime;
            this.closestNoteTime = closestNoteTime;
            this.hitSuccessfully = hitSuccessfully;
        }
        //Deconstructor
        public void Deconstruct(out int note, out int velocity, out long hitTime, out long closestNoteTime, out bool hitSuccessfully)
        {
            note = this.note;
            velocity = this.velocity;
            hitTime = this.hitTime;
            closestNoteTime = this.closestNoteTime;
            hitSuccessfully = this.hitSuccessfully;
        }

    }
    [Serializable]
    private class playthroughData
    {
        [SerializeField] public List<playthroughFrame> frames;

        public playthroughData(List<playthroughFrame> frames)
        {
            this.frames = frames;
        }
    }


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

    private void recordHit(int noteNumber,int velocity, long timeHit, long closestNote, bool hitNote)
    {
        if (savingPlaythrough)
        {
            Debug.Log("Hit note: " + noteNumber + " with velocity: "+ velocity +" Success: " + hitNote + " At: " + timeHit);
            savedPlaythrough.Enqueue(new playthroughFrame(noteNumber,velocity, timeHit,closestNote,hitNote)); //store note hit at what time + closest note. allows calculation of offsets + playing back a run at the correct ticks
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

    bool playingBack = false;

    private void Update()
    {
        
        playing = activeNoteSpawner.playing;
        if (playing)
        {
            currentTimeInTicks = activeNoteSpawner.GetCurrentOffsetMusicalTimeAsTicks(); //Update current time from active note spawner instance. unsure if necessary
        }

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

        if (playingBack)
        { //testing playing back user inputs
            if (savedPlaythrough.Count > 0)
            {
                (var note, var velocity, var time, var closest, var success) = savedPlaythrough.Peek();
                if (activeNoteSpawner.GetCurrentOffsetMusicalTimeAsTicks() >= time) //offset based on spawn window
                {
                    savedPlaythrough.Dequeue();
                    MidiEventCatcher MIDIEventCatcher = GameObject.FindWithTag("MIDI Input Handler").GetComponent<MidiEventCatcher>();
                    MIDIEventCatcher.checkForDrum(note, velocity);
                }
            }
        }

    }

    private void SavePlaythrough()
    {
        if (!savingPlaythrough && !hasSavedPlaythrough)
        {
            Debug.Log("Saving playthrough from tick " + currentTimeInTicks);
            savingPlaythrough = true;
        }
        else if (hasSavedPlaythrough)
        {
            playingBack = true;
        }
    }

    private void OnMIDIStartedPlaying()
    {
        Debug.Log("(Playback Manager) MIDI Started");

        SavePlaythrough();
    }

    private void OnMIDIFinishedPlaying()
    {
        Debug.Log("(Playback Manager) MIDI Finished");
        ControllerRecorder.StopRecording();
        savingPlaythrough = false;
        hasSavedPlaythrough = true;
        drumManager.clearNotes();

        int hitNotes = 0;
        int missedNotes;


        foreach (var dataPoint in savedPlaythrough)
        {
            (var note, var velocity, var noteTime, var closestNote, var hitNote) = dataPoint;
            if (hitNote)
            {
                hitNotes++;
            }
        }

        missedNotes = activeNoteSpawner.totalNotes - hitNotes;

        Debug.Log("Missed Notes: " + missedNotes);

        double percentageMissed = (double)missedNotes / activeNoteSpawner.totalNotes * 100;

        Debug.Log("Percentage missed: " + percentageMissed + "%. Percentage hit: " + (100 - percentageMissed) + "%.");

        //TESTING SAVING AND LOADING FROM JSON

        playthroughData dataToSave = new playthroughData(new List<playthroughFrame>(savedPlaythrough)); //List from queue, as queues are not serialisable

        string json = JsonUtility.ToJson(dataToSave); //convert to json format

        File.WriteAllText("Assets/file.json", json); //with any luck this file will have some content

        Queue<playthroughFrame> fromJSON = new Queue<playthroughFrame>(JsonUtility.FromJson<playthroughData>(   File.ReadAllText("Assets/file.json")   ).frames); //back to queue from list loaded from json file.

        (var newNote, var newVelocity, var newNoteTime, var newClosestNote, var newHitNote) = fromJSON.Dequeue();
        Debug.Log("From QUEUE from JSON, first note time: "+newNoteTime); //print first note from .json to check if it works


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
