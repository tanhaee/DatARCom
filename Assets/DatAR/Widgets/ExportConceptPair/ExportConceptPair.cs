using System;
using Microsoft.MixedReality.Toolkit.Utilities;
using Newtonsoft.Json;
using UniRx;
using UnityEngine;

namespace DatAR.Widgets.ExportConceptPair
{
    public class ExportConceptPair : MonoBehaviour, IQueryState
    {
        public BehaviorSubject<QueryState> IsLoading { get; }
        public string ErrorMessage { get; private set; }
        
        [SerializeField] private Receptacle diseaseReceptacle;
        [SerializeField] private Receptacle regionReceptacle;

        private GameObject _services;
        [SerializeField] private GridObjectCollection grid;
        public TMPro.TextMeshPro sentencePrefab;

        //private MessageBrokerService _backendSender;

        public ExportConceptPair()
        {
            IsLoading = new BehaviorSubject<QueryState>(QueryState.IsEmpty);
        }

        private void Awake()
        {
            _services = GameObject.Find("Services");
            //_backendSender = services.GetComponent<MessageBrokerService>();
        
            diseaseReceptacle.slottedResourceContainerHasChanged.Subscribe(currentBatchId => RetrieveSentences());
        
            regionReceptacle.slottedResourceContainerHasChanged.Subscribe(currentBatchId => RetrieveSentences());
        }

        public async void RetrieveSentences()
        {
            //IsLoading.OnNext(QueryState.IsEmpty);
            if (diseaseReceptacle.SlottedResourceContainer == null
                || diseaseReceptacle.SlottedResourceContainer.Resource == null
                || regionReceptacle.SlottedResourceContainer == null
                || regionReceptacle.SlottedResourceContainer.Resource == null)
            {
                return;
            }

            try
            {
                var sparqlService = _services.GetComponent<SparqlService>();
                var data = await sparqlService.GetCooccurrenceSentences(
                    regionReceptacle.SlottedResourceContainer.Resource.Id,
                    diseaseReceptacle.SlottedResourceContainer.Resource.Id);
                foreach(Transform child in grid.transform)
                {
                    Destroy(child.gameObject);
                }
                foreach(var sentence in data)
                {
                    var newSentenceObj = Instantiate(sentencePrefab, grid.transform);
                    newSentenceObj.text = sentence.Sentence;
                }
                grid.UpdateCollection();
            }
            catch (Exception e)
            {
                ErrorMessage = e.Message;
                Debug.LogError(e);
                IsLoading.OnNext(QueryState.HasError);
            }
        }

        //async private void SendResources()
        //{
        //    if (diseaseReceptacle.SlottedResourceContainer == null
        //        || diseaseReceptacle.SlottedResourceContainer.Resource == null
        //        || regionReceptacle.SlottedResourceContainer == null
        //        || regionReceptacle.SlottedResourceContainer.Resource == null)
        //    {
        //        return;
        //    }

        //    var payloadDisease = JsonConvert.SerializeObject(
        //        diseaseReceptacle.SlottedResourceContainer.Resource,
        //        SparqlService.JsonSerializerSettings);
        //    var payloadRegion = JsonConvert.SerializeObject(
        //        regionReceptacle.SlottedResourceContainer.Resource,
        //        SparqlService.JsonSerializerSettings);

        //    var payloadCombined =
        //        "{ " + 
        //        "\"@type\": \"datar:ConceptPair\"" + "," + 
        //        "\"datar:firstConcept\": " + payloadRegion + "," +
        //        "\"datar:secondConcept\": " + payloadDisease +
        //        "}";

        //    IsLoading.OnNext(QueryState.IsLoading);
        //    try
        //    {
        //        _backendSender.Send(payloadCombined);
        //        await UniTask.Delay(TimeSpan.FromSeconds(1));
        //        IsLoading.OnNext(QueryState.HasLoaded);

        //    }
        //    catch (Exception e)
        //    {
        //        IsLoading.OnNext(QueryState.HasError);
        //    }
        //}
    }
}