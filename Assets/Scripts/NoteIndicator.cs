using UnityEngine;

public class NoteIndicator : MonoBehaviour
{
    public double ScheduledTime;

    public void destroy()
    {
        Destroy(this.gameObject); //destroy own object
    }
}
