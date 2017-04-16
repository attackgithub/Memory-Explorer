using MemoryExplorer.Model;
using MemoryExplorer.Profiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.Processes
{
    public class Eprocess : StructureBase
    {
        private dynamic _ep;

        public Eprocess(DataModel model, ulong physicalAddress) : base(model, 0)
        {
            _physicalAddress = physicalAddress;
            _ep = _profile.GetStructure("_EPROCESS", physicalAddress);
        }
        public dynamic dynamicObject
        {
            get { return _ep; }
        }
        public ulong DTB
        {
            get
            {
                try
                {
                    var dtb = _ep.Pcb.DirectoryTableBase;
                    if (_profile.Architecture == "I386")
                    {
                        return (ulong)(dtb[0] + (dtb[1] * 0x100000000));
                    }
                    else
                    {
                        return (ulong)dtb;
                    }
                }
                catch (Exception)
                {
                    throw new ArgumentException("Couldn't extract DTB from current EPROCESS structure.");
                }                
            }
        }
        public uint Pid
        {
            get
            {
                try
                {
                    return (uint)_ep.UniqueProcessId;
                }
                catch (Exception)
                {
                    throw new ArgumentException("Couldn't extract UniqueProcessId from current EPROCESS structure.");
                }
            }
        }
        public uint Ppid
        {
            get
            {
                try
                {
                    return (uint)_ep.InheritedFromUniqueProcessId;
                }
                catch (Exception)
                {
                    throw new ArgumentException("Couldn't extract InheritedFromUniqueProcessId from current EPROCESS structure.");
                }                
            }
        }
    }
}
