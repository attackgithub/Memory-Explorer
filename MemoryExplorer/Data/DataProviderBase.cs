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

        public DataProviderBase(DataModel data)
        {
            _data = data;
        }
        protected abstract byte[] ReadMemoryPage(ulong address);
        public abstract Dictionary<string, object> GetInformation();
        public abstract byte[] ReadMemory(ulong startAddress, uint pageCount);

        public ulong ImageLength { get; set; }

    }
}
