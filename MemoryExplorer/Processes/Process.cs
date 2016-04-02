using MemoryExplorer.Address;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.Processes
{
    public class ProcessInfo
    {
        public AddressBase AddressSpace;
        public ulong Pid;
        public string ProcessName;
    }
}
