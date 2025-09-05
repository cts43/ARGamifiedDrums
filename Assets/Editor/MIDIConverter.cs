#if UNITY_EDITOR
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System.Text.RegularExpressions;
using UnityEngine;

public class MIDIConverter
{
    
    private const double hitWindowInMs = 300.0;

    private class NoteData
    {
        public int note;
        public int velocity;
        public long timeInTicks;
        public double timeInMs;

        public NoteData(int note, int velocity, long timeInTicks, double timeInMs){
            this.note = note;
            this.velocity = velocity;
            this.timeInTicks = timeInTicks;
            this.timeInMs = timeInMs;
        }
    }

    [MenuItem("Tools/Batch convert .mid to .json")]
    public static void ConvertMIDIPerformances()
    {
        var performancesFolder = EditorUtility.OpenFolderPanel("Open performances folder", "%USERPROFILE%/Documents", "");
        var referencesFolder = EditorUtility.OpenFolderPanel("Open references folder", "%USERPROFILE%/Documents", "");
        Directory.CreateDirectory(Path.Combine(performancesFolder, "out"));

        // ^(.*?)(?=\s*-\s*|$) regex to ignore ' - Final', ' - Pretest'  etc. anything before ' - '
        Regex regex = new Regex(@"^(.*?)(?=\s*-\s*|$)");

        foreach (var performance in Directory.GetFiles(performancesFolder))
        { //match by filename, ignoring suffixes per the above regex
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

    private static void ConvertMIDIPerformance(string performancePath, string referencePath, string savePath)
    {
        var performanceMidi = MidiFile.Read(performancePath);
        var referenceMidi = MidiFile.Read(referencePath);

        var perfTempoMap = performanceMidi.GetTempoMap();
        var refTempoMap = referenceMidi.GetTempoMap();

        var performanceNotes = GetNotes(performanceMidi, perfTempoMap);
        var referenceNotes = GetNotes(referenceMidi, refTempoMap);

        var outputData = new PlaybackManager.playthroughData(new List<PlaybackManager.playthroughFrame>());

        var hitNotes = new List<NoteData>();

        foreach (var note in performanceNotes)
        {
            NoteData matchedNote = null;

            foreach (var reference in referenceNotes){
                
                if (hitNotes.Contains(reference) || reference.note != note.note){
                    continue; //skip if already matched or wrong note
                }

                double diff = Math.Abs(reference.timeInMs - note.timeInMs);

                if (diff <= hitWindowInMs){
                    matchedNote = reference;
                    break;
                }
            }

            bool hitSuccessfully = (matchedNote != null);
            if (hitSuccessfully)
            {
                hitNotes.Add(matchedNote); //mark as hit
            }

            double? smallest = null;
            NoteData closest = null;
            //find closest time
            foreach (var reference in referenceNotes){
                double diff = Math.Abs(note.timeInMs - reference.timeInMs);
                if (smallest == null || diff < smallest){
                    smallest = diff;
                    closest = reference;
                }
            }

            //we still want every note in the .json, matching just determines hitSuccessfully
            var frame = new PlaybackManager.playthroughFrame(note.note,note.velocity,note.timeInTicks,closest.timeInTicks,hitSuccessfully);

            outputData.frames.Add(frame);
        }


        var json = JsonUtility.ToJson(outputData);
        File.WriteAllText(savePath, json);
        Debug.Log("Saved new json to " + savePath);
    }

    private static List<NoteData> GetNotes(MidiFile midiFile, TempoMap tempoMap, bool applyOffset = false)
    {

        long barInTicks = TimeConverter.ConvertFrom(new BarBeatTicksTimeSpan(1), tempoMap);
        double barInMs = TimeConverter.ConvertTo<MetricTimeSpan>(new BarBeatTicksTimeSpan(1), tempoMap).TotalMilliseconds;
        var notesList = new List<NoteData>();

        foreach (var note in midiFile.GetNotes()){
            
            long timeInTicks = note.Time;

            var timeAsMetric = TimeConverter.ConvertTo<MetricTimeSpan>(note.Time, tempoMap);
            double timeInMs = timeAsMetric.TotalMicroseconds / 1000.0;

            if (applyOffset) //offset to account for lack of spawn window
            {
                timeInTicks -= barInTicks;
                timeInMs -= barInMs;
            }

            var noteData = new NoteData(note.NoteNumber,note.Velocity,timeInTicks,timeInMs);

            notesList.Add(noteData);

        }

        return notesList;

    }

}
#endif