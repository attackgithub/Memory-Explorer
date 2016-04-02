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
        private bool _isLive;
        private string _cacheFolder;

        public DataProviderBase(DataModel data, string cacheFolder)
        {
            _data = data;
            _cacheFolder = cacheFolder;
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

        public bool IsLive
        {
            get
            {
                return _isLive;
            }

            set
            {
                _isLive = value;
            }
        }

        public string CacheFolder
        {
            get
            {
                return _cacheFolder;
            }
        }
    }
}
