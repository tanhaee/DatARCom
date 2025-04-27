using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatAR.Auxiliary.SharedScripts
{
    public static class Util
    {
		public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
		{
			if (source == null)
			{
				throw new ArgumentException("Argument cannot be null.", "source");
			}

			foreach (T value in source)
			{
				action(value);
			}
		}

		public static float Map(float value, float from1, float from2, float to1, float to2)
        {
			return (value - from1) * (to2 - to1) / (from2 - from1) + to1;
        }
	}
}
