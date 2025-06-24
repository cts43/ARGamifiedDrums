using System;
using System.Collections;
using Melanchall.DryWetMidi.Interaction;
using UnityEngine;

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

    public event Action<int,int,long,long,bool> HitDrum;
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
            double diff = System.Math.Abs(currentTime - note.ScheduledTimeInTicks);

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
                Debug.Log("Successfully Hit Note at Tick " + currentTime+", closest note: "+closestNoteTime);
                note.destroy(); //destroy hit note
                break; //avoid double hits on close together notes

            }

            prevDiff = diff;
        }
        if (!hitNote)
        {
            {
                Debug.Log("Missed Note at Tick " + currentTime);
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