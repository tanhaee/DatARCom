using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Extract the focus handling logic from <see cref="ObjectManipulator"/> into a separate class. 
/// Used for generating outlines on objects that should not have a <see cref="ObjectManipulator"/> but are still interactable (e.g. MinMaxFilter handles).
/// </summary>
[AddComponentMenu("Scripts/DatAR/FocusHandler")]
public class DatARFocusHandler : MonoBehaviour, IMixedRealityFocusChangedHandler
{
    [SerializeField]
    private bool AllowFarManipulation = true;

    [SerializeField]
    [Tooltip("Event which is triggered when focus begins.")]
    private UnityEvent<FocusEventData> onFocusEnterEvent = new UnityEvent<FocusEventData>();

    /// <summary>
    /// Event which is triggered when focus begins.
    /// </summary>
    public UnityEvent<FocusEventData> OnFocusEnterEvent
    {
        get { return onFocusEnterEvent; }
        set { onFocusEnterEvent = value; }
    }

    [SerializeField]
    [Tooltip("Event which is triggered when focus ends.")]
    private UnityEvent<FocusEventData> onFocusExitEvent = new UnityEvent<FocusEventData>();

    /// <summary>
    /// Event which is triggered when focus ends.
    /// </summary>
    public UnityEvent<FocusEventData> OnFocusExitEvent
    {
        get { return onFocusExitEvent; }
        set { onFocusExitEvent = value; }
    }

    public void OnFocusChanged(FocusEventData eventData)
    {
        bool isFar = !(eventData.Pointer is IMixedRealityNearPointer);
        if (isFar && !AllowFarManipulation)
        {
            return;
        }

        if (eventData.OldFocusedObject == null ||
                !eventData.OldFocusedObject.transform.IsChildOf(transform))
        {
            if (OnFocusEnterEvent != null)
            {
                OnFocusEnterEvent.Invoke(eventData);
            }
        }
        else if (eventData.NewFocusedObject == null ||
                 !eventData.NewFocusedObject.transform.IsChildOf(transform))
        {
            if (OnFocusExitEvent != null)
            {
                OnFocusExitEvent.Invoke(eventData);
            }
        }
    }

    public void OnBeforeFocusChange(FocusEventData eventData) { }
}
