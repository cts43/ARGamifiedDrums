#if UNITY_EDITOR
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Newtonsoft.Json;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class MIDIConverter
{
    //This script was adapted from ChatGPT to quickly convert the recorded MIDI files in the video demonstration portion of the study into the same format as the recordings from the AR conditions. 

    private const double hitWindowInMs = 300.0;

    [MenuItem("Tools/Batch convert .mid to .json")]
    public static void ConvertMIDIPerformances()
    {
        var performancesFolder = EditorUtility.OpenFolderPanel("Open performances folder", "%USERPROFILE%/Documents", "");
        var referencesFolder = EditorUtility.OpenFolderPanel("Open references folder", "%USERPROFILE%/Documents", "");
        Directory.CreateDirectory(Path.Combine(performancesFolder, "out"));

        // ^(.*?)(?=\s*-\s*|$) regex to ignore ' - Final', ' - Pretest'  etc. anything before ' - '
        Regex regex = new Regex(@"^(.*?)(?=\s*-\s*|$)");

        foreach (var performance in Directory.GetFiles(performancesFolder))
        {
            string performanceFileName = Path.GetFileNameWithoutExtension(performance);
            Match match = regex.Match(performanceFileName);
            if (match.Success)
            {
                string referenceFileName = match.Groups[0].Value;

                foreach (var reference in Directory.GetFiles(referencesFolder))
                {
                    Regex fileNameRegex = new Regex(referenceFileName);
                    match = fileNameRegex.Match(Path.GetFileNameWithoutExtension(reference));
                    if (match.Success)
                    {
                        ConvertMIDIPerformance(performance, reference, Path.Combine(performancesFolder, "out", performanceFileName + ".json"));

                        break;
                    }
                }

            }
        }
    }

    private static void ConvertMIDIPerformance(string performancePath, string referencePath, string outputJsonPath)
    {
        var performanceMidi = MidiFile.Read(performancePath);
        var referenceMidi = MidiFile.Read(referencePath);

        var perfTempoMap = performanceMidi.GetTempoMap();
        var refTempoMap = referenceMidi.GetTempoMap();

        var performanceNotes = GetNotes(performanceMidi, perfTempoMap, true);
        var referenceNotes = GetNotes(referenceMidi, refTempoMap);

        var outputFrames = new List<Dictionary<string, object>>();

        var matchedReferenceNotes = new HashSet<NoteData>();

        foreach (var perfNote in performanceNotes)
        {
            NoteData closest = null;
            double smallestTimeDiff = double.MaxValue;

            NoteData closestMatchingPitch = null;
            double smallestMatchingPitchTimeDiff = double.MaxValue;

            foreach (var refNote in referenceNotes)
            {
                if (matchedReferenceNotes.Contains(refNote)) continue;

                double timeDiff = Math.Abs(perfNote.TimeMs - refNote.TimeMs);

                // Track closest overall
                if (timeDiff < smallestTimeDiff)
                {
                    smallestTimeDiff = timeDiff;
                    closest = refNote;
                }

                // Track closest matching pitch
                if (refNote.NoteNumber == perfNote.NoteNumber && timeDiff < smallestMatchingPitchTimeDiff)
                {
                    smallestMatchingPitchTimeDiff = timeDiff;
                    closestMatchingPitch = refNote;
                }
            }

            bool hitSuccessfully = false;
            if (closestMatchingPitch != null && smallestMatchingPitchTimeDiff <= hitWindowInMs)
            {
                hitSuccessfully = true;
                matchedReferenceNotes.Add(closestMatchingPitch); // ✅ don't reuse it
            }

            var frame = new Dictionary<string, object>
            {
                { "note", perfNote.NoteNumber },
                { "velocity", perfNote.Velocity },
                { "hitTime", perfNote.TimeTicks },
                { "closestNoteTime", closest?.TimeTicks ?? 0 },
                { "hitSuccessfully", hitSuccessfully }
            };

            outputFrames.Add(frame);
        }

        var jsonObject = new Dictionary<string, object>
        {
            { "frames", outputFrames }
        };

        var json = JsonConvert.SerializeObject(jsonObject, Formatting.Indented);
        File.WriteAllText(outputJsonPath, json);
    }

    private static List<NoteData> GetNotes(MidiFile midiFile, TempoMap tempoMap, bool applyOffset = false)
    {

        long barInTicks = TimeConverter.ConvertFrom(new BarBeatTicksTimeSpan(1), tempoMap);
        double barInMs = TimeConverter.ConvertTo<MetricTimeSpan>(new BarBeatTicksTimeSpan(1), tempoMap).TotalMilliseconds;

        return midiFile.GetNotes().Select(note =>
        {

            long timeTicks = note.Time;

            var metricTime = TimeConverter.ConvertTo<MetricTimeSpan>(note.Time, tempoMap);
            double timeMs = metricTime.TotalMicroseconds / 1000.0;

            if (applyOffset)
            {
                timeTicks -= barInTicks;
                timeMs -= barInMs;
            }

            return new NoteData
            {
                NoteNumber = note.NoteNumber,
                Velocity = note.Velocity,
                TimeTicks = timeTicks,
                TimeMs = timeMs
            };
        }).ToList();
    }

    private class NoteData
    {
        public int NoteNumber { get; set; }
        public int Velocity { get; set; }
        public long TimeTicks { get; set; }
        public double TimeMs { get; set; }
    }

    [MenuItem("Tools/120 -> 60BPM in C drive folder")]
    public static void TempoChanger()
    {
        string inputFolder = @"C:\Midi\Input";
        string outputFolder = @"C:\Midi\Output";

        Directory.CreateDirectory(outputFolder);
        int targetTempo = 60000000 / 60; // 60 BPM → 1,000,000 µs per quarter note

        foreach (string file in Directory.GetFiles(inputFolder, "*.mid"))
        {
            var midi = MidiFile.Read(file);

            // Remove all tempo events
            foreach (var trackChunk in midi.GetTrackChunks())
            {
                var tempoEvents = trackChunk.Events.OfType<SetTempoEvent>().ToList();
                foreach (var tempoEvent in tempoEvents)
                    trackChunk.Events.Remove(tempoEvent);
            }

            // Insert new tempo event at time = 0
            var firstTrack = midi.GetTrackChunks().First();
            firstTrack.Events.Insert(0, new SetTempoEvent(targetTempo));

            // Save to output folder
            string outputPath = Path.Combine(outputFolder, Path.GetFileName(file));
            midi.Write(outputPath);
        }

        Console.WriteLine("All MIDI files processed.");
    }

}
#endif