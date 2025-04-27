using System;
using DatAR.DataModels.Resources;
using TMPro;
using UnityEngine;
using Newtonsoft.Json;

public class ResourceComponent : MonoBehaviour
{
    private IDynamicResource _resource;
    
    public IDynamicResource Resource
    {
        get => _resource;
        set
        {
            _resource = value;
            if (value == null) return;

            // Set text correctly
            if (_text)
            {
                _text.text = Resource.Label;
            }
            currentResource = JsonConvert.SerializeObject(Resource, Formatting.Indented, SparqlService.JsonSerializerSettings);
        }
    }

    private void OnValidate()
    {
        _text = GetComponentInChildren<TextMeshPro>();
        if (!string.IsNullOrEmpty(currentResource))
        {
            Resource = JsonConvert.DeserializeObject<DynamicResource>(currentResource, SparqlService.JsonSerializerSettings);
        }
        
        if (_text)
        {
            _text.text = Resource.Label;
        }
    }

    private TextMeshPro _text;

    [SerializeField] [TextArea(15,20)]
    [Tooltip("Outputs what the last loaded resource was. If set at start of scene, will be used to construct resource.")]
    private string currentResource = "";
    
    // Start is called before the first frame update
    private void Awake()
    {
        _text = GetComponentInChildren<TextMeshPro>();

        if (!string.IsNullOrEmpty(currentResource))
        {
            Resource = JsonConvert.DeserializeObject<DynamicResource>(currentResource, SparqlService.JsonSerializerSettings);
        }

    }
}
