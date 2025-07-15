using Melanchall.DryWetMidi.Multimedia;
using UnityEngine;

public class Metronome : MonoBehaviour
{
    long previousBeat = 3;
    public AudioClip metronomeClip;
    private AudioSource metronomeSource;

    private PlaybackManager playbackManager = null;

    private void Start()
    {
        playbackManager = PlaybackManager.Instance;
        metronomeSource = gameObject.AddComponent<AudioSource>();
    }

    private void Update()
    {
        if (playbackManager.playing)
        {
            var time = playbackManager.activeNoteSpawner.GetVisualTime();
            if (time.Beats != previousBeat && !time.negative)
            {
                metronomeSource.PlayOneShot(metronomeClip);
                previousBeat = time.Beats;
            }
        }    
    }
}
