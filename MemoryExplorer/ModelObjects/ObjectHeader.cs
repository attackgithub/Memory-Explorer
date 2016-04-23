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
    public class ObjectHeader : StructureBase
    {
        public enum Mask : byte
        {
            HEADER_CREATOR_INFO = 0x01,
            HEADER_NAME_INFO = 0x02,
            HEADER_HANDLE_INFO = 0x04,
            HEADER_QUOTA_INFO = 0x08,
            HEADER_PROCESS_INFO = 0x10,
            HEADER_AUDIT_INFO = 0x20,
            HEADER_PADDING_INFO = 0x40
        }

        private byte _infoMask;
        private ulong _headerSize = 0;
        private HeaderNameInfo _headerNameInfo = null;
        private HeaderHandleInfo _headerHandleInfo = null;



        public ObjectHeader(Profile profile)
        {
            _profile = profile;
            _structure = profile.GetEntries("_OBJECT_HEADER");
            Structure s = GetStructureMember("Body");
            if (s != null)
                _structureSize = (long)s.Offset;
        }
        public ObjectHeader(Profile profile, DataProviderBase dataProvider, ulong virtualAddress)
        {
            _dataProvider = dataProvider;
            _profile = profile;
            _virtualAddress = virtualAddress;
            if (virtualAddress == 0)
                throw new ArgumentException("Error - Offset is ZERO for _OBJECT_HEADER");
            _is64 = (_profile.Architecture == "AMD64");
            _structureSize = (uint)_profile.GetStructureSize("_OBJECT_HEADER");
            if (_structureSize == -1)
                throw new ArgumentException("Error - Profile didn't contain a definition for _OBJECT_HEADER");
            AddressBase addressSpace = dataProvider.ActiveAddressSpace;
            _buffer = _dataProvider.ReadMemoryBlock(_virtualAddress, (uint)_structureSize);
            _structure = _profile.GetEntries("_OBJECT_HEADER");
            Structure s = GetStructureMember("InfoMask");
            _infoMask = _buffer[s.Offset];
            ulong offsetMarker = _virtualAddress;
            if ((_infoMask & (byte)Mask.HEADER_CREATOR_INFO) > 0)
            {
                uint size = (uint)_profile.GetStructureSize("_OBJECT_HEADER_CREATOR_INFO");
                offsetMarker -= size;
                _headerSize += size;
            }
            if ((_infoMask & (byte)Mask.HEADER_NAME_INFO) > 0)
            {
                uint size = (uint)_profile.GetStructureSize("_OBJECT_HEADER_NAME_INFO");
                offsetMarker -= size;
                _headerNameInfo = new HeaderNameInfo(_profile, _dataProvider, offsetMarker);
                _headerSize += size;
            }
            if ((_infoMask & (byte)Mask.HEADER_HANDLE_INFO) > 0)
            {
                uint size = (uint)_profile.GetStructureSize("_OBJECT_HEADER_HANDLE_INFO");
                offsetMarker -= size;
                _headerHandleInfo = new HeaderHandleInfo(_profile, _dataProvider, offsetMarker);
                _headerSize += size;
            }
            if ((_infoMask & (byte)Mask.HEADER_QUOTA_INFO) > 0)
            {
                uint size = (uint)_profile.GetStructureSize("_OBJECT_HEADER_QUOTA_INFO");
                offsetMarker -= size;
                _headerSize += size;
            }
            if ((_infoMask & (byte)Mask.HEADER_PROCESS_INFO) > 0)
            {
                uint size = (uint)_profile.GetStructureSize("_OBJECT_HEADER_PROCESS_INFO");
                offsetMarker -= size;
                _headerSize += size;
            }
            if ((_infoMask & (byte)Mask.HEADER_AUDIT_INFO) > 0)
            {
                uint size = (uint)_profile.GetStructureSize("_OBJECT_HEADER_AUDIT_INFO");
                offsetMarker -= size;
                _headerSize += size;
            }
            s = GetStructureMember("Body");
            if (s != null)
                _structureSize = (long)s.Offset;
        }
        public ulong TypeInfo
        {
            get
            {
                try
                {
                    Structure s = GetStructureMember("TypeIndex");
                    return (ulong)_buffer[(int)s.Offset];
                }
                catch { return 0; }
            }
        }
        public ulong PointerCount
        {
            get
            {
                try
                {
                    Structure s = GetStructureMember("PointerCount");
                    return BitConverter.ToUInt64(_buffer, (int)s.Offset);
                }
                catch { return 0; }
            }
        }
        public ulong HandleCount
        {
            get
            {
                try
                {
                    Structure s = GetStructureMember("HandleCount");
                    return BitConverter.ToUInt64(_buffer, (int)s.Offset);
                }
                catch { return 0; }
            }
        }
    }
}
