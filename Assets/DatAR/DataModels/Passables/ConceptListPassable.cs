using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using DatAR.DataModels.Resources;
using Newtonsoft.Json;

namespace DatAR.DataModels.Passables
{
    public class ConceptListPassable : BaseResourceListPassable
    {
        public ConceptListPassable(List<DynamicResource> resources)
        {
            Resources = resources;
            OnDeserializedMethod(new StreamingContext());
        }
    
        [JsonProperty("datar:resources")]
        public List<DynamicResource> Resources { get; set; }
    
        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            Id = ComputeBlankNodeId(JsonConvert.SerializeObject(Resources));
            ResourceTypes = Resources.SelectMany(resource => resource.Types ?? Enumerable.Empty<string>()).Distinct().ToList();
        }
    }
}