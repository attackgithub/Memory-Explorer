using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.Address
{
    public class MemoryMap
    {
        public ulong EndAddress;
        public List<AddressRecord> MemoryRecords;
        public HashSet<ulong> P4Tables;
        public HashSet<ulong> PdeTables;
        public HashSet<ulong> PdpteTables;
        public HashSet<ulong> PteTables;
        public ulong StartAddress;        
    }
}
