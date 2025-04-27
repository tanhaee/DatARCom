using System.Collections.Generic;
using System.Runtime.Serialization;
using DatAR.DataModels.Resources;

namespace DatAR.DataModels.Passables
{
    public sealed class ClassListPassable : ConceptListPassable
    {
        public ClassListPassable(List<DynamicResource> resources) : base(resources)
        {
            Resources = resources;
            OnDeserializedMethod(new StreamingContext());
        }
    }
}