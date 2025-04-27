using System;
using System.Collections.Generic;
using System.Linq;
using DatAR.DataModels.Misc;
using DatAR.DataModels.Passables;
using DatAR.DataModels.Resources;
using UniRx;
using UnityEngine;

namespace DatAR.Widgets.ManipulationMinMaxFilter
{
    public class Histogram : MonoBehaviour
    {
        private DataflowOutlet _dataSource;
        private ManipulationMinMaxFilter _filter;
    
        [Tooltip( "Specify the data column you want to read in the Passable as a string" )]
        [SerializeField] private string dataColumnToDisplay;

        [SerializeField] private int numberOfBins = 10;
        private int[] _histogramOutput;

        public bool setRangeManually;
        public float overwriteMinValue = 0;
        public float overwriteMaxValue = 1;
        
        // Start is called before the first frame update
        void Awake()
        {
            _filter = gameObject.GetComponentInParent<ManipulationMinMaxFilter>();
            _dataSource = _filter.dataSender;
            _dataSource.data
                .Sample(TimeSpan.FromMilliseconds(600)) // throttle to avoid FPS slowdown
                .Subscribe(passable =>
                {
                    // Check if correct type
                    if (!(passable is Passable<CooccurrenceListPassable>))
                    {
                        if (passable.GetType().ToString() == "DatAR.DataModels.Passables.Passable")
                        {
                            Debug.Log("Histogram listening to data stream");
                        }
                        else
                        {
                            Debug.LogWarning($"Histogram received incompatible data type: {passable?.GetType()}");
                        }
                        return;
                    }

                    var resources = (passable as Passable<CooccurrenceListPassable>).data.Resources;
                    if (_filter != null && _filter.logTransformCooccurrences && dataColumnToDisplay == "Cooccurrences")
                    {
                        resources = resources.Select(resource =>
                        {
                            var copiedResource = new CooccurrenceResource(resource.Types, resource.Cooccurrences,
                                resource.ClassItem, resource.ProbabilityConceptGivenClassItem,
                                resource.ProbabilityClassItemGivenConcept, resource.FilterSelectionState);
                            copiedResource.Cooccurrences = Math.Log(resource.Cooccurrences);
                            return copiedResource;
                        }).ToList();
                    }

                    if (CalculateHistogramData(resources)) RenderHistogram();
                });
        }

        private void RenderHistogram()
        {
            // Reset state
            var barHolder = transform.GetChild(1);
            for (int i = barHolder.childCount - 1; i >= 0; --i) {
                Destroy(barHolder.GetChild(i).gameObject);
            }
            barHolder.DetachChildren();

            var maxHistogramCount = _histogramOutput.Max();
            for (var i = 0; i < _histogramOutput.Length; i++)
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(cube.GetComponent<BoxCollider>());
                cube.transform.SetParent(barHolder);
                cube.transform.localScale = new Vector3(1f / numberOfBins, ((float) _histogramOutput[i]) / maxHistogramCount, 1f);
                cube.transform.localPosition = new Vector3(((float) i)  / numberOfBins, ((float) _histogramOutput[i]) / maxHistogramCount / 2, 0f);
                cube.transform.localRotation = new Quaternion(0f, 0f, 0f, 0f);
            }
        }

        private bool CalculateHistogramData(List<CooccurrenceResource> data)
        {
            if (dataColumnToDisplay == "")
            {
                Debug.LogError("You haven't specified a data column for the histogram to display.");
                return false;
            }
            if (numberOfBins < 1)
            {
                Debug.LogError("Failed to render histogram: cannot divide across less than 1 bin.");
                return false;
            }
            if (data.Count < 1)
            {
                Debug.LogWarning("Failed to render histogram: no data received.");
                return false;
            }
        
            var histogramData = new List<float>();
            for (int i = 0; i < data.Count; i++)
            {
                // Todo: make filter state more generalisable across data models
                if (data[i].FilterSelectionState == FilterSelectionStateType.OutRange)
                {
                    continue;
                }
                var val = data[i][dataColumnToDisplay];
                if (val == null)
                {
                    Debug.LogError("Histogram received invalid instruction: data column not found.");
                    return false;
                }

                // Check if data type is compatible
                if (val is int)
                {
                    histogramData.Add((int) val);
                }
                else if (val is float)
                {
                    histogramData.Add((float) val);
                }
                else if (val is double)
                {
                    histogramData.Add(Convert.ToSingle(val));
                }
                else
                {
                    Debug.LogError($"Histogram received unsupported data type: {val.GetType()}");
                }
            }

            _histogramOutput = new int[numberOfBins];

            float minValue;
            float maxValue;
            if (setRangeManually)
            {
                minValue = overwriteMinValue;
                maxValue = overwriteMaxValue;
            }
            else
            {
                minValue = 0; // Hard-coded, to avoid cognitive bias
                if (_filter)
                {
                    // Given the slider can cut off max values
                    maxValue = (float) _filter._cooccurrencesMaxValue;
                }
                else maxValue = histogramData.Max();
            }
            histogramData.ForEach(val =>
            {
                // TODO: Currently probabilities that are NaN are not shown. This should be changed when Issue #23 is resolved.
                if (!Single.IsNaN(val))
                {
                    var normalisedVal = (val + minValue) / (maxValue - minValue);
                    var binNumber = (int) Math.Min(
                        Math.Floor(normalisedVal * numberOfBins),
                        numberOfBins - 1
                    );
                    // Debug.Log($"{val} {normalisedVal} {binNumber}"); // DEBUG
                    _histogramOutput[binNumber] = _histogramOutput[binNumber] + 1;
                }
            });
            return true;
        }
    }
}
