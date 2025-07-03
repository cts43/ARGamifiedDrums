using System;
using System.Collections.Generic;
using System.IO;
using Melanchall.DryWetMidi.Interaction;
using Meta.XR.ImmersiveDebugger.UserInterface.Generic;
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
    private bool motionPlaying = false;
    private bool savingPlaythrough = false;
    private Queue<playthroughFrame> savedPlaythrough = new Queue<playthroughFrame>();
    private Queue<playthroughFrame> savedPlaythroughCopy = new Queue<playthroughFrame>();
    private bool playingRecordedInputs = false;
    private bool playthroughLoaded = false;

    private bool readyToSaveMotion = false;
    private bool readyToSaveInput = false;


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

    [Serializable]
    private class motionData
    {
        [SerializeField] public List<ControllerRecorder.transformPair> controllerFrames;
        [SerializeField] public List<ControllerRecorder.handMotionFrame> leftHandFrames;
        [SerializeField] public List<ControllerRecorder.handMotionFrame> rightHandFrames;

        public motionData(List<ControllerRecorder.transformPair> controllerFrames, List<ControllerRecorder.handMotionFrame> leftHandFrames, List<ControllerRecorder.handMotionFrame> rightHandFrames)
        {
            this.controllerFrames = controllerFrames;
            this.leftHandFrames = leftHandFrames;
            this.rightHandFrames = rightHandFrames;
        }
    }

    //class that stores all motion + playthrough data to allow saving to single .json file
    [Serializable]
    private class recordingData
    {
        public motionData motion;
        public playthroughData inputs;

        public recordingData(motionData motion, playthroughData inputs)
        {
            this.motion = motion;
            this.inputs = inputs;
        }


    }


    private void loadNewMIDI(string Path)
    {
        if (!rhythmLoaded)
        {
            MIDIFilePath = Path;
            activeNoteSpawner.Initialise(MIDIFilePath);
            activeNoteSpawner.StartedPlaying += OnMIDIStartedPlaying;
            activeNoteSpawner.FinishedPlaying += OnMIDIFinishedPlaying;
            rhythmLoaded = true;

            //should search for recorded motion and if exists also load that in
        }
    }

    private void playMIDI()
    {
        if (rhythmLoaded)
        {
            activeNoteSpawner.Play();
        }
    }

    private void playWithRecord()
    {
        //reload and play with recording on
        loadNewMIDI(MIDIFilePath);
        playMIDI();
        ControllerRecorder.Record();//record controller motion
        SavePlaythrough(); //save drum inputs/notes played
    }

    private void playRecorded(bool motion, bool drumHits)
    {

        if (!rhythmLoaded)
        {
            loadNewMIDI(MIDIFilePath);
        }

        if (rhythmLoaded)
        {
            //play with recorded motion
            playMIDI();
            if (motion)
            {
                if (motionRecorded && !motionRecording)
                {
                    ControllerRecorder.Play();
                    motionPlaying = true;
                }
            }
            if (drumHits)
            {
                if (savedPlaythrough.Count > 0)
                {
                    savedPlaythroughCopy = new Queue<playthroughFrame>(savedPlaythrough);
                    playingRecordedInputs = true;
                }
            }
        }
    }

    private void recordHit(int noteNumber, int velocity, long timeHit, long closestNote, bool hitNote)
    {
        if (savingPlaythrough)
        {
            //Debug.Log("Hit note: " + noteNumber + " with velocity: " + velocity + " Success: " + hitNote + " At: " + timeHit);
            savedPlaythrough.Enqueue(new playthroughFrame(noteNumber, velocity, timeHit, closestNote, hitNote)); //store note hit at what time + closest note. allows calculation of offsets + playing back a run at the correct ticks
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
        TryLoadData("saved.json");
    }

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
                    playRecorded(true, true);
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

        if (playingRecordedInputs)
        { //testing playing back user inputs
            if (savedPlaythroughCopy.Count > 0)
            {
                (var note, var velocity, var time, var closest, var success) = savedPlaythroughCopy.Peek();
                if (activeNoteSpawner.GetCurrentOffsetMusicalTimeAsTicks() >= time) //offset based on spawn window
                {
                    savedPlaythroughCopy.Dequeue();
                    MidiEventCatcher MIDIEventCatcher = GameObject.FindWithTag("MIDI Input Handler").GetComponent<MidiEventCatcher>();
                    MIDIEventCatcher.checkForDrum(note, velocity);
                }
            }
        }

        if (motionPlaying)
        {
            activeNoteSpawner.showKickMotion = true;

        }
        else
        {
            activeNoteSpawner.showKickMotion = false;
        }

        TrySaveData();

    }

    private void SavePlaythrough()
    {
        if (!savingPlaythrough && !playthroughLoaded)
        {
            Debug.Log("Saving playthrough from tick " + currentTimeInTicks);
            savingPlaythrough = true;
        }
    }

    private void OnMIDIStartedPlaying()
    {
        Debug.Log("(Playback Manager) MIDI Started");
    }

    private void OnMIDIFinishedPlaying()
    {
        Debug.Log("(Playback Manager) MIDI Finished");
        ControllerRecorder.StopRecording();
        savingPlaythrough = false;
        playthroughLoaded = true;
        drumManager.clearNotes();
        readyToSaveInput = true;
        motionPlaying = false;

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

        readyToSaveMotion = true;

    }

    //Should implement saving accuracy etc. using these signals + saving recorded motion to file

    private void TrySaveData()
    {
        if (readyToSaveInput && readyToSaveMotion) //should probably just check if not playing/recording and if motion + input data exist
        {
            //recording motion + midi must have finished before this runs.

            //these are wrapped in the playthroughData + motionData classes because I can't directly serialise a list

            var (controllerRecording, leftHandRecording, rightHandRecording) = ControllerRecorder.getRecording();

            var recordedMotion = new motionData(new List<ControllerRecorder.transformPair>(controllerRecording), new List<ControllerRecorder.handMotionFrame>(leftHandRecording), new List<ControllerRecorder.handMotionFrame>(rightHandRecording)); //List from queue for serialisation.
            var recordedInput = new playthroughData(new List<playthroughFrame>(savedPlaythrough)); //same here

            //saving each of these in a combined class recordingData to have the recording as a single file. Can still load motion / inputs individually if we want to but they get recorded together
            var combinedRecording = new recordingData(recordedMotion, recordedInput);

            var savePath = Path.Combine(Application.persistentDataPath, "saved.json");

            string combinedJson = JsonUtility.ToJson(combinedRecording);

            File.WriteAllText(savePath, combinedJson);

            readyToSaveInput = false;
            readyToSaveMotion = false;
        }
    }

    private void TryLoadData(string filename)
    {
        var loadPath = Path.Combine(Application.persistentDataPath, filename);

        if (File.Exists(loadPath))
        {

            string loadedJson = File.ReadAllText(loadPath);

            recordingData loadedData = JsonUtility.FromJson<recordingData>(loadedJson);
            //set savedPlaythrough
            savedPlaythrough = new Queue<playthroughFrame>(loadedData.inputs.frames);
            //set ControllerRecorder hand and controller queues

            var controllerMotion = new Queue<ControllerRecorder.transformPair>(loadedData.motion.controllerFrames);
            var leftHandMotion = new Queue<ControllerRecorder.handMotionFrame>(loadedData.motion.leftHandFrames);
            var rightHandMotion = new Queue<ControllerRecorder.handMotionFrame>(loadedData.motion.rightHandFrames); //back to queues from serialised Lists 

            ControllerRecorder.loadRecording(controllerMotion, leftHandMotion, rightHandMotion);
            motionRecorded = true;

        }
        else
        {
            Debug.Log("(Playback Manager) Recording file doesn't exist!");
        }
    }


}
