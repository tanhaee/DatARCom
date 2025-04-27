using DatAR.DataModels.Resources;
using System.Linq;
using UnityEngine;

public class ResourceSphereManufacturer : MonoBehaviour
{
    private ColorService _colorService;
    
    // Creates new resource spheres on request at the given location.
    public Transform defaultSpawnLocation;

    [SerializeField] private GameObject resourceSpherePrefab;
    
    [SerializeField] [Range(0.0f, 1f)]
    public float pointScale = 0.25f;

    // Start is called before the first frame update
    void Awake()
    {
        if (defaultSpawnLocation == null)
        {
            defaultSpawnLocation = transform;
        }
        
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

    public GameObject Spawn(IDynamicResource resource, Vector3 location = default, Transform parent = null)
    {
        if (parent == null)
        {
            parent = defaultSpawnLocation;
        }
        
        // Instantiate as gameobject variable so that it can be manipulated within loop
        GameObject resourceSphere = Instantiate(
            resourceSpherePrefab,
            parent);
            
        var resourceComponent = resourceSphere.GetComponent<ResourceComponent>();
        resourceComponent.Resource = resource;

        resourceSphere.transform.name = resourceComponent.Resource.Id;
        resourceSphere.transform.localPosition = location;
        resourceSphere.transform.localScale = new Vector3(pointScale, pointScale, pointScale);

        if (resource.Types.Contains("rdf:Class"))
        {
            resourceSphere.GetComponent<Renderer>().material = _colorService.classColor;
        }
        else
        {
            resourceSphere.GetComponent<Renderer>().material = _colorService.neutralColor;
        }



        return resourceSphere;
    }
}
