using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ControllerRecorder : MonoBehaviour
{

    private struct recordedTransform
{
    public Vector3 position;
    public Vector3 rotation;

    public recordedTransform(Vector3 Position, Vector3 Rotation)
    { //custom struct for motion data per frame. position is transform.position, rotation is transform.eulerAngles
        position = Position;
        rotation = Rotation;
    }
}

    private GameObject RightHandAnchor;

    public GameObject playbackObject; //prefab object to use when playing back
    private GameObject playbackInstance;

    private Queue<recordedTransform> recordedTransforms = new Queue<recordedTransform>();
    private Queue<recordedTransform> recordedTransformsCopy = new Queue<recordedTransform>();

    private bool recording = false;
    private bool playing = false;

    private bool justStartedRecording = false;
    private bool instantiated = false;

    public event Action StartedRecording;

    public void RaiseStartedRecording()
    {
        StartedRecording?.Invoke();
    }

    public event Action FinishedRecording;

    public void RaiseFinishedRecording()
    {
        FinishedRecording?.Invoke();
    }

    private void Start()
    {
        RightHandAnchor = GameObject.FindGameObjectWithTag("RightHandAnchor");
    }

    public bool hasStoredRecording()
    {
        return recordedTransforms.Count != 0;
    }

    public void Play()
    {
        if (!recording && hasStoredRecording())
        {
            Debug.Log("Playing stored recoring");
            recordedTransformsCopy = new Queue<recordedTransform>(recordedTransforms);
            playing = true;
        }
        else
        {
            playing = false;
            Debug.Log("Didn't play. hasStoredRecoring() = " + hasStoredRecording());
        }
    }

    public void Record()
    {
        if (!playing)
        {
            recording = !recording; //Toggle alignment when 'A' button pressed
            Debug.Log("Toggled Recording");
            justStartedRecording = true;
        }
    }

    public void StopRecording()
    {
        if (recording)
        {
            recording = false;
            RaiseFinishedRecording();
            Debug.Log("Recording motion finished");
        }
    }

    private void Update()
    {
        if (recording)
        {

            if (!instantiated)
            {
                playbackInstance = Instantiate(playbackObject); //create 'drumstick' if doesn't exist
                instantiated = true;
            }

            if (justStartedRecording)
            {
                RaiseStartedRecording();
                recordedTransforms = new Queue<recordedTransform>(); //when recording starts, clear the queue
                justStartedRecording = false;
            }

            //start recording input

            recordedTransform recordedMotion = new recordedTransform(RightHandAnchor.transform.position, RightHandAnchor.transform.eulerAngles);
            recordedTransforms.Enqueue(recordedMotion);

            playbackInstance.transform.position = new Vector3(recordedMotion.position.x, recordedMotion.position.y, recordedMotion.position.z);
            playbackInstance.transform.rotation = Quaternion.Euler(new Vector3(recordedMotion.rotation.x, recordedMotion.rotation.y, recordedMotion.rotation.z));
        }

        else if (playing)
        {
            if (recordedTransformsCopy.Count != 0)
            {
                var playbackMotion = recordedTransformsCopy.Dequeue();

                playbackInstance.transform.position = new Vector3(playbackMotion.position.x, playbackMotion.position.y, playbackMotion.position.z);
                playbackInstance.transform.rotation = Quaternion.Euler(new Vector3(playbackMotion.rotation.x, playbackMotion.rotation.y, playbackMotion.rotation.z));
            }
            else
            {
                playing = false;
                Debug.Log("Stopped playing");
            }
        }
    }
}