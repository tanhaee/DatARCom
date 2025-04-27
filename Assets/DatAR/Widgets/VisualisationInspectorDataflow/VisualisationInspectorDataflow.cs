using System.Text;
using DatAR.DataModels.Misc;
using DatAR.DataModels.Passables;
using DatAR.DataModels.Resources;
using TMPro;
using UniRx;
using UnityEngine;

namespace DatAR.Widgets.VisualisationInspectorDataflow
{
    public class VisualisationInspectorDataflow : MonoBehaviour
    {
        [SerializeField] private DataflowInlet dataReceiver;

        [SerializeField] private TextMeshPro summaryText;
        [SerializeField] private TextMeshPro dataText;

        [SerializeField] private bool newLinePerConcept;

        void Start()
        {
            Init();
        }

        private void Init()
        {
            dataReceiver.data.Skip(1).Subscribe(rawPassable => {
                // Check if compatible type
                if (rawPassable is Passable<CooccurrenceListPassable>)
                {
                    RenderCooccurrenceData((rawPassable as Passable<CooccurrenceListPassable>).data);
                }
                else
                {
                    if (rawPassable.GetType().ToString() == "DatAR.DataModels.Passables.Passable")
                    {
                        summaryText.text = $"Received no items. This data stream is empty.";
                    }
                    else
                    {
                        Debug.LogWarning($"List received incompatible data type: {rawPassable?.GetType()}");
                    }
                    return;
                }
            });
        }

        private void RenderCooccurrenceData(CooccurrenceListPassable data)
        {
            var itemsInFilterRange = data.Resources.FindAll(resource =>
                resource.FilterSelectionState == FilterSelectionStateType.InRange);
            var itemsOutFilterRange = data.Resources.FindAll(resource =>
                resource.FilterSelectionState == FilterSelectionStateType.OutRange);

            itemsInFilterRange.Sort(CooccurrenceSort);
            itemsOutFilterRange.Sort(CooccurrenceSort);
            
            summaryText.text = $"Received {data.Resources.Count} {data.Concept.Label}–{data.Class.Label} co-occurrences\n" +
                               $"<size=-10>{itemsInFilterRange.Count} in filter range; {itemsOutFilterRange.Count} out of filter range</size>";

            StringBuilder dataPreview = new StringBuilder();

            itemsInFilterRange.ForEach((item) =>
            {
                dataPreview.Append($"<b><color=red>{data.Concept.Label} > {item.ClassItem.Label}  (x{item.Cooccurrences})</color></b>");

                if (newLinePerConcept)
                {
                    dataPreview.Append("\n");
                }
            });
            itemsOutFilterRange.ForEach((item) =>
            {
                dataPreview.Append($"<b><color=yellow>{data.Concept.Label} > {item.ClassItem.Label}  (x{item.Cooccurrences})</color></b>");

                if (newLinePerConcept)
                {
                    dataPreview.Append("\n");
                }
            });
            
            dataText.text = dataPreview.ToString();
        }
        
        private int CooccurrenceSort(CooccurrenceResource a, CooccurrenceResource b) {
            if (a.Cooccurrences == b.Cooccurrences)
            {
                return 0;
            }
            if (a.Cooccurrences < b.Cooccurrences)
            {
                return 1;
            }
            return -1;
        }
    }
}
