using System;
using Meta.XR.InputActions;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

public class RecordingMenuButton : MonoBehaviour
{

    public bool requiresUserInput;
    public bool isRecordingPath;
    public bool isMIDIPath;

    public string buttonID;
    public Action<string,string> ButtonPress;

    public GameObject userInputDialogue; 

    public void RaiseButtonPress(string buttonID, string argument)
    {
        ButtonPress?.Invoke(buttonID, argument);
    }

    public void Execute()
    {
        if (requiresUserInput)
        {
            //call delegate with string from user input
            Instantiate(userInputDialogue);
            UserInputDialogue inputGetter = userInputDialogue.GetComponent<UserInputDialogue>();

            string userInput;

            if (isRecordingPath)
            {
                userInput = inputGetter.GetMIDIString();
            }
            else if (isMIDIPath)
            {
                userInput = inputGetter.GetRecordingString();
            }
            else
            {
                Debug.Log("Invalid button setting!");
                return;
            }
            
            RaiseButtonPress(buttonID,userInput);
        }
        else
        {
            //call without/with null param
            RaiseButtonPress(buttonID,null);
        }
    }

}
