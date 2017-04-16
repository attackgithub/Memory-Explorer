using MemoryExplorer.Address;
using MemoryExplorer.Data;
using MemoryExplorer.Model;
using MemoryExplorer.Profiles;
using System;
using System.Text;

namespace MemoryExplorer.ModelObjects
{
    public class CmNameControlBlock : StructureBase
    {
        string _name = "";
        public CmNameControlBlock(DataModel model, ulong virtualAddress = 0) : base(model, virtualAddress)
        {
            Overlay("_CM_NAME_CONTROL_BLOCK");
            int nameLength = Members.NameLength;
            //_structure = _profile.GetEntries("_CM_NAME_CONTROL_BLOCK");
            _structureSize = (uint)_profile.GetStructureSize("_CM_NAME_CONTROL_BLOCK");
            if (_virtualAddress == 0)
                _buffer = _dataProvider.ReadPhysicalMemory(_physicalAddress, (uint)(_structureSize + nameLength));
            else
                _buffer = _dataProvider.ReadMemoryBlock(_virtualAddress, (uint)(_structureSize + nameLength));
            Structure s = GetStructureMember("Name");
            _name = Encoding.UTF8.GetString(_buffer, (int)s.Offset, nameLength);

            //_is64 = (_profile.Architecture == "AMD64");
            //AddressBase addressSpace = _dataProvider.ActiveAddressSpace;
            //if (_virtualAddress != 0)
            //    _physicalAddress = addressSpace.vtop(_virtualAddress);
            //if (_physicalAddress == 0)
            //    throw new ArgumentException("Error - Address is ZERO for _CM_NAME_CONTROL_BLOCK");
            //_structureSize = (uint)_profile.GetStructureSize("_CM_NAME_CONTROL_BLOCK");
            //if (_structureSize == -1)
            //    throw new ArgumentException("Error - Profile didn't contain a definition for _CM_NAME_CONTROL_BLOCK");
            //if (_virtualAddress == 0)
            //    _buffer = _dataProvider.ReadPhysicalMemory(_physicalAddress, (uint)_structureSize);
            //else
            //    _buffer = _dataProvider.ReadMemoryBlock(_virtualAddress, (uint)_structureSize);
            //_structure = _profile.GetEntries("_CM_NAME_CONTROL_BLOCK");
            //Structure s = GetStructureMember("NameLength");
            //int nameLength = BitConverter.ToInt16(_buffer, (int)s.Offset);
            //if (_virtualAddress == 0)
            //    _buffer = _dataProvider.ReadPhysicalMemory(_physicalAddress, (uint)(_structureSize + nameLength));
            //else
            //    _buffer = _dataProvider.ReadMemoryBlock(_virtualAddress, (uint)(_structureSize + nameLength));
            //s = GetStructureMember("Name");
            //_name = Encoding.UTF8.GetString(_buffer, (int)s.Offset, nameLength);
        }
        public string Name { get { return _name; } }
    }
}
