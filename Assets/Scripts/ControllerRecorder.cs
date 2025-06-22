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

    private GameObject LeftHandAnchor;
    private GameObject RightHandAnchor;

    public GameObject playbackObject; //prefab object to use when playing back
    private GameObject DrumStickL;
    private GameObject DrumStickR;

    private Queue<(recordedTransform, recordedTransform)> recordedTransforms = new Queue<(recordedTransform, recordedTransform)>();
    private Queue<(recordedTransform,recordedTransform)> recordedTransformsCopy = new Queue<(recordedTransform,recordedTransform)>();

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
        LeftHandAnchor = GameObject.FindGameObjectWithTag("LeftHandAnchor");
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
            recordedTransformsCopy = new Queue<(recordedTransform,recordedTransform)>(recordedTransforms);
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
                DrumStickL = Instantiate(playbackObject); //create drum sticks if don't exist
                DrumStickR = Instantiate(playbackObject);
                instantiated = true;
            }

            if (justStartedRecording)
            {
                RaiseStartedRecording();
                recordedTransforms = new Queue<(recordedTransform, recordedTransform)>(); //when recording starts, clear the queue
                justStartedRecording = false;
            }

            //start recording input
            recordedTransform recordedMotionL = new recordedTransform(LeftHandAnchor.transform.position, LeftHandAnchor.transform.eulerAngles);
            recordedTransform recordedMotionR = new recordedTransform(RightHandAnchor.transform.position, RightHandAnchor.transform.eulerAngles);
            recordedTransforms.Enqueue((recordedMotionL, recordedMotionR));

            DrumStickL.transform.position = new Vector3(recordedMotionL.position.x, recordedMotionL.position.y, recordedMotionL.position.z);
            DrumStickL.transform.rotation = Quaternion.Euler(new Vector3(recordedMotionL.rotation.x, recordedMotionL.rotation.y, recordedMotionL.rotation.z));
            
            DrumStickR.transform.position = new Vector3(recordedMotionR.position.x, recordedMotionR.position.y, recordedMotionR.position.z);
            DrumStickR.transform.rotation = Quaternion.Euler(new Vector3(recordedMotionR.rotation.x, recordedMotionR.rotation.y, recordedMotionR.rotation.z));
        }

        else if (playing)
        {
            if (recordedTransformsCopy.Count != 0)
            {
                (var playbackMotionL, var playbackMotionR) = recordedTransformsCopy.Dequeue();

                DrumStickL.transform.position = new Vector3(playbackMotionL.position.x, playbackMotionL.position.y, playbackMotionL.position.z);
                DrumStickL.transform.rotation = Quaternion.Euler(new Vector3(playbackMotionL.rotation.x, playbackMotionL.rotation.y, playbackMotionL.rotation.z));

                DrumStickR.transform.position = new Vector3(playbackMotionR.position.x, playbackMotionR.position.y, playbackMotionR.position.z);
                DrumStickR.transform.rotation = Quaternion.Euler(new Vector3(playbackMotionR.rotation.x, playbackMotionR.rotation.y, playbackMotionR.rotation.z));
            }
            else
            {
                playing = false;
                Debug.Log("Stopped playing");
            }
        }
    }
}