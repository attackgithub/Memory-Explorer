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
    public class UnicodeString : StructureBase
    {
        ulong _length;
        ulong _maximumLength;
        ulong _pointerBuffer;
        private AddressBase _addressSpace;
        private string _name = "";

        public string Name { get { return _name; } }

        // this will fail if the string runs off the end of the page
        // remember to set the dataProvider.ActiveAddressSpace before you call
        public UnicodeString(Profile profile, DataProviderBase dataProvider, ulong virtualAddress=0, ulong physicalAddress=0) : base(profile, dataProvider, virtualAddress)
        {
            _physicalAddress = physicalAddress;
            _is64 = (_profile.Architecture == "AMD64");
            _addressSpace = dataProvider.ActiveAddressSpace;
            _structureSize = (int)_profile.GetStructureSize("_UNICODE_STRING");
            if (_structureSize == -1)
                throw new ArgumentException("Error - Profile didn't contain a definition for _OBJECT_TYPE");
            //AddressBase addressSpace = dataProvider.ActiveAddressSpace;
            if (virtualAddress == 0)
                _buffer = _dataProvider.ReadPhysicalMemory(_physicalAddress, (uint)_structureSize);
            else
            {
                _physicalAddress = _addressSpace.vtop(_virtualAddress);
                _buffer = _dataProvider.ReadMemoryBlock(_virtualAddress, (uint)_structureSize);
            }
            if (_buffer == null)
                throw new ArgumentException("Invalid Address: " + virtualAddress.ToString("X08"));
            _structure = _profile.GetEntries("_UNICODE_STRING");
            Structure s = GetStructureMember("Length");
            //int realOffset = (int)s.Offset + (int)(_physicalAddress & 0xfff);
            _length = BitConverter.ToUInt16(_buffer, (int)s.Offset);
            s = GetStructureMember("MaximumLength");
            //realOffset = (int)s.Offset + (int)(_physicalAddress & 0xfff);
            _maximumLength = BitConverter.ToUInt16(_buffer, (int)s.Offset);
            s = GetStructureMember("Buffer");
            //realOffset = (int)s.Offset + (int)(_physicalAddress & 0xfff);
            if(_is64)
                _pointerBuffer = BitConverter.ToUInt64(_buffer, (int)s.Offset) & 0xffffffffffff;
            else
                _pointerBuffer = BitConverter.ToUInt32(_buffer, (int)s.Offset) & 0xffffffff;
            ulong pAddress = _addressSpace.vtop(_pointerBuffer);            
            if (pAddress != 0)
            {
                byte[] nameBuffer = _dataProvider.ReadMemory(pAddress & 0xfffffffff000, 1);
                _name = Encoding.Unicode.GetString(nameBuffer, (int)(pAddress & 0xfff), (int)_length);
            }
        }
    }
}
