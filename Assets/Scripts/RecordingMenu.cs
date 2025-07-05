using System.Collections;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public class RecordingMenu : MonoBehaviour
{

    TextMeshProUGUI[] labels;
    PlaybackManager playbackManager;
    int selectedLabel = 0;

    private float timeoutSeconds = 0.2f;

    private controllerActions inputActions;

    private bool acceptingInput = false;

    private void Start()
    {

        playbackManager = GameObject.FindWithTag("PlaybackManager").GetComponent<PlaybackManager>();
        labels = GetComponentsInChildren<TextMeshProUGUI>();
        HighlightLabel(selectedLabel);

        foreach (RecordingMenuButton button in GetComponentsInChildren<RecordingMenuButton>())
        {
            button.ButtonPress += OnButtonPress;
        }
    }

    private void HighlightLabel(int index)
    {
        labels[index].outlineWidth = 0.2f;
        labels[index].outlineColor = new Color32(0, 200, 0, 200);
    }

    private void OnDirectionPressed(InputAction.CallbackContext context)
    {
        Vector2 DPadValue = context.ReadValue<Vector2>();

        if (acceptingInput && gameObject.activeInHierarchy)
        {

            if (DPadValue.y > 0)
            {
                if (selectedLabel > 0)
                {
                    labels[selectedLabel].outlineWidth = 0;
                    selectedLabel -= 1;
                    HighlightLabel(selectedLabel);
                }
            }
            else if (DPadValue.y < 0)
            {
                if (selectedLabel < labels.Count() - 1)
                {
                    labels[selectedLabel].outlineWidth = 0;
                    selectedLabel += 1;
                    HighlightLabel(selectedLabel);
                }
            }

            StartCoroutine(InputAcceptTimeOut());
        }

    }

    private void OnSelectPressed(InputAction.CallbackContext context)
    {
        if (acceptingInput && gameObject.activeInHierarchy)
        {
            RecordingMenuButton Button = labels[selectedLabel].GetComponentInChildren<RecordingMenuButton>();
            StartCoroutine(Button.Execute());
            acceptingInput = false;

        }
    }

    private IEnumerator InputAcceptTimeOut()
    {
        acceptingInput = false;
        yield return new WaitForSeconds(timeoutSeconds);
        acceptingInput = true;
    }

    private void OnEnable()
    {
        acceptingInput = true; //always accept inputs when just been set active
        inputActions = new controllerActions();
        inputActions.Enable();

        inputActions.Controller.DPad.performed += OnDirectionPressed;
        inputActions.Controller.Select.performed += OnSelectPressed; //subscribe controller inputs
    }

    private void OnDisable()
    {
        acceptingInput = false;
        inputActions.Controller.DPad.performed -= OnDirectionPressed;
        inputActions.Controller.Select.performed -= OnSelectPressed;
        inputActions.Disable();
    }

    private void OnButtonPress(string buttonID, string argument)
    {
        StartCoroutine(ButtonPress(buttonID, argument));
    }

    private IEnumerator ButtonPress(string buttonID, string argument)
    {
        acceptingInput = true;
        if (buttonID == "LoadRecording")
        {
            if (playbackManager.TryLoadData(argument))
            {
                Debug.Log("LOADED");
                yield return null;
                this.gameObject.SetActive(false);
            }
            else
            {
                Debug.Log("Failed to load or open load menu");
            }
        }
        else if (buttonID == "SaveRecording")
        {
            if (playbackManager.TrySaveData())
            {
                Debug.Log("SAVED");
                yield return null;
                this.gameObject.SetActive(false);
            }
            else
            {
                Debug.Log("Failed to save MIDI"); //need an actual indicator for this
            }

        }
        else if (buttonID == "LoadMIDI")
        {
            if (playbackManager.loadNewMIDI(argument))
            {
                yield return null;
                this.gameObject.SetActive(false);
            }
            else
            {
                Debug.Log("Failed to load MIDI or open load menu");
            }
        }
        else
        {
            Debug.Log("Invalid button ID!");
        }

        yield return null;
        acceptingInput = true;

        yield break;

    }
}
