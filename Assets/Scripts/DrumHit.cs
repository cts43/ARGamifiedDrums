using System;
using System.Collections;
using Melanchall.DryWetMidi.Interaction;
using UnityEngine;
using UnityEngine.XR.ARFoundation;


public class DrumHit : MonoBehaviour
{

    Renderer selfRenderer;

    public Color drumColour;
    public Color changeColour;

    public int note;
    public bool isKick = false;

    private int waitFrames = 10; //default 10

    private int hitWindowInMs;

    private NoteSpawner noteSpawner;

    private Coroutine changeColourOnHit;

    private ScoreIndicator scoreIndicator;

    public event Action<int, int, long, long, bool> HitDrum;
    public void RaiseHitDrum(int note, int velocity, long timeHit,long closestNote, bool hitNote)
    {
        HitDrum?.Invoke(note,velocity,timeHit,closestNote,hitNote);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        DrumManager drumManager = GetComponentInParent<DrumManager>();
        waitFrames = drumManager.framesToHighlightOnHit;
        hitWindowInMs = drumManager.hitWindowInMs;

        selfRenderer = GetComponent<Renderer>();
        ResetColour(); //set initial colour to one set in Inspector
        noteSpawner = GameObject.FindGameObjectWithTag("Note Spawner").GetComponent<NoteSpawner>();

        scoreIndicator = GameObject.FindWithTag("Score").GetComponent<ScoreIndicator>();
    }

    private (long,long,bool) checkIfHitNote()
    {
        var hitWindowAsTimeSpan = new MetricTimeSpan(0, 0, 0, hitWindowInMs);
        bool hitNote = false;
        long currentTime = noteSpawner.GetCurrentOffsetMusicalTimeAsTicks();
        long closestNoteTime = 0;
        double? prevDiff = null;
        foreach (var note in GetComponentsInChildren<NoteIndicator>())
        {
            long diff = System.Math.Abs(currentTime - note.ScheduledTimeInTicks);

            if (prevDiff != null)
            {
                if (diff < prevDiff)
                {
                    closestNoteTime = note.ScheduledTimeInTicks;
                }
            }
            else
            {
                closestNoteTime = note.ScheduledTimeInTicks;
            }

            if (diff <= TimeConverter.ConvertFrom(hitWindowAsTimeSpan, note.TempoMap) / 2)
            {
                hitNote = true;
                Debug.Log("Successfully Hit Note at Tick " + currentTime + ", closest note: " + closestNoteTime + " Diff: "+diff);
                note.destroy(); //destroy hit note
                Debug.Log(scoreIndicator);

                if (diff <= 50)
                {
                    Debug.Log("Perfect!");
                    StartCoroutine(scoreIndicator.ReplaceLabel("Perfect!"));

                }
                else if (diff <= 100)
                {
                    Debug.Log("Great!");
                    StartCoroutine(scoreIndicator.ReplaceLabel("Great!"));
                }
                else
                {
                    Debug.Log("OK!");
                    StartCoroutine(scoreIndicator.ReplaceLabel("OK!"));
                }

                // To avoid TimeConverter complaining about currentTime being negative, we add an arbitrary number of ticks here and then subtract them again from the double

                long bufferTime = 480;
                currentTime += bufferTime;

                double diffAsMs = (int)((TimeConverter.ConvertTo<MetricTimeSpan>(currentTime, note.TempoMap).TotalMilliseconds - TimeConverter.ConvertTo<MetricTimeSpan>(bufferTime,note.TempoMap).TotalMilliseconds) - TimeConverter.ConvertTo<MetricTimeSpan>(note.ScheduledTimeInTicks, note.TempoMap).TotalMilliseconds);

                currentTime -= bufferTime;

                string aheadVsBehind;

                if (diffAsMs >= 0){
                    aheadVsBehind = "ahead";
                }
                else {
                    aheadVsBehind = "behind";
                }

                StartCoroutine(scoreIndicator.AddToLabel(diffAsMs.ToString()+"ms "+aheadVsBehind));


                break; //avoid double hits on close together notes

            }

            prevDiff = diff;
        }
        if (!hitNote)
        {
            {
                Debug.Log("Missed Note at Tick " + currentTime);
                StartCoroutine(scoreIndicator.ReplaceLabel("Missed!"));
            }
        }

        return (currentTime,closestNoteTime,hitNote);
    }

    public void OnDrumHit(int velocity)
    {
        if (changeColourOnHit != null) //If still running
        {
            StopCoroutine(changeColourOnHit);
            ResetColour();
        }

        changeColourOnHit = StartCoroutine(ShowDrumHitbyChangeColour());
        if (PlaybackManager.playing)
        {
            (var timeHit,var closestNote,var hitNote) = checkIfHitNote();
            RaiseHitDrum(note, velocity, timeHit, closestNote, hitNote); //send hit drum signal with int note number, velocity (not here yet), time hit
            
        }
    }

    public IEnumerator ShowDrumHitbyChangeColour()
    {
        Color oldColour = selfRenderer.material.color;
        selfRenderer.material.color = changeColour;

        yield return waitForFrames(waitFrames);

        ResetColour();
    }

    IEnumerator waitForFrames(int frames)
    {
        for (int frame = 0; frame < frames; frame++)
        {
            yield return null; //takes 1 frame to return. Looping this allow wait for i frames
        }

    }

    void ResetColour()
    {
        selfRenderer.material.color = drumColour;
    }
}