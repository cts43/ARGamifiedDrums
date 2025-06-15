using UnityEngine;
using Melanchall.DryWetMidi.Interaction;
using System.Collections;
using System.Collections.Generic;
using Melanchall.DryWetMidi.Core;
using System;
using TMPro;
using System.IO;
using UnityEngine.Networking;
using System.Threading.Tasks;
using System.Linq;



public class NoteSpawner : MonoBehaviour
{

    //Ideally this will be refactored to use musical time (Bar:Beat:Seconds) rather than only seconds. That will allow easier looping and representation of time signatures etc.
    private long currentTick = 0;

    //private long ticksPerQuarterNote;

    public string MIDIFilePath;

    private string localFilePath;

    public Vector3 targetPos;

    public Vector3 startPos;

    public long spawnWindow; //How long before a beat to spawn a note in seconds

    private long spawnWindowinUs;

    private BarBeatTicksTimeSpan spawnWindowAsBarsBeats;

    public GameObject visualNotePrefab;

    private TextMeshProUGUI currentBeatLabel;

    private TempoMap tempoMap;

    private Queue<(int, int, BarBeatTicksTimeSpan)> notesList;

    private Queue<(int, int, BarBeatTicksTimeSpan)> originalNotesList;

    private bool playing = false;

    private BarBeatTicksTimeSpan finalNoteTime;

    private void startPlaying()
    {
        if (!playing)
        {

            //notesList = originalNotesList;
            LoadMIDI();
            currentTick = 0;
            playing = true;
        }
    }

    private async Task<string> LoadFileFromStreamingAssets(string path)
    {
        //File loading for Android builds adapted from Unity docs
        UnityWebRequest request = UnityWebRequest.Get(path);
        UnityWebRequestAsyncOperation operation = request.SendWebRequest();

        while (!operation.isDone)
        {
            await Task.Yield();
        }
        if (request.result == UnityWebRequest.Result.Success)
        {
            //Debug.Log(request.downloadHandler.text); the binary file contents
            string newPath = Path.Combine(Application.persistentDataPath, "received.mid");
            File.WriteAllBytes(newPath, request.downloadHandler.data); //write file to received.mid

            return newPath;
        }
        else
        {
            Debug.LogError("Cannot load file at " + MIDIFilePath);
            return null;
        }
    }

    private void LoadMIDI()
    {
        MIDIReader MIDIReader = GameObject.FindWithTag("MIDI Reader").GetComponent<MIDIReader>();
        (Queue<(int, int, BarBeatTicksTimeSpan)>, TempoMap) notesAndMap = MIDIReader.LoadMIDIFile(localFilePath); //returns tuple collection of notes + tempo map
        notesList = notesAndMap.Item1;
        originalNotesList = notesList;
        tempoMap = notesAndMap.Item2;
        finalNoteTime = notesList.Last().Item3; //last item in queue's note time should be the final note
    }

    async void InitLoadMIDI()
    {
        //Load MIDI from file
        MIDIFilePath = Path.Combine(Application.streamingAssetsPath, MIDIFilePath);
        localFilePath = await LoadFileFromStreamingAssets(MIDIFilePath);
        LoadMIDI();

        //unnecessary but leaving here so I remember how to access all of these values
        //var timeDivision = (TicksPerQuarterNoteTimeDivision)tempoMap.TimeDivision; //cast type
        //ticksPerQuarterNote = timeDivision.TicksPerQuarterNote; //get ticks per 1/4 note from tempo map
        //Debug.Log("Ticks/ 1/4 note: " + ticksPerQuarterNote);
        //Debug.Log("Tempo: " + tempoMap.GetTempoAtTime(new MidiTimeSpan(0)));

        //init spawn window as bars beats for easy operations
        var spawnWindowAsTimespan = new MetricTimeSpan(spawnWindowinUs); //time in microseconds
        spawnWindowAsBarsBeats = TimeConverter.ConvertTo<BarBeatTicksTimeSpan>(spawnWindowAsTimespan, tempoMap);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        //init text label that displays current musical time in Bars:Beats:Ticks
        GameObject currentBeatLabelObject = GameObject.FindWithTag("BeatIndicatorText");
        currentBeatLabel = currentBeatLabelObject.GetComponent<TextMeshProUGUI>();

        spawnWindowinUs = spawnWindow * 1000000;

        InitLoadMIDI();
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
        spawnedNote.GetComponent<NoteIndicator>().ScheduledTimeInTicks = noteTimeInTicks;// + TimeConverter.ConvertFrom(spawnWindowAsBarsBeats,tempoMap);
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

        if (OVRInput.GetDown(OVRInput.RawButton.RHandTrigger))
        {
            startPlaying();
        }

        if (!playing)
        {
            return;
        }

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
        long deltaTimeinuS = (long)(Time.deltaTime * 1000000);

        MetricTimeSpan deltaAsTimeSpan = new MetricTimeSpan(deltaTimeinuS);
        long deltaAsTicks = TimeConverter.ConvertFrom(deltaAsTimeSpan, tempoMap);
        currentTick += deltaAsTicks;


        //show beats on label
        currentBeatLabel.text = GetCurrentOffsetMusicalTime().ToString();

        if (GetCurrentOffsetMusicalTime() >= finalNoteTime){
            playing = false;
        }

    }

    
}
