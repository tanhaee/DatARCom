using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DatAR.Auxiliary.SharedScripts;
using DatAR.DataModels.Misc;
using DatAR.DataModels.Passables;
using DatAR.DataModels.Resources;
using Newtonsoft.Json;
using TMPro;
using UniRx;
using UnityEngine;

// Inspired by https://sites.psu.edu/bdssblog/2017/04/06/basic-data-visualization-in-unity-scatterplot-creation/
namespace DatAR.Widgets.VisualisationTopicModel
{
    public class VisualisationTopicModel : MonoBehaviour, IQueryState
    {
        public BehaviorSubject<QueryState> IsLoading { get; }
        public string ErrorMessage { get; private set; }

        private readonly Dictionary<string, GameObject> _pointsPool = new Dictionary<string, GameObject>();
        public readonly Dictionary<string, Vector3> pointsLocation = new Dictionary<string, Vector3>();

        [Range(0.0f, 0.5f)]
        public float pointScale = 0.02f;

        public bool showLabels = true;
        private bool failedDataRetrieval = false;

        // The prefab for the data points that will be instantiated
        public GameObject pointPrefab;

        // Object which will contain instantiated prefabs in scene hierarchy
        public GameObject pointPool;

        [SerializeField] private DataflowInlet highlightDataReceiver;
        [SerializeField] private Receptacle topicModelClassReceptacle;

        private ColorService _colorService;
        private SparqlService _sparqlService;

        private Passable _bufferedPassable;

        public VisualisationTopicModel()
        {
            IsLoading = new BehaviorSubject<QueryState>(QueryState.IsEmpty);
        }

        private void Awake()
        {
            var services = GameObject.Find("Services");
            _colorService = services.GetComponent<ColorService>();
            _sparqlService = services.GetComponent<SparqlService>();
        }

        private void Start()
        {
            topicModelClassReceptacle.slottedResourceContainerHasChanged.Subscribe(async (currentBatchId) =>
            {
                await UpdatePlot();
                if (_bufferedPassable != null)
                {
                    UpdateHighlights(_bufferedPassable);
                }
            });

            highlightDataReceiver.data.Subscribe(UpdateHighlights);
        }

        /**
         * TODO: Stop execution if new request comes in while running (which replaces it)
         */
        private void UpdateHighlights(Passable rawPassable)
        {
            if (rawPassable == null) // Demo purposes
            {
                // First reset topic model
                _pointsPool.ForEach(point =>
                {
                    point.Value.GetComponent<Renderer>().material = _colorService.notFoundColor;
                    point.Value.GetComponentInChildren<TMP_Text>().alpha = _colorService.notFoundColor.color.a;
                });

                // Randomly assign highlights in the model
                _pointsPool.ForEach(point =>
                {
                    if (UnityEngine.Random.Range(0, 100) < 5)
                    {
                        point.Value.GetComponent<Renderer>().material = _colorService.inFilterRangeColor;
                        point.Value.GetComponentInChildren<TMP_Text>().alpha = _colorService.inFilterRangeColor.color.a;
                    }
                });

                return;
            }

            // Check if correct type
            if (!(rawPassable is Passable<CooccurrenceListPassable>))
            {
                if (rawPassable.GetType().ToString() == "DatAR.DataModels.Passables.Passable")
                {
                    Debug.Log("3D Plot listening to data stream");
                }
                else
                {
                    Debug.LogWarning($"3D Plot received incompatible data type: {rawPassable?.GetType()}");
                }
                _bufferedPassable = null;
                return;
            }
            _bufferedPassable = rawPassable;

            var passable = (Passable<CooccurrenceListPassable>)rawPassable;

            if (passable.data.Resources.Count < 1)
            {
                return;
            }
            // Debug.Log($"3D PLOT OBJECT: Received {passable.data.Resources.Count} items. Objects: {JsonConvert.SerializeObject(passable.data)}"); // DEBUG

            _pointsPool.ForEach(point =>
            {
                var match = passable.data.Resources.Find(item => item.ClassItem.Id == point.Key);
                if (match == null)
                {
                    // Would trigger with every disease not at all related to the region
                    // Debug.Log("Did not find corresponding disease");

                    point.Value.GetComponent<Renderer>().material = _colorService.notFoundColor;
                    point.Value.GetComponentInChildren<TMP_Text>().alpha = _colorService.notFoundColor.color.a;
                    return;
                }

                if (match.FilterSelectionState == FilterSelectionStateType.InRange)
                {
                    point.Value.GetComponent<Renderer>().material = _colorService.inFilterRangeColor;
                    point.Value.GetComponentInChildren<TMP_Text>().alpha = _colorService.inFilterRangeColor.color.a;
                }
                else if (match.FilterSelectionState == FilterSelectionStateType.OutRange)
                {
                    point.Value.GetComponent<Renderer>().material = _colorService.outFilterRangeColor;
                    point.Value.GetComponentInChildren<TMP_Text>().alpha = _colorService.outFilterRangeColor.color.a;
                }
                else
                {
                    Debug.LogError("Unknown state");
                }
            });
        }

        async UniTask<bool> UpdatePlot()
        {
            if (topicModelClassReceptacle.SlottedResourceContainer == null
                || topicModelClassReceptacle.SlottedResourceContainer.Resource == null)
            {
                return false;
            }

            // Take class form receptacle
            await UpdatePlot(topicModelClassReceptacle.SlottedResourceContainer.Resource.Id);
            return true;
        }

        async UniTask<bool> UpdatePlot(string type, bool fixedAspectRatio = false)
        {
            // Clear previous plot data
            _pointsPool.Clear();
            pointsLocation.Clear();
            for (int i = pointPool.transform.childCount - 1; i >= 0; --i)
            {
                Destroy(pointPool.transform.GetChild(i).gameObject);
            }
            pointPool.transform.DetachChildren();

            IsLoading.OnNext(QueryState.IsLoading);
            // Query endpoint for topic model.
            // Note that currently only brain diseases from LBD are supported.
            // TODO: trigger dynamic topic model generator in case coordinates are not yet in the data set or outdated
            // That would likely require a back-end service of some kind
            List<CoordsResource> coords;
            try
            {
                coords = await _sparqlService.GetTopicModelCoordsOfType(type, "http://www.linked-brain-data.org:8890/sparql", "lbdg:region2disease2");
                if (coords.Count < 1)
                {
                    ErrorMessage = "Topic model not yet generated for this class; please try another one.";
                    IsLoading.OnNext(QueryState.HasError);
                    return false;
                }
            }
            catch (Exception e) // Failed to retrieve coordinates from data source. Load back-up data instead.
            {
                // Show error message
                // ErrorMessage = e.Message;
                // IsLoading.OnNext(QueryState.HasError);

                List<Dictionary<string, object>> data = CSVReader.Read("DiseaseCoords");
                List<string> data_type = new List<string>() { "disease" };
                coords = new List<CoordsResource>();

                for (var i = 0; i < data.Count; i++)
                {
                    CoordsResource coord = new CoordsResource(
                        data[i]["disease Names"].ToString(),
                        data_type,
                        Convert.ToSingle(data[i][" X"].ToString().Replace(".", ",")),
                        Convert.ToSingle(data[i][" Y"].ToString().Replace(".", ",")),
                        Convert.ToSingle(data[i][" Z"].ToString().Replace(".", ",")));

                    coords.Add(coord);
                }

                failedDataRetrieval = true;
            }
            IsLoading.OnNext(QueryState.HasLoaded);

            // Get max values on each axis
            float xMax = coords.Select(resource => resource.CoordX).Max();
            float yMax = coords.Select(resource => resource.CoordY).Max();
            float zMax = coords.Select(resource => resource.CoordZ).Max();
            float trueMax = Math.Max(xMax, Math.Max(yMax, zMax));

            // Get min values of each axis
            float xMin = coords.Select(resource => resource.CoordX).Min();
            float yMin = coords.Select(resource => resource.CoordY).Min();
            float zMin = coords.Select(resource => resource.CoordZ).Min();
            float trueMin = Math.Min(xMin, Math.Min(yMin, zMin));

            coords.ForEach(resource =>
            {
                float x, y, z;
                if (!fixedAspectRatio)
                {
                    x = (Convert.ToSingle(resource.CoordX) - xMin)
                        / (xMax - xMin);

                    y = (Convert.ToSingle(resource.CoordY) - yMin)
                        / (yMax - yMin);

                    z = (Convert.ToSingle(resource.CoordZ) - zMin)
                        / (zMax - zMin);
                }
                else
                {
                    x = (Convert.ToSingle(resource.CoordX) - trueMin)
                        / (trueMax - trueMin);

                    y = (Convert.ToSingle(resource.CoordY) - trueMin)
                        / (trueMax - trueMin);

                    z = (Convert.ToSingle(resource.CoordZ) - trueMin)
                        / (trueMax - trueMin);
                }

                GameObject dataPoint = Instantiate(
                    pointPrefab,
                    pointPool.transform,
                    false);

                var resourceComponent = dataPoint.GetComponent<ResourceComponent>();
                resourceComponent.Resource = resource;

                if (failedDataRetrieval) resourceComponent.transform.GetChild(0).GetComponent<TextMeshPro>().text = resource.Id;
                if (!showLabels) resourceComponent.transform.GetChild(0).gameObject.SetActive(false);

                dataPoint.transform.name = resourceComponent.Resource.Id;
                dataPoint.transform.localScale = new Vector3(pointScale, pointScale, pointScale);
                dataPoint.transform.localPosition = new Vector3(x, y, z);

                dataPoint.GetComponent<Renderer>().material = _colorService.notFoundColor;

                _pointsPool.Add(resourceComponent.Resource.Id, dataPoint);
                pointsLocation.Add(resourceComponent.Resource.Id,
                    dataPoint.transform.localPosition);
            });
            return true;
        }
    }
}
