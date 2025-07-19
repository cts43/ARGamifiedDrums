using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using TMPro;
using System;

public class DemonstrationPlayer : MonoBehaviour
{
    public List<string> recordingsToPlay;

    public int numberOfDemonstrations;
    public int numberOfPlaythroughs;
    public int numberOfEvalutations;

    private int demonstrationNo = 0;
    private bool SequenceFinished = false;

    public bool Enabled = false;

    private bool saveInputs = false;

    private PlaybackManager playbackManager;
    private StatusIndicator statusIndicator;
    private TextMeshProUGUI DemonstrationLabel;
    controllerActions inputActions;

    private enum PlaybackMode
    {
        ActionObservation = 0,
        FallingNotes = 1,
        Combined = 2
    }
    PlaybackMode playbackMode;
    bool showMotion;
    bool showFallingNotes;

    PlaybackMode[] playbackModes;

    private void Start()
    {
        inputActions = new controllerActions();
        inputActions.Enable();
        playbackManager = PlaybackManager.Instance;
        playbackManager.FinishedPlaying += OnDemonstrationFinished;
        inputActions.Controller.Next.performed += OnNextPressed;
        inputActions.Controller.LeftTrigger.performed += OnTriggerPressed;
        DemonstrationLabel = GameObject.FindWithTag("DemonstrationLabel").GetComponent<TextMeshProUGUI>();
        statusIndicator = GameObject.FindWithTag("StatusIndicator").GetComponent<StatusIndicator>();
        SetPlaybackMode(PlaybackMode.ActionObservation);
        playbackModes = (PlaybackMode[])Enum.GetValues(typeof(PlaybackMode));
    }

    private void PlayNextDemonstration()
    {
        int totalStages = numberOfDemonstrations + numberOfPlaythroughs + numberOfEvalutations;
        int currentRecording = demonstrationNo / totalStages;
        int nextRecording = (demonstrationNo + 1) / totalStages;

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

        if (demonstrationNo % totalStages < numberOfDemonstrations)
        {
            saveInputs = false;
            playbackManager.playRecorded(motion: showMotion, drumHits: true, showNotes: showFallingNotes, recordInput: false);
        }
        else if (demonstrationNo % totalStages < numberOfDemonstrations + numberOfPlaythroughs)
        {
            saveInputs = false;
            playbackManager.playRecorded(motion: showMotion, drumHits: false, showNotes: showFallingNotes, recordInput: false);
        }
        else
        {
            saveInputs = true;
            playbackManager.playRecorded(motion: false, drumHits: false, showNotes: false, recordInput: true);
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

    private void OnNextPressed(InputAction.CallbackContext context)
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

    private void OnTriggerPressed(InputAction.CallbackContext context)
    {
        if (!playbackManager.playing && Enabled)
        {
            int currentMode = (int)playbackMode;
            if (currentMode < playbackModes.Length - 1)
            {
                currentMode++;
            }
            else
            {
                currentMode = (int)playbackModes[0];
            }
            SetPlaybackMode((PlaybackMode)currentMode);
            statusIndicator.ShowStatus($"Mode set to {playbackMode}");
        }
    }

    private void Reset()
    {
        demonstrationNo = 0;
        SequenceFinished = false;
    }

    private void SetPlaybackMode(PlaybackMode mode)
    {
        playbackMode = mode;
        switch (playbackMode)
        {
            case PlaybackMode.ActionObservation:
                showMotion = true;
                showFallingNotes = false;
                break;
            case PlaybackMode.FallingNotes:
                showMotion = false;
                showFallingNotes = true;
                break;
            case PlaybackMode.Combined:
                showMotion = true;
                showFallingNotes = true;
                break;
        }
    }


}
