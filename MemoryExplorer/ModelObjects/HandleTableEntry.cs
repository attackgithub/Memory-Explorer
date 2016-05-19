using MemoryExplorer.Data;
using MemoryExplorer.Profiles;
using System;

namespace MemoryExplorer.ModelObjects
{
    public class HandleTableEntry : StructureBase
    {
        private int _index;
        private bool _unlocked;
        private bool _valid;
        private ulong _referenceCount;
        private ulong _objectPointer;
        private ulong _typeInfo;
        private ulong _grantedAccess;
        private ulong _eprocess;
        public HandleTableEntry(Profile profile, DataProviderBase dataProvider, ulong virtualAddress, int index) : base(profile, dataProvider, virtualAddress)
        {
            _is64 = (_profile.Architecture == "AMD64");
            if (_virtualAddress == 0)
                throw new ArgumentException("Error - Offset is ZERO for _HANDLE_TABLE_ENTRY");
            _structureSize = _profile.GetStructureSize("_HANDLE_TABLE_ENTRY");
            if (_structureSize == -1)
                throw new ArgumentException("Error - Profile didn't contain a definition for _HANDLE_TABLE_ENTRY");
            _buffer = dataProvider.ReadMemoryBlock(virtualAddress, (uint)_structureSize);
            if(_buffer == null)
                throw new ArgumentException("Error - Invalid Virtual Address");
            Parse();
        }
        public HandleTableEntry(Profile profile, byte[] buffer, int index) : base(profile, null, 0)
        {
            _index = index * 4;
            _is64 = (_profile.Architecture == "AMD64");
            int structureSize = (int)_profile.GetStructureSize("_HANDLE_TABLE_ENTRY");
            if (structureSize == -1 || structureSize < buffer.Length)
                throw new ArgumentException("Error - Profile didn't contain a definition for _HANDLE_TABLE_ENTRY");
            _buffer = buffer;
            Parse();
        }
        private void Parse()
        {
            _structure = _profile.GetEntries("_HANDLE_TABLE_ENTRY");
            if (_is64)
            {
                ulong part1 = BitConverter.ToUInt64(_buffer, 0);
                ulong part2 = BitConverter.ToUInt64(_buffer, 8);
                _valid = (part1 != 0);
                _unlocked = ((part1 & 0x01) == 1);
                _referenceCount = (part1 & 0x1fffe) >> 1;
                _objectPointer = ((part1 & 0xfffffffffff00000) >> 16);
                var t1 = _objectPointer;
                var t2 = t1 * 16;
                _typeInfo = (part2 & 0xffffffff00000000) >> 32;
                _grantedAccess = (part2 & 0x1ffffff);
            }
            else
            {

            }
        }
        public bool IsValid { get { return _valid; } } // really dodge - change to something much better!
        public int Index { get { return _index; } }
        public bool Unlocked { get { return _unlocked; } }
        public ulong ReferenceCount { get { return _referenceCount; } }
        public ulong ObjectPointer { get { return _objectPointer; } }
        public ulong TypeInfo { get { return _typeInfo; } }
        public ulong GrantedAccess { get { return _grantedAccess; } }
        public ulong Eprocess { get { return _eprocess; } set { _eprocess = value; } }
    }
}
