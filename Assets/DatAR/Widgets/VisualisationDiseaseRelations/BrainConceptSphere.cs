using DatAR.DataModels.Resources;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BrainConceptSphere : MonoBehaviour
{
    public TMPro.TextMeshPro conceptText;
    public TMPro.TextMeshPro disease1AppearTimesText;
    public TMPro.TextMeshPro disease2AppearTimesText;
    public GameObject disease1Bar;
    public GameObject disease2Bar;

    [HideInInspector] public string BrainConceptLabel { get; private set; }
    [HideInInspector] public int Disease1AppearTimes { get; private set; }
    [HideInInspector] public int Disease2AppearTimes { get; private set; }

    public void PopulateData(string brainConcept, string brainClass, int disease1AppearTimes, int disease2AppearTimes)
    {
        BrainConceptLabel = brainConcept;
        Disease1AppearTimes = disease1AppearTimes;
        Disease2AppearTimes = disease2AppearTimes;

        conceptText.text = BrainConceptLabel;
        disease1AppearTimesText.text = Disease1AppearTimes.ToString();
        disease2AppearTimesText.text = Disease2AppearTimes.ToString();

        switch (brainClass)
        {
            case "lbd:region":
                GetComponent<Renderer>().material = Resources.Load<Material>("BrainClasses/Concept_BrainRegion");
                break;
            case "lbd:transmitter":
                GetComponent<Renderer>().material = Resources.Load<Material>("BrainClasses/Concept_Neurotransmitter");
                break;
            case "lbd:function":
                GetComponent<Renderer>().material = Resources.Load<Material>("BrainClasses/Concept_CognitiveFunction");
                break;
            case "lbd:protein":
                GetComponent<Renderer>().material = Resources.Load<Material>("BrainClasses/Concept_Protein");
                break;
            case "lbd:neuron":
                GetComponent<Renderer>().material = Resources.Load<Material>("BrainClasses/Concept_Neuron");
                break;
            case "lbd:gene":
                GetComponent<Renderer>().material = Resources.Load<Material>("BrainClasses/Concept_Gene");
                break;
        }
    }
}