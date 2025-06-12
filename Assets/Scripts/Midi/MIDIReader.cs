using UnityEngine;
using Melanchall.DryWetMidi.Core;
using System.IO;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.MusicTheory;
using System.Collections.Generic;

public class MIDIReader : MonoBehaviour
{

    public string MIDIFilePath;

    public Queue<(int, int, MetricTimeSpan)> LoadedFile { get; private set; }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        LoadedFile = LoadMIDIFile(MIDIFilePath);
    }

    Queue<(int, int, MetricTimeSpan)> LoadMIDIFile(string Path)
    {

        var readNotes = new Queue<(int, int, MetricTimeSpan)> { };
        //list of tuples. Note (No.), Velocity, Time 
        //Time is based on Tempo set within MIDI file

        if (File.Exists(Path))
        {
            MidiFile file = MidiFile.Read(Path);
            ICollection<Melanchall.DryWetMidi.Interaction.Note> notes = file.GetNotes();
            TempoMap tempoMap = file.GetTempoMap();
            //^^^explicit types here based on GetType() output. Unsure if necessary

            foreach (Melanchall.DryWetMidi.Interaction.Note note in notes)
            {
                int noteNumber = note.NoteNumber;
                int noteVelocity = note.Velocity;
                MetricTimeSpan noteTime = note.TimeAs<MetricTimeSpan>(tempoMap);

                var readNote = (noteNumber, noteVelocity, noteTime);

                readNotes.Enqueue(readNote);
            }

        }
        else
        {
            Debug.Log("No file found at path!");
        }

        return readNotes;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
