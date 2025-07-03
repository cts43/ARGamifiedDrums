using System;
using System.Collections.Generic;
using NUnit;
using UnityEngine;

public class ControllerRecorder : MonoBehaviour
{

    [Serializable]
    public struct recordedTransform
    {
        public Vector3 position;
        public Vector3 rotation;

        public recordedTransform(Vector3 Position, Vector3 Rotation)
        { //custom struct for motion data per frame. position is transform.position, rotation is transform.eulerAngles
            position = Position;
            rotation = Rotation;
        }
    }
    [Serializable]
    public class transformPair
    {
        public recordedTransform leftControllerMotion;
        public recordedTransform rightControllerMotion;

        public transformPair(recordedTransform leftControllerMotion, recordedTransform rightControllerMotion)
        {
            this.leftControllerMotion = leftControllerMotion;
            this.rightControllerMotion = rightControllerMotion;
        }

        public void Deconstruct(out recordedTransform leftControllerMotion, out recordedTransform rightControllerMotion)
        {
            leftControllerMotion = this.leftControllerMotion;
            rightControllerMotion = this.rightControllerMotion;
        }
    }

    private GameObject LeftHandAnchor; //Controller anchors. Named this way for consistency with hierarchy but should probably rename to avoid confusion between hand+controller tracking
    private GameObject RightHandAnchor;

    
    private Transform[] leftHandJoints;
    private Transform[] rightHandJoints;

    public GameObject leftHandPrefab;
    public GameObject rightHandPrefab;

    private GameObject GhostHandL;
    private GameObject GhostHandR;

    public GameObject playbackObject; //prefab object to use when playing back
    private GameObject DrumStickL;
    private GameObject DrumStickR;

    private Queue<transformPair> recordedControllerTransforms = new Queue<transformPair>();
    private Queue<transformPair> recordedControllerTransformsCopy = new Queue<transformPair>();

    private Queue<handMotionFrame> recordedLeftHandTransforms = new Queue<handMotionFrame>(); //since each hand has multiple transforms, must be a list of pairs for each frame
    private Queue<handMotionFrame> recordedRightHandTransforms = new Queue<handMotionFrame>(); //since each hand has multiple transforms, must be a list of pairs for each frame
    private Queue<handMotionFrame> recordedLeftHandTransformsCopy = new Queue<handMotionFrame>();
    private Queue<handMotionFrame> recordedRightHandTransformsCopy = new Queue<handMotionFrame>();

    [Serializable]
    public class handMotionFrame
    {
        public List<recordedTransform> frames;

        public handMotionFrame(List<recordedTransform> frames)
        {
            this.frames = frames;
        }
    }


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
        return recordedControllerTransforms.Count != 0;
    }

    public void Play()
    {
        if (!recording && hasStoredRecording())
        {
            Debug.Log("Playing stored recoring");
            recordedControllerTransformsCopy = new Queue<transformPair>(recordedControllerTransforms);
            recordedLeftHandTransformsCopy = new Queue<handMotionFrame>(recordedLeftHandTransforms);
            recordedRightHandTransformsCopy = new Queue<handMotionFrame>(recordedRightHandTransforms);
            for (int i = 0; i < 5; i++) //testing simple offset to account for hand vs controller tracking differences
            {
                recordedLeftHandTransforms.Dequeue();
                recordedRightHandTransforms.Dequeue();
            }
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

            leftHandJoints = GameObject.FindGameObjectWithTag("LeftHandTracker").GetComponentsInChildren<Transform>(); //store pointers to transforms for every joint
            rightHandJoints = GameObject.FindGameObjectWithTag("RightHandTracker").GetComponentsInChildren<Transform>();

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
            //Debug.Log("right: "+recordedRightHandTransforms.Count + " left: " + recordedLeftHandTransforms.Count); //checking whether hand transforms were recorded
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
                //drum sticks visible when recording is fine so here is OK but should move ghost hands to only when playing back - or *only* use the ghost hands and have the main OVR hands invisible
                GhostHandL = Instantiate(leftHandPrefab);
                GhostHandR = Instantiate(rightHandPrefab);

                instantiated = true;
            }


            if (justStartedRecording)
            {
                RaiseStartedRecording();
                recordedControllerTransforms = new Queue<transformPair>(); //when recording starts, clear the queue
                recordedLeftHandTransforms = new Queue<handMotionFrame>();
                recordedRightHandTransforms = new Queue<handMotionFrame>();
                justStartedRecording = false;
            }

            //CONTROLLER RECORDING//////////////////////
            //start recording input
            recordedTransform recordedMotionL = new recordedTransform(LeftHandAnchor.transform.position, LeftHandAnchor.transform.eulerAngles);
            recordedTransform recordedMotionR = new recordedTransform(RightHandAnchor.transform.position, RightHandAnchor.transform.eulerAngles);
            recordedControllerTransforms.Enqueue(new transformPair(recordedMotionL, recordedMotionR));

            //set drum stick prefab transforms
            DrumStickL.transform.position = new Vector3(recordedMotionL.position.x, recordedMotionL.position.y, recordedMotionL.position.z);
            DrumStickL.transform.rotation = Quaternion.Euler(new Vector3(recordedMotionL.rotation.x, recordedMotionL.rotation.y, recordedMotionL.rotation.z));

            DrumStickR.transform.position = new Vector3(recordedMotionR.position.x, recordedMotionR.position.y, recordedMotionR.position.z);
            DrumStickR.transform.rotation = Quaternion.Euler(new Vector3(recordedMotionR.rotation.x, recordedMotionR.rotation.y, recordedMotionR.rotation.z));
            ////////////////////////////////////////////

            //HAND RECORDING////////////////////////////
            //

            //build list of all transforms here
            List<recordedTransform> leftHandTransforms = new List<recordedTransform>();
            List<recordedTransform> rightHandTransforms = new List<recordedTransform>();
            foreach (var joint in leftHandJoints)
            {
                recordedTransform transform = new recordedTransform(joint.position, joint.eulerAngles);
                leftHandTransforms.Add(transform);
            }
            foreach (var joint in rightHandJoints)
            {
                recordedTransform transform = new recordedTransform(joint.position, joint.eulerAngles);
                rightHandTransforms.Add(transform);
            }

            //then enqueue into recordedHandTransforms
            recordedLeftHandTransforms.Enqueue(new handMotionFrame(leftHandTransforms));
            recordedRightHandTransforms.Enqueue(new handMotionFrame(rightHandTransforms));
            
        }

        else if (playing)
        {
            if (recordedControllerTransformsCopy.Count > 0 || recordedLeftHandTransforms.Count > 0)
            {
                //CONTROLLER/DRUMSTICK PLAYBACK

                if (recordedControllerTransformsCopy.Count > 0)
                {

                    (var playbackMotionL, var playbackMotionR) = recordedControllerTransformsCopy.Dequeue();

                    //set transforms from queued recording
                    DrumStickL.transform.position = new Vector3(playbackMotionL.position.x, playbackMotionL.position.y, playbackMotionL.position.z);
                    DrumStickL.transform.rotation = Quaternion.Euler(new Vector3(playbackMotionL.rotation.x, playbackMotionL.rotation.y, playbackMotionL.rotation.z));

                    DrumStickR.transform.position = new Vector3(playbackMotionR.position.x, playbackMotionR.position.y, playbackMotionR.position.z);
                    DrumStickR.transform.rotation = Quaternion.Euler(new Vector3(playbackMotionR.rotation.x, playbackMotionR.rotation.y, playbackMotionR.rotation.z));
                }

                if (recordedLeftHandTransformsCopy.Count > 0)
                {
                    //HAND MOTION PLAYBACK
                    //don't really need to reassign this every frame but fine for testing
                    Transform[] ghostHandLTransforms = GhostHandL.GetComponentsInChildren<Transform>();
                    Transform[] ghostHandRTransforms = GhostHandR.GetComponentsInChildren<Transform>();

                    List<recordedTransform> recordedLeftHandTransformFrame = recordedLeftHandTransformsCopy.Dequeue().frames;
                    List<recordedTransform> recordedRightHandTransformFrame = recordedRightHandTransformsCopy.Dequeue().frames;

                    for (int i = 0; i < recordedLeftHandTransformFrame.Count; i++)
                    {
                        ghostHandLTransforms[i].position = recordedLeftHandTransformFrame[i].position;
                        ghostHandLTransforms[i].rotation = Quaternion.Euler(recordedLeftHandTransformFrame[i].rotation);

                        ghostHandRTransforms[i].position = recordedRightHandTransformFrame[i].position;
                        ghostHandRTransforms[i].rotation = Quaternion.Euler(recordedRightHandTransformFrame[i].rotation);

                    }

                }


            }
                else
                {
                    playing = false;
                    Debug.Log("Stopped playing");
                }
        }
    }

    public (Queue<transformPair>,Queue<handMotionFrame>,Queue<handMotionFrame>) getRecording()
    {
        return (recordedControllerTransforms, recordedLeftHandTransforms, recordedRightHandTransforms);
    }
}