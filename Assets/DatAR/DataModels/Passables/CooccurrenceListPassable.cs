using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using DatAR.DataModels.Resources;
using Newtonsoft.Json;

namespace DatAR.DataModels.Passables
{
    public sealed class CooccurrenceListPassable : BaseResourceListPassable
    {
        public CooccurrenceListPassable(DynamicResource concept, DynamicResource withClass, List<CooccurrenceResource> resources)
        {
            Concept = concept;
            Resources = resources;
            Class = withClass;
            isMakingComparison = false;
            OnDeserializedMethod(new StreamingContext());
        }
    
        [JsonProperty("datar:concept")]
        public DynamicResource Concept { get; set; }
        
        [JsonProperty("datar:class")]
        public DynamicResource Class { get; set; }

        [JsonProperty("datar:resources")]
        public List<CooccurrenceResource> Resources { get; set; }

        public bool isMakingComparison { get; set; }

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            Id = ComputeBlankNodeId(JsonConvert.SerializeObject(Resources));
            ResourceTypes = Resources.SelectMany(resource => resource.Types ?? Enumerable.Empty<string>()).Distinct().ToList();
        }
    }
}