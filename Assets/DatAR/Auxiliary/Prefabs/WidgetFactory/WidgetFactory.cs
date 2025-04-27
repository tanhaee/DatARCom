using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Physics;
using Microsoft.MixedReality.Toolkit.UI;
using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;


public class WidgetFactory : MonoBehaviour,  IMixedRealityFocusHandler
{

    [SerializeField] private GameObject prefabToGenerate;

    private void Awake()
    {
        var widgetPool = GameObject.Find("StandaloneWidgetPool");

        if (!widgetPool)
        {
            Debug.LogError("No `StandaloneWidgetPool` Prefab was not found in the scene. "
                           + "This GameObject is required to allow widget generation");
            gameObject.SetActive(false);
        }
    }
    public void GenerateWidget(ManipulationEventData data)
    {
        StartCoroutine(SetNewManip(data.Pointer));
    }

    private IEnumerator SetNewManip(IMixedRealityPointer pointer)
    {
        var duplicate = Instantiate(prefabToGenerate, GameObject.Find("StandaloneWidgetPool").transform);
        duplicate.transform.position = transform.position;
        var oldPos = transform.position;

        yield return 0;

        GetComponent<ObjectManipulator>().ForceEndManipulation();
        transform.position = oldPos;
        CoreServices.FocusProvider.TryGetFocusDetails(pointer, out FocusDetails oldFocusDetails);
        oldFocusDetails.Object = duplicate.gameObject;
        CoreServices.FocusProvider.TryOverrideFocusDetails(pointer, oldFocusDetails);
        CoreServices.InputSystem.RaisePointerDown(pointer, MixedRealityInputAction.None, pointer.Controller.ControllerHandedness);
    }

    public void OnFocusEnter(FocusEventData eventData)
    {
        var collider = GetComponent<BoxCollider>();
        Vector3 worldCenter = collider.transform.TransformPoint(collider.center);
        Vector3 worldHalfExtents = collider.transform.TransformVector(collider.size * 0.5f);
        var overlappingObjectManipulators = Physics
            .OverlapBox(worldCenter, worldHalfExtents, collider.transform.rotation)
            .Where(x => x.gameObject != gameObject && x.GetComponent<ObjectManipulator>() != null);

        if(overlappingObjectManipulators.Count() > 0)
        {
            var newFocusObj = overlappingObjectManipulators.First();
            CoreServices.FocusProvider.TryGetFocusDetails(eventData.Pointer, out FocusDetails oldFocusDetails);
            oldFocusDetails.Object = newFocusObj.gameObject;
            CoreServices.FocusProvider.TryOverrideFocusDetails(eventData.Pointer, oldFocusDetails);
        }
    }

    public void OnFocusExit(FocusEventData eventData)
    {
    }
}
