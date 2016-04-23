using MemoryExplorer.Data;
using MemoryExplorer.Profiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.ModelObjects
{
    public class HeaderNameInfo : StructureBase
    {
        string _name;
        ulong _referenceCount;
        public HeaderNameInfo(Profile profile, DataProviderBase dataProvider, ulong virtualAddress)
        {
            _profile = profile;
            _dataProvider = dataProvider;
            _virtualAddress = virtualAddress;
            _is64 = (_profile.Architecture == "AMD64");
            _structureSize = _profile.GetStructureSize("_OBJECT_HEADER_NAME_INFO");
            if (_structureSize == -1)
                throw new ArgumentException("Error - Profile didn't contain a definition for _OBJECT_HEADER_NAME_INFO");
            _buffer = _dataProvider.ReadMemoryBlock(_virtualAddress, (uint)_structureSize);
            _structure = _profile.GetEntries("_OBJECT_HEADER_NAME_INFO");
            Structure s = GetStructureMember("ReferenceCount");
            _referenceCount = BitConverter.ToUInt32(_buffer, (int)s.Offset);
            s = GetStructureMember("Name");
            if (s.EntryType == "_UNICODE_STRING")
            {
                UnicodeString us = new UnicodeString(_profile, _dataProvider, _physicalAddress + s.Offset);
                _name = us.Name;
            }
            // TO DO Parse the Directory member of structure
        }

        public string Name { get { return _name; } }
        public ulong ReferenceCount { get { return _referenceCount; } }
    }
}
