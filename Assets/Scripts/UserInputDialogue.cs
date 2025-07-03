using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class UserInputDialogue : MonoBehaviour
{

    TextMeshProUGUI[] labels;
    int selectedLabel = 0;

    private void Start()
    {
        labels = GetComponentsInChildren<TextMeshProUGUI>();
        HighlightLabel(selectedLabel);
        GetMIDIString();
    }

    private void HighlightLabel(int index)
    {
        labels[index].outlineWidth = 0.2f;
        labels[index].outlineColor = new Color32(0, 200, 0, 200);
    }

    private void Update()
    {

    }

    public string GetMIDIString()
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

        for (int i = 0 ; i < labels.Count(); i++) {

            Debug.Log(i + "" + validMidiFiles.Count());

            if (i < validMidiFiles.Count())
            {
                labels[i].text = validMidiFiles[i].Name;
            }
        }

        return null;
    }

    public string GetRecordingString()
    {
        return null;
    }
}
