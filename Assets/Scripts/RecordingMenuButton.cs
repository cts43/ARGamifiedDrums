using System;
using System.Collections;
using UnityEngine;

public class RecordingMenuButton : MonoBehaviour
{

    public bool requiresUserInput;
    public bool isRecordingPath;
    public bool isMIDIPath;

    public string buttonID;
    public Action<string,string> ButtonPress;
    public Action ClosedMenu;

    public GameObject userInputDialoguePrefab; 

    public void RaiseButtonPress(string buttonID, string argument)
    {
        ButtonPress?.Invoke(buttonID, argument);
    }

    public IEnumerator Execute()
    {
        if (requiresUserInput)
        {
            //call delegate with string from user input
            var dialogueInstance = Instantiate(userInputDialoguePrefab, transform.parent.parent);
            UserInputDialogue inputGetter = dialogueInstance.GetComponent<UserInputDialogue>();

            string userInput;

            if (isRecordingPath)
            {
                inputGetter.showRecordingFiles();
            }
            else if (isMIDIPath)
            {
                inputGetter.showMIDIFiles();
            }
            else
            {
                Debug.Log("Invalid button setting!");
                yield break;
            }
            yield return new WaitUntil(() =>  inputGetter.closed);
            
            if (!inputGetter.hasSelectedString)
            {
                Debug.Log("nah");
                ClosedMenu?.Invoke();
            }
            else
            {
                Debug.Log("yuh");
                userInput = inputGetter.selectedString;
                RaiseButtonPress(buttonID, userInput);
            }
        }
        else
        {
            //call without/with null param
            RaiseButtonPress(buttonID, null);
        }
    }

}
