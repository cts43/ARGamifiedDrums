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

    public string GetMIDIPath()
    {
        if (ExtractedMIDIPath != null)
        {
            return ExtractedMIDIPath;
        }
        else
        {
            Debug.Log("Zip not loaded!");
            return null;
        }
    }

    public string GetRecordingsPath()
    {
        if (ExtractedRecordingsPath != null)
        {
            return ExtractedRecordingsPath;
        }
        else
        {
            Debug.Log("Zip not loaded!");
            return null;
        }
    }
}
