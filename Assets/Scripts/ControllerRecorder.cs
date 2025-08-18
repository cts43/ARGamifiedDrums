using System;
using System.Collections.Generic;
using UnityEngine;

public class ControllerRecorder : MonoBehaviour
{

    public int handDrumstickOffsetFrames = 10;

    [Serializable]
    public class recordedTransform
    {
        public Vector3 position;
        public Vector3 rotation;

        public recordedTransform(Vector3 Position, Vector3 Rotation)
        {
            position = Position;
            rotation = Rotation;
        }
    }
    [Serializable]
    public class transformPair
    {
        public recordedTransform leftControllerMotion;
        public recordedTransform rightControllerMotion;

        public long currentTick;

        public transformPair(recordedTransform leftControllerMotion, recordedTransform rightControllerMotion, long currentTick)
        {
            this.leftControllerMotion = leftControllerMotion;
            this.rightControllerMotion = rightControllerMotion;
            this.currentTick = currentTick;
        }

        public void Deconstruct(out recordedTransform leftControllerMotion, out recordedTransform rightControllerMotion)
        {
            leftControllerMotion = this.leftControllerMotion;
            rightControllerMotion = this.rightControllerMotion;
        }
    }

    private Transform[] leftHandJoints;
    private Transform[] rightHandJoints;

    public GameObject leftHandPrefab;
    public GameObject rightHandPrefab;
    private GameObject leftHand;
    private GameObject rightHand;

    private GameObject GhostHandL;
    private GameObject GhostHandR;

    private Transform moveableSceneTransform;
    private Quaternion moveableSceneRotation;

    public GameObject DrumStickLPrefab;
    public GameObject DrumStickRPrefab; //prefab object to use when playing back
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
    private bool playbackStarted = false;
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
        
        moveableSceneTransform = GameObject.FindWithTag("Moveable Scene").transform;
        moveableSceneRotation = moveableSceneTransform.rotation;

        playing = false;
        leftHand = GameObject.FindWithTag("LeftHandTracker");
        rightHand = GameObject.FindWithTag("RightHandTracker");

        leftHandJoints = leftHand.GetComponentsInChildren<Transform>();
        rightHandJoints = rightHand.GetComponentsInChildren<Transform>();

        recording = true;
        Debug.Log("Recording started");
        justStartedRecording = true;
    }

    public void Reset()
    {
        Debug.Log("(Controller Recorder) Resetting..");
        if (recording)
        {
            RaiseFinishedRecording();
            Debug.Log("Recording motion finished");

            for (int i = 0; i < handDrumstickOffsetFrames; i++)
            {
                recordedLeftHandTransforms.Dequeue();
                recordedRightHandTransforms.Dequeue();
            }
        }
        playing = false;
        playbackStarted = false;
        recording = false;

        Destroy(DrumStickL);
        Destroy(DrumStickR);
        Destroy(GhostHandL);
        Destroy(GhostHandR);
    }

    private void Update()
    {
        if (playing || recording)
        {
            if (DrumStickL == null)
            {
                DrumStickL = Instantiate(DrumStickLPrefab, moveableSceneTransform); //create drum sticks if don't exist
                DrumStickL.SetActive(false);
            }
            if (DrumStickR == null)
            {
                DrumStickR = Instantiate(DrumStickRPrefab, moveableSceneTransform);
                DrumStickR.SetActive(false);
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
                DrumStickL.SetActive(true);
                DrumStickR.SetActive(true);

            }

            //CONTROLLER RECORDING//////////////////////

            Vector3 leftControllerPosition = OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch);
            Quaternion leftControllerRotation = OVRInput.GetLocalControllerRotation(OVRInput.Controller.LTouch);
            Vector3 rightControllerPosition = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch);
            Quaternion rightControllerRotation = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch);
            
            Vector3 localPosL = moveableSceneTransform.InverseTransformPoint(leftControllerPosition);
            Vector3 localRotL = (Quaternion.Inverse(moveableSceneRotation) * leftControllerRotation).eulerAngles;
            

            Vector3 localPosR = moveableSceneTransform.InverseTransformPoint(rightControllerPosition);
            Vector3 localRotR = (Quaternion.Inverse(moveableSceneRotation) * rightControllerRotation).eulerAngles;

            recordedTransform recordedMotionL = new recordedTransform(localPosL, localRotL);
            recordedTransform recordedMotionR = new recordedTransform(localPosR, localRotR);

            recordedControllerTransforms.Enqueue(new transformPair(recordedMotionL, recordedMotionR,PlaybackManager.Instance.currentTimeInTicks));

            DrumStickL.transform.position = leftControllerPosition;
            DrumStickL.transform.rotation = leftControllerRotation;

            DrumStickR.transform.position = rightControllerPosition;
            DrumStickR.transform.rotation = rightControllerRotation;

            //HAND RECORDING////////////////////////////

            List<recordedTransform> leftHandTransforms = new List<recordedTransform>();
            List<recordedTransform> rightHandTransforms = new List<recordedTransform>();
            foreach (var joint in leftHandJoints)
            {
                recordedTransform transform = new recordedTransform(joint.localPosition, joint.localEulerAngles);
                leftHandTransforms.Add(transform);
            }
            foreach (var joint in rightHandJoints)
            {
                recordedTransform transform = new recordedTransform(joint.localPosition, joint.localEulerAngles);
                rightHandTransforms.Add(transform);
            }

            var LeftRootTransform = new recordedTransform(moveableSceneTransform.InverseTransformPoint(leftHand.transform.position), (Quaternion.Inverse(moveableSceneRotation) * leftHand.transform.rotation).eulerAngles);
            var RightRootTransform = new recordedTransform(moveableSceneTransform.InverseTransformPoint(rightHand.transform.position), (Quaternion.Inverse(moveableSceneRotation) * rightHand.transform.rotation).eulerAngles);

            recordedLeftHandTransforms.Enqueue(new handMotionFrame(leftHandTransforms,LeftRootTransform));
            recordedRightHandTransforms.Enqueue(new handMotionFrame(rightHandTransforms,RightRootTransform));

        }

        else if (playing)
        {

            if (GhostHandL == null)
            {
                GhostHandL = Instantiate(leftHandPrefab, moveableSceneTransform);
                GhostHandL.SetActive(false);
            }
            if (GhostHandR == null)
            {
                GhostHandR = Instantiate(rightHandPrefab, moveableSceneTransform);
                GhostHandR.SetActive(false);
            }

            if (recordedControllerTransformsCopy.Count > 0 && recordedLeftHandTransforms.Count > 0)
            {
                //CONTROLLER/DRUMSTICK PLAYBACK

                //current tick could be less than first tick

                while (recordedControllerTransformsCopy.Count > 0 && recordedControllerTransformsCopy.Peek().currentTick <= PlaybackManager.Instance.currentTimeInTicks)
                {

                    if (!playbackStarted)
                    {
                        GhostHandL.SetActive(true);
                        GhostHandR.SetActive(true);
                        DrumStickL.SetActive(true);
                        DrumStickR.SetActive(true);
                    }

                    if (recordedControllerTransformsCopy.Count > 0)
                    {

                        (var playbackMotionL, var playbackMotionR) = recordedControllerTransformsCopy.Dequeue();

                        DrumStickL.transform.localPosition = playbackMotionL.position;
                        DrumStickL.transform.localRotation = Quaternion.Euler(playbackMotionL.rotation);

                        DrumStickR.transform.localPosition = playbackMotionR.position;
                        DrumStickR.transform.localRotation = Quaternion.Euler(playbackMotionR.rotation);
                    }

                    if (recordedLeftHandTransformsCopy.Count > 0 && recordedRightHandTransformsCopy.Count > 0)
                    {
                        //HAND MOTION PLAYBACK

                        Transform[] ghostHandLTransforms = GhostHandL.GetComponentsInChildren<Transform>();
                        Transform[] ghostHandRTransforms = GhostHandR.GetComponentsInChildren<Transform>();

                        var (leftFrames, leftRootPos) = recordedLeftHandTransformsCopy.Dequeue();
                        var (rightFrames, rightRootPos) = recordedRightHandTransformsCopy.Dequeue();

                        GhostHandL.transform.localPosition = leftRootPos.position;
                        GhostHandL.transform.localRotation = Quaternion.Euler(leftRootPos.rotation);
                        GhostHandR.transform.localPosition = rightRootPos.position;
                        GhostHandR.transform.localRotation = Quaternion.Euler(rightRootPos.rotation);

                        int indexBound = Mathf.Min(Mathf.Min(ghostHandLTransforms.Length, ghostHandRTransforms.Length), Mathf.Min(leftFrames.Count, rightFrames.Count));

                        for (int i = 1; i < indexBound; i++)
                        {

                            ghostHandLTransforms[i].localPosition = leftFrames[i].position;
                            ghostHandLTransforms[i].localRotation = Quaternion.Euler(leftFrames[i].rotation);
                            ghostHandRTransforms[i].localPosition = rightFrames[i].position;
                            ghostHandRTransforms[i].localRotation = Quaternion.Euler(rightFrames[i].rotation);

                        }

                    }
                }


            }
            else
            {
                playing = false;
                playbackStarted = false;
                Debug.Log("Stopped playing");
            }
        }
    }

    public (Queue<transformPair>, Queue<handMotionFrame>, Queue<handMotionFrame>,recordedTransform) getRecording()
    {
        return (recordedControllerTransforms, recordedLeftHandTransforms, recordedRightHandTransforms,new recordedTransform(moveableSceneTransform.position,moveableSceneRotation.eulerAngles));
    }

    public void loadRecording(Queue<transformPair> controllerMotion, Queue<handMotionFrame> leftHandMotion, Queue<handMotionFrame> rightHandMotion, recordedTransform moveableSceneTransform)
    {
        recordedControllerTransforms = controllerMotion;
        recordedLeftHandTransforms = leftHandMotion;
        recordedRightHandTransforms = rightHandMotion;
        this.moveableSceneTransform = GameObject.FindWithTag("Moveable Scene").transform;
        this.moveableSceneRotation = this.moveableSceneTransform.rotation;


    }
}