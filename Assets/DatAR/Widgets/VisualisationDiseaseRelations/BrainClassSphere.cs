using DatAR.DataModels.Resources;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BrainClassSphere : MonoBehaviour
{
    public TMPro.TextMeshPro classText;
    public TMPro.TextMeshPro disease1AppearTimesText;
    public TMPro.TextMeshPro disease2AppearTimesText;
    public BrainConceptSphere conceptPrefab;
    public Transform conceptsParent;
    public GameObject BackButton;

    [HideInInspector] public BrainClass brainClass { get; private set; }
    [HideInInspector] public int Disease1AppearTimes { get; private set; }
    [HideInInspector] public int Disease2AppearTimes { get; private set; }

    private List<FormattedBrainConcept> Cooccurrences;

    public void PopulateData(string brainClassId, int disease1AppearTimes, int disease2AppearTimes)
    {
        switch (brainClassId)
        {
            case "lbd:region":
                brainClass = BrainClass.REGION;
                classText.text = "Brain Region";
                GetComponent<Renderer>().material = Resources.Load<Material>("BrainClasses/Concept_BrainRegion");
                break;
            case "lbd:transmitter":
                brainClass = BrainClass.TRANSMITTER;
                classText.text = "Transmitter";
                GetComponent<Renderer>().material = Resources.Load<Material>("BrainClasses/Concept_Neurotransmitter");
                break;
            case "lbd:function":
                brainClass = BrainClass.FUNCTION;
                classText.text = "Cognitive Function";
                GetComponent<Renderer>().material = Resources.Load<Material>("BrainClasses/Concept_CognitiveFunction");
                break;
            case "lbd:protein":
                brainClass = BrainClass.PROTEIN;
                classText.text = "Protein";
                GetComponent<Renderer>().material = Resources.Load<Material>("BrainClasses/Concept_Protein");
                break;
            case "lbd:neuron":
                brainClass = BrainClass.NEURON;
                classText.text = "Neuron";
                GetComponent<Renderer>().material = Resources.Load<Material>("BrainClasses/Concept_Neuron");
                break;
            case "lbd:gene":
                brainClass = BrainClass.GENE;
                classText.text = "Gene";
                GetComponent<Renderer>().material = Resources.Load<Material>("BrainClasses/Concept_Gene");
                break;
        }

        Disease1AppearTimes = disease1AppearTimes;
        Disease2AppearTimes = disease2AppearTimes;

        disease1AppearTimesText.text = Disease1AppearTimes.ToString();
        disease2AppearTimesText.text = Disease2AppearTimes.ToString();
    }

    public void SetCooccurrences(IEnumerable<DiseaseRelationResource> cooccurrences, string disease1Id, string disease2Id)
    {
        Cooccurrences = cooccurrences.GroupBy(x => x.Concept, (key, value) => new FormattedBrainConcept
        {
            Concept = key,
            Disease1AppearTimes = value.Where(x => x.Disease.Id == disease1Id).Sum(x => x.AppearTimes),
            Disease2AppearTimes = value.Where(x => x.Disease.Id == disease2Id).Sum(x => x.AppearTimes)
        }).OrderByDescending(x => x.Disease1AppearTimes + x.Disease2AppearTimes).ToList();
    }
    
    public void DisplayConcepts()
    {
        foreach (Transform concept in conceptsParent.transform)
        {
            Destroy(concept.gameObject);
        }

        int limit = 5;
        for(int i = 0; i < limit; i++)
        {
            if(i > Cooccurrences.Count)
            {
                break;
            }

            var conceptObject = Instantiate(conceptPrefab, conceptsParent);
            //conceptObject.PopulateData(Cooccurrences[i].Concept, Cooccurrences[i].Disease1AppearTimes, Cooccurrences[i].Disease2AppearTimes);
            conceptObject.transform.localPosition -= new Vector3(0, i * 2, 0);
            conceptObject.GetComponent<MeshRenderer>().material = GetComponent<MeshRenderer>().material;
        }

        conceptsParent.gameObject.SetActive(true);
        BackButton.SetActive(true);
        transform.parent.gameObject.SetActive(false);
    }

    internal class FormattedBrainConcept
    {
        public DynamicResource Concept { get; set; }
        public int Disease1AppearTimes { get; set; }
        public int Disease2AppearTimes { get; set; }
    }
}



public enum BrainClass
{
    REGION,
    FUNCTION,
    GENE,
    NEURON,
    TRANSMITTER,
    PROTEIN
}