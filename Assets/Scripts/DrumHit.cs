using System.Collections;
using Melanchall.DryWetMidi.Interaction;
using UnityEngine;

public class DrumHit : MonoBehaviour
{

    Renderer selfRenderer;

    public Color drumColour;
    public Color changeColour;

    public int note;

    private int waitFrames = 10; //default 10

    private int hitWindowInMs;

    private NoteSpawner noteSpawner;

    private Coroutine changeColourOnHit;

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

    private void checkIfHitNote()
    {
        var hitWindowAsTimeSpan = new MetricTimeSpan(0, 0, 0, hitWindowInMs);
        bool hitNote = false;
        long currentTime = noteSpawner.GetCurrentOffsetMusicalTimeAsTicks();
        foreach (var note in GetComponentsInChildren<NoteIndicator>())
        {
            double diff = System.Math.Abs(currentTime - note.ScheduledTimeInTicks);
            if (diff <= (TimeConverter.ConvertFrom(hitWindowAsTimeSpan, note.TempoMap)) / 2)
            {
                hitNote = true;
                Debug.Log("Successfully Hit Note at Tick " + note.ScheduledTimeInTicks);
                note.destroy(); //destroy hit note
                break; //avoid double hits on close together notes

            }
        }
        if (!hitNote)
            {
                {
                    Debug.Log("Missed Note");
                }
            }
    }

    public void OnDrumHit()
    {
        if (changeColourOnHit != null) //If still running
        {
            StopCoroutine(changeColourOnHit);
            ResetColour();
        }

        changeColourOnHit = StartCoroutine(ShowDrumHitbyChangeColour());
        checkIfHitNote();
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