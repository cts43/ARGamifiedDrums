using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using Unity.SharpZipLib.Utils;

public class UserInputDialogue : MonoBehaviour
{

    TextMeshProUGUI[] labels;
    List<string> items = new List<string>();
    int selectedLabel = 0;
    int topLabel = 0;
    public bool hasSelectedString { private set; get; } = false;

    public string selectedString { private set; get; }

    private controllerActions inputActions;
    private bool acceptingInput = true;
    public bool closed { private set; get; } = false;

    private float timeoutSeconds = 0.2f;
    private void HighlightLabel(int index)
    {
        labels[index].outlineWidth = 0.2f;
        labels[index].outlineColor = new Color32(0, 200, 0, 200);
    }

    private void Awake()
    {
        inputActions = new controllerActions();
        inputActions.Enable();

        inputActions.Controller.DPad.performed += OnDirectionPressed;
        inputActions.Controller.Select.performed += OnSelectPressed;
        inputActions.Controller.Back.performed += OnBackPressed;

        labels = GetComponentsInChildren<TextMeshProUGUI>();
        HighlightLabel(selectedLabel);
    }

    private void OnBackPressed(InputAction.CallbackContext context)
    {
        StartCoroutine(CloseMenu());
    }

    private void OnDirectionPressed(InputAction.CallbackContext context)
    {
        Vector2 DPadValue = context.ReadValue<Vector2>();

        if (acceptingInput)
        {
            if (DPadValue.y > 0)
            {
                if (selectedLabel > 0)
                {
                    labels[selectedLabel].outlineWidth = 0;
                    selectedLabel -= 1;
                    HighlightLabel(selectedLabel);
                }
                else if (topLabel > 0)
                {
                    topLabel -= 1;
                    refreshVisibleItems();
                }
            }
            else if (DPadValue.y < 0)
            {
                int itemsIndex = topLabel + selectedLabel;

                if (itemsIndex < items.Count - 1)
                {
                    if (items[itemsIndex + 1].Length > 0 && selectedLabel < labels.Length - 1)
                    {
                        labels[selectedLabel].outlineWidth = 0;
                        selectedLabel += 1;
                        HighlightLabel(selectedLabel);
                    }
                    else
                    {
                        topLabel += 1;
                        refreshVisibleItems();

                    }
                }
            }
            StartCoroutine(InputAcceptTimeOut());
        }
    }

    private IEnumerator InputAcceptTimeOut()
    {
        acceptingInput = false;
        yield return new WaitForSeconds(timeoutSeconds);
        acceptingInput = true;
    }

    private void OnSelectPressed(InputAction.CallbackContext context)
    {
        if (acceptingInput)
        {
            int itemsIndex = selectedLabel + topLabel;
            selectedString = items[itemsIndex];
            hasSelectedString = true;
            Debug.Log("Selected " + items[itemsIndex]);
            StartCoroutine(CloseMenu());


        }
    }

    private IEnumerator CloseMenu()
    {
        yield return null;
        inputActions.Controller.DPad.performed -= OnDirectionPressed;
        inputActions.Controller.Select.performed -= OnSelectPressed; //unsubscribe methods
        inputActions.Controller.Back.performed -= OnBackPressed;
        inputActions.Disable();
        closed = true;
        Destroy(this.gameObject);
    }

    public void showMIDIFiles()
    {

        Debug.Log(FileManager.Instance);

        var info = new DirectoryInfo(FileManager.Instance.GetMIDIPath());

        List<FileInfo> validMidiFiles = new List<FileInfo>();

        foreach (var file in info.GetFiles())
        {
            if (file.Extension == ".mid")
            {
                validMidiFiles.Add(file);
                items.Add(file.Name);
            }
        }

        refreshVisibleItems();

    }

    public void showRecordingFiles()
    {
        string path = Application.persistentDataPath;

        var info = new DirectoryInfo(path);

        List<FileInfo> validFiles = new List<FileInfo>();

        foreach (var file in info.GetFiles())
        {
            Debug.Log(file.Extension);
            if (file.Extension == ".json")
            {
                validFiles.Add(file);
                items.Add(file.Name);
            }
        }

        for (int i = 0; i < labels.Count(); i++)
        {

            if (i < validFiles.Count())
            {
                labels[i].text = validFiles[i].Name;
            }
        }
    }

    private void refreshVisibleItems()
    {
        for (int i = 0; i < labels.Count(); i++)
        {
            try
            {
                labels[i].text = items[i + topLabel];
            }
            catch (Exception)
            {
                labels[i].text = "";
            }
        }
    }
}

