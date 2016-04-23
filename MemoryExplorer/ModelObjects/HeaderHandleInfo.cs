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
        public HeaderHandleInfo(Profile profile, DataProviderBase dataProvider, ulong virtualAddress)
        {
            _profile = profile;
            _dataProvider = dataProvider;
            _virtualAddress = virtualAddress;
            _is64 = (_profile.Architecture == "AMD64");
            int structureSize = (int)_profile.GetStructureSize("_OBJECT_HEADER_HANDLE_INFO");
            if (structureSize == -1)
                throw new ArgumentException("Error - Profile didn't contain a definition for _OBJECT_HEADER_HANDLE_INFO");
            _buffer = _dataProvider.ReadMemoryBlock(_virtualAddress, (uint)structureSize);
            _structure = _profile.GetEntries("_OBJECT_HEADER_HANDLE_INFO");

        }
    }
}
