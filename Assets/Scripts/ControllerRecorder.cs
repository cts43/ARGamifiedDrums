using System;
using System.Collections.Generic;
using NUnit;
using Unity.Mathematics;
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

    private GameObject moveableScene;

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


    public bool recording { get; private set; } = false;
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
        moveableScene = GameObject.FindWithTag("Moveable Scene");
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
        playing = false;
        leftHandJoints = GameObject.FindGameObjectWithTag("LeftHandTracker").GetComponentsInChildren<Transform>(); //store pointers to transforms for every joint
        rightHandJoints = GameObject.FindGameObjectWithTag("RightHandTracker").GetComponentsInChildren<Transform>();

        recording = true;
        Debug.Log("Recording started");
        justStartedRecording = true;
    }

    public void Reset()
    {
        if (recording)
        {
            recording = false;
            RaiseFinishedRecording();
            Debug.Log("Recording motion finished");
            //Debug.Log("right: "+recordedRightHandTransforms.Count + " left: " + recordedLeftHandTransforms.Count); //checking whether hand transforms were recorded
        }
        Destroy(DrumStickL);
        Destroy(DrumStickR);
        Destroy(GhostHandL);
        Destroy(GhostHandR);
        instantiated = false;
    }

    private void Update()
    {
        if (!instantiated)
            {
                DrumStickL = Instantiate(playbackObject,moveableScene.transform); //create drum sticks if don't exist
                DrumStickR = Instantiate(playbackObject,moveableScene.transform);
                //drum sticks visible when recording is fine so here is OK but should move ghost hands to only when playing back - or *only* use the ghost hands and have the main OVR hands invisible
                GhostHandL = Instantiate(leftHandPrefab,moveableScene.transform);
                GhostHandR = Instantiate(rightHandPrefab,moveableScene.transform);

                instantiated = true;
            }
            
        if (recording)
        {

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
            // world space -> local position relative to moveableScene logic -> not mine!! <<- Rewrite or cite
            
            Vector3 localPosL = moveableScene.transform.InverseTransformPoint(LeftHandAnchor.transform.position);
            Vector3 localRotL = (Quaternion.Inverse(moveableScene.transform.rotation) * LeftHandAnchor.transform.rotation).eulerAngles;
            

            Vector3 localPosR = moveableScene.transform.InverseTransformPoint(RightHandAnchor.transform.position);
            Vector3 localRotR = (Quaternion.Inverse(moveableScene.transform.rotation) * RightHandAnchor.transform.rotation).eulerAngles;

            recordedTransform recordedMotionL = new recordedTransform(localPosL, localRotL);
            recordedTransform recordedMotionR = new recordedTransform(localPosR, localRotR);

            recordedControllerTransforms.Enqueue(new transformPair(recordedMotionL, recordedMotionR));

            //set drum stick prefab transforms
            DrumStickL.transform.localPosition = localPosL;
            DrumStickL.transform.localEulerAngles = localRotL;

            DrumStickR.transform.localPosition = localPosR;
            DrumStickR.transform.localEulerAngles = localRotR;
            ////////////////////////////////////////////

            //HAND RECORDING////////////////////////////
            // world space -> local position relative to moveableScene logic -> not mine!! <<- Rewrite or cite

            //build list of all transforms here
            List<recordedTransform> leftHandTransforms = new List<recordedTransform>();
            List<recordedTransform> rightHandTransforms = new List<recordedTransform>();
            foreach (var joint in leftHandJoints)
            {

                Vector3 relativePosition = moveableScene.transform.InverseTransformPoint(joint.position);
                Vector3 relativeRotation = (Quaternion.Inverse(moveableScene.transform.rotation) * joint.rotation).eulerAngles;

                recordedTransform transform = new recordedTransform(relativePosition, relativeRotation);
                leftHandTransforms.Add(transform);
            }
            foreach (var joint in rightHandJoints)
            {

                Vector3 relativePosition = moveableScene.transform.InverseTransformPoint(joint.position);
                Vector3 relativeRotation = (Quaternion.Inverse(moveableScene.transform.rotation) * joint.rotation).eulerAngles;

                recordedTransform transform = new recordedTransform(relativePosition, relativeRotation);
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
                    DrumStickL.transform.localPosition = new Vector3(playbackMotionL.position.x, playbackMotionL.position.y, playbackMotionL.position.z);
                    DrumStickL.transform.localRotation = Quaternion.Euler(new Vector3(playbackMotionL.rotation.x, playbackMotionL.rotation.y, playbackMotionL.rotation.z));

                    DrumStickR.transform.localPosition = new Vector3(playbackMotionR.position.x, playbackMotionR.position.y, playbackMotionR.position.z);
                    DrumStickR.transform.localRotation = Quaternion.Euler(new Vector3(playbackMotionR.rotation.x, playbackMotionR.rotation.y, playbackMotionR.rotation.z));
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

                        //to world position
                        Vector3 worldPositionL = moveableScene.transform.TransformPoint(recordedLeftHandTransformFrame[i].position);
                        Quaternion worldRotationL = moveableScene.transform.rotation * Quaternion.Euler(recordedLeftHandTransformFrame[i].rotation);
                        Vector3 worldPositionR = moveableScene.transform.TransformPoint(recordedRightHandTransformFrame[i].position);
                        Quaternion worldRotationR = moveableScene.transform.rotation * Quaternion.Euler(recordedRightHandTransformFrame[i].rotation);

                        ghostHandLTransforms[i].position = worldPositionL;
                        ghostHandLTransforms[i].rotation = worldRotationL;
                        ghostHandRTransforms[i].position = worldPositionR;
                        ghostHandRTransforms[i].rotation = worldRotationR;

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

    public (Queue<transformPair>, Queue<handMotionFrame>, Queue<handMotionFrame>) getRecording()
    {
        return (recordedControllerTransforms, recordedLeftHandTransforms, recordedRightHandTransforms);
    }

    public void loadRecording(Queue<transformPair> controllerMotion, Queue<handMotionFrame> leftHandMotion, Queue<handMotionFrame> rightHandMotion)
    {
        recordedControllerTransforms = controllerMotion;
        recordedLeftHandTransforms = leftHandMotion;
        recordedRightHandTransforms = rightHandMotion;
    }
}