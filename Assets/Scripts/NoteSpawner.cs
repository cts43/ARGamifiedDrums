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
using Unity.VisualScripting;
using UnityEngine.Timeline;



public class NoteSpawner : MonoBehaviour
{

    public class VisualBarsBeatsTicksTimeSpan
    {
        //custom Bars:Beats:Ticks timespan that allows negatives
        public int Bars { private set; get; }
        public int Beats { private set; get; }
        public int Ticks { private set; get; } 
        public bool negative { private set; get; }

        public VisualBarsBeatsTicksTimeSpan(int Bars, int Beats, int Ticks, bool negative)
        {
            this.Bars = Bars;
            this.Beats = Beats;
            this.Ticks = Ticks;
            this.negative = negative;
        }
    }
                
    private long currentTick = 0;
    public string MIDIFilePath;
    private string localFilePath;

    private Vector3 targetPos = new Vector3(0, 0, 0);
    private Vector3 startPos = new Vector3(0, 1, 0);

    public long spawnWindow = 0; //How long before a beat to spawn a note in seconds

    private long spawnWindowinUs = 0;

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
    private long kickAnimationOffset = 330; // time in ms that the foot takes to hit the floor

    long previousBeat = 0;
    public AudioClip metronomeClip;
    private AudioSource metronomeSource;

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
        string streamedPath = Path.Combine(Application.streamingAssetsPath, "MIDI Files/"+path);
        //File loading for Android builds adapted from Unity docs
        UnityWebRequest request = UnityWebRequest.Get(streamedPath);
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
            Debug.LogError("Cannot load file at " + streamedPath);
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
        localFilePath = await LoadFileFromStreamingAssets(MIDIFilePath);
        LoadMIDI();

        //init spawn window as bars beats for easy operations
        var spawnWindowAsTimespan = new MetricTimeSpan(spawnWindowinUs); //time in microseconds
        spawnWindowAsBarsBeats = TimeConverter.ConvertTo<BarBeatTicksTimeSpan>(spawnWindowAsTimespan, tempoMap);
        kickAnimationOffset = TimeConverter.ConvertFrom(new MetricTimeSpan(0, 0, 0, (int)kickAnimationOffset), tempoMap); //convert to ticks
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        //init text label that displays current musical time in Bars:Beats:Ticks
        GameObject currentBeatLabelObject = GameObject.FindWithTag("BeatIndicatorText");
        currentBeatLabel = currentBeatLabelObject.GetComponent<TextMeshProUGUI>();

        GameObject Legs = GameObject.FindWithTag("Legs");
        kickMotion = Legs.GetComponent<Animator>();

        metronomeSource = gameObject.AddComponent<AudioSource>();
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
            currentTick = 0;
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

    public VisualBarsBeatsTicksTimeSpan GetVisualTime()
    {   //Returns real, musical time that notes hit their window. If exception because negative time, keep at 0 until can increment

        long timeAsTicks = 0;
        long BarAsTicks = TimeConverter.ConvertFrom(new BarBeatTicksTimeSpan(1, 0), tempoMap);

        timeAsTicks = currentTick + BarAsTicks - TimeConverter.ConvertFrom(spawnWindowAsBarsBeats, tempoMap); //add a bar for visual accuracy

        var timeDiv = (TicksPerQuarterNoteTimeDivision)tempoMap.TimeDivision;
        short TPQN = timeDiv.TicksPerQuarterNote;
        TimeSignature timeSig = tempoMap.GetTimeSignatureAtTime(GetCurrentMusicalTime());

        int ticksPerBar = (TPQN * timeSig.Numerator) / (4 / timeSig.Denominator);

        int Bars = (int)timeAsTicks / ticksPerBar;
        int remainder = (int)timeAsTicks % ticksPerBar;
        int Beats = remainder / TPQN;
        int Ticks = remainder % TPQN;
        bool negative = (timeAsTicks < 0);

        if (negative)
        {
            Bars = 1 - Bars;
            Beats = 1 - Beats;
            Ticks = 1 - Ticks;
        }

        return new VisualBarsBeatsTicksTimeSpan(Math.Abs(Bars), Math.Abs(Beats), Math.Abs(Ticks), negative);



        //calculate beats bars ticks



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

    IEnumerator playKickMotion(long scheduledTime) //refactor this. bad
    {
        while (GetCurrentOffsetMusicalTimeAsTicks() < scheduledTime-kickAnimationOffset) //where 1 is kick animation window
        {
            yield return null; //wait one frame and check again
        }
        kickMotion.Play("Kick", 0, 0); //finally play the animation
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
            if (showKickMotion)
            {
                StartCoroutine(playKickMotion(noteTimeInTicks));
            }
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

    void FixedUpdate() //fixed update is set to run at 120Hz so better for fast updates like this.
    {
        if (playing) {
            if (GetVisualTime().Beats != previousBeat)
            {
                if (GetVisualTime().Beats == 0)
                {
                    metronomeSource.pitch = 2;
                }
                else
                {
                    metronomeSource.pitch = 1;
                }

                previousBeat = GetVisualTime().Beats;
                metronomeSource.PlayOneShot(metronomeClip);
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
        if (playing)
        {
            VisualBarsBeatsTicksTimeSpan VisualTime = GetVisualTime();
            string sign = "+";
            if (VisualTime.negative)
            {
                sign = "-";
            }

            var newText = sign+VisualTime.Bars.ToString() + ":" + (VisualTime.Beats + 1).ToString() + ":" + VisualTime.Ticks.ToString();
            currentBeatLabel.text = newText;
        }

    }

    
}
