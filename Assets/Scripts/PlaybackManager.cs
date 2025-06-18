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

    public static bool playing = false;
    private bool motionRecording = false;
    private bool motionRecorded = false;

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

    private void Start()
    {
        activeNoteSpawner = Instantiate(noteSpawnerObj).GetComponent<NoteSpawner>();
        ControllerRecorder = ControllerRecorderObj.GetComponent<ControllerRecorder>();
        drumManager = drumManagerObj.GetComponent<DrumManager>();

        ControllerRecorder.StartedRecording += OnStartedRecording;
        ControllerRecorder.FinishedRecording += OnFinishedRecording;
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
    }

    private void OnMIDIStartedPlaying()
    {
        Debug.Log("(Playback Manager) MIDI Started");
    }

    private void OnMIDIFinishedPlaying()
    {
        Debug.Log("(Playback Manager) MIDI Finished");
        ControllerRecorder.StopRecording();
        drumManager.clearNotes();
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

}
