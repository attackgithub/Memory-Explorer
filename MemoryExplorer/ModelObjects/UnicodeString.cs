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
        public UnicodeString(Profile profile, DataProviderBase dataProvider, ulong offset)
        {
            _profile = profile;
            _dataProvider = dataProvider;
            _physicalAddress = offset;
            _is64 = (_profile.Architecture == "AMD64");
            _addressSpace = _profile.KernelAddressSpace;
            int structureSize = (int)_profile.GetStructureSize("_UNICODE_STRING");
            if (structureSize == -1)
                throw new ArgumentException("Error - Profile didn't contain a definition for _OBJECT_TYPE");
            _buffer = _dataProvider.ReadMemory(_physicalAddress & 0xfffffffff000, 1);
            _structure = _profile.GetEntries("_UNICODE_STRING");
            Structure s = GetStructureMember("Length");
            int realOffset = (int)s.Offset + (int)(_physicalAddress & 0xfff);
            _length = BitConverter.ToUInt16(_buffer, realOffset);
            s = GetStructureMember("MaximumLength");
            realOffset = (int)s.Offset + (int)(_physicalAddress & 0xfff);
            _maximumLength = BitConverter.ToUInt16(_buffer, realOffset);
            s = GetStructureMember("Buffer");
            realOffset = (int)s.Offset + (int)(_physicalAddress & 0xfff);
            _pointerBuffer = BitConverter.ToUInt64(_buffer, realOffset) & 0xffffffffffff;
            ulong pAddress = _addressSpace.vtop(_pointerBuffer);            
            if (pAddress != 0)
            {
                byte[] nameBuffer = _dataProvider.ReadMemory(pAddress & 0xfffffffff000, 1);
                _name = Encoding.Unicode.GetString(nameBuffer, (int)(pAddress & 0xfff), (int)_length);
            }
        }
    }
}
