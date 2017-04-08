using MemoryExplorer.Address;
using MemoryExplorer.Data;
using MemoryExplorer.Profiles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public HeaderNameInfo HeaderNameInfo { get { return _headerNameInfo; } }
        public HeaderHandleInfo HeaderHandleInfo { get { return _headerHandleInfo; } }
        public ulong HeaderSize { get { return _headerSize; } }
        public byte InfoMask { get { return _infoMask; } }

        public ObjectHeader(Profile profile) : base(profile, null, 0)
        {
            ////_structure = profile.GetEntries("_OBJECT_HEADER");
            Structure s = GetStructureMember("Body");
            if (s != null)
                _structureSize = (long)s.Offset;
        }
        /// <summary>
        /// You should normally be using the virtual address
        /// But when you do pool scans you'll only have a physical address
        /// So in that case expect a VA=0 and a valid PA
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="dataProvider"></param>
        /// <param name="virtualAddress"></param>
        /// <param name="physicalAddress"></param>
        public ObjectHeader(Profile profile, DataProviderBase dataProvider, ulong virtualAddress=0, ulong physicalAddress=0) : base(profile, dataProvider, virtualAddress)
        {
            _physicalAddress = physicalAddress;
            if (virtualAddress == 0 && physicalAddress == 0)
                throw new ArgumentException("Error - Offset is ZERO for _OBJECT_HEADER");
            _is64 = (_profile.Architecture == "AMD64");
            _structureSize = (uint)_profile.GetStructureSize("_OBJECT_HEADER");
            if (_structureSize == -1)
                throw new ArgumentException("Error - Profile didn't contain a definition for _OBJECT_HEADER");
            AddressBase addressSpace = dataProvider.ActiveAddressSpace;
            if (virtualAddress == 0)
                _buffer = _dataProvider.ReadPhysicalMemory(_physicalAddress, (uint)_structureSize);
            else
            {
                _physicalAddress = addressSpace.vtop(_virtualAddress);
                _buffer = _dataProvider.ReadMemoryBlock(_virtualAddress, (uint)_structureSize);
            }
            //Debug.WriteLine("PADDR: " + _physicalAddress.ToString("X08"));
            Initialise();
        }
        private void Initialise()
        {
            try
            {
                ////_structure = _profile.GetEntries("_OBJECT_HEADER");
                Structure s = GetStructureMember("InfoMask");
                _infoMask = _buffer[s.Offset];
                ulong offsetMarker = _physicalAddress;
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
                    _headerNameInfo = new HeaderNameInfo(_profile, _dataProvider, physicalAddress: offsetMarker);
                    _headerSize += size;
                }
                if ((_infoMask & (byte)Mask.HEADER_HANDLE_INFO) > 0)
                {
                    uint size = (uint)_profile.GetStructureSize("_OBJECT_HEADER_HANDLE_INFO");
                    offsetMarker -= size;
                    _headerHandleInfo = new HeaderHandleInfo(_profile, _dataProvider, physicalAddress: offsetMarker);
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
            catch (Exception ex)
            {
                return;
            }
            
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
