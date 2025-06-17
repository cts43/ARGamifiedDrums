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

    private bool recording = false;
    private bool playing = false;

    private bool justStartedRecording = false;
    private bool instantiated = false;

    private void Start()
    {
        RightHandAnchor = GameObject.FindGameObjectWithTag("RightHandAnchor");
    }

    private void Update()
    {

        if (OVRInput.GetDown(OVRInput.RawButton.RThumbstick))
        {
            recording = !recording; //Toggle alignment when 'A' button pressed
            Debug.Log("Toggled Recording");
            justStartedRecording = true;
        }

        if (recording)
        {

            if (!instantiated)
            {
                playbackInstance = Instantiate(playbackObject); //create 'drumstick' if doesn't exist
                instantiated = true;
            }

            if (justStartedRecording)
            {
                recordedTransforms = new Queue<recordedTransform>(); //when recording starts, clear the queue
                justStartedRecording = false;
            }

            //start recording input

            recordedTransform recordedMotion = new recordedTransform(RightHandAnchor.transform.position,RightHandAnchor.transform.eulerAngles);
            recordedTransforms.Enqueue(recordedMotion);

            playbackInstance.transform.position = new Vector3(recordedMotion.position.x, recordedMotion.position.y, recordedMotion.position.z);
            playbackInstance.transform.rotation = Quaternion.Euler(new Vector3(recordedMotion.rotation.x, recordedMotion.rotation.y, recordedMotion.rotation.z));
        }

        else if (playing && recordedTransforms.Count() != 0)
        {

            Debug.Log("Playing Back");

            var playbackMotion = recordedTransforms.Dequeue();

            playbackInstance.transform.position = new Vector3(playbackMotion.position.x, playbackMotion.position.y, playbackMotion.position.z);
            playbackInstance.transform.rotation = Quaternion.Euler(new Vector3(playbackMotion.rotation.x, playbackMotion.rotation.y, playbackMotion.rotation.z));
        }
    }
}