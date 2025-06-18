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
    private bool motionRecorded = false;

    private void loadNewRhythm(string Path)
    {
        if (!rhythmLoaded)
        {
            MIDIFilePath = Path;
            activeNoteSpawner.Initialise(MIDIFilePath);
            activeNoteSpawner.StartedPlaying += OnStartedPlaying;
            activeNoteSpawner.FinishedPlaying += OnFinishedPlaying;
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
        motionRecorded = true;
    }

    private void playRecorded()
    {
        if (motionRecorded && rhythmLoaded)
        {
            //play with recorded motion
        }
    }

    private void Start()
    {
        activeNoteSpawner = Instantiate(noteSpawnerObj).GetComponent<NoteSpawner>();
        ControllerRecorder = ControllerRecorderObj.GetComponent<ControllerRecorder>();
        drumManager = drumManagerObj.GetComponent<DrumManager>();
    }

    private void Update()
    {
        if (OVRInput.GetDown(OVRInput.RawButton.B))
        {
            loadNewRhythm(MIDIFilePath);
            playRhythm();

        }

        playing = activeNoteSpawner.playing;
    }

    private void OnStartedPlaying()
    {
        Debug.Log("NOTE SPAWNER STARTED PLAYING");
    }

    private void OnFinishedPlaying()
    {
        Debug.Log("NOTE SPAWNER FINISHED PLAYING");
        drumManager.clearNotes();
    }

}
