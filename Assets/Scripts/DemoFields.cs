using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class DemoFields : MonoBehaviour
{

    private DemonstrationPlayer demonstrationPlayer;
    private controllerActions inputActions;

    public int demoField { get; private set; }
    public int playthroughField { get; private set; }
    public int evalField { get; private set; }
    private bool acceptingInput = true;

    private int max = 3;
    private float timeoutSeconds = 0.2f;

    public Color selectedColour;
    SelectableField selectedField;
    SelectableField[] selectableFields;
    TextMeshProUGUI[] labels;

    private enum SelectableField
    {
        DemoField = 0,
        PlaythroughField = 1,
        EvaluationField = 2

    }

    private void Start()
    {
        inputActions = new controllerActions();
        inputActions.Enable();
        demonstrationPlayer = GameObject.FindWithTag("DemonstrationPlayer").GetComponent<DemonstrationPlayer>();
        demoField = demonstrationPlayer.numberOfDemonstrations;
        playthroughField = demonstrationPlayer.numberOfPlaythroughs;
        evalField = demonstrationPlayer.numberOfEvaluations;
        selectableFields = (SelectableField[])Enum.GetValues(typeof(SelectableField));
        labels = GetComponentsInChildren<TextMeshProUGUI>();


    }

    private void OnEnable()
    {
        acceptingInput = true;
    }

    private void SelectNextField()
    {
        if ((int)selectedField < selectableFields.Length - 1)
        {
            selectedField++;
        }
        else
        {
            selectedField = selectableFields[0];
        }
    }

    private void SelectPreviousField()
    {
        if ((int)selectedField > 0)
        {
            selectedField--;
        }
        else
        {
            selectedField = (SelectableField)selectableFields.Length-1;
        }
    }
    
    private IEnumerator InputAcceptTimeOut()
    {
        acceptingInput = false;
        yield return new WaitForSeconds(timeoutSeconds);
        acceptingInput = true;
    }

    private void Update()
    {

        if (acceptingInput)
        {
            var direction = inputActions.Controller.DPad.ReadValue<Vector2>();

            if (direction == new Vector2(0, -1))
            {
                SelectNextField();
                StartCoroutine(InputAcceptTimeOut());
            }
            else if (direction == new Vector2(0, 1))
            {
                SelectPreviousField();
                StartCoroutine(InputAcceptTimeOut());
            }
            else if (direction == new Vector2(1, 0))
            {
                var currentLabel = labels[(int)selectedField];
                if (Convert.ToInt32(currentLabel.text) < max)
                {
                    currentLabel.text = (Convert.ToInt32(currentLabel.text) + 1).ToString();
                }
                StartCoroutine(InputAcceptTimeOut());
            }
            else if (direction == new Vector2(-1, 0))
            {
                var currentLabel = labels[(int)selectedField];
                if (Convert.ToInt32(currentLabel.text) > 0)
                {
                    currentLabel.text = (Convert.ToInt32(currentLabel.text) - 1).ToString();
                }
                StartCoroutine(InputAcceptTimeOut());
            }
        }

        for (int i = 0; i < labels.Length; i++)
        {
            if (i == (int)selectedField)
            {
                labels[i].color = selectedColour;
            }
            else
            {
                labels[i].color = new Color(1, 1, 1);
            }
            switch (labels[i].gameObject.name)
            {
                case "DemoField":
                    demoField = Convert.ToInt32(labels[i].text);
                    break;
                case "PlaythroughField":
                    playthroughField = Convert.ToInt32(labels[i].text);
                    break;
                case "EvalField":
                    evalField = Convert.ToInt32(labels[i].text);
                    break;
            }
        }
    }
}
