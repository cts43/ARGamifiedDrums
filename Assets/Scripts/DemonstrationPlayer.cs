using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;

public class DemonstrationPlayer : MonoBehaviour
{
    public List<string> recordingsToPlay;
    private Queue<string> recordingsToPlayCopy;

    public int numberOfDemonstrations;
    public int numberOfPlaythroughs;

    public bool Enabled = false;

    private PlaybackManager playbackManager;

    private void Start()
    {
        recordingsToPlayCopy = new Queue<string>(recordingsToPlay);
        playbackManager = PlaybackManager.Instance;
        playbackManager.FinishedPlaying += PromptToContinue;
        if (Enabled)
        {
            StartRoutine();
        }
    }

    private async void StartRoutine()
    {
        await FileManager.Instance.GetRecordingsPathAsync();
        await Task.Delay(2000); //for debug purpose only
        PlayNextDemonstation();
    }

    private void PlayNextDemonstation()
    {

        if (recordingsToPlayCopy.Count <= 0)
        {
            return;
        }

        var currentMIDI = recordingsToPlayCopy.Dequeue();
        var currentRecording = currentMIDI + ".json";

        playbackManager.TryLoadData(currentRecording);
        playbackManager.loadNewMIDI(currentMIDI);
        playbackManager.playRecorded(true, true);

    }

    private async void PromptToContinue()
    {
        if (Enabled)
        {
            Debug.Log("(Demonstration Player) Received finished signal");
            await Task.Delay(1000);
            PlayNextDemonstation();
        }
    }

}
