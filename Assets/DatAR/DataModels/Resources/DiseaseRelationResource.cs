using DatAR.DataModels.Misc;
using DatAR.DataModels.Resources;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DatAR.DataModels.Resources
{
    public sealed class DiseaseRelationResource : DynamicResource
    {
        public DiseaseRelationResource(List<string> types, int appearTimes, DynamicResource concept, DynamicResource disease)
        {
            Types = types;
            AppearTimes = appearTimes;
            Concept = concept;
            Disease = disease;
        }

        public object this[string propertyName]
        {
            get => GetType().GetProperty(propertyName)?.GetValue(this, null);
            set => GetType().GetProperty(propertyName)?.SetValue(this, value, null);
        }

        [JsonProperty("datar:appearTimes")]
        public int AppearTimes { get; set; }

        [JsonProperty("datar:concept")]
        public DynamicResource Concept { get; set; }

        [JsonProperty("datar:disease")]
        public DynamicResource Disease { get; set; }

        //[OnDeserializing]
        //internal void OnDeserializingMethod(StreamingContext context)
        //{
        //    if(context.Context)
        //}

        [OnDeserialized]
        internal new void OnDeserializedMethod(StreamingContext context)
        {
            base.OnDeserializedMethod(context);
            //FilterSelectionState = FilterSelectionStateType.InRange;
        }
    }
}
