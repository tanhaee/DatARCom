using System;
using DatAR.DataModels.Passables;
using DatAR.DataModels.Resources;
using UniRx;
using UnityEngine;
using System.Collections.Generic;

namespace DatAR.Widgets.QueryCooccurrences
{
    public class QueryCooccurrences : MonoBehaviour, IQueryState
    {
        public BehaviorSubject<QueryState> IsLoading { get; }
        public string ErrorMessage { get; private set; }

        [SerializeField] private DataflowOutlet dataSender;
        private GameObject _services;
        [SerializeField] private Receptacle classReceptacle;

        [SerializeField] private Receptacle conceptReceptacle;

        private QueryCooccurrences()
        {
            IsLoading = new BehaviorSubject<QueryState>(QueryState.IsEmpty);
        }

        private void Awake()
        {
            _services = GameObject.Find("Services");

            conceptReceptacle.slottedResourceContainerHasChanged.Subscribe(currentBatchId => RetrieveCooccurrences());

            classReceptacle.slottedResourceContainerHasChanged.Subscribe(currentBatchId => RetrieveCooccurrences());
        }

        public async void RetrieveCooccurrences()
        {
            IsLoading.OnNext(QueryState.IsEmpty);
            if (conceptReceptacle.SlottedResourceContainer == null
                || conceptReceptacle.SlottedResourceContainer.Resource == null
                || classReceptacle.SlottedResourceContainer == null
                || classReceptacle.SlottedResourceContainer.Resource == null)
            {
                return;
            }

            IsLoading.OnNext(QueryState.IsLoading);

            try
            {
                var sparqlService = _services.GetComponent<SparqlService>();
                var passable = new Passable<CooccurrenceListPassable>
                {
                    data = await sparqlService.GetCooccurrenceStatementsWithConcept(
                        conceptReceptacle.SlottedResourceContainer.Resource.Id,
                        classReceptacle.SlottedResourceContainer.Resource.Id)
                };
                if (passable.data.Resources.Count < 1)
                {
                    ErrorMessage = $"No co-occurrences found between {conceptReceptacle.SlottedResourceContainer.Resource.Label} and items of type {classReceptacle.SlottedResourceContainer.Resource.Label}";
                    IsLoading.OnNext(QueryState.HasError);
                    return;
                }

                dataSender.Send(passable);
                IsLoading.OnNext(QueryState.HasLoaded);
            }
            catch (Exception e)
            {
                // Old error handling
                // if(conceptReceptacle.SlottedResourceContainer.Resource.Id == "")

                // ErrorMessage = e.Message;
                // Debug.LogError(e);
                // IsLoading.OnNext(QueryState.HasError);

                Debug.Log("Couldn't retrieve data from API. Retrieve local data instead.");

                List<CooccurrenceResource> backUp = new List<CooccurrenceResource>();

                string dataSource = "brainregion-disease(29-4-22)";
                string concept = conceptReceptacle.SlottedResourceContainer.Resource.Id;
                string className = classReceptacle.SlottedResourceContainer.Resource.Id;

                string classType = "Brain Region";
                string conceptType = "Brain Disease";
                string matchTerm = "\"disease\"";
                string searchTerm = "\"brainregion\"";

                if (className == "lbd:region")
                {
                    classType = "Brain Disease";
                    conceptType = "Brain Region";
                    matchTerm = "\"brainregion\"";
                    searchTerm = "\"disease\"";
                }

                List<Dictionary<string, object>> data = CSVReader.Read(dataSource);
                List<string> conceptTypes = new List<string>();
                conceptTypes.Add(conceptType);

                List<string> classTypes = new List<string>();
                classTypes.Add(classType);

                for (var i = 0; i < data.Count; i++)
                {
                    if (concept == data[i][searchTerm].ToString())
                    {
                        DynamicResource resource = new DynamicResource(
                            data[i][matchTerm].ToString(),
                            classTypes);

                        CooccurrenceResource cooccurrence = new CooccurrenceResource(
                            classTypes,
                            Convert.ToDouble(data[i]["\"count\""]),
                            resource,
                            0.0, // No local data
                            0.0, // No local data
                            DatAR.DataModels.Misc.FilterSelectionStateType.InRange
                            );

                        cooccurrence.Label = data[i][matchTerm].ToString();

                        backUp.Add(cooccurrence);
                    }
                }

                DynamicResource conceptResource = new DynamicResource(conceptReceptacle.SlottedResourceContainer.Resource.Id, conceptTypes);
                DynamicResource classResource = new DynamicResource(classReceptacle.SlottedResourceContainer.Resource.Id, classTypes);
                CooccurrenceListPassable newFormat = new CooccurrenceListPassable(conceptResource, classResource, backUp);

                var passable = new Passable<CooccurrenceListPassable>();
                passable.data = newFormat;

                dataSender.Send(passable);
            }
        }
    }
}