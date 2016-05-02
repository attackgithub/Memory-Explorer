using MemoryExplorer.Address;
using MemoryExplorer.Data;
using MemoryExplorer.Profiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.ModelObjects
{
    public class DriverObject : StructureBase
    {
        string _driverName;
        string _name;
        DriverExtension _driverExtension = null;

        public DriverObject(Profile profile, DataProviderBase dataProvider, ulong virtualAddress=0, ulong physicalAddress=0)
        {
            _profile = profile;
            _dataProvider = dataProvider;
            _virtualAddress = virtualAddress;
            _physicalAddress = physicalAddress;
            _is64 = (_profile.Architecture == "AMD64");
            AddressBase addressSpace = dataProvider.ActiveAddressSpace;
            if (virtualAddress != 0)
                _physicalAddress = addressSpace.vtop(_virtualAddress);
            Initialise();
            ObjectHeader oh = new ObjectHeader(_profile);
            long headerSize = oh.Size;
            if (headerSize != -1)
                _header = new ObjectHeader(_profile, _dataProvider, _virtualAddress - (uint)headerSize);
        }
        public DriverObject(Profile profile, DataProviderBase dataProvider, ObjectHeader header, ulong virtualAddress=0, ulong physicalAddress = 0)
        {
            _profile = profile;
            _dataProvider = dataProvider;
            _virtualAddress = virtualAddress;
            _physicalAddress = physicalAddress;
            _is64 = (_profile.Architecture == "AMD64");
            _header = header;
            AddressBase addressSpace = dataProvider.ActiveAddressSpace;
            if (virtualAddress != 0)
                _physicalAddress = addressSpace.vtop(_virtualAddress);
            Initialise();
        }

        private void Initialise()
        {
            if (_physicalAddress == 0)
                throw new ArgumentException("Error - Address is ZERO for _DRIVER_OBJECT");
            //_physicalAddress = _dataProvider.ActiveAddressSpace.vtop(_virtualAddress, _dataProvider.IsLive);

            _structureSize = (uint)_profile.GetStructureSize("_DRIVER_OBJECT");
            if (_structureSize == -1)
                throw new ArgumentException("Error - Profile didn't contain a definition for _DRIVER_OBJECT");
            if (_virtualAddress == 0)
                _buffer = _dataProvider.ReadPhysicalMemory(_physicalAddress, (uint)_structureSize);
            else
                _buffer = _dataProvider.ReadMemoryBlock(_virtualAddress, (uint)_structureSize);
            _structure = _profile.GetEntries("_DRIVER_OBJECT");
            Structure s = GetStructureMember("DriverName");
            if (s.EntryType == "_UNICODE_STRING")
            {
                UnicodeString us = new UnicodeString(_profile, _dataProvider, physicalAddress: _physicalAddress + s.Offset);
                _driverName = us.Name;
            }
            // get the driver extension
            if (DriverExtensionVirtualAddress != 0)
            {
                _driverExtension = new DriverExtension(_profile, _dataProvider, physicalAddress: _physicalAddress + (ulong)_structureSize);
            }
        }
        public ulong DriverSize
        {
            get
            {
                Structure s = GetStructureMember("DriverSize");
                return (BitConverter.ToUInt64(_buffer, (int)s.Offset) & 0xffffffffffff);
            }
        }
        public ulong DriverStartPointer
        {
            get
            {
                Structure s = GetStructureMember("DriverStart");
                return (BitConverter.ToUInt64(_buffer, (int)s.Offset) & 0xffffffffffff);
            }
        }
        public ulong DriverStartPhysicalAddress
        {
            get
            {
                return _dataProvider.ActiveAddressSpace.vtop(DriverStartPointer);
            }
        }
        public ulong DriverExtensionVirtualAddress
        {
            get
            {
                Structure s = GetStructureMember("DriverExtension");
                return (BitConverter.ToUInt64(_buffer, (int)s.Offset) & 0xffffffffffff);
            }
        }
        public DriverExtension DriverExtension { get { return _driverExtension; } }
        public ulong HandleCount { get { return _header.HandleCount; } }
        public ulong PointerCount { get { return _header.PointerCount; } }
        public string DriverName { get { return _driverName; } }
        public ulong PhysicalAddress { get { return _physicalAddress; } }
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }
    }
}
