using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class UserInputDialogue : MonoBehaviour
{

    TextMeshProUGUI[] labels;
    int selectedLabel = 0;
    int topLabel = 0;
    public bool hasSelectedString { private set; get; } = false; 

    public string selectedString { private set; get; }

    private void HighlightLabel(int index)
    {
        labels[index].outlineWidth = 0.2f;
        labels[index].outlineColor = new Color32(0, 200, 0, 200);
    }

    private void Awake()
    {
        labels = GetComponentsInChildren<TextMeshProUGUI>();
        HighlightLabel(selectedLabel);
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
            selectedString = labels[selectedLabel + topLabel].text;
            hasSelectedString = true;
            Destroy(this.gameObject);
        }
    }

    public void showMIDIFiles()
    {

        string path = Path.Combine(Application.streamingAssetsPath, "MIDI Files");

        var info = new DirectoryInfo(path);

        List<FileInfo> validMidiFiles = new List<FileInfo>();

        foreach (var file in info.GetFiles())
        {
            Debug.Log(file.Extension);
            if (file.Extension == ".mid")
            {
                validMidiFiles.Add(file);
            }
        }

        Debug.Log(labels.Count());

        for (int i = 0; i < labels.Count(); i++)
        {

            Debug.Log(i + "" + validMidiFiles.Count());

            if (i < validMidiFiles.Count())
            {
                labels[i].text = validMidiFiles[i].Name;
            }
        }

    }

    public void showRecordingFiles()
    {
        string path = Application.persistentDataPath;

        var info = new DirectoryInfo(path);

        List<FileInfo> validFiles = new List<FileInfo>();

        foreach (var file in info.GetFiles())
        {
            Debug.Log(file.Extension);
            if (file.Extension == ".json")
            {
                validFiles.Add(file);
            }
        }

        Debug.Log(labels.Count());

        for (int i = 0; i < labels.Count(); i++)
        {

            if (i < validFiles.Count())
            {
                labels[i].text = validFiles[i].Name;
            }
        }
    }
}
