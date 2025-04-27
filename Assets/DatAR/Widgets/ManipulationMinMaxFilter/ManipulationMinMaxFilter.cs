using DatAR.DataModels.Misc;
using DatAR.DataModels.Passables;
using System;
using System.Linq;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.Serialization;

namespace DatAR.Widgets.ManipulationMinMaxFilter
{
    public class ManipulationMinMaxFilter : MonoBehaviour
    {
        public DataflowInlet dataReceiver;
        public DataflowOutlet dataSender;

        private Slider _cooccurrencesSlider;
        private Slider _conceptGivenClassSlider;
        private Slider _classItemGivenConceptSlider;

        public bool logTransformCooccurrences = true;

        class Slider
        {
            public TextMeshPro Top { get; set; }
            public TextMeshPro Bottom { get; set; }
            public BehaviorSubject<Vector2> SelectedRange { get; set; }
            public TextMeshPro Label { get; set; }
        }
    
        private Passable<CooccurrenceListPassable> _passable = new Passable<CooccurrenceListPassable>();
        public double _cooccurrencesMaxValue;
        private void Awake()
        {
            // Make value text components accessible
            _cooccurrencesSlider = CreateSliderInterface("FilterSliderCooccurrences");
            _conceptGivenClassSlider = CreateSliderInterface("FilterSliderConceptGivenClassItem");
            _classItemGivenConceptSlider = CreateSliderInterface("FilterSliderClassItemGivenConcept");
        }

        void Start()
        {
            Init();
        }

        private void Init()
        {
            // Listen to changes in data from source
            dataReceiver.data.Subscribe(passable =>
            {
                // Check if supported type
                if (!(passable is Passable<CooccurrenceListPassable>))
                {
                    if (passable.GetType().ToString() == "DatAR.DataModels.Passables.Passable")
                    {
                        Debug.Log("Filter listening to data stream");
                    }
                    else
                    {
                        Debug.LogWarning($"Filter received incompatible data type: {passable?.GetType()}");
                    }
                    return;
                }
            
                if (((Passable<CooccurrenceListPassable>) passable).data.Resources.Count < 1)
                {
                    return;
                }
            
                _passable = (passable as Passable<CooccurrenceListPassable>).DeepCopy();
                if (logTransformCooccurrences)
                {
                    _passable.data.Resources = _passable.data.Resources.Select((resource) =>
                    {
                        resource.Cooccurrences = Math.Log(resource.Cooccurrences);
                        return resource;
                    }).ToList();
                }
                _cooccurrencesMaxValue = _passable.data.Resources.Max(item => item.Cooccurrences);

                
                // Debug.Log($"FILTER OBJECT: Received {_passable.data.Count} items. Objects: {JsonConvert.SerializeObject(_passable.data)}"); // DEBUG

                // Update labels
                _cooccurrencesSlider.Label.text = $"Number of times {_passable.data.Concept.Label} co-occurs with given {_passable.data.Class.Label}";
                _conceptGivenClassSlider.Label.text = $"If {_passable.data.Concept.Label} mentioned, given {_passable.data.Class.Label} also mentioned";
                _classItemGivenConceptSlider.Label.text = $"If given {_passable.data.Class.Label} mentioned, {_passable.data.Concept.Label} also mentioned";
                
                DataToSender();
            });

            // Listen to changes in slider values
            _cooccurrencesSlider.SelectedRange
                .Merge(_conceptGivenClassSlider.SelectedRange, _classItemGivenConceptSlider.SelectedRange)
                .Sample(TimeSpan.FromMilliseconds(200)) // throttle to avoid FPS slowdown
                .Subscribe(incomingData => 
                {
                    DataToSender(); 
                }); 
        }

        private void DataToSender()
        {
            // Check if data is available
            if (_passable.data == null)
            {
                Debug.LogWarning($"Filter updater received empty data object");
                return;
            }

            // Update 
            SetSliderValueTexts(_cooccurrencesSlider, false);
            SetSliderValueTexts(_conceptGivenClassSlider);
            SetSliderValueTexts(_classItemGivenConceptSlider);

            var filteredPassable = _passable.DeepCopy();
            
            // Without doing the ForEach separately, rounding can cause discrepancies with interface text
            if (logTransformCooccurrences)
            {
                filteredPassable.data.Resources = filteredPassable.data.Resources.Select((resource) =>
                {
                    resource.Cooccurrences = (int) Math.Round(Math.Pow(Math.E, resource.Cooccurrences));
                    return resource;
                }).ToList();
                
                filteredPassable.data.Resources.ForEach(item =>
                {
                    if (!(item.Cooccurrences >= (int) Math.Round(Math.Pow(Math.E, _cooccurrencesSlider.SelectedRange.Value.x * _cooccurrencesMaxValue))
                          && item.Cooccurrences <= (int) Math.Round(Math.Pow(Math.E, _cooccurrencesSlider.SelectedRange.Value.y * _cooccurrencesMaxValue))
                          && (Double.IsNaN(item.ProbabilityConceptGivenClassItem)
                              || (item.ProbabilityConceptGivenClassItem >= _conceptGivenClassSlider.SelectedRange.Value.x
                                  && item.ProbabilityConceptGivenClassItem <= _conceptGivenClassSlider.SelectedRange.Value.y)
                          )
                          && (Double.IsNaN(item.ProbabilityClassItemGivenConcept)
                              || (item.ProbabilityClassItemGivenConcept >= _classItemGivenConceptSlider.SelectedRange.Value.x
                                  && item.ProbabilityClassItemGivenConcept <= _classItemGivenConceptSlider.SelectedRange.Value.y)
                          )))
                    {
                        item.FilterSelectionState = FilterSelectionStateType.OutRange;
                    }
                });
            }
            else
            {
                filteredPassable.data.Resources.ForEach(item =>
                {
                    if (!(item.Cooccurrences >= _cooccurrencesSlider.SelectedRange.Value.x * _cooccurrencesMaxValue
                          && item.Cooccurrences <= _cooccurrencesSlider.SelectedRange.Value.y * _cooccurrencesMaxValue
                          && (Double.IsNaN(item.ProbabilityConceptGivenClassItem)
                              || (item.ProbabilityConceptGivenClassItem >= _conceptGivenClassSlider.SelectedRange.Value.x
                                  && item.ProbabilityConceptGivenClassItem <= _conceptGivenClassSlider.SelectedRange.Value.y)
                          )
                          && (Double.IsNaN(item.ProbabilityClassItemGivenConcept)
                              || (item.ProbabilityClassItemGivenConcept >= _classItemGivenConceptSlider.SelectedRange.Value.x
                                  && item.ProbabilityClassItemGivenConcept <= _classItemGivenConceptSlider.SelectedRange.Value.y)
                          )))
                    {
                        item.FilterSelectionState = FilterSelectionStateType.OutRange;
                    }
                });
            }
            
            dataSender.Send(filteredPassable);
        }
    
        private Slider CreateSliderInterface(string sliderName)
        { 
            return new Slider
            {
                Bottom = gameObject.transform.Find(sliderName)
                    .GetComponent<FilterSliderControllerComponent>().BottomValueText,
                Top = gameObject.transform.Find(sliderName)
                    .GetComponent<FilterSliderControllerComponent>().TopValueText,
                SelectedRange = gameObject.transform.Find(sliderName)
                    .GetComponent<FilterSliderControllerComponent>().selectedRange,
                Label = gameObject.transform.Find(sliderName)
                    .GetComponent<FilterSliderControllerComponent>().LabelText
            };
        }
    
        private void SetSliderValueTexts(Slider slider, bool isFloat = true)
        {
            string topValue;
            string bottomValue;
            if (!isFloat)
            {
                if (logTransformCooccurrences)
                {
                    bottomValue = ((int) Math.Round(Math.Pow(Math.E, slider.SelectedRange.Value.x * _cooccurrencesMaxValue))).ToString();
                    topValue = ((int) Math.Round(Math.Pow(Math.E, slider.SelectedRange.Value.y * _cooccurrencesMaxValue))).ToString();
                }
                else
                {
                    bottomValue = ((int) Math.Round(slider.SelectedRange.Value.x * _cooccurrencesMaxValue)).ToString();
                    topValue = ((int) Math.Round(slider.SelectedRange.Value.y * _cooccurrencesMaxValue)).ToString();
                }
            }
            else
            {
                bottomValue = (slider.SelectedRange.Value.x).ToString("n4");
                topValue = (slider.SelectedRange.Value.y).ToString("n4");
            }
            slider.Top.text = topValue;
            slider.Bottom.text = bottomValue;
        }
    }
}
