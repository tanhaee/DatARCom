using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace DatAR.DataModels.Resources
{
    /**
     * Auxiliary model to load in resources and coordinates in space (for, e.g., topic models)
     */
    public interface ICoordsResource : IDynamicResource
    {
        float CoordX { get; set; }
        float CoordY { get; set; }
        float CoordZ { get; set; }
    }

    public class CoordsResource : DynamicResource, ICoordsResource
    {
        public CoordsResource(string id, List<string> types, float x, float y, float z)
        {
            Id = id;
            Types = types;
            CoordX = x;
            CoordY = y;
            CoordZ = z;
        }

        protected CoordsResource()
        {
            // Doesn't do anything.
            // Required to exist for derivative classes and their constructor.
        }

        [JsonProperty("datar:CoordX")] public float CoordX { get; set; }
        [JsonProperty("datar:CoordY")] public float CoordY { get; set; }
        [JsonProperty("datar:CoordZ")] public float CoordZ { get; set; }
    }
}