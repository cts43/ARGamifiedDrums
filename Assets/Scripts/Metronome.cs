using Melanchall.DryWetMidi.Multimedia;
using UnityEngine;

public class Metronome : MonoBehaviour
{
    long previousBeat = 3;
    public AudioClip metronomeClip;
    private AudioSource metronomeSource;

    public float BarPitch = 2;
    public float BeatPitch = 1;

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
                if (time.Beats == 0)
                {
                    metronomeSource.pitch = BarPitch;
                    metronomeSource.volume = 1;
                }
                else
                {
                    metronomeSource.pitch = BeatPitch;
                    metronomeSource.volume = 0.5f;
                }
                metronomeSource.PlayOneShot(metronomeClip);
                previousBeat = time.Beats;
            }
        }    
    }
}
