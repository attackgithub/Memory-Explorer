using MemoryExplorer.Data;
using MemoryExplorer.Profiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.Processes
{
    public class EProcess : StructureBase
    {
        ///private ulong _virtualAddress;
        //private ulong _objectTable;
        private ulong _dtbOffset;
        private uint _dtbSize;
        private ulong _offset;
        //private AddressBase _addressSpace;
        //private HandleTable _hndTable = null;

        public EProcess(Profile profile, DataProviderBase dataProvider, ulong physicalAddress)
        {
            _profile = profile;
            _dataProvider = dataProvider;
            _physicalAddress = physicalAddress;
            Initialise();
        }
        private void Initialise()
        {
            _is64 = (_profile.Architecture == "AMD64");
            _structureSize = (uint)_profile.GetStructureSize("_EPROCESS");
            if (_structureSize == -1)
                throw new ArgumentException("Error - Profile didn't contain a definition for _EPROCESS");
            _structure = _profile.GetEntries("_EPROCESS");
            _buffer = _dataProvider.ReadMemory(_physicalAddress & 0xfffffffff000, 2);
            _offset = _physicalAddress - (_physicalAddress & 0xfffffffff000);
            _dtbOffset = _profile.GetOffset("_EPROCESS", "Pcb.DirectoryTableBase");
            _dtbSize = _profile.GetSize("_EPROCESS", "Pcb.DirectoryTableBase");

        }
        public ulong DTB
        {
            get
            {
                MemberInfo mi = _profile.GetMemberInfo("_EPROCESS", "Pcb.DirectoryTableBase");
                if (mi.IsArray && mi.Count == 2 && mi.Size == 4)
                {
                    var a = BitConverter.ToUInt32(_buffer, (int)(mi.Offset + _offset));
                    var b = BitConverter.ToUInt32(_buffer, (int)(mi.Offset + 4 + _offset));
                    return a;
                }
                if (!mi.IsArray && mi.Size == 8)
                {
                    var a = BitConverter.ToUInt64(_buffer, (int)(mi.Offset + _offset));
                    var b = BitConverter.ToUInt64(_buffer, (int)(mi.Offset + 4 + _offset));
                    return a;
                }
                return 0;
            }
        }
        public uint Pid
        {
            get
            {
                Structure s = GetStructureMember("UniqueProcessId");
                if (s == null)
                    return 0;
                int realOffset = (int)(s.Offset + _offset);
                var a = (s.Size == 4) ? BitConverter.ToUInt32(_buffer, realOffset) : BitConverter.ToUInt64(_buffer, realOffset);
                return (uint)a;
            }
        }
        public uint Ppid
        {
            get
            {
                Structure s = GetStructureMember("InheritedFromUniqueProcessId");
                if (s == null)
                    return 0;
                int realOffset = (int)(s.Offset + _offset);
                var a = (s.Size == 4) ? BitConverter.ToUInt32(_buffer, realOffset) : BitConverter.ToUInt64(_buffer, realOffset);
                return (uint)a;
            }
        }
    }
}
