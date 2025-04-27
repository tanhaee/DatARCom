using System.Collections;
using System.Linq;
using System.Collections.Generic;
using DatAR.DataModels.Passables;
using DatAR.DataModels.Resources;
using UniRx;
using UnityEngine;
using UnityEngine.Serialization;
using System;

namespace DatAR.Widgets.QueryConceptsOfClass
{
    public class QueryConceptsOfClass : MonoBehaviour
    {
        private SparqlService _sparqlService;
        [SerializeField] private ResourceSphereManufacturer manufacturer;
        [SerializeField] private Receptacle receptacle;

        [SerializeField] private GameObject pool;

        private void Awake()
        {
            var services = GameObject.Find("Services");
            if (!services)
            {
                Debug.LogError("The `Services` Prefab was not found in the scene. "
                               + "This GameObject is required for this script to function.");
                gameObject.SetActive(false);
                return;
            }

            _sparqlService = services.GetComponent<SparqlService>();
        }

        private void Start()
        {
            receptacle.slottedResourceContainerHasChanged.Subscribe(currentBatchId =>
            {
                // We record batch id to ensure the enumerator can be stopped if it has changed.

                // Reset items in container
                for (int i = pool.transform.childCount - 1; i >= 0; --i)
                {
                    // Add wait period to avoid completely locking the process
                    // For large collections this still causes lower FPS.
                    // TODO: resolve performance issues with large amount of resources, e.g., by pooling / ECS.
                    Destroy(pool.transform.GetChild(i).gameObject, i * .01f);
                }

                if (receptacle.SlottedResourceContainer == null || receptacle.SlottedResourceContainer.Resource == null)
                {
                    return;
                }

                RetrieveAllOfClass(receptacle.SlottedResourceContainer.Resource, currentBatchId);
            });
        }

        async void RetrieveAllOfClass(IResource classItem, int currentBatchId)
        {
            if (currentBatchId != receptacle.slottedResourceContainerHasChanged.Value)
            {
                return;
            }

            try
            {
                // TODO: handle empty case, and add progress indicator
                var concepts = await _sparqlService.GetAllConceptsOfClass(classItem.Id);
                _ = StartCoroutine(SpawnConcept(concepts, currentBatchId));
            }
            catch (Exception e)
            {
                Debug.Log("Couldn't retrieve data from API. Retrieve local data instead.");

                #region
                // RETRIEVE DATA AVAILABLE IN SBA BRAIN MODEL /////////////////////////////////////////////////////////////////////////////////////////////////////
                // Retrieve all IDs and labels available in Triply
                // <UMLS ID, Disease Label>
                /* Code to filter the original triply csv
                Dictionary<string,string> UMLS2TRIPLY = new Dictionary<string,string>();
                TextAsset datass = Resources.Load ("Triply_BrainRegion_ID") as TextAsset;
                string[] triplyIDs = datass.text.Split(new string[] { "\r\n" }, StringSplitOptions.None); 
                IEnumerable<string> cleanTriplyIDs = triplyIDs.Distinct();
                foreach(var id in cleanTriplyIDs) // TO DO: Handle duplicate entries and escape commas
                {
                    string[] entry = id.Split('\t');
                    UMLS2TRIPLY.Add(entry[0], entry[1]);
                    // type.Add(entry[1]); // Add UMLS ID to this list
                }

                // Retrieve UMLS IDs which can be found in the SBA Brain model
                List<Dictionary<string,object>> sba_data = CSVReader.Read("SBA2UMLS(12-6-22)");
                List<string> availableUMLSids = new List<string>();

                // TO DO: Handle duplicate entries and escape commas
                foreach(Dictionary<string,object> item in sba_data)
                {
                    try 
                    {
                        availableUMLSids.Add(item["UMLS ID (Boyu)"].ToString());
                    }
                    catch(Exception ex)
                    {
                        // For some reason, numerous entries are not read correctly from CSV... 
                        string[] itemString = item["Order"].ToString().Split(',');
                        // Reads random tab in ID? Maybe this is what breaks the above. Dirty fix
                        if(itemString[7].Contains("\t"))
                            itemString[7] = itemString[7].Split('\t')[0];

                        availableUMLSids.Add(itemString[7]);
                    }
                }

                // Get brain regions that show up in the brain model
                availableUMLSids = availableUMLSids.Distinct().ToList();

                // Reverse dictionary from UMLS2TRIPLY 
                Dictionary<string, string> LABEL_ID = new Dictionary<string, string>();
                foreach(string id in availableUMLSids)
                    LABEL_ID.Add(UMLS2TRIPLY[id], id);

                List<DynamicResource> backUp = new List<DynamicResource>();
                List<Dictionary<string,object>> data = CSVReader.Read("brainregion-disease(29-4-22)");
                */
                #endregion

                // Get either all brain regions or brain diseases 

                List<DynamicResource> backUp = new List<DynamicResource>();

                string csvPath = "SBA Available Brain Regions";
                string columnName = "Brain Region";
                string typeOfClass = "Triply Brain Region";

                if (classItem.Id == "lbd:disease")
                {
                    csvPath = "SBA Available Brain Diseases";
                    columnName = "Brain Diseases";
                    typeOfClass = "Triply Brain Disease";
                }

                List<Dictionary<string, object>> data = CSVReader.Read(csvPath);
                List<string> type = new List<string>();
                type.Add(typeOfClass);

                foreach (Dictionary<string, object> item in data)
                {
                    DynamicResource resource = new DynamicResource(
                        item[columnName].ToString(),
                        type);
                    backUp.Add(resource);
                }

                ConceptListPassable newFormat = new ConceptListPassable(backUp);

                _ = StartCoroutine(SpawnConcept(newFormat, currentBatchId));
            }
        }

        // TODO: move to ResourceSphereManufacturer class
        private IEnumerator SpawnConcept(ConceptListPassable concepts, int currentBatchId)
        {
            if (currentBatchId != receptacle.slottedResourceContainerHasChanged.Value)
            {
                yield break;
            }

            var offsetLocation = new Vector3(0, 0, 0);
            foreach (var concept in concepts.Resources)
            {
                if (currentBatchId != receptacle.slottedResourceContainerHasChanged.Value)
                {
                    yield break;
                }

                manufacturer.Spawn(concept, offsetLocation);
                offsetLocation.y -= manufacturer.pointScale * 1.25f;
                if (offsetLocation.y < -5)
                {
                    offsetLocation.y = 0;
                    offsetLocation.x -= manufacturer.pointScale * 4f;
                }
                yield return new WaitForEndOfFrame();
            }
        }
    }
}