
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DatAR.DataModels.Passables;
using DatAR.DataModels.Resources;
using JsonLD.Core;
using JsonSubTypes;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using DatAR.Auxiliary.SharedScripts;
using Cysharp.Threading.Tasks;

public class SparqlService : MonoBehaviour
{
    [SerializeField] public SparqlConfig sparqlConfig;
    private JObject _context;
    private string _prefixes;

    public static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings();
    private static readonly JsonLdOptions JsonLdOptions = new JsonLdOptions();

    private DebugService _debugService;

    private void Awake()
    {
        CheckSparqlConfigValidity();

        if (!string.IsNullOrEmpty(sparqlConfig.context))
        {
            _context = JObject.Parse(sparqlConfig.context);
            _prefixes = GeneratePrefixes();
        }

        JsonLdOptions.SetUseNativeTypes(true);
        JsonLdOptions.SetCompactArrays(false);

        JsonSerializerSettings.Converters.Add(JsonSubtypesConverterBuilder
            .Of(typeof(BaseResourcePassable), "@type")
            .RegisterSubtype(typeof(ConceptPassable), "datar:ConceptPassable")
            .RegisterSubtype(typeof(ClassPassable), "datar:ClassPassable")
            .RegisterSubtype(typeof(ClassListPassable), "datar:ClassListPassable")
            .RegisterSubtype(typeof(ConceptListPassable), "datar:ConceptListPassable")
            .RegisterSubtype(typeof(CooccurrenceListPassable), "datar:CooccurrenceListPassable")
            .SerializeDiscriminatorProperty() // ask to serialize the type property
            .Build());

        _debugService = GetComponent<DebugService>();
    }

    public async UniTask<ClassListPassable> GetAvailableClasses()
    {
        var queryRequest = $@"
            CONSTRUCT {{
                ?type a rdf:Class . # Note: hardcoded
                ?type rdfs:label ?label .
            }}
            { (!string.IsNullOrEmpty(sparqlConfig.defaultSubGraph) ? $"FROM {sparqlConfig.defaultSubGraph}" : "") }

            WHERE {{
                {{
                    SELECT distinct ?type ?label
                    WHERE {{
                        ?resource rdf:type ?type .
                        OPTIONAL {{ ?type rdfs:label ?label }}
                    }}
                }}
            }}";

        var queryResponseRaw = await QueryEndpoint(queryRequest);
        return new ClassListPassable(await ConvertRdfToResourceList<DynamicResource>(queryResponseRaw));
    }

    public async UniTask<List<DescriptionResource>> GetConceptDescription(string id)
    {
        var queryRequest = $@"
            CONSTRUCT {{
              ?id schema:description ?desc .
            }}
            WHERE {{
              {id} skos:closeMatch | skos:closeMatch/skos:closeMatch | ^skos:closeMatch | ^skos:closeMatch/skos:closeMatch  ?id .
              {{
                        SERVICE <https://query.wikidata.org/sparql> {{
                            ?id schema:description ?desc .
                        FILTER(LANG(?desc) = """" || LANGMATCHES(LANG(?desc), ""en""))
                    }}
              }}
              UNION
              {{
                    SERVICE <https://id.nlm.nih.gov/mesh/sparql> {{
                        ?id meshv:preferredConcept ?concept .
                            ?concept meshv:scopeNote ?desc.
                    }}
              }}
            }}";

        var queryResponseRaw = await QueryEndpoint(queryRequest, "http://157.245.79.168:8890/sparql");
        //var queryResponseRaw = await QueryEndpoint(queryRequest, "http://165.22.192.208:8890/sparql");

        return await ConvertRdfToResourceList<DescriptionResource>(queryResponseRaw);
    }

    public async UniTask<List<CoordsResource>> GetTopicModelCoordsOfType(string type, string serviceUrl = null, string serviceSubGraphUrl = null)
    {
        serviceSubGraphUrl = serviceSubGraphUrl != null ? $"FROM {serviceSubGraphUrl}" : "";

        var queryRequest = $@"
            CONSTRUCT {{
                ?id datar:coordX ?x ;
                    datar:coordY ?y ;
                    datar:coordZ ?z ;
                    a {type} .
            }}
            WHERE {{
              {(serviceUrl != null ? $"SERVICE <{serviceUrl}> {{ SELECT * {serviceSubGraphUrl} WHERE {{ " : "")}
              ?id a {type} .
              {(serviceUrl != null ? $"}} }}" : "")}

              ?id datar:coordX ?x ;
                  datar:coordY ?y ;
                  datar:coordZ ?z .
            }}";

        var queryResponseRaw = await QueryEndpoint(queryRequest, "http://157.245.79.168:8890/sparql");
        //var queryResponseRaw = await QueryEndpoint(queryRequest, "http://165.22.192.208:8890/sparql");

        return await ConvertRdfToResourceList<CoordsResource>(queryResponseRaw);
    }

    // Union class as an input, class to get all of the ids
    public async UniTask<List<DynamicResource>> GetCloseMatchingIds(List<string> resourceUris)
    {
        // Fix issue with Virtuoso failing to encode apostrophes correctly...
        // Proper solution would be using the developer version of Virtuoso, or using the full URI with \'.
        // See https://github.com/openlink/virtuoso-opensource/issues/318
        resourceUris = resourceUris.FindAll(uri => !uri.Contains("\\u0027"));

        // Debug.Log(JsonConvert.SerializeObject(resourceUris));

        if (resourceUris.Count == 0)
        {
            return new List<DynamicResource>(); // empty list
        }

        var queryRequest = $@"
            CONSTRUCT {{
                datar:testing skos:closeMatch ?id . 
            }}
        WHERE {{";

        for (var i = 0; i < resourceUris.Count; i++)
        {
            queryRequest += $" {{ {resourceUris[i]} ^skos:closeMatch ?id }} ";
            if (i < resourceUris.Count - 1)
            {
                queryRequest += "UNION";
            }
        }

        queryRequest += $"}}";
        // Debug.Log(queryRequest);

        var queryResponseRaw = await QueryEndpoint(queryRequest, "http://157.245.79.168:8890/sparql");
        //var queryResponseRaw = await QueryEndpoint(queryRequest, "http://165.22.192.208:8890/sparql");
        // Debug.Log(queryResponseRaw);
        return await ConvertRdfToResourceList<DynamicResource>(queryResponseRaw);
    }

    // Single ID as an input, returns all other matching IDs
    public async UniTask<List<DynamicResource>> GetCloseMatchingIds(string uri)
    {
        // Fix issue with Virtuoso failing to encode apostrophes correctly...
        // Proper solution would be using the developer version of Virtuoso, or using the full URI with \'.
        // See https://github.com/openlink/virtuoso-opensource/issues/318
        if (uri.Contains("\\u0027"))
        {
            Debug.LogError($"Problematic URI received: { uri }");
            throw new Exception($"Problematic URI received: { uri }");
        }

        // Debug.Log(JsonConvert.SerializeObject(resourceUris));

        var queryRequest = $@"
            CONSTRUCT {{
                ?id a ?type .
                ?id rdf:label ?id .
            }}
        WHERE {{";

        queryRequest += $" {{ {uri} skos:closeMatch | skos:closeMatch/skos:closeMatch | ^skos:closeMatch | ^skos:closeMatch/skos:closeMatch ?id }} ";

        queryRequest += $@"
            SERVICE <http://www.linked-brain-data.org:8890/sparql> {{
                SELECT * WHERE {{
                OPTIONAL {{ ?id a ?type . }}
                }}
            }}";

        queryRequest += "OPTIONAL { ?id a ?type } .\n";
        // if (filter.Length != null)
        // {
        //     queryRequest += $@"FILTER(regex(str(?id), ""{filter}"" ) )" + "\n";
        // }

        queryRequest += $@"FILTER(!regex(?id, {uri} ) )" + "\n";
        queryRequest += $"}}";
        // Debug.Log(queryRequest);

        var queryResponseRaw = await QueryEndpoint(queryRequest, "http://157.245.79.168:8890/sparql");
        //var queryResponseRaw = await QueryEndpoint(queryRequest, "http://165.22.192.208:8890/sparql");
        // Debug.Log(queryResponseRaw);
        return await ConvertRdfToResourceList<DynamicResource>(queryResponseRaw);
    }

    public async UniTask<ConceptListPassable> GetAllConceptsOfClass(string classUri)
    {
        var queryRequest = $@"
            CONSTRUCT {{
                ?resource a {classUri} .
                ?resource rdfs:label ?label .
            }}
            { (!string.IsNullOrEmpty(sparqlConfig.defaultSubGraph) ? $"FROM {sparqlConfig.defaultSubGraph}" : "") }
            WHERE {{
                {{
                    SELECT distinct ?resource ?label
                    WHERE {{
                        ?resource rdf:type {classUri} .
                        OPTIONAL {{ ?resource rdfs:label ?label }}
                    }}
                }}
            }}";

        var queryResponseRaw = await QueryEndpoint(queryRequest);
        return new ConceptListPassable(await ConvertRdfToResourceList<DynamicResource>(queryResponseRaw));
    }

    public async UniTask<DynamicResource> GetSingleConcept(string conceptUri)
    {
        var queryRequest = $@"
            CONSTRUCT {{
                {conceptUri} a ?type .
                {conceptUri} rdfs:label ?label .
            }}
            { (!string.IsNullOrEmpty(sparqlConfig.defaultSubGraph) ? $"FROM {sparqlConfig.defaultSubGraph}" : "") }
            WHERE {{
                {{
                    SELECT distinct ?type ?label 
                    WHERE {{
                        {conceptUri} rdf:type ?type .
                        OPTIONAL {{ {conceptUri} rdfs:label ?label }}
                    }}
                }}
            }}";

        var queryResponseRaw = await QueryEndpoint(queryRequest);
        return ConvertRdfToSingleResource<DynamicResource>(queryResponseRaw);
    }

    public async UniTask<DynamicResource> GetSingleClass(string classUri)
    {
        var queryRequest = $@"
            CONSTRUCT {{
                {classUri} a rdf:Class . # Note: hardcoded
                ?type rdfs:label ?label .
            }}
            { (!string.IsNullOrEmpty(sparqlConfig.defaultSubGraph) ? $"FROM {sparqlConfig.defaultSubGraph}" : "") }
            WHERE {{
                {{
                    SELECT distinct ?type ?label
                    WHERE {{
                        ?resource rdf:type {classUri} .
                        OPTIONAL {{ {classUri} rdfs:label ?label }}
                    }}
                }}
            }}";

        var queryResponseRaw = await QueryEndpoint(queryRequest);
        return ConvertRdfToSingleResource<DynamicResource>(queryResponseRaw);
    }

    public async UniTask<CooccurrenceListPassable> GetCooccurrenceStatementsWithConcept(string concept, string withClass)
    {
        var conceptResource = await GetSingleConcept(concept);
        var classResource = await GetSingleClass(withClass);

        var queryRequest = $@"
            CONSTRUCT {{
              ?statement a datar:CooccurrenceStatement ;
                         datar:cooccurrences ?appearTimes ;
                         datar:classItemMentionedWhenConceptItemMentioned ?Pba ;
                         datar:conceptMentionedWhenClassItemMentioned ?Pab ;
                         datar:classItem ?classItem .
              ?classItem rdfs:label ?classItemLabel ;
                         a {withClass} .
            }}
            { (!string.IsNullOrEmpty(sparqlConfig.defaultSubGraph) ? $"FROM {sparqlConfig.defaultSubGraph}" : "") }
            WHERE {{
              {{ ?statement rdf:subject {concept} ;
                         rdf:object ?classItem ;
                         lbdp:appearTimes ?appearTimes ;
                         lbdp:Pba ?Pba ;
                         lbdp:Pab ?Pab .
                 OPTIONAL {{ ?classItem rdfs:label ?classItemLabel . }} }}
              UNION
              {{ ?statement rdf:subject ?classItem ;
                         rdf:object {concept} ;
                         lbdp:appearTimes ?appearTimes ;
                         lbdp:Pba ?Pba ;
                         lbdp:Pab ?Pab .
                 OPTIONAL {{ ?classItem rdfs:label ?classItemLabel . }} }}
              ?classItem a {withClass} .
            }}
            ORDER BY ?appearTimes";


        JObject frame = (JObject)_context.DeepClone();
        JToken frame2 = JToken.Parse($@"{{
            ""datar:classItem"": {{
                ""@type"": ""{withClass}""
            }}
        }}");
        frame.Add(frame2.First);

        var queryResponseRaw = await QueryEndpoint(queryRequest);

        return new CooccurrenceListPassable(conceptResource, classResource, await ConvertRdfToResourceList<CooccurrenceResource>(queryResponseRaw, frame));
    }

    public async UniTask<List<DiseaseRelationResource>> GetDiseaseRelations(string disease1, string disease2)
    {
        var queryRequest = $@"
            CONSTRUCT {{
                ?statement a datar:cooccurrenceStatement .
                ?statement datar:disease ?disease .
                ?disease a lbd:disease .
                ?statement datar:appearTimes ?appearTimes .
                ?statement datar:concept ?concept .
                ?concept a ?conceptClass .
            }}
            WHERE {{
                {{
                    ?statement rdf:object ?disease .
                    FILTER(?disease = {disease1} || ?disease = {disease2}) .
                    ?statement rdf:subject ?concept .
                    ?statement lbdp:appearTimes ?appearTimes .
                    FILTER(?appearTimes > 10) .
                    ?concept a ?conceptClass .
                }}
                UNION
                {{
                    ?statement rdf:subject ?disease .
                    FILTER(?disease = {disease1} || ?disease = {disease2}) .
                    ?statement rdf:object ?concept .
                    ?statement lbdp:appearTimes ?appearTimes .
                    FILTER(?appearTimes > 10) .
                    ?concept a ?conceptClass .
                }}
            }}";

        // Need this to properly frame the data structure in ConvertRdfToResourceList. Not sure why...
        JObject frame = (JObject)_context.DeepClone();
        JToken frame2 = JToken.Parse($@"{{
            ""datar:disease"": {{
                ""@type"": ""lbd:disease""
            }}
        }}");
        frame.Add(frame2.First);

        var queryResponseRaw = await QueryEndpoint(queryRequest);

        return await ConvertRdfToResourceList<DiseaseRelationResource>(queryResponseRaw, frame);
    }

    public async UniTask<List<DiseaseTopicsResource>> GetTopicsRelatedToDisease(string disease, int appearTimes = 10)
    {
        var queryRequest = $@"
            CONSTRUCT {{
                	?statement 		a 			        datar:cooccurrenceStatement .
                	?statement 		datar:disease       ?disease .
                	?disease 		a 			        lbd:disease .
                	?statement 		datar:appearTimes 	?appearTimes .
                	?statement 		datar:concept 		?concept .
                	?concept 		a 			        ?conceptClass .
            }}
            WHERE {{
                        {{
                    		?statement	    rdf:object	        ?disease .
                    		FILTER 	(?disease = {disease}) .
                            ?statement 	    rdf:subject 	    ?concept .
                   		    ?statement 	    lbdp:appearTimes 	?appearTimes .
                            FILTER 	        (?appearTimes > {appearTimes}) .
                    		?concept 	    a 		            ?conceptClass .
              	        }}
                	    UNION
                	    {{
                    		?statement      rdf:subject 		?disease .
                    		FILTER 	        (?disease = {disease}) .
                    		?statement 	    rdf:object 		    ?concept .
                    		?statement 	    lbdp:appearTimes 	?appearTimes .
                            FILTER 	        (?appearTimes > {appearTimes}) .
                    		?concept 	    a 			        ?conceptClass .
                	    }}
            }}";

        // Need this to properly frame the data structure in ConvertRdfToResourceList. Not sure why...
        JObject frame = (JObject)_context.DeepClone();
        JToken frame2 = JToken.Parse($@"{{
            ""datar:disease"": {{
                ""@type"": ""lbd:disease""
            }}
        }}");
        frame.Add(frame2.First);

        var queryResponseRaw = await QueryEndpoint(queryRequest);

        return await ConvertRdfToResourceList<DiseaseTopicsResource>(queryResponseRaw, frame);
    }

    public async UniTask<List<DiseaseTopicsResource>> GetTopicEdges(string filter, int appearTimes = 10)
    {
        var queryRequest = $@"
            CONSTRUCT {{
                	?statement 		a 			        datar:cooccurrenceStatement .
                	?statement 		datar:disease       ?disease .
                	?disease 		a 			        lbd:disease .
                	?statement 		datar:appearTimes 	?appearTimes .
                	?statement 		datar:concept 		?concept .
                	?concept 		a 			        ?conceptClass .
            }}
            WHERE {{
                        {{
                    		?statement	    rdf:object	        ?disease .
                    		FILTER 	({filter}) .
                            ?statement 	    rdf:subject 	    ?concept .
                   		    ?statement 	    lbdp:appearTimes 	?appearTimes .
                            FILTER 	        (?appearTimes > {appearTimes}) .
                    		?concept 	    a 		            ?conceptClass .
              	        }}
            }}";


        // Need this to properly frame the data structure in ConvertRdfToResourceList. Not sure why...
        JObject frame = (JObject)_context.DeepClone();
        JToken frame2 = JToken.Parse($@"{{
            ""datar:disease"": {{
                ""@type"": ""lbd:disease""
            }}
        }}");
        frame.Add(frame2.First);

        var queryResponseRaw = await QueryEndpoint(queryRequest);

        return await ConvertRdfToResourceList<DiseaseTopicsResource>(queryResponseRaw, frame);
    }

    public async UniTask<List<SentenceResource>> GetCooccurrenceSentences(string region, string disease)
    {
        var query = $@"
            CONSTRUCT {{
                ?statement lbdp:articleId ?aId ;
                        lbdp:sentence ?s .
            }}
            FROM lbdg:region2disease2
            WHERE {{
                ?statement rdf:object {disease} ;
                    rdf:subject {region} ;
                    lbdp:articleId ?aId ;
                    lbdp:sentence ?s .
            }}";

        var queryResponse = await QueryEndpoint(query);
        return await ConvertRdfToResourceList<SentenceResource>(queryResponse);
    }

    /// <summary>
    /// Takes in a list of N-Triples and converts the contents to a uniform class.
    /// Note that this class only works if all described LOD objects have the same structure.
    ///
    /// This class exists because the behaviour of the Virtuoso JSON-LD exporter is a bit peculiar.
    /// It wraps the results array within an "@id" field, which ought to be an "@graph" field if anything.
    /// </summary>
    /// <param name="queryResponseRaw"></param>
    /// <returns></returns>
    private async UniTask<List<T>> ConvertRdfToResourceList<T>(string queryResponseRaw, JObject frame = null)
    {
        if (queryResponseRaw == null)
        {
            return new List<T>();
        }

        await UniTask.SwitchToThreadPool();
        if (frame == null)
        {
            frame = _context;
        }

        // Debug.Log(queryResponseRaw);

        // The value provided by the endpoint is not recognised as a valid double.
        queryResponseRaw = Regex.Replace(queryResponseRaw, @"\d+.\d+e-\d+", match =>
            Decimal.Parse(match.Value, NumberStyles.Any).ToString(CultureInfo.InvariantCulture));

        var queryResponse = JsonLdProcessor.FromRDF(queryResponseRaw, JsonLdOptions);

        // Debug.Log(queryResponse); // DEBUG

        // Using frame rather than compact keeps the nesting correct (even if only providing a context)
        var compactQueryResponse = JsonLdProcessor.Frame(queryResponse, frame, JsonLdOptions);

        // Debug.Log(compactQueryResponse); // DEBUG

        List<T> resources = new List<T>();
        compactQueryResponse["@graph"].ToList().ForEach((resource) =>
        {
            // A few resources have multiple disease appeartimes.
            // Before catching an error, we should check this
            if (resource.SelectToken("datar:appearTimes") != null &&
                resource.SelectToken("datar:appearTimes").Count<object>() > 1)
            {
                // Take the highest appeartime in this resource
                int maxAppeartime = 0;
                foreach (int appearTime in resource.SelectToken("datar:appearTimes"))
                {
                    if (maxAppeartime < appearTime)
                        maxAppeartime = appearTime;
                }
                resource.SelectToken("datar:appearTimes").Replace(maxAppeartime);
            }

            try
            {
                resources.Add(resource.ToObject<T>());
            }
            catch (Exception e)
            {
                JToken jToken = resource.SelectToken("datar:appearTimes");
                Debug.Log(jToken.Count<object>());

                Debug.LogError($"{e.Message} {JsonConvert.SerializeObject(resource)}");
            }
        });

        await UniTask.SwitchToMainThread();
        return resources;
    }

    private T ConvertRdfToSingleResource<T>(string queryResponseRaw)
    {
        if (queryResponseRaw == null)
        {
            Debug.LogError("Failed to convert single resource: query returned empty.");
            throw new Exception("Insufficient data available to perform this operation.");
        }

        var queryResponse = JsonLdProcessor.FromRDF(queryResponseRaw, JsonLdOptions);
        var compactQueryResponse = JsonLdProcessor.Compact(queryResponse, _context, JsonLdOptions);
        return compactQueryResponse["@graph"].First.ToObject<T>();
    }

    /// <summary>
    /// Queries provided SPARQL endpoint and returns JSON object.
    /// </summary>
    /// <param name="queryRequest">Dictionary key of the query to be executed.</param>
    /// <param name="customUrl">Use if custom SPARQL endpoint is needed for query.</param>
    /// <returns>(async) Serialised JSON object as string with query results.</returns>
    /// <returns>Raw response of endpoint as string (async).</returns>
    private async UniTask<string> QueryEndpoint(string queryRequest, string customUrl = null)
    {
        if (queryRequest == null)
        {
            throw new Exception("No query provided to Sparql Service GenerateUrl() function.");
        }

        var webRequest = CreateQueryRequest(queryRequest, customUrl);
        UniTask.SwitchToThreadPool();
        var response = await webRequest.SendWebRequest();
        UniTask.SwitchToMainThread();

        var queryResult = response.downloadHandler.text;

        if (!string.IsNullOrEmpty(response.error))
        {
            throw new Exception($"The query did not succeed: {response.error} (are you offline?)\n{queryResult}");
        }

        if (queryResult.Trim() == "[]" || queryResult.Trim().StartsWith("#"))
        {
            Debug.Log($"The query did not yield any objects." + (_debugService.debugOn ? $"This was the sparql query sent: {queryRequest}" : ""));
            queryResult = null;
        }

        return queryResult;
    }

    // LBD Format. Deprecated.
    // private UnityWebRequest CreateQueryRequest(string queryRequest, string customUrl = null)
    // {
    //     var webRequest = new UnityWebRequest();
    //     WWWForm form = new WWWForm();
    //     webRequest.method = "POST";

    //     // Set URL
    //     string baseUrl = sparqlConfig.endpointUrl;
    //     if (customUrl != null) baseUrl = customUrl;
    //     webRequest.uri = new Uri(baseUrl + "?");

    //     // Determine data type to use. If string contains construct, request json-ld rather than json.
    //     var returnType = "json";
    //     if (queryRequest.ToLower().Contains("construct"))
    //     {
    //         returnType = "text/plain";
    //     }
    //     webRequest.SetRequestHeader("Accept", returnType);
    //     webRequest.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");

    //     // Combine prefixes and current query
    //     string combinedQuery = $"{_prefixes} {queryRequest}";
    //     form.AddField("query", combinedQuery);
    //     UploadHandler uploadHandler = new UploadHandlerRaw(form.data);
    //     webRequest.uploadHandler = uploadHandler;

    //     // Attach download handler
    //     DownloadHandlerBuffer downloadHandler = new DownloadHandlerBuffer();
    //     webRequest.downloadHandler = downloadHandler;

    //     return webRequest;
    // }

    public async UniTask<List<SentenceResource>> Test(string region, string disease)
    {
        var query = $@"
        CONSTRUCT {{
            ?id1 a ?brainregion.
            ?id2 a ?gene
        }}
        WHERE {{
            ?id1 sct:hasTUI sct:T023.
            ?t1s1 ztonekg:SenseURL ?id1.
            ?id1 sct:hasEnglishLabel ?brainregion.
            ?t1 ztonekg:hasSense ?t1s1.
            ?s7 ztonekg:hasSenses ?t1.
            ?s ztonekg:hasTerm ?s7.
            ?s1 ztonekg:hasAnnotation ?s;
                ztonekg:hasSource ""Abstract"";
                ztonekg:hasAnnotation ?sb.
            ?sb ztonekg:hasTerm ?s7b.
            ?s7b ztonekg:hasSenses ?t1b.
            ?t1b ztonekg:hasSense ?t1s1b.
            ?t1s1b ztonekg:SenseURL ?id2.
            ?id2 sct:hasTUI sct:T028;
                sct:hasEnglishLabel ?gene.
            ?s1 ztonekg:hasText ?text.
        }}
        GROUP BY ?brainregion ?gene
        ORDER BY DESC (?count)
        LIMIT 10000";

        var queryResponse = await QueryEndpoint(query);
        return await ConvertRdfToResourceList<SentenceResource>(queryResponse);
    }

    // Triply endpoint
    private UnityWebRequest CreateQueryRequest(string queryRequest, string customUrl = null)
    {
        var webRequest = new UnityWebRequest();
        WWWForm form = new WWWForm();
        webRequest.method = "POST";

        // Set URL
        string baseUrl = sparqlConfig.endpointUrl;
        if (customUrl != null) baseUrl = customUrl;
        webRequest.uri = new Uri(baseUrl + "?");

        // Determine data type to use. If string contains construct, request json-ld rather than json.
        // var returnType = "*/*";
        var returnType = "json";
        if (queryRequest.ToLower().Contains("construct"))
        {
            returnType = "text/plain";
        }
        webRequest.SetRequestHeader("Accept", returnType);
        webRequest.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
        webRequest.SetRequestHeader("Authorization", "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJhdWQiOiJ1bmtub3duIiwiaXNzIjoiaHR0cHM6Ly9hcGkua3JyLnRyaXBseS5jYyIsImp0aSI6IjQ5ZDAxNDMwLTg1NWItNDY3NS1hZjkwLTVkNThiY2Q0MTk1NyIsInVpZCI6IjYyMjdkYmJjZmUzYTNjM2Y1YTA3YWQzYSIsImlhdCI6MTY1MDI3NTk5Nn0.ivqSezthy-CG11ASrIbvb5lAsRSzSN-wFKWWEaIJdpA");

        // Combine prefixes and current query
        string combinedQuery = $"{_prefixes} {queryRequest}";
        form.AddField("query", combinedQuery);
        UploadHandler uploadHandler = new UploadHandlerRaw(form.data);
        webRequest.uploadHandler = uploadHandler;

        // Attach download handler
        DownloadHandlerBuffer downloadHandler = new DownloadHandlerBuffer();
        webRequest.downloadHandler = downloadHandler;

        return webRequest;
    }

    /// <summary>
    /// Checks if Sparql config is assigned and valid. If not, throw exception.
    /// </summary>
    /// <exception cref="UnassignedReferenceException"></exception>
    private void CheckSparqlConfigValidity()
    {
        if (sparqlConfig == null || sparqlConfig.endpointUrl.Length < 1)
        {
            throw new UnassignedReferenceException(
                $"Sparql Config is not assigned or incomplete on game object '{gameObject.name}'.");
        }
    }

    private string GeneratePrefixes()
    {
        var stringBuilder = new StringBuilder();
        var context = _context.First.First.ToObject<Dictionary<string, dynamic>>();
        context.ForEach(item =>
        {
            if (item.Value is string)
            {
                stringBuilder.Append($"PREFIX {item.Key}: <{item.Value}> \n");
            } // If not a string, ignore.
        });
        return stringBuilder.ToString();
    }
}
