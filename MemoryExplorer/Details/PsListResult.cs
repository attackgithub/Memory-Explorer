using MemoryExplorer.Artifacts;
using MemoryExplorer.Processes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.Details
{    
    public class PsListResult
    {
        private readonly string _name;
        private readonly string _pid;
        private readonly string _parent;
        private readonly string _dtb;
        private readonly string _startTime;
        private readonly string _exitTime;
        private readonly string _session;
        private readonly string _m1;
        private readonly string _m2;
        private readonly string _m3;
        private readonly string _m4;
        private readonly string _eprocessVirtualAddress;
        private readonly string _eprocessPhysicalAddress;


        public PsListResult(ProcessInfo info)
        {
            _name = info.ProcessName;
            _pid = info.Pid.ToString();
            _parent = info.ParentPid.ToString();
            _dtb = "0x" + info.Dtb.ToString("X08").ToLower(); ;
            if (info.StartTime.Ticks == 0)
                _startTime = "---";
            else
                _startTime = info.StartTime.ToString();
            if (info.ExitTime.Ticks == 0)
                _exitTime = "---";
            else
                _exitTime = info.ExitTime.ToString();
            _m1 = info.FoundByMethod1 ? "X" : ".";
            _m2 = info.FoundByMethod2 ? "X" : ".";
            _m3 = info.FoundByMethod3 ? "X" : ".";
            _m4 = info.FoundByMethod4 ? "X" : ".";
            _session = info.Session.ToString("X08");
            _eprocessVirtualAddress = "0x" + info.VirtualAddress.ToString("X8").ToLower();
            _eprocessPhysicalAddress = "0x" + info.PhysicalAddress.ToString("X").ToLower();

        }
        public string Name { get { return _name; } }
        public string Pid { get { return _pid; } }
        public string Parent { get { return _parent; } }
        public string Dtb { get { return _dtb; } }
        public string StartTime { get { return _startTime; } }
        public string ExitTime { get { return _exitTime; } }
        public string Session { get { return _session; } }
        public string M1 { get { return _m1; } }
        public string M2 { get { return _m2; } }
        public string M3 { get { return _m3; } }
        public string M4 { get { return _m4; } }
        public string EprocessVirtualAddress { get { return _eprocessVirtualAddress; } }
        public string EprocessPhysicalAddress { get { return _eprocessPhysicalAddress; } }

    }
}
