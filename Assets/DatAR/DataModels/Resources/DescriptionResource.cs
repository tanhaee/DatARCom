using System;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace DatAR.DataModels.Resources
{
    /**
     * Auxiliary model to load in additional LOD descriptions.
     */
    public interface IDescriptionResource : IResource
    {
        string Description { get; set; }
    }

    public class DescriptionResource : Resource, IDescriptionResource
    {
        public DescriptionResource(string id, string description)
        {
            Id = id;
            Description = description;
        }

        protected DescriptionResource()
        {
            // Doesn't do anything.
            // Required to exist for derivative classes and their constructor.
        }

        [JsonProperty("schema:description")] public string Description { get; set; }
        
    }
}