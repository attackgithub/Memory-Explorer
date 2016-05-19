using MemoryExplorer.Data;
using MemoryExplorer.Profiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.ModelObjects
{
    public class HandleTable : StructureBase
    {
        private ulong _tableCode;
        private ulong _tableStartAddress;
        private uint _level;
        private const ulong LEVEL_MASK = 7;


        public HandleTable(Profile profile, DataProviderBase dataProvider, ulong virtualAddress) : base(profile, dataProvider, virtualAddress)
        {
            _is64 = (_profile.Architecture == "AMD64");
            int structureSize = (int)_profile.GetStructureSize("_HANDLE_TABLE");
            if (structureSize == -1)
                throw new ArgumentException("Error - Profile didn't contain a definition for _HANDLE_TABLE");
            _buffer = _dataProvider.ReadMemoryBlock(_virtualAddress, (uint)structureSize);
            _structure = _profile.GetEntries("_HANDLE_TABLE");
            Structure s = GetStructureMember("TableCode");
            if (s != null)
            {
                _tableCode = _is64 ? (BitConverter.ToUInt64(_buffer, (int)s.Offset) & 0xffffffffffff) : BitConverter.ToUInt32(_buffer, (int)s.Offset);
                _tableStartAddress = _tableCode & ~LEVEL_MASK;
                _level = (uint)(_tableCode & LEVEL_MASK);
            }
        }
        public uint? HandleCount
        {
            get
            {
                try
                {
                    MemberInfo mi = _profile.GetMemberInfo("_HANDLE_TABLE", "HandleCount");
                    return BitConverter.ToUInt32(_buffer, (int)mi.Offset);
                }
                catch { return null; }
            }
        }
        public LIST_ENTRY TableList
        {
            get
            {
                try
                {
                    MemberInfo mi = _profile.GetMemberInfo("_HANDLE_TABLE", "HandleTableList");
                    LIST_ENTRY le = new LIST_ENTRY(_buffer, (ulong)mi.Offset, _is64);
                    return le;
                }
                catch { return null; }
            }
        }
        public ulong TableCode { get { return _tableCode; } }
        public ulong TableStartAddress { get { return _tableStartAddress; } }
        public uint Level { get { return _level; } }
    }
}
