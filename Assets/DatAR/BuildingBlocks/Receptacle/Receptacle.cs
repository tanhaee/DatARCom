using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using System.Collections;
using System.Linq;
using System.Reflection;
using UniRx;
using UnityEngine;

public class Receptacle : MonoBehaviour
{
    public ResourceComponent SlottedResourceContainer
    {
        get => slottedResourceContainer;
        private set
        {
            slottedResourceContainer = value;
            slottedResourceContainerHasChanged.OnNext(slottedResourceContainerHasChanged.Value + 1);
        }
    }
    [SerializeField] private ResourceComponent slottedResourceContainer;
    public BehaviorSubject<int> slottedResourceContainerHasChanged = new BehaviorSubject<int>(0);

    [SerializeField] private GameObject receptacle;

    [SerializeField] private string expectedType;

    private int _prevInstanceId;
    private bool isTriggering = false;

    private void OnValidate()
    {
        slottedResourceContainerHasChanged.OnNext(slottedResourceContainerHasChanged.Value + 1);
    }

    private void OnTriggerExit(Collider other)
    {
        GameObject otherGameObject = other.gameObject;
        // Check if Collision is with Resource Sphere
        var resourceComponent = otherGameObject.GetComponent<ResourceComponent>();
        if (!resourceComponent)
        {
            return;
        }
        Debug.Log("OnTriggerExit");

        if (GetComponent<SphereCollider>().bounds.Intersects(other.bounds))
        {
            return;
        }

        if (isTriggering)
        {
            return;
        }
        StartCoroutine(SetCurrentlyTriggering());

        // Check if currently slotted resource
        // If so, remove
        if (resourceComponent.GetInstanceID() == _prevInstanceId)
        {
            _prevInstanceId = -1;
            //Now ensure parent at-rest is the resource sphere pool
            //var om = other.gameObject.GetComponent<ObjectManipulator>();
            //var reflection_isManipulationStarted = typeof(ObjectManipulator).GetField("isManipulationStarted", BindingFlags.NonPublic | BindingFlags.Instance);
            //if (om && (bool) reflection_isManipulationStarted.GetValue(om))
            //{
            //    om.OnManipulationEnded.AddListener(CleanUp);
            //}
            //else
            //{

            //}

            //SlottedResourceContainer.transform.SetParent(GameObject.Find("StandaloneResourceSpherePool").transform);
            SlottedResourceContainer = null;
        }
    }

    private void CleanUp(ManipulationEventData hand)
    {
        if (!slottedResourceContainer)
        {
            Debug.LogWarning("Receptacle clean-up wrongly called");
            return;
        }

        SlottedResourceContainer.transform.SetParent(GameObject.Find("StandaloneResourceSpherePool").transform);
        SlottedResourceContainer.GetComponent<ObjectManipulator>().OnManipulationEnded.RemoveListener(CleanUp);
        SlottedResourceContainer = null;
    }


    private void OnTriggerEnter(Collider other)
    {
        GameObject otherGameObject = other.gameObject;

        // Check if Collision is with Resource Sphere
        var resourceComponent = otherGameObject.GetComponent<ResourceComponent>();
        if (!resourceComponent)
        {
            return;
        }

        Debug.Log("OnTriggerEnter");

        // Check if not resource sphere should stay fixed.
        if (!otherGameObject.GetComponent<ResourceSphereDuplicationBehaviour>() || otherGameObject.GetComponent<ResourceSphereDuplicationBehaviour>().shouldDuplicateOnGrab)
        {
            return;
        }
        
        // Check if type fits expectation, if provided.
        if (!string.IsNullOrEmpty(expectedType))
        {
            // Negative selection
            if (expectedType.StartsWith("!"))
            {
                if (resourceComponent.Resource.Types.Contains(expectedType.Substring(1)))
                {
                    return;

                }
            }
            // Positive selection
            else if (!resourceComponent.Resource.Types.Contains(expectedType))
            {
                return;
            }
        }

        if (isTriggering)
        {
            return;
        }

        Debug.Log($"Prev id: {_prevInstanceId} Cur id: {resourceComponent.GetInstanceID()}");

        // Check if not the same as previous resource
        // If not checked, this function runs three times. Yet to figure out why...
        if (_prevInstanceId == resourceComponent.GetInstanceID())
        {
            return;
        }
        _prevInstanceId = resourceComponent.GetInstanceID();

        StartCoroutine(SetCurrentlyTriggering());
        
        // Remove previous item in receptacle if present
        if (receptacle.transform.childCount > 0)
        {
            var previousItem = receptacle.transform.GetChild(0);
            previousItem.SetParent(GameObject.Find("StandaloneResourceSpherePool").transform);
            previousItem.localPosition += new Vector3(0, previousItem.transform.lossyScale.y * 1.5f, 0f);
        }

        // Detach from hand
        otherGameObject.GetComponent<ObjectManipulator>().ForceEndManipulation();

        // Move sphere to center of receptacle
        otherGameObject.transform.SetParent(receptacle.transform);
        otherGameObject.transform.localPosition = Vector3.zero;

        SlottedResourceContainer = resourceComponent;
    }

    private IEnumerator SetCurrentlyTriggering()
    {
        isTriggering = true;
        for(var i = 0; i < 4; i++)
        {
            yield return null;
        }
        isTriggering = false;
    }
}
