using UnityEngine;
using UnityEngine.InputSystem;

public class ManualAlign : MonoBehaviour
{

    private GameObject RightHandAnchor;
    private GameObject MoveableScene;
    private bool align = false;

    private void Start()
    {
        RightHandAnchor = GameObject.FindGameObjectWithTag("RightControllerAnchor");
        MoveableScene = GameObject.FindGameObjectWithTag("Moveable Scene");
    }

    private void Update()
    {

        if (OVRInput.GetDown(OVRInput.RawButton.A))
        {
            align = !align; //Toggle alignment when 'A' button pressed
        }

        if (align)
        {

            if (OVRInput.Get(OVRInput.RawButton.RThumbstickDown))
            {
                MoveableScene.transform.Translate(0, -0.001f, 0);
            }
            else if (OVRInput.Get(OVRInput.RawButton.RThumbstickUp))
            {
                MoveableScene.transform.Translate(0, 0.001f, 0);
            }

            var newXPos = RightHandAnchor.transform.position.x;
            var newYPos = MoveableScene.transform.position.y; //existing position
            var newZPos = RightHandAnchor.transform.position.z;

            var newXRot = MoveableScene.transform.eulerAngles.x;
            var newYRot = RightHandAnchor.transform.eulerAngles.y; //right hand rotation
            var newZRot = MoveableScene.transform.eulerAngles.z;

            MoveableScene.transform.position = (new Vector3(newXPos, newYPos, newZPos));
            MoveableScene.transform.rotation = Quaternion.Euler(new Vector3(newXPos, newYRot, newZPos));
        }  
    }
}