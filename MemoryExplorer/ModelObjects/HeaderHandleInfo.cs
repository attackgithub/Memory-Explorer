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
    public class HeaderHandleInfo : StructureBase
    {
        public HeaderHandleInfo(Profile profile, DataProviderBase dataProvider, ulong virtualAddress=0, ulong physicalAddress=0) : base(profile, dataProvider, virtualAddress)
        {
            _physicalAddress = physicalAddress;
            _is64 = (_profile.Architecture == "AMD64");
            int structureSize = (int)_profile.GetStructureSize("_OBJECT_HEADER_HANDLE_INFO");
            if (structureSize == -1)
                throw new ArgumentException("Error - Profile didn't contain a definition for _OBJECT_HEADER_HANDLE_INFO");
            AddressBase addressSpace = dataProvider.ActiveAddressSpace;
            if (virtualAddress == 0)
            {
                _buffer = _dataProvider.ReadPhysicalMemory(_physicalAddress, (uint)_structureSize);
            }
            else
            {
                _physicalAddress = addressSpace.vtop(_virtualAddress);
                _buffer = _dataProvider.ReadMemoryBlock(_virtualAddress, (uint)_structureSize);
            }
            _structure = _profile.GetEntries("_OBJECT_HEADER_HANDLE_INFO");

        }
    }
}
