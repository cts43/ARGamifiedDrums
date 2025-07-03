using System.Linq;
using TMPro;
using UnityEngine;

public class RecordingMenu : MonoBehaviour
{

    TextMeshProUGUI[] labels;
    PlaybackManager playbackManager;
    int selectedLabel = 0;

    private void Start()
    {
        playbackManager = GameObject.FindWithTag("PlaybackManager").GetComponent<PlaybackManager>();
        labels = GetComponentsInChildren<TextMeshProUGUI>();
        HighlightLabel(selectedLabel);

        foreach (RecordingMenuButton button in GetComponentsInChildren<RecordingMenuButton>())
        {
            button.ButtonPress += OnButtonPress;
        }
    }

    private void HighlightLabel(int index)
    {
        labels[index].outlineWidth = 0.2f;
        labels[index].outlineColor = new Color32(0, 200, 0, 200);
    }

    private void Update()
    {
        if (OVRInput.GetDown(OVRInput.RawButton.LThumbstickUp))
        {
            if (selectedLabel > 0)
            {
                labels[selectedLabel].outlineWidth = 0;
                selectedLabel -= 1;
                HighlightLabel(selectedLabel);
            }
        }
        else if (OVRInput.GetDown(OVRInput.RawButton.LThumbstickDown))
        {
            if (selectedLabel < labels.Count() - 1)
            {
                labels[selectedLabel].outlineWidth = 0;
                selectedLabel += 1;
                HighlightLabel(selectedLabel);
            }
        }

        if (OVRInput.GetDown(OVRInput.RawButton.B))
        {
            RecordingMenuButton Button = labels[selectedLabel].GetComponentInChildren<RecordingMenuButton>();
            Button.Execute();
        }


    }

    private void OnButtonPress(string buttonID, string argument)
    {
        if (buttonID == "LoadRecording")
        {
            playbackManager.TryLoadData(argument);
        }
        else if (buttonID == "SaveRecording")
        {
            playbackManager.TrySaveData();
        }
        else if (buttonID == "LoadMIDI")
        {
            playbackManager.loadNewMIDI(argument);
        }
        else
        {
            Debug.Log("Invalid button ID!");
        }
    }

}
