using MemoryExplorer.Info;
using MemoryExplorer.ModelObjects;
using System.Collections.Generic;

namespace MemoryExplorer.Details
{
    public class KvpResult
    {
        private readonly string _name;
        private readonly string _value;
        private readonly object _object = new object();

        public KvpResult(KeyValuePair<string, InfoHelper> record)
        {
            _name = record.Key;
            _value = record.Value.Name;
            _object = record.Value;
        }
        public KvpResult(ObjectTypeRecord record)
        {
            _name = record.Index.ToString();
            _value = record.Name;
            _object = record;
        }

        public string InfoKey { get { return _name; } }
        public string InfoValue { get { return _value; } }
        public object Helper { get { return _object; } }


    }
}
