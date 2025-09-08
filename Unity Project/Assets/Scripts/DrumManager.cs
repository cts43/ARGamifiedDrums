using UnityEngine;

public class DrumManager : MonoBehaviour
{

    public int framesToHighlightOnHit; //amount of time in frames to change colour when a drum is hit.
    public int hitWindowInMs;

    public void clearNotes()
    {
        foreach (DrumHit drum in GetComponentsInChildren<DrumHit>()) {
            foreach (NoteIndicator note in GetComponentsInChildren<NoteIndicator>())
            {
                Destroy(note.transform.gameObject);
            }
        }
    }
}
