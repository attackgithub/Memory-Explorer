using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.Address
{
    public class AddressRecord
    {
        public uint Flags;
        public bool IsSoftware;
        public ulong PhysicalAddress;
        public uint Size;
        public ulong VirtualAddress;

        public AddressRecord()
        {
        }
    }
}
