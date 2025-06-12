using UnityEngine;
using Melanchall.DryWetMidi.Core;
using System.IO;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.MusicTheory;
using System.Collections.Generic;
using UnityEngine.UIElements;
using System.Linq;

public class NoteSpawner : MonoBehaviour
{

    private float startTime;
    private float currentTime;

    public GameObject visualNotePrefab;

    private Queue<(int, int, MetricTimeSpan)> notesList;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        MIDIReader MIDIReader = GameObject.FindWithTag("MIDI Reader").GetComponent<MIDIReader>();
        notesList = MIDIReader.LoadedFile;

        startTime = Time.time;

    }

    float getCurrentTime()
    {
        return Time.time - startTime;
    }

    void spawnNote(int note, int velocity)
    {
        Debug.Log("Note " + note + ", velocity " + velocity + " spawned");
        var noteObject = Instantiate(visualNotePrefab);

    }

    // Update is called once per frame
    void Update()
    {
        if (notesList.Count > 0)
        {
            var currentNote = notesList.Peek();
            var currentNoteTime = currentNote.Item3.TotalSeconds;
            //Debug.Log(getCurrentTime() + " " + currentNoteTime + " " + ((getCurrentTime() >= currentNoteTime)));
            if ((getCurrentTime() >= currentNoteTime) && (notesList.Count > 0))
            {
                var noteToSpawn = notesList.Dequeue();
                spawnNote(noteToSpawn.Item1, noteToSpawn.Item2);

            }
        }
    }

    
}
