using MemoryExplorer.Address;
using MemoryExplorer.Data;
using MemoryExplorer.Profiles;
using System;

namespace MemoryExplorer.ModelObjects
{
    public class DriverExtension : StructureBase
    {
        public DriverExtension(Profile profile, DataProviderBase dataProvider, ulong virtualAddress = 0, ulong physicalAddress = 0) : base(profile, dataProvider, virtualAddress)
        {            
            _physicalAddress = physicalAddress;
            _is64 = (_profile.Architecture == "AMD64");
            AddressBase addressSpace = dataProvider.ActiveAddressSpace;
            if (virtualAddress != 0)
                _physicalAddress = addressSpace.vtop(_virtualAddress);
            if (_physicalAddress == 0)
                throw new ArgumentException("Error - Address is ZERO for _DRIVER_EXTENSION");
            _structureSize = (uint)_profile.GetStructureSize("_DRIVER_EXTENSION");
            if (_structureSize == -1)
                throw new ArgumentException("Error - Profile didn't contain a definition for _DRIVER_EXTENSION");
            // _physicalAddress = _dataProvider.ActiveAddressSpace.vtop(_virtualAddress, _dataProvider.IsLive);
            if (_virtualAddress == 0)
                _buffer = _dataProvider.ReadPhysicalMemory(_physicalAddress, (uint)_structureSize);
            else
                _buffer = _dataProvider.ReadMemoryBlock(_virtualAddress, (uint)_structureSize);
            _structure = _profile.GetEntries("_DRIVER_EXTENSION");
        }
        public string ServiceKeyName
        {
            get
            {
                Structure s = GetStructureMember("ServiceKeyName");
                if (s.EntryType == "_UNICODE_STRING")
                {
                    UnicodeString us = new UnicodeString(_profile, _dataProvider, physicalAddress: _physicalAddress + s.Offset);
                    return us.Name;
                }
                return string.Empty;
            }
        }
        public ulong PhysicalAddress { get { return _physicalAddress; } }
        public ulong VirtualAddress { get { return _virtualAddress; } }

    }
}
