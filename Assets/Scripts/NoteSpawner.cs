using UnityEngine;
using Melanchall.DryWetMidi.Interaction;
using System.Collections;
using System.Collections.Generic;


public class NoteSpawner : MonoBehaviour
{

    //Ideally this will be refactored to use musical time (Bar:Beat:Seconds) rather than only seconds. That will allow easier looping and representation of time signatures etc.
    private float currentTime = 0f;

    public Vector3 targetPos;

    public Vector3 startPos;

    public float spawnWindow; //How long before a beat to spawn a note in seconds

    public GameObject visualNotePrefab;

    private Queue<(int, int, MetricTimeSpan)> notesList;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        MIDIReader MIDIReader = GameObject.FindWithTag("MIDI Reader").GetComponent<MIDIReader>();
        notesList = MIDIReader.LoadedFile;

    }

    float getCurrentTime()
    {
        return currentTime - spawnWindow; //offset time by spawn window
    }

    IEnumerator SpawnNote(int note, int velocity, double noteTime)
    {
        GameObject spawnedNote = Instantiate(visualNotePrefab, startPos, Quaternion.identity);

        Debug.Log(spawnedNote.transform.position + "SPAWNEDNOTEPOS");

        float initialSpawnTime = getCurrentTime();
        Vector3 distanceFromTarget = targetPos - startPos;

        Vector3 speedToMove = distanceFromTarget / spawnWindow;

        while (true)
        {
            spawnedNote.transform.Translate(speedToMove*Time.deltaTime);
            yield return null;
        }


    }


    // Update is called once per frame
    void Update()
    {
        while (notesList.Count > 0) //while rather than if to allow multiple notes on the same frame
        {
            var currentNote = notesList.Peek();
            var currentNoteTime = currentNote.Item3.TotalSeconds;
            //Debug.Log(getCurrentTime() + " " + currentNoteTime + " " + ((getCurrentTime() >= currentNoteTime)));
            if ((getCurrentTime() >= currentNoteTime))
            {
                var noteToSpawn = notesList.Dequeue(); //remove note from queue and spawn
                StartCoroutine(SpawnNote(noteToSpawn.Item1, noteToSpawn.Item2,currentNoteTime));

            }
            else
            {
                break; //but break if out of notes
            }
        }
        currentTime += Time.deltaTime; //time since previous frame added to current time each frame
    }

    
}
