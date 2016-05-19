using MemoryExplorer.Data;
using MemoryExplorer.Profiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.ModelObjects
{
    public class PoolHeader : StructureBase
    {
        public PoolHeader(Profile profile, DataProviderBase dataProvider, ulong physicalAddress) : base(profile, dataProvider, 0)
        {
            _physicalAddress = physicalAddress;
            _is64 = (_profile.Architecture == "AMD64");
            if (_physicalAddress == 0)
                throw new ArgumentException("Error - Address is ZERO for _POOL_HEADER");
            _structureSize = (uint)_profile.GetStructureSize("_POOL_HEADER");
            if (_structureSize == -1)
                throw new ArgumentException("Error - Profile didn't contain a definition for _POOL_HEADER");
            _buffer = _dataProvider.ReadPhysicalMemory(_physicalAddress, (uint)_structureSize);
            _structure = _profile.GetEntries("_POOL_HEADER");
        }
        public ulong BlockSize
        {
            get
            {
                Structure s = GetStructureMember("BlockSize");
                if (s.EntryType != "BitField")
                    return 0;
                ulong mask = GetMask(s.StartBit, s.EndBit);
                ulong value = 0;
                switch (s.Size)
                {
                    case 2:
                        value = BitConverter.ToUInt16(_buffer, (int)s.Offset);
                        break;
                    default:
                        break;
                }
                value = value & mask;
                value = value >> (int)s.StartBit;
                return value;
            }
        }
        public ulong PoolType
        {
            get
            {
                Structure s = GetStructureMember("PoolType");
                if (s.EntryType != "BitField")
                    return 0;
                ulong mask = GetMask(s.StartBit, s.EndBit);
                ulong value = 0;
                switch (s.Size)
                {
                    case 2:
                        value = BitConverter.ToUInt16(_buffer, (int)s.Offset);
                        break;
                    default:
                        break;
                }
                value = value & mask;
                value = value >> (int)s.StartBit;
                return value;
            }
        }
        public string Tag
        {
            get
            {
                Structure s = GetStructureMember("PoolTag");
                return Encoding.UTF8.GetString(_buffer, (int)s.Offset, (int)s.Size);
            }
        }
        public ulong PoolIndex
        {
            get
            {
                Structure s = GetStructureMember("PoolIndex");
                if (s.EntryType != "BitField")
                    return 0;
                ulong mask = GetMask(s.StartBit, s.EndBit);
                ulong value = 0;
                switch (s.Size)
                {
                    case 2:
                        value = BitConverter.ToUInt16(_buffer, (int)s.Offset);
                        break;
                    default:
                        break;
                }
                value = value & mask;
                value = value >> (int)s.StartBit;
                return value;
            }
        }
        public ulong ProcessBilled
        {
            get
            {
                Structure s = GetStructureMember("ProcessBilled");
                return BitConverter.ToUInt64(_buffer, (int)s.Offset) & 0xffffffffffff;
            }
        }
    }
}
