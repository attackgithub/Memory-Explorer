using MemoryExplorer.Address;
using MemoryExplorer.Data;
using MemoryExplorer.ModelObjects;
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
        private ulong _objectTable;
        private ulong _dtbOffset;
        private uint _dtbSize;
        //private ulong _offset;
        private AddressBase _addressSpace;
        //private HandleTable _hndTable = null;

        public EProcess(Profile profile, DataProviderBase dataProvider, ulong virtualAddress=0, ulong physicalAddress = 0) : base(profile, dataProvider, virtualAddress)
        {
            ObjectHeader oh = new ObjectHeader(_profile);
            if (virtualAddress == 0)
            {
                _physicalAddress = physicalAddress;
                Initialise();
            }
            else
            {
                _addressSpace = _dataProvider.ActiveAddressSpace;
                _physicalAddress = _addressSpace.vtop(_virtualAddress, _dataProvider.IsLive);
                Initialise();
                long headerSize = oh.Size;
                if (headerSize != -1)
                    _header = new ObjectHeader(_profile, _dataProvider, _virtualAddress - (uint)headerSize);
            }
            
        }
        private void Initialise()
        {
            _is64 = (_profile.Architecture == "AMD64");
            _structureSize = (uint)_profile.GetStructureSize("_EPROCESS");
            if (_structureSize == -1)
                throw new ArgumentException("Error - Profile didn't contain a definition for _EPROCESS");
            _structure = _profile.GetEntries("_EPROCESS");
            if (_virtualAddress == 0)
                _buffer = _dataProvider.ReadPhysicalMemory(_physicalAddress, (uint)_structureSize);
            else
                _buffer = _dataProvider.ReadMemoryBlock(_virtualAddress, (uint)_structureSize);
            //_offset = _physicalAddress - (_physicalAddress & 0xfffffffff000);
            _dtbOffset = _profile.GetOffset("_EPROCESS", "Pcb.DirectoryTableBase");
            _dtbSize = _profile.GetSize("_EPROCESS", "Pcb.DirectoryTableBase");
            Structure s = GetStructureMember("ObjectTable");
            if (s != null && s.PointerType == "_HANDLE_TABLE")
            {
                var a = (s.Size == 4) ? BitConverter.ToUInt32(_buffer, (int)s.Offset) : (BitConverter.ToUInt64(_buffer, (int)s.Offset) & 0xffffffffffff);
                if (a != 0)
                {
                    _objectTable = (ulong)a;
                }
            }
        }
        public object Get(string member)
        {
            try
            {
                Structure s = GetStructureMember(member);
                if (s == null)
                    return null;
                if (s.EntryType == "Array")
                {
                    byte[] array = new byte[s.Size];
                    Array.Copy(_buffer, (int)s.Offset, array, 0, (int)s.Size);
                    return array;
                }



                //int offset = (int)_profile.GetOffset("_EPROCESS", member);
                //int objectSize = _profile.GetSize("_EPROCESS", member);
                //bool isArray = _profile.IsArray("_EPROCESS", member);
                switch (s.Size)
                {
                    case 1:
                        return _buffer[(int)s.Offset];
                    case 2:
                        return BitConverter.ToUInt16(_buffer, (int)s.Offset);
                    case 4:
                        return BitConverter.ToUInt32(_buffer, (int)s.Offset);
                    case 8:
                        return BitConverter.ToUInt64(_buffer, (int)s.Offset);
                    default:
                        return null;
                }
            }
            catch
            {
                return null;
            }
        }
        public ulong DTB
        {
            get
            {
                MemberInfo mi = _profile.GetMemberInfo("_EPROCESS", "Pcb.DirectoryTableBase");
                if (mi.IsArray && mi.Count == 2 && mi.Size == 4)
                {
                    var a = BitConverter.ToUInt32(_buffer, (int)(mi.Offset));
                    var b = BitConverter.ToUInt32(_buffer, (int)(mi.Offset + 4));
                    return a;
                }
                if (!mi.IsArray && mi.Size == 8)
                {
                    var a = BitConverter.ToUInt64(_buffer, (int)(mi.Offset));
                    var b = BitConverter.ToUInt64(_buffer, (int)(mi.Offset + 4));
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
                int realOffset = (int)(s.Offset);
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
                int realOffset = (int)(s.Offset);
                var a = (s.Size == 4) ? BitConverter.ToUInt32(_buffer, realOffset) : BitConverter.ToUInt64(_buffer, realOffset);
                return (uint)a;
            }
        }
        public string ImageFileName { get { return (Encoding.UTF8.GetString((byte[])Get("ImageFileName"))).Trim(new char[] { '\x0' }); } }
        public uint ActiveThreads
        {
            get
            {
                MemberInfo mi = _profile.GetMemberInfo("_EPROCESS", "ActiveThreads");
                if (!mi.IsArray && mi.Size == 4)
                {
                    var a = BitConverter.ToUInt32(_buffer, (int)mi.Offset);
                    return a;
                }
                return 0;
            }
        }
        //public uint? HandleCount
        //{
        //    get
        //    {
        //        if (_hndTable != null)
        //            return _hndTable.HandleCount;
        //        return null;
        //    }
        //}
        public ulong Session
        {
            get
            {
                Structure s = GetStructureMember("Session");
                if (s == null)
                    return 0;
                return _is64 ? BitConverter.ToUInt64(_buffer, (int)s.Offset) & 0xffffffffffff : BitConverter.ToUInt32(_buffer, (int)s.Offset);
            }
        }
        public DateTime StartTime
        {
            get
            {
                Structure s = GetStructureMember("CreateTime");
                if (s == null)
                    return new DateTime(1666, 1, 1);
                try
                {
                    long longVar = BitConverter.ToInt64(_buffer, (int)s.Offset);
                    if (longVar == 0)
                        return DateTime.MinValue;
                    DateTime dateTimeVar = new DateTime(longVar).AddYears(1600);
                    return dateTimeVar;
                }
                catch
                {
                    return new DateTime(1666, 1, 1);
                }
            }
        }
        public DateTime ExitTime
        {
            get
            {
                Structure s = GetStructureMember("ExitTime");
                if (s == null)
                    return new DateTime(1666, 1, 1);
                try
                {
                    long longVar = BitConverter.ToInt64(_buffer, (int)s.Offset);
                    if (longVar == 0)
                        return DateTime.MinValue;
                    DateTime dateTimeVar = new DateTime(longVar).AddYears(1600);
                    return dateTimeVar;
                }
                catch
                {
                    return new DateTime(1666, 1, 1);
                }
            }
        }
        public ulong ObjectTable { get { return _objectTable; } }
    }
}
