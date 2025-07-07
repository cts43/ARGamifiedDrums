using System;
using System.Collections.Generic;
using System.Linq;
using NUnit;
using Unity.Mathematics;
using UnityEngine;

public class ControllerRecorder : MonoBehaviour
{

    public int handDrumstickOffsetFrames = 10;

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
    private GameObject leftHand;
    private GameObject rightHand;

    private recordedTransform OGLeftHandTransform;
    private recordedTransform OGRightHandTransform;

    private GameObject GhostHandL;
    private GameObject GhostHandR;

    private Transform moveableSceneTransform;
    private Quaternion moveableSceneRotation;

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
        public recordedTransform rootTransform;

        public handMotionFrame(List<recordedTransform> frames, recordedTransform rootTransform)
        {
            this.frames = frames;
            this.rootTransform = rootTransform;
        }

        internal void Deconstruct(out List<recordedTransform> frames, out recordedTransform rootTransform)
        {
            frames = this.frames;
            rootTransform = this.rootTransform;
        }
    }


    public bool recording { get; private set; } = false;
    private bool playing = false;

    private bool justStartedRecording = false;

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
        moveableSceneTransform = GameObject.FindWithTag("Moveable Scene").transform;
        moveableSceneRotation = moveableSceneTransform.rotation;
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
        leftHand = GameObject.FindWithTag("LeftHandTracker");
        rightHand = GameObject.FindWithTag("RightHandTracker");

        //moveableScene.transform.InverseTransformPoint(leftHandRoot.position)
        //(Quaternion.Inverse(moveableScene.transform.rotation) * leftHandRoot.rotation).eulerAngles

        OGLeftHandTransform = new recordedTransform(moveableSceneTransform.InverseTransformPoint(leftHand.transform.position), (Quaternion.Inverse(moveableSceneRotation) * leftHand.transform.rotation).eulerAngles);
        OGRightHandTransform = new recordedTransform(moveableSceneTransform.InverseTransformPoint(rightHand.transform.position), (Quaternion.Inverse(moveableSceneRotation) * rightHand.transform.rotation).eulerAngles);

        leftHandJoints = leftHand.GetComponentsInChildren<Transform>(); //store pointers to transforms for every joint
        rightHandJoints = rightHand.GetComponentsInChildren<Transform>();

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

            for (int i = 0; i < handDrumstickOffsetFrames; i++) //testing simple offset to account for hand vs controller tracking differences. only do this after a recording
            {
                recordedLeftHandTransforms.Dequeue();
                recordedRightHandTransforms.Dequeue();
            }
        }
        moveableSceneTransform = GameObject.FindGameObjectWithTag("Moveable Scene").transform;
        moveableSceneRotation = moveableSceneTransform.rotation;
        Destroy(DrumStickL);
        Destroy(DrumStickR);
        Destroy(GhostHandL);
        Destroy(GhostHandR);
    }

    private void Update()
    {
        if (playing || recording)
        {
            if (DrumStickL == null || DrumStickR == null)
            {
                DrumStickL = Instantiate(playbackObject, moveableSceneTransform); //create drum sticks if don't exist
                DrumStickR = Instantiate(playbackObject, moveableSceneTransform);

            }   
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
            
            Vector3 localPosL = moveableSceneTransform.InverseTransformPoint(LeftHandAnchor.transform.position);
            Vector3 localRotL = (Quaternion.Inverse(moveableSceneRotation) * LeftHandAnchor.transform.rotation).eulerAngles;
            

            Vector3 localPosR = moveableSceneTransform.InverseTransformPoint(RightHandAnchor.transform.position);
            Vector3 localRotR = (Quaternion.Inverse(moveableSceneRotation) * RightHandAnchor.transform.rotation).eulerAngles;

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

                // Vector3 relativePosition = moveableSceneTransform.InverseTransformPoint(joint.position);
                // Vector3 relativeRotation = (Quaternion.Inverse(moveableSceneRotation) * joint.rotation).eulerAngles;

                recordedTransform transform = new recordedTransform(joint.localPosition, joint.localEulerAngles);
                leftHandTransforms.Add(transform);
            }
            foreach (var joint in rightHandJoints)
            {

                // Vector3 relativePosition = moveableSceneTransform.InverseTransformPoint(joint.position);
                // Vector3 relativeRotation = (Quaternion.Inverse(moveableSceneRotation) * joint.rotation).eulerAngles;

                recordedTransform transform = new recordedTransform(joint.localPosition, joint.localEulerAngles);
                rightHandTransforms.Add(transform);
            }
            
            //for the root of each hand use same method as drum sticks
            var LeftRootTransform = new recordedTransform(moveableSceneTransform.InverseTransformPoint(leftHand.transform.position), (Quaternion.Inverse(moveableSceneRotation) * leftHand.transform.rotation).eulerAngles);
            var RightRootTransform = new recordedTransform(moveableSceneTransform.InverseTransformPoint(rightHand.transform.position), (Quaternion.Inverse(moveableSceneRotation) * rightHand.transform.rotation).eulerAngles);
        

            //then enqueue into recordedHandTransforms
            recordedLeftHandTransforms.Enqueue(new handMotionFrame(leftHandTransforms,LeftRootTransform));
            recordedRightHandTransforms.Enqueue(new handMotionFrame(rightHandTransforms,RightRootTransform));

        }

        else if (playing)
        {

            if (GhostHandL == null || GhostHandR == null)
            {
                GhostHandL = Instantiate(leftHandPrefab, moveableSceneTransform);
                GhostHandR = Instantiate(rightHandPrefab, moveableSceneTransform);

            }

            if (recordedControllerTransformsCopy.Count > 0 || recordedLeftHandTransforms.Count > 0)
            {
                //CONTROLLER/DRUMSTICK PLAYBACK

                if (recordedControllerTransformsCopy.Count > 0)
                {

                    (var playbackMotionL, var playbackMotionR) = recordedControllerTransformsCopy.Dequeue();

                    //set transforms from queued recording
                    DrumStickL.transform.localPosition = playbackMotionL.position;
                    DrumStickL.transform.localRotation = Quaternion.Euler(playbackMotionL.rotation);

                    DrumStickR.transform.localPosition = playbackMotionR.position;
                    DrumStickR.transform.localRotation = Quaternion.Euler(playbackMotionR.rotation);
                }

                if (recordedLeftHandTransformsCopy.Count > 0 && recordedRightHandTransformsCopy.Count > 0)
                {
                    //HAND MOTION PLAYBACK
                    //don't really need to reassign this every frame but fine for testing


                    Transform[] ghostHandLTransforms = GhostHandL.GetComponentsInChildren<Transform>();
                    Transform[] ghostHandRTransforms = GhostHandR.GetComponentsInChildren<Transform>();

                    var (leftFrames, leftRootPos) = recordedLeftHandTransformsCopy.Dequeue();
                    var (rightFrames, rightRootPos) = recordedRightHandTransformsCopy.Dequeue();

                    GhostHandL.transform.localPosition = leftRootPos.position;
                    GhostHandL.transform.localRotation = Quaternion.Euler(leftRootPos.rotation);
                    GhostHandR.transform.localPosition = rightRootPos.position;
                    GhostHandR.transform.localRotation = Quaternion.Euler(rightRootPos.rotation);

                    int indexBound = Mathf.Min( Mathf.Min(ghostHandLTransforms.Length,ghostHandRTransforms.Length) , Mathf.Min(leftFrames.Count,rightFrames.Count) );
                    //use shortest array length for max index value to prevent out of bounds

                    for (int i = 1; i < indexBound; i++)
                    {
                        //to world position
                        // Vector3 worldPositionL = moveableSceneTransform.TransformPoint(recordedLeftHandTransformFrame[i].position);
                        // Quaternion worldRotationL = moveableSceneRotation * Quaternion.Euler(recordedLeftHandTransformFrame[i].rotation);
                        // Vector3 worldPositionR = moveableSceneTransform.TransformPoint(recordedRightHandTransformFrame[i].position);
                        // Quaternion worldRotationR = moveableSceneRotation * Quaternion.Euler(recordedRightHandTransformFrame[i].rotation);

                        ghostHandLTransforms[i].localPosition = leftFrames[i].position;
                        ghostHandLTransforms[i].localRotation = Quaternion.Euler(leftFrames[i].rotation);
                        ghostHandRTransforms[i].localPosition = rightFrames[i].position;
                        ghostHandRTransforms[i].localRotation = Quaternion.Euler(rightFrames[i].rotation);

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