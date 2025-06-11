using UnityEngine;
using Melanchall.DryWetMidi.Core;
using UnityEditorInternal;
using System.IO;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.MusicTheory;
using UnityEditor.Experimental.GraphView;
using System.Collections.Generic;

public class MIDIReader : MonoBehaviour
{

    public string MIDIFilePath;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        LoadMIDIFile(MIDIFilePath);
    }

    void LoadMIDIFile(string Path)
    {
        if (File.Exists(Path))
        {
            MidiFile file = MidiFile.Read(Path);
            ICollection<Melanchall.DryWetMidi.Interaction.Note> notes = file.GetNotes();
            TempoMap tempoMap = file.GetTempoMap();
            //explicit types here based on GetType() output. Unsure if necessary
            foreach (Melanchall.DryWetMidi.Interaction.Note note in notes)
            {
                Debug.Log(note.NoteNumber);
            }
            
        }
        else
        {
            Debug.Log("No file found at path!");
            return;
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
