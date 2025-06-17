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
        if (rhythmLoaded)
        {
            MIDIFilePath = Path;
            activeNoteSpawner.Initialise(MIDIFilePath);
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
      activeNoteSpawner = Instantiate(noteSpawnerObj).GetComponent<NoteSpawner>();
    }

    private void Update()
    {
        if (OVRInput.GetDown(OVRInput.RawButton.B))
        {
            loadNewRhythm(MIDIFilePath);
            playRhythm();

        }
    }

}
