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
    private long currentTick = 0;
    public string MIDIFilePath;
    private string localFilePath;

    private Vector3 targetPos = new Vector3(0, 0, 0);
    private Vector3 startPos = new Vector3(0, 1, 0);

    public long spawnWindow; //How long before a beat to spawn a note in seconds

    private long spawnWindowinUs;

    private BarBeatTicksTimeSpan spawnWindowAsBarsBeats;

    private string visualNotePrefabPath = "Prefabs/NoteIndicator";
    private GameObject visualNotePrefab;

    private TextMeshProUGUI currentBeatLabel;

    private TempoMap tempoMap;

    private Queue<(int, int, BarBeatTicksTimeSpan)> notesList;

    private BarBeatTicksTimeSpan finalNoteTime;

    public int totalNotes { get; private set; }

    public bool playing { get; private set; } = false;

    public bool showKickMotion = false;
    private Animator kickMotion;

    public event Action StartedPlaying;
    public void RaiseStartedPlaying()
    {
        StartedPlaying?.Invoke();
    }

    public event Action FinishedPlaying;
    public void RaiseFinishedPlaying()
    {
        FinishedPlaying?.Invoke();
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
        //first clear existing note queue if exists
        notesList = new Queue<(int, int, BarBeatTicksTimeSpan)>();

        MIDIReader MIDIReader = GameObject.FindWithTag("MIDI Reader").GetComponent<MIDIReader>();
        (Queue<(int, int, BarBeatTicksTimeSpan)> notes, TempoMap TempoMap) = MIDIReader.LoadMIDIFile(localFilePath); //returns tuple collection of notes + tempo map

        //queue every note onto our local queue. This way when we call this again old notes are not discarded.
        foreach (var note in notes)
        {
            notesList.Enqueue(note);
        }

        tempoMap = TempoMap;
        finalNoteTime = notesList.Last().Item3; //last item in queue's note time should be the final note
        totalNotes = notesList.Count;
    }

    async void InitLoadMIDI()
    {
        //Load MIDI from file
        MIDIFilePath = Path.Combine(Application.streamingAssetsPath, MIDIFilePath);
        localFilePath = await LoadFileFromStreamingAssets(MIDIFilePath);
        LoadMIDI();

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

        GameObject Legs = GameObject.FindWithTag("Legs");
        kickMotion = Legs.GetComponent<Animator>();
    }

    public void Initialise(string filePath, int window = 2)
    {
        visualNotePrefab = Resources.Load<GameObject>(visualNotePrefabPath);
        MIDIFilePath = filePath;
        spawnWindow = window;

        spawnWindowinUs = spawnWindow * 1000000;

        //

        InitLoadMIDI();
    }

    public void Play()
    {
        if (!playing)
        {
            playing = true;
            RaiseStartedPlaying();
        }
    }

    public BarBeatTicksTimeSpan GetCurrentMusicalTime()
    {
        if (playing)
        {
            return TimeConverter.ConvertTo<BarBeatTicksTimeSpan>(currentTick, tempoMap);
        }
        else return new BarBeatTicksTimeSpan(0, 0, 0);
    }

    public BarBeatTicksTimeSpan GetVisualTime()
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
        if (playing)
        {
            return currentTick - TimeConverter.ConvertFrom(spawnWindowAsBarsBeats, tempoMap);
        }
        else
        {
            return -TimeConverter.ConvertFrom(spawnWindowAsBarsBeats, tempoMap);
        }
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

        if (noteDrum.GetComponent<DrumHit>().isKick)
        {
            //if the drum is designated as a kick drum, then the kick note should be spawned - big line like in guitar hero? 
            //would then line up with the other drums visually
            //but recorded 'motion' could be animated leg

            //big line doesn't necessarily work as drums will be in different positions.
            startPos = new Vector3(0, 0, 1); //right now kick drum is the same as the others but spawns from Z+1 instead of Y+1
            kickMotion.Play("Kick",0,0);
        }
        else
        {
            startPos = new Vector3(0, 1, 0);
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

        kickMotion.transform.gameObject.SetActive(showKickMotion); //legs are only shown when bool showKickMotion is true, set by PlaybackManager

        if (playing && notesList != null)
        {

            while (notesList.Count > 0) //while rather than if to allow multiple notes on the same frame
            {
                (var note, var velocity, var noteTime) = notesList.Peek();

                if (GetCurrentMusicalTime() >= noteTime)
                {
                    (var noteNumber, var noteVelocity, var currentNoteTime) = notesList.Dequeue(); //remove note from queue and spawn
                    //Debug.Log($"Spawning note {noteNumber} at time {currentNoteTime}");
                    StartCoroutine(SpawnNote(noteNumber, noteVelocity, currentNoteTime)); //time as long for spawner
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

            if (GetCurrentOffsetMusicalTimeAsTicks() >= TimeConverter.ConvertFrom(finalNoteTime, tempoMap) + 480) //quarter note window to allow for final hits
            {
                playing = false;
                currentTick = 0;
                LoadMIDI();

                RaiseFinishedPlaying();
            }

        }
        
        
        //show beats on label
        currentBeatLabel.text = GetVisualTime().ToString();

    }

    
}
