using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class ResourceSphereDuplicationBehaviour : MonoBehaviour
{
    [FormerlySerializedAs("isFixed")] public bool shouldDuplicateOnGrab = true;

    private ResourceSphereManufacturer _manufacturer;
    
    private ColorService _colorService;

    private void Awake()
    {
        var resourceSpherePool = GameObject.Find("StandaloneResourceSpherePool");

        if (!resourceSpherePool)
        {
            Debug.LogError("No `StandaloneResourceSpherePool` Prefab was not found in the scene. "
                           + "This GameObject is required to allow free-floating resource spheres.");
            gameObject.SetActive(false);
            return;
        }
        
        _manufacturer = resourceSpherePool.GetComponent<ResourceSphereManufacturer>();
        
        var services = GameObject.Find("Services");
        if (!services)
        {
            Debug.LogError("The `Services` Prefab was not found in the scene. "
                           + "This GameObject is required for this script to function.");
            gameObject.SetActive(false);
            return;
        }
        
        _colorService = services.GetComponent<ColorService>();
    }

    // If not fixed, simply move when picked up.
    public void DuplicateIfFixed()
    {
        if (shouldDuplicateOnGrab)
        {
            var duplicate = Instantiate(gameObject, transform.parent);
            duplicate.transform.position = transform.position;
            duplicate.transform.localScale = transform.localScale;
            duplicate.GetComponent<Renderer>().material = GetComponent<Renderer>().material;

            transform.SetParent(GameObject.Find("StandaloneResourceSpherePool").transform, true);
            transform.localScale = new Vector3(_manufacturer.pointScale,_manufacturer.pointScale,_manufacturer.pointScale);
            GetComponent<ResourceSphereDuplicationBehaviour>().shouldDuplicateOnGrab = false;
            tag = "Deletable";

            // Set color to neutral
            if (GetComponent<ResourceComponent>().Resource.Types.Contains("rdf:Class"))
            {
                GetComponent<Renderer>().material = _colorService.classColor;
            }
            else
            {
                GetComponent<Renderer>().material = _colorService.neutralColor;
            }
            GetComponent<OutlineMaterialFix>().OverrideOldMaterials(GetComponent<Renderer>().materials);
            GetComponentInChildren<TMP_Text>().alpha = GetComponent<Renderer>().material.color.a;
        }
    }
}
