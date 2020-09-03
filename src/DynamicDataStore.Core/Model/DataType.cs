using System;

namespace DynamicDataStore.Core.Model
{
    public struct DataType
    {
        public string DbType;
        public Type SystemType;

        public override string ToString()
        {
            return $"({DbType}) {SystemType.Name}";
        }
    }
}
