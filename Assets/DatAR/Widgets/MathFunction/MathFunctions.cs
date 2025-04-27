using System.Collections;
using System.Text;

using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using TMPro;
using UniRx;
using UnityEngine.Serialization;
using Cysharp.Threading.Tasks;
using DatAR.Auxiliary.SharedScripts;
using DatAR.DataModels.Misc;
using DatAR.DataModels.Passables;
using DatAR.DataModels.Resources;
using Newtonsoft.Json;

namespace DatAR.Widgets.MathFunctions
{
    public class MathFunctions : MonoBehaviour//, //IQueryState
    {
        public BehaviorSubject<QueryState> IsLoading { get; }
        public string ErrorMessage { get; private set; }

        public DataflowInletL dataReceiver;
        public DataflowOutletL dataSender;
        public DataflowInletR dataReceiverR;
        public DataflowOutletR dataSenderR;

        //private Tuple<float, Passable> _awaitingPassable;

        //public Toggle _SelectedToggle0; //intersection
        //public Toggle _SelectedToggle1; //union
        //public Toggle _SelectedToggle2; //difference

        //bool tgvalue = false;

        //[SerializeField] private TextMeshPro summaryText;
        //[SerializeField] private TextMeshPro dataText;
        public Button _IntersectButton;
        public Button _UnionButton;
        public Button _DiffButton;
        private int _fun;
        //public Pressa
        //[SerializeField] Button _Union;
        //[SerializeField] Button _Difference;

        [SerializeField] private TextMeshPro summaryTextMath;
        [SerializeField] private TextMeshPro dataTextMath;

        [SerializeField] private bool newLinePerConceptMath;

        //public BehaviorSubject<Passable> CoList1;
        //public BehaviorSubject<Passable> CoList2;
        //public BehaviorSubject<Passable> UnionResult;
        //public BehaviorSubject<Passable> intersectionResult;
        //public BehaviorSubject<Passable> DifferenceResult1;
        //private Passable<CooccurrenceListPassable> _result;


        public void Intersection()
        {
            
            _fun = 1;
            Init();
        }
        public void Union()
        {
            _fun = 2;
            Init();
        }
        public void Difference()
        {
            _fun = 3;
            Init();
        }
        // Start is called before the first frame update
        private void Start()
        {
            //var ItemsRight = passable.dataR.Resources.ToList();
            //var ItemsLeft = passable.dataL.Resources.ToList();
            
           // Init();
            



        }
        

        private void Init()
        {
            
                dataReceiver.dataL.Subscribe(rawPassableL =>
                {
                // Check if compatible type
                if (rawPassableL is Passable<CooccurrenceListPassable>)
                    {
                        dataReceiverR.dataR.Subscribe(rawPassableR =>
                        {
                        // Check if compatible type
                        if (rawPassableR is Passable<CooccurrenceListPassable>)

                                   RenderMathFunction((rawPassableL as Passable<CooccurrenceListPassable>).data, (rawPassableR as Passable<CooccurrenceListPassable>).data);
                        });
                    }

                });

            
        }
      

        public void RenderMathFunction(CooccurrenceListPassable dataFL, CooccurrenceListPassable dataSR)
        {

           
            Passable<CooccurrenceListPassable> passable = new Passable<CooccurrenceListPassable>();
            Passable<CooccurrenceListPassable> passableR = new Passable<CooccurrenceListPassable>();
            List<CooccurrenceResource> matchingCooccurrences = new List<CooccurrenceResource>();
            List<CooccurrenceResource> matchingCooccurrencesRight = new List<CooccurrenceResource>();

            var itemsInWigdetL = dataFL.Resources;
            
            var ClassL = dataFL.Class;
            var ConceptL = dataFL.Concept;

            var itemsInWidgetR = dataSR.Resources;
            
            var ClassR = dataSR.Class;
            var ConceptR = dataSR.Concept;
            //var Result = result.data.Resources;

            //IntersectionValueChangedOccour();
            //UnionValueChangedOccour();
            //DifferenceValueChangedOccour();

            if (_fun == 1)
            { IntersectionValueChangedOccour(); }
            if (_fun == 2)
            { UnionValueChangedOccour(); }
            if (_fun == 3)
            { DifferenceValueChangedOccour(); }
            //if (_fun == 4)
            //{ }
            //_IntersectButton.onClick.AddListener(delegate { IntersectionValueChangedOccour(); });
            //_UnionButton.onClick.AddListener(delegate { UnionValueChangedOccour(); });
            //_DiffButton.onClick.AddListener(delegate { DifferenceValueChangedOccour(); });

            
            //_SelectedToggle0.onValueChanged.AddListener(delegate
            //{
            //    IntersectionValueChangedOccour(_SelectedToggle0);
            //});
            //_SelectedToggle1.onValueChanged.AddListener(delegate
            //{
            //    UnionValueChangedOccour(_SelectedToggle1);
            //});

            //_SelectedToggle2.onValueChanged.AddListener(delegate
            //{
            //    DifferenceValueChangedOccour(_SelectedToggle2);
            //});


            void IntersectionValueChangedOccour()
            {
               

                itemsInWigdetL.ForEach((itemL) =>
                {
                    itemsInWidgetR.ForEach((itemR) =>
                    {
                        if (itemL.ClassItem.Label.Equals(itemR.ClassItem.Label))
                        {
                            matchingCooccurrences.Add(itemR);
                            
                        }
                    });
                });

                
                CooccurrenceListPassable newFormat = new CooccurrenceListPassable(ClassL, ConceptL, matchingCooccurrences);
                newFormat.isMakingComparison = true;
                passable.data = newFormat;
                CooccurrenceListPassable newFormatR = new CooccurrenceListPassable(ClassR, ConceptR, matchingCooccurrences);
                newFormatR.isMakingComparison = true;
                passableR.data = newFormatR;
                dataSender.Send(passable);
                dataSenderR.Send(passableR);

                summaryTextMath.text = $"Received {newFormat.Resources.Count} shaired–{newFormat.Concept.Label}\n";

                StringBuilder dataPreviewMath = new StringBuilder();

                newFormat.Resources.ForEach((item) =>
                {
                    dataPreviewMath.Append($"<b><color=yellow>{item.ClassItem.Label} </color></b>");

                    if (newLinePerConceptMath)
                    {
                        dataPreviewMath.Append("\n");
                    }
                });
                

                dataTextMath.text = dataPreviewMath.ToString();


            }

            void UnionValueChangedOccour()
            {
                //itemsInWigdetL.ForEach((itemL) =>
                //{
                //    itemsInWidgetR.ForEach((itemR) =>
                //    {

                //            matchingCooccurrences.Add(itemL);
                //            matchingCooccurrences.Add(itemR);


                //    });
                //});
                itemsInWigdetL.ForEach((itemL) =>
                {
                    matchingCooccurrences.Add(itemL);
                    itemsInWidgetR.ForEach((itemR) =>
                    {

                        if (itemL.ClassItem.Label.Equals(itemR.ClassItem.Label))
                        {
                            matchingCooccurrences.Remove(itemL);
                        }

                    });
                    //matchingCooccurrences.Add(itemL);
                });

                itemsInWidgetR.ForEach((itemR) =>
                {
                    matchingCooccurrences.Add(itemR);
                });

                CooccurrenceListPassable newFormat = new CooccurrenceListPassable(ClassL, ConceptL, matchingCooccurrences);
                newFormat.isMakingComparison = true;
                passable.data = newFormat;
                CooccurrenceListPassable newFormatR = new CooccurrenceListPassable(ClassR, ConceptR, matchingCooccurrences);
                newFormatR.isMakingComparison = true;
                passableR.data = newFormatR;
                dataSender.Send(passable);
                dataSenderR.Send(passableR);

                summaryTextMath.text = $"Received {newFormat.Resources.Count} {newFormat.Concept.Label}\n";

                StringBuilder dataPreviewMath = new StringBuilder();

                newFormat.Resources.ForEach((item) =>
                {
                    dataPreviewMath.Append($"<b><color=red>{item.ClassItem.Label} </color></b>");

                    if (newLinePerConceptMath)
                    {
                        dataPreviewMath.Append("\n");
                    }
                });


                dataTextMath.text = dataPreviewMath.ToString();

            }

            void DifferenceValueChangedOccour()
            {

                
                itemsInWigdetL.ForEach((itemL) =>
                {
                    matchingCooccurrences.Add(itemL);
                    itemsInWidgetR.ForEach((itemR) =>
                    {

                        if (itemL.ClassItem.Label.Equals(itemR.ClassItem.Label))
                        {
                            matchingCooccurrences.Remove(itemL);
                        }

                    });
                    
                });


                CooccurrenceListPassable newFormat = new CooccurrenceListPassable(ClassL, ConceptL, matchingCooccurrences);
                
                newFormat.isMakingComparison = true;
                passable.data = newFormat;
                dataSender.Send(passable);

                itemsInWidgetR.ForEach((itemR) =>
                {
                    matchingCooccurrencesRight.Add(itemR);
                    itemsInWigdetL.ForEach((itemL) =>
                    {

                        if (itemR.ClassItem.Label.Equals(itemL.ClassItem.Label))
                        {
                            matchingCooccurrencesRight.Remove(itemR);
                        }

                    });
                  
                });


                CooccurrenceListPassable newFormatRight = new CooccurrenceListPassable(ClassR, ConceptR, matchingCooccurrencesRight);

                newFormatRight.isMakingComparison = true;
                passableR.data = newFormatRight;
               
                dataSenderR.Send(passableR);

                summaryTextMath.text = $"Received {newFormat.Resources.Count} different {newFormatRight.Concept.Label} in Brain Model 1 \n " +
                    $" and {newFormatRight.Resources.Count} different {newFormatRight.Concept.Label} in Brain Model 2\n";

                StringBuilder dataPreviewMath = new StringBuilder();

                newFormat.Resources.ForEach((item) =>
                {
                    dataPreviewMath.Append($"<b><color=green>{item.ClassItem.Label} </color></b>");

                    if (newLinePerConceptMath)
                    {
                        dataPreviewMath.Append("\n");
                    }
                });
                newFormatRight.Resources.ForEach((item) =>
                {
                    dataPreviewMath.Append($"<b><color=white>{item.ClassItem.Label} </color></b>");

                    if (newLinePerConceptMath)
                    {
                        dataPreviewMath.Append("\n");
                    }
                });


                dataTextMath.text = dataPreviewMath.ToString();


                
            }
            //void ShowText()
            //{

            //}

            //void IntersectionValueChangedOccour()
            //{
            //    //if (tgvalue.isOn)
            //    //{

            //    itemsInWigdetL.ForEach((item) =>
            //    {
            //        itemsInWidgetR.ForEach((item) =>
            //        {
            //            if (dataFL.Concept.Label == dataSR.Concept.Label)

            //                Result.ForEach((item) => { result.data.Concept.Label = dataFL.Concept.Label; });
            //        });
            //    });
            //    dataSenderR.Send(result);
            //    dataSender.Send(result);
            //    //}
            //}

            //void UnionValueChangedOccour(Toggle tgvalue)
            //{
            //    if (tgvalue.isOn)
            //    {

            //        itemsInWigdetL.ForEach((item) =>
            //        {
            //            Result.ForEach((item) =>
            //            {
            //                result.data.Concept.Label = dataFL.Concept.Label;
            //            });
            //        });
            //        itemsInWidgetR.ForEach((item) =>
            //        {
            //            Result.ForEach((item) =>
            //            {
            //                result.data.Concept.Label = dataFL.Concept.Label;
            //            });
            //        });
            //        dataSenderR.Send(result);
            //        dataSender.Send(result);

            //    }
            //}
            //void DifferenceValueChangedOccour(Toggle tgvalue)
            //{
            //    if (tgvalue.isOn)
            //    {

            //        itemsInWigdetL.ForEach((item) =>
            //        {
            //            itemsInWidgetR.ForEach((item) =>
            //            {
            //                if (dataFL.Concept.Label != dataSR.Concept.Label)

            //                    Result.ForEach((item) => { result.data.Concept.Label = dataFL.Concept.Label; });
            //            });
            //        });
            //        dataSenderR.Send(result);
            //        dataSender.Send(result);
            //    }
            //}


        }

    }
}

//      itemsInFilterRange.Sort(CooccurrenceSort);
//      itemsOutFilterRange.Sort(CooccurrenceSort);

//      summaryText.text = $"Received {data.Resources.Count} {data.Concept.Label}–{data.Class.Label} co-occurrences\n" +
//                         $"<size=-10>{itemsInFilterRange.Count} in filter range; {itemsOutFilterRange.Count} out of filter range</size>";

//      StringBuilder dataPreview = new StringBuilder();

//      itemsInFilterRange.ForEach((item) =>
//      {
//          dataPreview.Append($"<b><color=red>{data.Concept.Label} > {item.ClassItem.Label}  (x{item.Cooccurrences})</color></b>");

//          if (newLinePerConcept)
//          {
//              dataPreview.Append("\n");
//          }
//      });
//      itemsOutFilterRange.ForEach((item) =>
//      {
//          dataPreview.Append($"<b><color=yellow>{data.Concept.Label} > {item.ClassItem.Label}  (x{item.Cooccurrences})</color></b>");

//          if (newLinePerConcept)
//          {
//              dataPreview.Append("\n");
//          }
//      });

//      dataText.text = dataPreview.ToString();
//  }



//  /// <summary>
//  /// ///////////////////////////
//  /// </summary>

//  public void Init()
//  {

//      dataReceiver.dataL.Subscribe(MathDataL);
//      dataReceiverR.dataR.Subscribe(MathDataR);

//      void MathDataL(Passable rawPassable)
//      {
//          CoList1 = rawPassable;
//      }

//      void MathDataR(Passable rawPassable)
//      {
//          CoList2 = rawPassable;
//      }

//      MathData(CoList1, CoList2);
//  }




//  //private void RenderCooccurrenceData(CooccurrenceListPassable data1, CooccurrenceListPassable data2)
//  //{

////  }





//  private void MathData(Passable rawPassable, Passable rawRPassable)
//  {



//      _SelectedToggle0.onValueChanged.AddListener(delegate
//      {
//          IntersectionValueChangedOccour(_SelectedToggle0);
//      });
//      _SelectedToggle1.onValueChanged.AddListener(delegate
//      {
//          UnionValueChangedOccour(_SelectedToggle1);
//      });

//      _SelectedToggle2.onValueChanged.AddListener(delegate
//      {
//          DifferenceValueChangedOccour(_SelectedToggle2);
//      });



//      void IntersectionValueChangedOccour(Toggle tgvalue)
//      {
//          if (tgvalue.isOn)
//          {

//              //List<DynamicResource> CoList1 = new List<DynamicResource>(), CoList2 = new List<DynamicResource>(), intersectionResult = new List<DynamicResource>();

//              intersectionResult = CoList1.AsQueryable().Intersect(CoList2).ToList();

//          }
//      }
//      void UnionValueChangedOccour(Toggle tgvalue)
//      {
//          if (tgvalue.isOn)
//          {


//              //List<DynamicResource> CoList1 = new List<DynamicResource>(), CoList2 = new List<DynamicResource>(), UnionResult = new List<DynamicResource>();

//              // type = dataReceiver.dataL.GetType();
//              // ConstructorInfo constructor = type.GetConstructor(Type.CoList1);

//             // CoList1 = dataReceiver.dataL;
//              //CoList1 = dataReceiver.dataL.ToList();
//              // CoList2 = dataReceiverR.dataR.ToList();

//              //int k = 0;
//              //for (int i = 0; i <= CoList1.Count; i++)
//              //    for (int j = 0; j <= CoList2.Count; j++)
//               //       if (CoList1[i] == CoList2[j])
//                //      {
//                          //UnionResult[K] = CoList1[i];
//                          //K++;
//                //      }

//              //  (CoList1, CoList2) = await UniTask.WhenAll(dataReceiver.dataL.ToList(), dataReceiverR.dataR.ToList());

//              //UnionResult = CoList1.Union(CoList2).ToString();
//              //let intersection = CoList1.filter(dataL => CoList2.includes(dataR));

//              //dataSender.Send(dataReceiver.dataL.AsQueryable().Intersect(dataReceiverR.dataR));
//          }
//      }
//      void DifferenceValueChangedOccour(Toggle tgvalue)
//      {
//          if (tgvalue.isOn)
//          {


//              //List<DynamicResource> CoList1 = new List<DynamicResource>(), CoList2 = new List<DynamicResource>(), DifferenceResult1 = new List<DynamicResource>(), DifferenceResult2 = new List<DynamicResource>();
//              DifferenceResult1 = CoList1.Except(CoList2).ToList();
//              DifferenceResult2 = CoList2.Except(CoList1).ToList();
//          }
//      }
//  }





