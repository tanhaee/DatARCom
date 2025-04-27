using DatAR.DataModels.Resources;
using Newtonsoft.Json;
using System.Linq;

namespace DatAR.DataModels.Passables
{
    public class Passable
    {
    }

    public class Passable<T> : Passable where T : IResource
    {
        public T data { get; set; }

        public Passable<T> DeepCopy()
        {
            var intermediary = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<Passable<T>>(intermediary);
        }
    }
    //public static System.Collections.Generic.IEnumerable<TSource> Union<TSource>(this System.Collections.Generic.IEnumerable<TSource> first, System.Collections.Generic.IEnumerable<TSource> second);
}