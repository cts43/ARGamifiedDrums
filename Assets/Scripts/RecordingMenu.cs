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

    StatusIndicator statusIndicator;

    private void Start()
    {

        statusIndicator = GameObject.FindWithTag("StatusIndicator").GetComponent<StatusIndicator>();
        playbackManager = GameObject.FindWithTag("PlaybackManager").GetComponent<PlaybackManager>();
        labels = GetComponentsInChildren<TextMeshProUGUI>();
        HighlightLabel(selectedLabel);

        foreach (RecordingMenuButton button in GetComponentsInChildren<RecordingMenuButton>())
        {
            button.ButtonPress += OnButtonPress;
            button.ClosedMenu += OnSubMenuClosed;
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
        inputActions.Controller.Back.performed += OnBackPressed;
    }

    private void OnBackPressed(InputAction.CallbackContext context)
    {
        if (acceptingInput && gameObject.activeInHierarchy)
        {
            gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        acceptingInput = false;
        inputActions.Controller.DPad.performed -= OnDirectionPressed;
        inputActions.Controller.Select.performed -= OnSelectPressed;
        inputActions.Controller.Back.performed -= OnBackPressed;
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
                statusIndicator.ShowStatus("Loaded recording "+argument+"!");
                yield return null;
                this.gameObject.SetActive(false);
            }
            else
            {
                statusIndicator.ShowStatus("Failed to load!");
                Debug.Log("Failed to load or open load menu");
            }
        }
        else if (buttonID == "SaveRecording")
        {
            if (playbackManager.TrySaveData())
            {
                statusIndicator.ShowStatus("Saved recording!");
                yield return null;
                this.gameObject.SetActive(false);
            }
            else
            {
                statusIndicator.ShowStatus("Failed to save recording!");
                Debug.Log("Failed to save MIDI");
            }

        }
        else if (buttonID == "LoadMIDI")
        {
            if (playbackManager.loadNewMIDI(argument))
            {
                statusIndicator.ShowStatus("Loaded MIDI " + argument + "!");
                yield return null;
                this.gameObject.SetActive(false);
            }
            else
            {
                statusIndicator.ShowStatus("Failed to load MIDI!");
                Debug.Log("Failed to load MIDI or open load menu");
            }
        }
        else
        {
            statusIndicator.ShowStatus("Internal Error");
            Debug.Log("Invalid button ID!");
        }

        yield return null;
        acceptingInput = true;

        yield break;

    }

    private void OnSubMenuClosed()
    {
        acceptingInput = true;
    }

}
