using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using Unity.SharpZipLib.Utils;
using UnityEngine;
using UnityEngine.Networking;

public class FileManager : MonoBehaviour
{
    public static FileManager Instance { get; private set; }
    private string ExtractedMIDIPath;
    private string ExtractedRecordingsPath;
    public bool Finished = false;

    private void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        extractFiles();
    }

    private async void extractFiles()
    {
        ExtractedMIDIPath = await GetFromZip("MIDI Files.zip", "Extracted MIDI");
        ExtractedRecordingsPath = await GetFromZip("Motion Recordings.zip", "Extracted Recordings");
        Finished = true;
    }

    private async Task<string> GetFromZip(string filename, string outFolder)
    {
        string copiedPath;

        var zipPath = Path.Combine(Application.streamingAssetsPath, filename);
        var zipRequest = UnityWebRequest.Get(zipPath);

        await zipRequest.SendWebRequest();

        if (zipRequest.result == UnityWebRequest.Result.Success)
        {
            copiedPath = Path.Combine(Application.persistentDataPath, filename);
            File.WriteAllBytes(copiedPath, zipRequest.downloadHandler.data);
        }
        else
        {
            return null;
        }

        string outPath = Path.Combine(Application.persistentDataPath, outFolder);
        ZipUtility.UncompressFromZip(copiedPath, null, outPath);
        var ExtractedPath = outPath;
        return ExtractedPath;
    }

    public string GetRecordingsPath()
    {
        return ExtractedRecordingsPath;
    }

    public string GetMIDIPath()
    {
        return ExtractedMIDIPath;
    }

    public async Awaitable<string> GetRecordingsPathAsync()
    {
        while (!Finished)
        {
            await Task.Delay(100);
        }

        return ExtractedRecordingsPath;
    }

    public async Awaitable<string> GetMIDIPathAsync()
    {
        while (!Finished)
        {
            await Task.Delay(100);
        }

        return ExtractedMIDIPath;
    }
}
