using Midi;
using NUnit.Framework.Internal;
using UnityEngine;

public class TestingScript : MonoBehaviour
{

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnEnable()
    {
        Debug.Log("Testing Script Enabled");
        MidiEventHandler.OnNoteOn += TestMethod;
    }

    private void OnDisable()
    {
        MidiEventHandler.OnNoteOn -= TestMethod;
    }

    public void TestMethod(int note, int velocity)
    {
        Debug.Log("Test Test Test");
    }
}
