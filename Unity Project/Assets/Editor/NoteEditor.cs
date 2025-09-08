#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class NoteEditor
{
    [MenuItem("Tools/Note Editor/Extract Notes")]
    public static void ExtractAllNotes()
    {
        string jsonPath = EditorUtility.OpenFilePanel("Select JSON File", "%USERPROFILE%/Documents", "");

        string loadedJson = File.ReadAllText(jsonPath);

        PlaybackManager.recordingData recordedData = JsonUtility.FromJson<PlaybackManager.recordingData>(loadedJson);

        PlaybackManager.playthroughData recordedNotes = recordedData.inputs;

        var newJson = JsonUtility.ToJson(recordedNotes);

        string saveFolder = EditorUtility.OpenFolderPanel("Select folder", "%USERPROFILE%/Documents", "");
        string savePath = Path.Combine(saveFolder, $"{Path.GetFileNameWithoutExtension(jsonPath)}-extracted.json");
        int i = 0;
        while (File.Exists(savePath))
        {
            savePath = Path.Combine(saveFolder, $"{Path.GetFileNameWithoutExtension(jsonPath)}-extracted+{i}.json");
            i++;
        }
        File.WriteAllText(savePath, newJson);
        
        Debug.Log("Saved new json to " + savePath);

        
    }

    [MenuItem("Tools/Note Editor/Replace Notes")]
    public static void ReplaceNotes()
    {
        string oldJsonPath = EditorUtility.OpenFilePanel("Select JSON to update", "%USERPROFILE%/Documents", "");
        string newJsonPath = EditorUtility.OpenFilePanel("Select new notes JSON", "%USERPROFILE%/Documents", "");

        string loadedNotesJson = File.ReadAllText(newJsonPath);
        string loadedOldJson = File.ReadAllText(oldJsonPath);

        PlaybackManager.recordingData existingFileData = JsonUtility.FromJson<PlaybackManager.recordingData>(loadedOldJson);

        PlaybackManager.motionData oldMotionData = existingFileData.motion;
        PlaybackManager.playthroughData newNotesData = JsonUtility.FromJson<PlaybackManager.playthroughData>(loadedNotesJson);

        PlaybackManager.recordingData replacedNotesData = new PlaybackManager.recordingData(oldMotionData, newNotesData);

        string jsonToSave = JsonUtility.ToJson(replacedNotesData);

        string saveFolder = EditorUtility.OpenFolderPanel("Select folder", "%USERPROFILE%/Documents", "");
        string savePath = Path.Combine(saveFolder, $"{Path.GetFileNameWithoutExtension(oldJsonPath)}-replaced.json");
        int i = 0;
        while (File.Exists(savePath))
        {
            savePath = Path.Combine(saveFolder, $"{Path.GetFileNameWithoutExtension(oldJsonPath)}-replaced+{i}.json");
            i++;
        }
        File.WriteAllText(savePath, jsonToSave);

        Debug.Log("Saved new json to " + savePath);


    }
}
#endif