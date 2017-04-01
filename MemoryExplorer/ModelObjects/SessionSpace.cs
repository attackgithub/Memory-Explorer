using MemoryExplorer.Data;
using MemoryExplorer.Profiles;
using System;

namespace MemoryExplorer.ModelObjects
{
    public class SessionSpace : StructureBase
    {
        public SessionSpace(Profile_Deprecated profile, DataProviderBase dataProvider, ulong virtualAddress) : base(profile, dataProvider, virtualAddress)
        {
            Overlay("_MM_SESSION_SPACE");

            if (_virtualAddress == 0)
                throw new ArgumentException("Error - Offset is ZERO for _MM_SESSION_SPACE");
            _is64 = (_profile.Architecture == "AMD64");
            _structureSize = (uint)_profile.GetStructureSize("_MM_SESSION_SPACE");
            if (_structureSize == -1)
                throw new ArgumentException("Error - Profile didn't contain a definition for _MM_SESSION_SPACE");
            _buffer = _dataProvider.ReadMemoryBlock(_virtualAddress, (uint)_structureSize);
            _structure = _profile.GetEntries("_MM_SESSION_SPACE");
        }
        public LIST_ENTRY ProcessList
        {
            get
            {
                Structure s = GetStructureMember("ProcessList");
                LIST_ENTRY le = new LIST_ENTRY(_buffer, s.Offset, _is64);
                return le;
            }
        }
    }
}
