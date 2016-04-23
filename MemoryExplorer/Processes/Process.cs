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
        public ulong ParentPid;
        public string ProcessName;
        public bool FoundByMethod1 = false; // active process head
        public bool FoundByMethod2 = false; // active process head
        public bool FoundByMethod3 = false; // active process head
        public bool FoundByMethod4 = false; // active process head
        public ulong Dtb;
        public ulong Session;
        public uint ActiveThreads;
        public DateTime StartTime;
        public DateTime ExitTime;
        public ulong ObjectTableAddress;
        public ulong PhysicalAddress;
        public ulong VirtualAddress;
    }
}
