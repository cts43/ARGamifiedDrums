using UnityEngine;
using Melanchall.DryWetMidi.Interaction;
using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using Unity.VisualScripting;


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

    public float getCurrentOffsetTime()
    {
        return currentTime - spawnWindow; //offset time by spawn window
    }

    private GameObject GetDrum(int note)
    {
        var drums = GameObject.FindGameObjectsWithTag("Drum");

        //Checks whether the MIDI note hit exists as a drum in DrumManager, and runs relevant function if so

        foreach (GameObject drum in drums)
        {
            var DrumScript = drum.GetComponent<DrumHit>();
            if (note == DrumScript.note)
            {
                //Debug.Log("Found drum!");
                return drum;
            }
        }
        //if not found, return null
        return null;
    }

    IEnumerator SpawnNote(int note, int velocity, double noteTime)
    {

        GameObject noteDrum = GetDrum(note);

        if (noteDrum == null)
        {
            Debug.Log("Tried to spawn note for drum " + note + ", but doesn't exist!");
            yield break;
        }

        GameObject spawnedNote = Instantiate(visualNotePrefab, noteDrum.transform);
        spawnedNote.transform.Translate(startPos);
        spawnedNote.GetComponent<NoteIndicator>().ScheduledTime = noteTime + spawnWindow;
        Vector3 distanceFromTarget = targetPos - startPos;

        Vector3 speedToMove = distanceFromTarget / spawnWindow;

        while (true)
        {
            if (spawnedNote != null)
            {
                spawnedNote.transform.Translate(speedToMove * Time.deltaTime);
                yield return null;
            }
            else
            {
                yield break;
            }
        }


    }


    // Update is called once per frame
    void Update()
    {

        while (notesList.Count > 0) //while rather than if to allow multiple notes on the same frame
        {
            var currentNote = notesList.Peek();
            var currentNoteTime = currentNote.Item3.TotalSeconds;

            if ((getCurrentOffsetTime() >= currentNoteTime))
            {
                var noteToSpawn = notesList.Dequeue(); //remove note from queue and spawn
                StartCoroutine(SpawnNote(noteToSpawn.Item1, noteToSpawn.Item2, currentNoteTime));
            }
            else
            {
                break; //but break if out of notes
            }


        }
        currentTime += Time.deltaTime; //time since previous frame added to current time each frame
    }

    
}
