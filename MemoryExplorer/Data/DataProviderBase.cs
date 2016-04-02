using MemoryExplorer.Memory;
using MemoryExplorer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.Data
{
    public abstract class DataProviderBase
    {
        protected DataModel _data;
        protected List<MemoryRange> _memoryRangeList = new List<MemoryRange>();

        public DataProviderBase(DataModel data)
        {
            _data = data;
        }
        protected abstract byte[] ReadMemoryPage(ulong address);
        public abstract Dictionary<string, object> GetInformation();
        public abstract byte[] ReadMemory(ulong startAddress, uint pageCount);

        public ulong ImageLength { get; set; }

        public List<MemoryRange> MemoryRangeList
        {
            get
            {
                return _memoryRangeList;
            }
        }
    }
}
