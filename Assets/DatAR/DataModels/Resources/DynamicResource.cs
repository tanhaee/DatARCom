using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using DatAR.Auxiliary.JsonUtil;
using Newtonsoft.Json;

namespace DatAR.DataModels.Resources
{
    /**
     * If data item is NOT managed by C# type system, use this interface to build new implementations.
     * This is true for stand-alone Resources, which are part of the LOD cloud (and follow the open-world assumption).
     */
    public interface IDynamicResource : IResource
    {
        List<string> Types { get; set; }
        string Label { get; set; }
    }

    public class DynamicResource : Resource, IDynamicResource
    {
        public DynamicResource(string id, List<string> types)
        {
            Id = id;
            Types = types ?? new List<string>();
            OnDeserializedMethod(new StreamingContext());
        }

        protected DynamicResource()
        {
            // Doesn't do anything.
            // Required to exist for derivative classes and their constructor.
        }

        [JsonProperty("@type")]
        [JsonConverter(typeof(SingleOrArrayConverter<string>))]
        public List<string> Types { get; set; } = new List<string>();

        [JsonProperty("rdfs:label")] public string Label { get; set; }

        public IEnumerable<DynamicResource> SplitTypes()
        {
            return Types.Select(type => new DynamicResource(Id, new List<string>{ type }));
        }

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            if (String.IsNullOrEmpty(Label))
            {
                // If URL instead of shorthand: Regex.Match(Id, @"^.*[\/#](.*)$").Groups[1].Value;
                Label = Id.Split(':').Last();
            }
        
            // Always clean up ugly labels, even if provided by source
            // TODO: catch more url-encoded cases
            Label = Label
                .Replace("_", " ")
                .Replace("&#39;", "'");
        }
    }
}