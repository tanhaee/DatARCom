using DatAR.DataModels.Resources;
using Newtonsoft.Json;

namespace DatAR.DataModels.Passables
{
    public abstract class BaseResourcePassable : Resource
    {
        [JsonProperty("datar:resource")]
        public DynamicResource Resource { get; set; }
    }
}