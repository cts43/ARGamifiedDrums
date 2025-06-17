using UnityEngine;

public class PlaybackManager : MonoBehaviour
{

    public GameObject noteSpawnerObj; //prefab for note spawner class
    public string MIDIFilePath;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var noteSpawner = Instantiate(noteSpawnerObj).GetComponent<NoteSpawner>();
        noteSpawner.Initialise(MIDIFilePath);
        noteSpawner.startPlaying();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
