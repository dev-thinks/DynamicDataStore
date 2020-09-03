using System.Collections.Generic;

namespace DynamicDataStore.Core.Model
{
    public class CustomList<T> : List<T>
    {
        public IEnumerable<dynamic> AsDynamic()
        {
            foreach (var obj in this) yield return obj;
        }
    }
}
