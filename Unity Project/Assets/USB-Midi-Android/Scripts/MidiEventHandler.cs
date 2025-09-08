using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

//From the USB-MIDI-Android package by Arthaïr, slightly modified for this project by Chris Seadon

namespace Midi
{
    public class MidiEventHandler : MonoBehaviour, IMidiEventHandler
    {

        [SerializeField] private Boolean hideRawMidi; //To hide constant messages to console as module sends more MIDI messages than just drum hits

        public static event Action<int, int> OnNoteOn;
        public static event Action<int> OnNoteOff;



        private void Awake()
        {
            gameObject.AddComponent<MidiManager>();
        }

        private void Start()
        {
            MidiManager.Instance.RegisterEventHandler(this);
        }

        public void RawMidi(sbyte a, sbyte b, sbyte c)
        {
            if (!hideRawMidi)
            {
                Debug.Log("RawMidi a " + a + " b " + b + " c " + c);

            }
        }

        public void NoteOn(int note, int velocity)
        {
            //Debug.Log("Note On " + note + " velocity " + velocity);

            OnNoteOn?.Invoke(note, velocity);

        }

        public void NoteOff(int note)
        {
            //Debug.Log("Note off " + note);

            OnNoteOff?.Invoke(note);
        }

        public void DeviceAttached(string deviceName)
        {
            Debug.Log("Device Attached " + deviceName);

        }

        public void DeviceDetached(string deviceName)
        {
            Debug.Log("Device Detached " + deviceName);
        }
    }
}