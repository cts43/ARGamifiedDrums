using System.Linq;
using TMPro;
using UnityEngine;

public class RecordingMenu : MonoBehaviour
{

    TextMeshProUGUI[] labels;
    PlaybackManager playbackManager;
    int selectedLabel = 0;

    private bool acceptingInput = true;

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
        if (acceptingInput)
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
                StartCoroutine(Button.Execute());
                acceptingInput = false;
            }
        }


    }

    private void OnEnable()
    {
        acceptingInput = true; //always accept inputs when just been set active  
    }

    private void OnButtonPress(string buttonID, string argument)
    {
        acceptingInput = true;
        if (buttonID == "LoadRecording")
        {
            if (playbackManager.TryLoadData(argument))
            {
                Debug.Log("LOADED");
                this.gameObject.SetActive(false);
            }
        }
        else if (buttonID == "SaveRecording")
        {
            if (playbackManager.TrySaveData())
            {
                Debug.Log("SAVED");
                this.gameObject.SetActive(false);
            }

        }
        else if (buttonID == "LoadMIDI")
        {
            bool success = playbackManager.loadNewMIDI(argument);
            if (success)
            {
                this.gameObject.SetActive(false);
            }
        }
        else
        {
            Debug.Log("Invalid button ID!");
        }
    }

}
