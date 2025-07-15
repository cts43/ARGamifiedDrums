using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.InputSystem;
using TMPro;
using Oculus.Interaction.PoseDetection;
using Melanchall.DryWetMidi.Core;

public class DemonstrationPlayer : MonoBehaviour
{
    public List<string> recordingsToPlay;

    public int numberOfDemonstrations;
    public int numberOfPlaythroughs;

    private int demonstrationNo = 0;
    private bool SequenceFinished = false;

    public bool Enabled = false;

    private bool saveInputs = false;

    private PlaybackManager playbackManager;

    private TextMeshProUGUI DemonstrationLabel;
    controllerActions inputActions;

    private void Start()
    {
        inputActions = new controllerActions();
        inputActions.Enable();
        playbackManager = PlaybackManager.Instance;
        playbackManager.FinishedPlaying += OnDemonstrationFinished;
        inputActions.Controller.Next.performed += OnButtonPress;
        DemonstrationLabel = GameObject.FindWithTag("DemonstrationLabel").GetComponent<TextMeshProUGUI>();
    }

    private void PlayNextDemonstration()
    {

        int currentRecording = demonstrationNo / (numberOfDemonstrations + numberOfPlaythroughs);
        int nextRecording = demonstrationNo+1 / (numberOfDemonstrations + numberOfPlaythroughs);

        if (currentRecording >= recordingsToPlay.Count)
        {
            return;
        }
        else if (nextRecording >= recordingsToPlay.Count)
        {
            SequenceFinished = true;
        }

        Debug.Log("Playing recording index: " + currentRecording);
        string currentMIDI = recordingsToPlay[currentRecording];
        string currentJSON = currentMIDI + ".json";

        playbackManager.TryLoadData(currentJSON);
        playbackManager.loadNewMIDI(currentMIDI);

        if (demonstrationNo % (numberOfDemonstrations + numberOfPlaythroughs) < numberOfDemonstrations)
        {
            saveInputs = false;
            playbackManager.playRecorded(true, true);
        }
        else
        {
            saveInputs = true;
            playbackManager.playRecorded(true, false);
        }

        demonstrationNo++;
    }

    private void OnDemonstrationFinished()
    {
        if (saveInputs)
        {
            playbackManager.TrySavePlaythroughData();
        }

        if (SequenceFinished)
        {
            return;
        }

        DemonstrationLabel.enabled = true;
        DemonstrationLabel.text = $"Press Y to continue...";
    }

    private void OnButtonPress(InputAction.CallbackContext context)
    {
        if (!playbackManager.playing && Enabled)
        {

            if (SequenceFinished)
            {
                Reset();
            }

            DemonstrationLabel.enabled = false;
            PlayNextDemonstration();
        }
    }

    private void Reset()
    {
        demonstrationNo = 0;
        SequenceFinished = false;    
    }



}
