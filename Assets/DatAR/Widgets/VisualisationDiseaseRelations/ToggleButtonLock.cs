using Microsoft.MixedReality.Toolkit.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleButtonLock : MonoBehaviour
{
    public GameObject OtherButton;
    public QueryDiseaseCoocurrences Visualization;
    public bool SetDisplayRatio;
    public bool toggled;

    void Start()
    {
        if (toggled)
        {
            GetComponent<Interactable>().enabled = false;
            GetComponent<PressableButtonHoloLens2>().enabled = false;
        }
    }

    public void Toggle()
    {
        OtherButton.GetComponent<Interactable>().enabled = true;
        OtherButton.GetComponent<Interactable>().IsToggled = false;
        OtherButton.GetComponent<PressableButtonHoloLens2>().enabled = true;
        Visualization.SetDisplayRatio(SetDisplayRatio);
        StartCoroutine(DisableButton());
    }

    private IEnumerator DisableButton()
    {
        // Need to wait one frame to properly untoggle button
        yield return null;
        GetComponent<Interactable>().IsToggled = true;
        GetComponent<Interactable>().enabled = false;
        GetComponent<PressableButtonHoloLens2>().enabled = false;
    }
}
