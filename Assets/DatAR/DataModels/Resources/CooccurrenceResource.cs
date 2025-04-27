using System.Collections.Generic;
using System.Runtime.Serialization;
using DatAR.DataModels.Misc;
using Newtonsoft.Json;

namespace DatAR.DataModels.Resources
{
    public sealed class CooccurrenceResource : DynamicResource
    {
        public CooccurrenceResource(List<string> types, double cooccurrences, DynamicResource classItem, double probabilityConceptGivenClassItem, double probabilityClassItemGivenConcept, FilterSelectionStateType filterSelectionState)
        {
            Types = types;
            Cooccurrences = cooccurrences;
            ClassItem = classItem;
            ProbabilityConceptGivenClassItem = probabilityConceptGivenClassItem;
            ProbabilityClassItemGivenConcept = probabilityClassItemGivenConcept;
            FilterSelectionState = filterSelectionState;
        }
    
        // Allows dynamic reading and writing of object properties
        // Source: https://stackoverflow.com/a/24919811
        public object this[string propertyName]
        {
            get => GetType().GetProperty(propertyName)?.GetValue(this, null);
            set => GetType().GetProperty(propertyName)?.SetValue(this, value, null);
        }

        [JsonProperty("datar:cooccurrences")]
        public double Cooccurrences { get; set; }

        [JsonProperty("datar:classItem")]
        public DynamicResource ClassItem { get; set; }
    
        [JsonProperty("datar:conceptMentionedWhenClassItemMentioned")]
        public double ProbabilityConceptGivenClassItem { get; set; }
        
        [JsonProperty("datar:classItemMentionedWhenConceptItemMentioned")]
        public double ProbabilityClassItemGivenConcept { get; set; }

        [JsonProperty("datar:filterSelectionState")]
        public FilterSelectionStateType FilterSelectionState { get; set; }
    
        [OnDeserialized]
        internal new void OnDeserializedMethod(StreamingContext context)
        {
            base.OnDeserializedMethod(context);
            FilterSelectionState = FilterSelectionStateType.InRange;
        }
    }
}
