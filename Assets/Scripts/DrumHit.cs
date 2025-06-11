using System.Collections;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

public class DrumHit : MonoBehaviour
{

    Renderer selfRenderer;

    public Color drumColour;
    public Color changeColour;

    public int note;

    private int waitFrames = 10; //default 10

    private Coroutine changeColourOnHit;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        waitFrames = GetComponentInParent<DrumManager>().framesToHighlightOnHit;
        selfRenderer = GetComponent<Renderer>();
        ResetColour(); //set initial colour to one set in Inspector
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void OnDrumHit()
    {
        if (changeColourOnHit != null) //If still running
        {
            StopCoroutine(changeColourOnHit);
            ResetColour();
        }

        changeColourOnHit = StartCoroutine(ShowDrumHitbyChangeColour());
    }

    public IEnumerator ShowDrumHitbyChangeColour()
    {
        Color oldColour = selfRenderer.material.color;
        selfRenderer.material.color = changeColour;

        yield return waitForFrames(waitFrames);

        ResetColour();
    }

    IEnumerator waitForFrames(int frames)
    {
        for (int frame = 0; frame < frames; frame++)
        {
            yield return null; //takes 1 frame to return. Looping this allow wait for i frames
        }

    }

    void ResetColour()
    {
        selfRenderer.material.color = drumColour;
    }
}