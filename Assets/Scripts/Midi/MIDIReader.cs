using UnityEngine;
using Melanchall.DryWetMidi.Core;
using System.IO;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.MusicTheory;
using System.Collections.Generic;

public class MIDIReader : MonoBehaviour
{

    public Queue<(int, int, BarBeatTicksTimeSpan)> LoadedFile { get; private set; }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    public (Queue<(int, int, BarBeatTicksTimeSpan)>,TempoMap) LoadMIDIFile(string Path)
    {

        var readNotes = new Queue<(int, int, BarBeatTicksTimeSpan)> { };
        TempoMap tempoMap = null;
        //queue of tuples. Note (No.), Velocity, Time 
        //Time is based on Tempo set within MIDI file

        if (File.Exists(Path))
        {
            MidiFile file = MidiFile.Read(Path);
            ICollection<Melanchall.DryWetMidi.Interaction.Note> notes = file.GetNotes();
            tempoMap = file.GetTempoMap();
            //^^^explicit types here based on GetType() output. Unsure if necessary

            foreach (Melanchall.DryWetMidi.Interaction.Note note in notes)
            {
                int noteNumber = note.NoteNumber;
                int noteVelocity = note.Velocity;
                BarBeatTicksTimeSpan noteTime = note.TimeAs<BarBeatTicksTimeSpan>(tempoMap);

                var readNote = (noteNumber, noteVelocity, noteTime);

                readNotes.Enqueue(readNote);
            }

        }
        else
        {
            Debug.Log("No file found at path!");
        }

        return (readNotes,tempoMap);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
