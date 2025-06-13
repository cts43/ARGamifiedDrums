using UnityEngine;
using Melanchall.DryWetMidi.Interaction;
using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using Unity.VisualScripting;
using Melanchall.DryWetMidi.Core;
using System;
using TMPro;


public class NoteSpawner : MonoBehaviour
{

    //Ideally this will be refactored to use musical time (Bar:Beat:Seconds) rather than only seconds. That will allow easier looping and representation of time signatures etc.
    private long currentTick = 0;

    //private long ticksPerQuarterNote;

    public string MIDIFilePath;

    public Vector3 targetPos;

    public Vector3 startPos;

    public long spawnWindow; //How long before a beat to spawn a note in seconds

    private long spawnWindowinUs;

    private BarBeatTicksTimeSpan spawnWindowAsBarsBeats;

    public GameObject visualNotePrefab;

    private TempoMap tempoMap;

    private Queue<(int, int, BarBeatTicksTimeSpan)> notesList;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        spawnWindowinUs = spawnWindow * 1000000;

        MIDIReader MIDIReader = GameObject.FindWithTag("MIDI Reader").GetComponent<MIDIReader>();
        (Queue<(int, int, BarBeatTicksTimeSpan)>, TempoMap) notesAndMap = MIDIReader.LoadMIDIFile(MIDIFilePath); //returns tuple collection of notes + tempo map
        notesList = notesAndMap.Item1;
        tempoMap = notesAndMap.Item2;
        var timeDivision = (TicksPerQuarterNoteTimeDivision)tempoMap.TimeDivision; //cast type
        //ticksPerQuarterNote = timeDivision.TicksPerQuarterNote; //get ticks per 1/4 note from tempo map
        //Debug.Log("Ticks/ 1/4 note: " + ticksPerQuarterNote);
        Debug.Log("Tempo: " + tempoMap.GetTempoAtTime(new MidiTimeSpan(0)));

        var spawnWindowAsTimespan = new MetricTimeSpan((long)spawnWindowinUs); //time in microseconds
        spawnWindowAsBarsBeats = TimeConverter.ConvertTo<BarBeatTicksTimeSpan>(spawnWindowAsTimespan,tempoMap);
    }

    public BarBeatTicksTimeSpan GetCurrentMusicalTime() {
        return TimeConverter.ConvertTo<BarBeatTicksTimeSpan>(currentTick, tempoMap);
    }

    public BarBeatTicksTimeSpan GetCurrentOffsetMusicalTime()
    {   //Returns real, musical time that notes hit their window. If exception because negative time, keep at 0 until can increment

        BarBeatTicksTimeSpan time;
        try
        {
            time = TimeConverter.ConvertTo<BarBeatTicksTimeSpan>(currentTick, tempoMap) - spawnWindowAsBarsBeats;
        }
        catch (Exception)
        {
            time = new BarBeatTicksTimeSpan(0,0,0);
        }
        return time;
    }

    public long GetCurrentOffsetMusicalTimeAsTicks()
    {
        return currentTick - TimeConverter.ConvertFrom(spawnWindowAsBarsBeats,tempoMap);
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

    IEnumerator SpawnNote(int note, int velocity, BarBeatTicksTimeSpan noteTime)
    {

        long noteTimeInTicks = TimeConverter.ConvertFrom(noteTime, tempoMap);

        GameObject noteDrum = GetDrum(note);

        if (noteDrum == null)
        {
            Debug.Log("Tried to spawn note for drum " + note + ", but doesn't exist!");
            yield break;
        }

        GameObject spawnedNote = Instantiate(visualNotePrefab, noteDrum.transform);
        spawnedNote.transform.Translate(startPos);
        spawnedNote.GetComponent<NoteIndicator>().ScheduledTimeInTicks = noteTimeInTicks + TimeConverter.ConvertFrom(spawnWindowAsBarsBeats,tempoMap);
        spawnedNote.GetComponent<NoteIndicator>().TempoMap = tempoMap;
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
            var currentNoteTime = currentNote.Item3;

            if ((GetCurrentMusicalTime() >= currentNoteTime))
            {
                var noteToSpawn = notesList.Dequeue(); //remove note from queue and spawn
                StartCoroutine(SpawnNote(noteToSpawn.Item1, noteToSpawn.Item2, currentNoteTime)); //time as long for spawner
            }
            else
            {
                break; //but break if out of notes
            }


        }

        //convert delta time (seconds) to microseconds and then to midi ticks
        long deltaTimeinuS = (long)(Time.deltaTime * spawnWindowinUs);

        MetricTimeSpan deltaAsTimeSpan = new MetricTimeSpan(deltaTimeinuS);
        long deltaAsTicks = TimeConverter.ConvertFrom(deltaAsTimeSpan, tempoMap);

        //Debug.Log("Time in uS: "+deltaTimeinuS+" Time as timespan: "+deltaAsTimeSpan+" Time as ticks: "+deltaAsTicks);
        currentTick += deltaAsTicks;
        //Debug.Log(GetCurrentMusicalTime());
        //currentTime += Time.deltaTime; //time since previous frame added to current time each frame
    }

    
}
