using MemoryExplorer.Address;
using MemoryExplorer.Data;
using MemoryExplorer.Profiles;
using System;

namespace MemoryExplorer.ModelObjects
{
    public class KeyControlBlock :StructureBase
    {
        CmNameControlBlock _nameControlBlock = null;
        KeyControlBlock _parent = null;

        public CmNameControlBlock NameControlBlock { get { return _nameControlBlock; } }
        public KeyControlBlock Parent { get { return _parent; } }

        public KeyControlBlock(Profile profile, DataProviderBase dataProvider, ulong virtualAddress = 0) : base(profile, dataProvider, virtualAddress)
        {
            Overlay("_CM_KEY_CONTROL_BLOCK");
            ulong nameBlockPtr = Members.NameBlock & 0xffffffffffff;
            _nameControlBlock = new CmNameControlBlock(_profile, _dataProvider, nameBlockPtr);
            ulong parentKcbPtr = Members.ParentKcb & 0xffffffffffff;
            if (parentKcbPtr != 0)
                _parent = new KeyControlBlock(_profile, _dataProvider, virtualAddress: parentKcbPtr );

            //_is64 = (_profile.Architecture == "AMD64");
            //AddressBase addressSpace = _dataProvider.ActiveAddressSpace;
            //if (_virtualAddress != 0)
            //    _physicalAddress = addressSpace.vtop(_virtualAddress);
            //if (_physicalAddress == 0)
            //    throw new ArgumentException("Error - Address is ZERO for _CM_KEY_CONTROL_BLOCK");
            //_structureSize = (uint)_profile.GetStructureSize("_CM_KEY_CONTROL_BLOCK");
            //if (_structureSize == -1)
            //    throw new ArgumentException("Error - Profile didn't contain a definition for _CM_KEY_CONTROL_BLOCK");
            //if (_virtualAddress == 0)
            //    _buffer = _dataProvider.ReadPhysicalMemory(_physicalAddress, (uint)_structureSize);
            //else
            //    _buffer = _dataProvider.ReadMemoryBlock(_virtualAddress, (uint)_structureSize);
            //_structure = _profile.GetEntries("_CM_KEY_CONTROL_BLOCK");
            //Structure s = GetStructureMember("NameBlock");
            //if (s == null)
            //    return;
            //if(_is64)
            //{
            //    ulong addr = BitConverter.ToUInt64(_buffer, (int)s.Offset) & 0xffffffffffff;
            //    _nameControlBlock = new CmNameControlBlock(_profile, _dataProvider, addr);
            //    s = GetStructureMember("ParentKcb");
            //    addr = BitConverter.ToUInt64(_buffer, (int)s.Offset) & 0xffffffffffff;
            //    if (addr != 0)
            //        _parent = new KeyControlBlock(_profile, _dataProvider, addr);
            //}
            //else
            //{

            //}


        }
    }
}
