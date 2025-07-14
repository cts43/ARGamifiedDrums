using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Meta.XR.ImmersiveDebugger.UserInterface.Generic;
using UnityEngine;


public class PlaybackManager : MonoBehaviour
{

    public static PlaybackManager Instance;

    public GameObject noteSpawnerObj; //prefab for note spawner class
    public GameObject ControllerRecorderObj;
    private NoteSpawner activeNoteSpawner;
    private ControllerRecorder ControllerRecorder;
    public GameObject drumManagerObj;
    private DrumManager drumManager;
    private GameObject RecordingMenu;

    public Action FinishedPlaying;

    private StatusIndicator statusIndicator;

    public string MIDIFilePath;

    public static bool MIDILoaded = false;

    public long currentTimeInTicks { get; private set; }

    private controllerActions inputActions;

    private MidiEventCatcher MIDIEventCatcher;

    public static bool playing = false;
    private bool motionRecording = false;
    private bool motionRecorded = false;
    private bool motionPlaying = false;
    private bool savingPlaythrough = false;
    private Queue<playthroughFrame> savedPlaythrough = new Queue<playthroughFrame>();
    private Queue<playthroughFrame> savedPlaythroughCopy = new Queue<playthroughFrame>();
    private bool playingRecordedInputs = false;
    private bool readyToSaveMotion = false;
    private bool readyToSaveInput = false;


    //Serialisable classes for saving playthrough to file -- needed for plotting graphs etc.
    [Serializable]
    public class playthroughFrame
    {
        [SerializeField] public int note;
        [SerializeField] public int velocity;
        [SerializeField] public long hitTime;
        [SerializeField] public long closestNoteTime;
        [SerializeField] public bool hitSuccessfully;
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
    public class playthroughData
    {
        [SerializeField] public List<playthroughFrame> frames;

        public playthroughData(List<playthroughFrame> frames)
        {
            this.frames = frames;
        }
    }

    [Serializable]
    public class motionData
    {
        [SerializeField] public List<ControllerRecorder.transformPair> controllerFrames;
        [SerializeField] public List<ControllerRecorder.handMotionFrame> leftHandFrames;
        [SerializeField] public List<ControllerRecorder.handMotionFrame> rightHandFrames;
        [SerializeField] public ControllerRecorder.recordedTransform moveableSceneTransform;

        public motionData(List<ControllerRecorder.transformPair> controllerFrames, List<ControllerRecorder.handMotionFrame> leftHandFrames, List<ControllerRecorder.handMotionFrame> rightHandFrames, ControllerRecorder.recordedTransform moveableSceneTransform = null)
        {
            this.controllerFrames = controllerFrames;
            this.leftHandFrames = leftHandFrames;
            this.rightHandFrames = rightHandFrames;
            this.moveableSceneTransform = moveableSceneTransform;
        }
    }

    //class that stores all motion + playthrough data to allow saving to single .json file
    [Serializable]
    public class recordingData
    {
        public motionData motion;
        public playthroughData inputs;

        public recordingData(motionData motion, playthroughData inputs)
        {
            this.motion = motion;
            this.inputs = inputs;
        }
    }

    public bool loadNewMIDI(string Path)
    {

        try
        {

            MIDIFilePath = Path;
            activeNoteSpawner.Initialise(MIDIFilePath);
            MIDILoaded = true;

            currentTimeInTicks = -activeNoteSpawner.spawnWindowAsTicks;

            return true;

        }
        catch (Exception)
        {
            Debug.Log($"Failed to load MIDI: {Path}");
            return false;
        }


    }

    private void playMIDI()
    {
        if (MIDILoaded)
        {
            activeNoteSpawner.Play();
        }
    }

    private IEnumerator playWithRecord()
    {
        //reload and play with recording on

        for (int i = 5; i >= 0; i--)
        {
            statusIndicator.ShowStatus("Recording in " + i + " seconds...");
            yield return new WaitForSeconds(1);
        }

        if (MIDILoaded)
        {
            loadNewMIDI(MIDIFilePath);
            Debug.Log("New MIDI Loaded");
            motionPlaying = false;
            motionRecorded = false;
            motionRecording = true;
            playingRecordedInputs = false;
            savedPlaythrough = new Queue<playthroughFrame>();
            playMIDI();
            ControllerRecorder.Record();//record controller motion
            SavePlaythrough(); //save drum inputs/notes played
        }
        else
        {
            statusIndicator.ShowStatus("No MIDI file loaded!");
        }
    }

    public void playRecorded(bool motion, bool drumHits) //arguments decide whether the recorded notes hit should be played back. vital for demonstating while the player is playing
    {

        if (!MIDILoaded)
        {
            loadNewMIDI(MIDIFilePath);
        }

        if (MIDILoaded)
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

        if (Instance == null)
        {
            Instance = this;
        }

        inputActions = new controllerActions();
        inputActions.Enable();

        activeNoteSpawner = Instantiate(noteSpawnerObj).GetComponent<NoteSpawner>();
        ControllerRecorder = ControllerRecorderObj.GetComponent<ControllerRecorder>();
        drumManager = drumManagerObj.GetComponent<DrumManager>();

        MIDIEventCatcher = GameObject.FindWithTag("MIDI Input Handler").GetComponent<MidiEventCatcher>();

        activeNoteSpawner.StartedPlaying += OnMIDIStartedPlaying;
        activeNoteSpawner.FinishedPlaying += OnMIDIFinishedPlaying;
        ControllerRecorder.StartedRecording += OnStartedRecording;
        ControllerRecorder.FinishedRecording += OnFinishedRecording;
        subscribeToDrumHits();

        RecordingMenu = GameObject.FindWithTag("RecordingMenu");
        RecordingMenu.SetActive(false);

        statusIndicator = GameObject.FindWithTag("StatusIndicator").GetComponent<StatusIndicator>();

        loadNewMIDI(MIDIFilePath); //load MIDI file initially

    }

    private void Update()
    {

        playing = activeNoteSpawner.playing;
        bool subMenuOpen = GameObject.FindWithTag("SubMenu") != null;
        if (playing)
        {
            currentTimeInTicks = activeNoteSpawner.GetCurrentOffsetMusicalTimeAsTicks(); //Update current time from active note spawner instance. unsure if necessary
        }

        if (inputActions.Controller.OpenMenu.triggered && !playing)
        {

            if (!RecordingMenu.activeInHierarchy)
            {
                RecordingMenu.SetActive(true);
            }
            else if (!subMenuOpen)
            {
                RecordingMenu.SetActive(false);
            }
        }

        if (RecordingMenu.activeInHierarchy)
        {
            return; //don't allow other inputs if menu is open
        }

        if (inputActions.Controller.Back.triggered)
        {
            MIDIEventCatcher.ToggleDrumSounds();
            statusIndicator.ShowStatus("Toggled drum sounds " + (MIDIEventCatcher.playDrumSounds ? "on" : "off"));
        }



        if (OVRInput.GetDown(OVRInput.RawButton.B))
        {
            if (!playing)
            {
                if (motionRecorded)
                {
                    playRecorded(true, true);
                }
                else
                {
                    Debug.Log("No recording loaded!");
                    statusIndicator.ShowStatus("No recording loaded!");
                }
            }
        }
        else if (OVRInput.GetDown(OVRInput.RawButton.Y))
        {
            if (!playing)
            {
                StartCoroutine(playWithRecord());
            }
        }

        if (playingRecordedInputs)
        { //testing playing back user inputs
            playbackRecordedInputs();
        }

        if (motionPlaying)
        {
            activeNoteSpawner.showKickMotion = true;

        }
        else
        {
            activeNoteSpawner.showKickMotion = false;
        }

    }

    private void playbackRecordedInputs()
    {   //to be called inside Update(). iterate through recorded notes as in NoteSpawner
        while (savedPlaythroughCopy.Count > 0)
        {
            (var note, var velocity, var time, _, _) = savedPlaythroughCopy.Peek();
            if (activeNoteSpawner.GetCurrentOffsetMusicalTimeAsTicks() >= time) //offset based on spawn window
            {
                //Debug.Log("Note time: " + time + " Playback time: " + activeNoteSpawner.GetCurrentOffsetMusicalTimeAsTicks());
                savedPlaythroughCopy.Dequeue();
                MIDIEventCatcher.checkForDrum(note, velocity);
            }
            else
            {
                break;
            }
        }
    }

    private void SavePlaythrough()
    {
        if (!savingPlaythrough)
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
        FinishedPlaying?.Invoke();

        ControllerRecorder.Reset();
        savingPlaythrough = false;
        drumManager.clearNotes();
        readyToSaveInput = true;
        motionPlaying = false;
        playingRecordedInputs = false;
        currentTimeInTicks = -activeNoteSpawner.spawnWindowAsTicks;
        int hitNotes = 0;
        int missedNotes;

        if (savedPlaythrough.Count > 0)
        {

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

            statusIndicator.ShowStatus($"Hit {hitNotes}/{activeNoteSpawner.totalNotes} notes.");

            double percentageMissed = (double)missedNotes / activeNoteSpawner.totalNotes * 100;

            Debug.Log("Percentage missed: " + percentageMissed + "%. Percentage hit: " + (100 - percentageMissed) + "%.");
        }



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

    public bool TrySaveData()
    {
        if (readyToSaveInput && readyToSaveMotion) //should probably just check if not playing/recording and if motion + input data exist
        {
            //recording motion + midi must have finished before this runs.

            //these are wrapped in the playthroughData + motionData classes because I can't directly serialise a list

            try
            {

                var (controllerRecording, leftHandRecording, rightHandRecording, moveableSceneTransform) = ControllerRecorder.getRecording();

                var recordedMotion = new motionData(new List<ControllerRecorder.transformPair>(controllerRecording), new List<ControllerRecorder.handMotionFrame>(leftHandRecording), new List<ControllerRecorder.handMotionFrame>(rightHandRecording), moveableSceneTransform); //List from queue for serialisation.
                var recordedInput = new playthroughData(new List<playthroughFrame>(savedPlaythrough)); //same here

                //saving each of these in a combined class recordingData to have the recording as a single file. Can still load motion / inputs individually if we want to but they get recorded together
                var combinedRecording = new recordingData(recordedMotion, recordedInput);

                string date = System.DateTime.Now.ToString("yyyyMMdd-HH-mm");

                string recordingFolder = FileManager.Instance.GetRecordingsPath();

                string savePath = Path.Combine(recordingFolder, MIDIFilePath + ".json");

                int i = 1;
                while (File.Exists(savePath))
                {
                    savePath = Path.Combine(recordingFolder, MIDIFilePath + " - " + i + ".json");
                    i++;
                }

                string combinedJson = JsonUtility.ToJson(combinedRecording);

                File.WriteAllText(savePath, combinedJson);

                readyToSaveInput = false;
                readyToSaveMotion = false;

                return true;

            }
            catch (Exception e)
            {
                Debug.Log(e);
                return false;
            }
        }

        //if it wasn't in the correct state to save, return false
        return false;
    }

    public bool TryLoadData(string filename)
    {

        Debug.Log("(Playback Manager) Loading recording from file: " + filename);

        var loadPath = Path.Combine(FileManager.Instance.GetRecordingsPath(), filename);

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
            var moveableSceneTransform = loadedData.motion.moveableSceneTransform;

            ControllerRecorder.loadRecording(controllerMotion, leftHandMotion, rightHandMotion, moveableSceneTransform);
            motionRecorded = true;

            return true;

        }
        else
        {
            Debug.Log("(Playback Manager) Recording file doesn't exist!");
            return false;
        }
    }

    public bool IsPlayingRecordedInputs()
    {
        return playingRecordedInputs;
    }


}
