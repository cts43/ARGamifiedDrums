using UnityEngine;
using Melanchall.DryWetMidi.Interaction;

public class NoteIndicator : MonoBehaviour
{
    public long ScheduledTimeInTicks;

    public TempoMap TempoMap;

    public void destroy()
    {
        Destroy(this.gameObject); //destroy own object
    }
}
