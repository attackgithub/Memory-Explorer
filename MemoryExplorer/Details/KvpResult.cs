using System.Collections.Generic;

namespace MemoryExplorer.Details
{
    public class KvpResult
    {
        private readonly string _name;
        private readonly string _value;

        public KvpResult(KeyValuePair<string, string> record)
        {
            _name = record.Key;
            _value = record.Value;
        }

        public string InfoKey { get { return _name; } }
        public string InfoValue { get { return _value; } }

    }
}
