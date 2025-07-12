using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class MotionQuantiser
{
    [MenuItem("Tools/Quantise Motion JSON")]
    public static void QuantiseMotionData()
    {

        string jsonPath = EditorUtility.OpenFilePanel("Select JSON File", "%USERPROFILE%/Documents", "");

        string loadedJson = File.ReadAllText(jsonPath);

        PlaybackManager.recordingData recordedData = JsonUtility.FromJson<PlaybackManager.recordingData>(loadedJson);

        var recordedNotes = recordedData.inputs.frames;
        var quantisedNotes = new List<PlaybackManager.playthroughFrame>();

        foreach (var playedNote in recordedData.inputs.frames)
        {
            var (note, velocity, hitTime, closestNoteTime, hitSuccessfully) = playedNote;
            //hit time is replaced by closest note time:
            var newFrame = new PlaybackManager.playthroughFrame(note, velocity, closestNoteTime, closestNoteTime, hitSuccessfully);

            quantisedNotes.Add(newFrame);
        }
        var quantisedNotesData = new PlaybackManager.playthroughData(quantisedNotes);
        var existingMotionData = recordedData.motion;
        var quantisedDataToWrite = new PlaybackManager.recordingData(existingMotionData, quantisedNotesData);
        string newJson = JsonUtility.ToJson(quantisedDataToWrite);
        string saveFolder = EditorUtility.OpenFolderPanel("Select JSON File", "%USERPROFILE%/Documents", "");
        string savePath = Path.Combine(saveFolder, Path.GetFileNameWithoutExtension(jsonPath) + ".json");

        int i = 0;
        while (File.Exists(savePath))
        {
            string filename = Path.GetFileNameWithoutExtension(jsonPath) + " " + i + ".json";
            savePath = Path.Combine(saveFolder, filename);
            i++;
        }

        File.WriteAllText(savePath, newJson);
        Debug.Log($"Quantised motion data saved to {savePath}!");


    }
}
