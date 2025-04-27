using UnityEngine;

[CreateAssetMenu(fileName = "SparqlConfig", menuName = "ScriptableObjects/SparqlConfig")]
public class SparqlConfig : ScriptableObject
{
    public string endpointUrl;


    [TextArea(15, 20)]
    public string context; // Requires a JSON-LD context object. This context is also used to generate SPARQL prefixes.
    public string defaultSubGraph;
}

