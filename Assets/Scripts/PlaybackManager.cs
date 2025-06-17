using Unity.VisualScripting;
using UnityEngine;

public class PlaybackManager : MonoBehaviour
{

    public GameObject noteSpawnerObj; //prefab for note spawner class
    public string MIDIFilePath;

    private bool rhythmLoaded = false;
    private bool motionRecorded = false;
    private NoteSpawner activeNoteSpawner;

    private void loadNewRhythm(string Path)
    {
        MIDIFilePath = Path;
        activeNoteSpawner = Instantiate(noteSpawnerObj).GetComponent<NoteSpawner>();
        activeNoteSpawner.Initialise(MIDIFilePath);
        rhythmLoaded = true;

    }

    private void playRhythm()
    {
        if (rhythmLoaded)
        {
            //activeNoteSpawner.startPlaying();
        }
    }

    private void playWithRecord()
    {
        //reload and play with recording on
        loadNewRhythm(MIDIFilePath);
        //incomplete
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
        loadNewRhythm(MIDIFilePath);
        playRhythm();  
    }

}
