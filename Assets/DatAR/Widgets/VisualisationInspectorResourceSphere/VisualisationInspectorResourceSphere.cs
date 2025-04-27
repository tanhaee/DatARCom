using System;
using System.Collections;
using System.Collections.Generic;
using DatAR.DataModels.Passables;
using DatAR.DataModels.Resources;
using Newtonsoft.Json;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.Serialization;

namespace DatAR.Widgets.VisualisationInspectorResourceSphere
{
    public class VisualisationInspectorResourceSphere : MonoBehaviour, IQueryState
    {
        public BehaviorSubject<QueryState> IsLoading { get; }
        public string ErrorMessage { get; private set; }

        [SerializeField] private Receptacle conceptReceptacle;
        [SerializeField] private TextMeshPro dataText;
        [FormerlySerializedAs("_manufacturer")] [SerializeField] private ResourceSphereManufacturer manufacturer;

        private SparqlService _sparqlService;
        private DebugService _debugService;

        public VisualisationInspectorResourceSphere()
        {
            IsLoading = new BehaviorSubject<QueryState>(QueryState.IsEmpty);
        }

        void Start()
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
            _debugService = services.GetComponent<DebugService>();

            Init();
        }

        private void Init()
        {
            conceptReceptacle.slottedResourceContainerHasChanged.Subscribe(currentBatchId =>
            {

                var pool = manufacturer.defaultSpawnLocation;
                // Reset items in container
                for (int i = pool.transform.childCount - 1; i >= 0; --i)
                {
                    // Add wait period to avoid completely locking the process
                    // For large collections this still causes lower FPS.
                    // TODO: resolve performance issues with large amount of resources, e.g., by pooling / ECS.
                    Destroy(pool.transform.GetChild(i).gameObject, i * .01f);
                }

                if (conceptReceptacle.SlottedResourceContainer == null || conceptReceptacle.SlottedResourceContainer.Resource == null)
                {
                    return;
                }

                InspectResource(currentBatchId);
            });
        }

        async private void InspectResource(int currentBatchId)
        {
            if (currentBatchId != conceptReceptacle.slottedResourceContainerHasChanged.Value)
            {
                return;
            }

            IsLoading.OnNext(QueryState.IsEmpty);
            dataText.text = "";

            if (conceptReceptacle.SlottedResourceContainer == null
                || conceptReceptacle.SlottedResourceContainer.Resource == null)
            {
                return;
            }

            var text = "";
            // Debug: output resource data
            if (_debugService.debugOn)
            {
                text += JsonConvert.SerializeObject(
                            conceptReceptacle.SlottedResourceContainer.Resource,
                            SparqlService.JsonSerializerSettings) + "\n\n";
                dataText.text = text;
            }
            // Output descriptions related ot concept (if available)
            try
            {
                // Check if additional description of this item is available
                IsLoading.OnNext(QueryState.IsLoading);
                var descriptions =
                    await _sparqlService.GetConceptDescription(conceptReceptacle.SlottedResourceContainer.Resource.Id);

                if (descriptions.Count < 1)
                {
                    dataText.text = "<i>No further descriptions are available for this item at this moment.</i>";
                }
                else
                {
                    // Debug.Log(JsonConvert.SerializeObject(descriptions, SparqlService.JsonSerializerSettings));
                    descriptions.ForEach(desc => { text += $"{desc.Description} ({desc.Id})\n\n"; });
                    dataText.text = text;
                }
            }
            catch (Exception e)
            {
                ErrorMessage = e.Message;
                IsLoading.OnNext(QueryState.HasError);
            }

            // If related items are available of type LBD, show these
            List<DynamicResource> concepts = null;
            try
            {
                // Get related LBD items
                concepts = await _sparqlService.GetCloseMatchingIds(conceptReceptacle.SlottedResourceContainer.Resource.Id);
            }
            catch (Exception e)
            {
                ErrorMessage = e.Message;
                IsLoading.OnNext(QueryState.HasError);
            }

            IsLoading.OnNext(QueryState.HasLoaded);

            _ = StartCoroutine(SpawnConcept(new ConceptListPassable(concepts), currentBatchId));
        }

        // TODO: move to ResourceSphereManufacturer class
        private IEnumerator SpawnConcept(ConceptListPassable concepts, int currentBatchId)
        {
            if (currentBatchId != conceptReceptacle.slottedResourceContainerHasChanged.Value)
            {
                yield break;
            }

            var offsetLocation = new Vector3(0, 0, 0);
            foreach (var concept in concepts.Resources)
            {
                if (currentBatchId != conceptReceptacle.slottedResourceContainerHasChanged.Value)
                {
                    yield break;
                }

                manufacturer.Spawn(concept, offsetLocation);
                offsetLocation.y += manufacturer.pointScale * 1.25f;
                if (offsetLocation.y > 4)
                {
                    offsetLocation.y = 0;
                    offsetLocation.x -= manufacturer.pointScale * 4f;
                }
                yield return new WaitForEndOfFrame();
            }
        }
    }
}
