using UnityEngine;

public class DetachedHandAnchor : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        foreach (var renderer in GetComponentsInChildren<Renderer>(true))
        {
            renderer.enabled = false;
            Debug.Log("Disabled renderer"+renderer.transform.parent);
        }
    }
}
