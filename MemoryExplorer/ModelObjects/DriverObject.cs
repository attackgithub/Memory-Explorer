using MemoryExplorer.Address;
using MemoryExplorer.Data;
using MemoryExplorer.Model;
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
        private ulong _driverExtensionVirtualAddress;
        private ulong _driverSize;
        private ulong _driverStart;


        public DriverObject(DataModel model, ulong virtualAddress=0, ulong physicalAddress=0) : base(model, virtualAddress)
        {            
            _physicalAddress = physicalAddress;
            Initialise();
            ObjectHeader oh = new ObjectHeader(_model);
            long headerSize = oh.Size;
            if (headerSize != -1)
                _header = new ObjectHeader(_model, _virtualAddress - (uint)headerSize);
        }
        public DriverObject(DataModel model, ObjectHeader header, ulong virtualAddress=0, ulong physicalAddress = 0) : base(model, virtualAddress)
        {
            _physicalAddress = physicalAddress;
            _header = header;
            Initialise();
        }

        private void Initialise()
        {
            Overlay("_DRIVER_OBJECT");
            byte[] dnBuffer = Members.DriverName;
            UnicodeString us = new UnicodeString(_model, dnBuffer);
            _driverName = us.Name;
            _driverExtensionVirtualAddress = Members.DriverExtension & 0xffffffffffff;
            if(_driverExtensionVirtualAddress != 0)
                _driverExtension = new DriverExtension(_model, virtualAddress: _driverExtensionVirtualAddress);
            _driverSize = Members.DriverSize;
            _driverStart = Members.DriverStart & 0xffffffffffff;


            //_is64 = (_profile.Architecture == "AMD64");
            //AddressBase addressSpace = _dataProvider.ActiveAddressSpace;
            //if (_virtualAddress != 0)
            //    _physicalAddress = addressSpace.vtop(_virtualAddress);
            //if (_physicalAddress == 0)
            //    throw new ArgumentException("Error - Address is ZERO for _DRIVER_OBJECT");
            ////_physicalAddress = _dataProvider.ActiveAddressSpace.vtop(_virtualAddress, _dataProvider.IsLive);

            //_structureSize = (uint)_profile.GetStructureSize("_DRIVER_OBJECT");
            //if (_structureSize == -1)
            //    throw new ArgumentException("Error - Profile didn't contain a definition for _DRIVER_OBJECT");
            //if (_virtualAddress == 0)
            //    _buffer = _dataProvider.ReadPhysicalMemory(_physicalAddress, (uint)_structureSize);
            //else
            //    _buffer = _dataProvider.ReadMemoryBlock(_virtualAddress, (uint)_structureSize);
            //_structure = _profile.GetEntries("_DRIVER_OBJECT");
            //Structure s = GetStructureMember("DriverName");
            //if (s.EntryType == "_UNICODE_STRING")
            //{
            //    UnicodeString us = new UnicodeString(_profile, _dataProvider, physicalAddress: _physicalAddress + s.Offset);
            //    _driverName = us.Name;
            //}
            //// get the driver extension
            //if (DriverExtensionVirtualAddress != 0)
            //{
            //    _driverExtension = new DriverExtension(_profile, _dataProvider, physicalAddress: _physicalAddress + (ulong)_structureSize);
            //}
        }
        public ulong DriverSize
        {
            get
            {
                return _driverSize;
            }
        }
        public ulong DriverStartPointer
        {
            get
            {
                return _driverStart;
            }
        }
        public ulong DriverStartPhysicalAddress
        {
            get
            {
                return _model.ActiveAddressSpace.vtop(_driverStart);
            }
        }
        public ulong DriverExtensionVirtualAddress
        {
            get
            {
                return _driverExtensionVirtualAddress;
            }
        }
        public DriverExtension DriverExtension { get { return _driverExtension; } }
        public ulong HandleCount { get { return _header.HandleCount; } }
        public ulong PointerCount { get { return _header.PointerCount; } }
        public string DriverName { get { return _driverName; } }
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
