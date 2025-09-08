using System.Collections;
using TMPro;
using UnityEngine;

public class ScoreIndicator : MonoBehaviour
{

    public int clearTime = 100;

    public IEnumerator ReplaceLabel(string stringToReplace)
    {
        var label = GetComponentInChildren<TextMeshProUGUI>();
        label.text = stringToReplace;
        for (int i = 0; i < clearTime; i++)
        {
            yield return null; //wait clearTime frames
        }
        label.text = "";
    }

    public IEnumerator AddToLabel(string stringToAdd)
    {
        var label = GetComponentInChildren<TextMeshProUGUI>();
        label.text = label.text + "\n" + stringToAdd;
        for (int i = 0; i < clearTime; i++)
        {
            yield return null; //wait 20 frames
        }
        label.text = "";
    }
}
