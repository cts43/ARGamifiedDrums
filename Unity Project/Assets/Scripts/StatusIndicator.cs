using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class StatusIndicator : MonoBehaviour
{
    public int clearTimeInSeconds = 1;
    TextMeshProUGUI label;
    private Coroutine currentFade;

    private void Start()
    {
        label = GetComponentInChildren<TextMeshProUGUI>();
        label.alpha = 0f;

    }

    public void ShowStatus(string newText)
    {
        if (currentFade != null)
        {
            StopCoroutine(currentFade);
        }
        currentFade = StartCoroutine(ReplaceLabel(newText));
    }

    private IEnumerator ReplaceLabel(string stringToReplace)
    {
        label.text = stringToReplace;
        StartCoroutine(Fade(0.5f, 1f));
        yield return new WaitForSeconds(clearTimeInSeconds);
        StartCoroutine(Fade(0.5f, 0f));
    }

    private IEnumerator Fade(float time, float target)
    {
        float elapsedTime = 0f;
        float startValue = label.alpha;

        while (elapsedTime < time)
        {
            elapsedTime += Time.deltaTime;
            label.alpha = Mathf.Lerp(startValue, target, elapsedTime / time);
            yield return null; //wait 1 frame
        }
    }
}
