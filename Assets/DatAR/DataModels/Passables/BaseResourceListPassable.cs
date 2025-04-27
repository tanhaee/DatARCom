using System.Collections.Generic;
using DatAR.DataModels.Resources;
using Newtonsoft.Json;

namespace DatAR.DataModels.Passables
{
    public abstract class BaseResourceListPassable : Resource
    {
        [JsonProperty("datar:resourceTypes")]
        public List<string> ResourceTypes { get; set; }
    }
}