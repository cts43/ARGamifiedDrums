using System.IO;
using Unity.SharpZipLib.Utils;
using UnityEngine;
using UnityEngine.Networking;

public class FileManager : MonoBehaviour
{
    public static FileManager Instance { get; private set; }

    private bool MIDIExtracted = false;
    private string ExtractedPath;

    private void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        GetMIDIFromZip();
    }

    private async void GetMIDIFromZip()
    {
        string filename = "MIDI Files.zip";
        string copiedPath;

        var MIDIZipPath = Path.Combine(Application.streamingAssetsPath, filename);
        var MIDIZipRequest = UnityWebRequest.Get(MIDIZipPath);

        await MIDIZipRequest.SendWebRequest();

        if (MIDIZipRequest.result == UnityWebRequest.Result.Success)
        {
            //yay
            copiedPath = Path.Combine(Application.persistentDataPath, filename);
            File.WriteAllBytes(copiedPath, MIDIZipRequest.downloadHandler.data);
        }
        else
        {
            //noo
            return;
        }

        string outPath = Path.Combine(Application.persistentDataPath, "Extracted MIDI");
        ZipUtility.UncompressFromZip(copiedPath, null, outPath);
        ExtractedPath = outPath;
        MIDIExtracted = true;
    }

    public string GetMIDIPath()
    {
        if (MIDIExtracted)
        {
            return ExtractedPath;
        }
        else
        {
            Debug.Log("Zip not loaded!");
            return null;
        }
    }
}
