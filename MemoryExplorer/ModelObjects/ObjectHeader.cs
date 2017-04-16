using MemoryExplorer.Address;
using MemoryExplorer.Data;
using MemoryExplorer.Model;
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

        private dynamic _oh;
        private byte _infoMask;
        private ulong _headerSize = 0;
        private HeaderNameInfo _headerNameInfo = null;
        private HeaderHandleInfo _headerHandleInfo = null;
        private HeaderCreatorInfo _headerCreatorInfo = null;
        private HeaderQuotaInfo _headerQuotaInfo = null;
        private HeaderProcessInfo _headerProcessInfo = null;
        private HeaderAuditInfo _headerAuditInfo = null;

        public HeaderNameInfo HeaderNameInfo { get { return _headerNameInfo; } }
        public HeaderHandleInfo HeaderHandleInfo { get { return _headerHandleInfo; } }
        public ulong HeaderSize { get { return _headerSize; } }
        public byte InfoMask { get { return _infoMask; } }

        public ObjectHeader(DataModel model) : base(model, 0)
        {
            _structureSize = (long)_profile.GetStructureSize("_OBJECT_HEADER");
            
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
        public ObjectHeader(DataModel model, ulong virtualAddress=0, ulong physicalAddress=0) : base(model, virtualAddress, physicalAddress)
        {
            if (virtualAddress == 0 && physicalAddress == 0)
                throw new ArgumentException("Error - Offset is ZERO for _OBJECT_HEADER");
            _oh = _profile.GetStructure("_OBJECT_HEADER", _physicalAddress);


            _is64 = (_profile.Architecture == "AMD64");
            _headerSize = (uint)_profile.GetStructureSize("_OBJECT_HEADER");
            try
            {
                _headerSize -= (uint)_oh.Body.MxStructureSize;
            }
            catch { }
            
            Initialise();
        }
        private void Initialise()
        {
            try
            {
                ////_structure = _profile.GetEntries("_OBJECT_HEADER");
                var s = _oh.InfoMask;
                _infoMask = (byte)s;
                ulong offsetMarker = _physicalAddress;
                Debug.WriteLine("_OBJECT_HEADER starts at: 0x" + offsetMarker.ToString("X8"));
                if ((_infoMask & (byte)Mask.HEADER_CREATOR_INFO) > 0)
                {
                    uint size = (uint)_profile.GetStructureSize("_OBJECT_HEADER_CREATOR_INFO");
                    offsetMarker -= size;
                    Debug.WriteLine("\tHeaderCreatorInfo found at: 0x" + offsetMarker.ToString("X8") + " Size: " + size);
                    _headerCreatorInfo = new HeaderCreatorInfo(_model, physicalAddress: offsetMarker);
                    _headerSize += size;
                }
                if ((_infoMask & (byte)Mask.HEADER_NAME_INFO) > 0)
                {
                    uint size = (uint)_profile.GetStructureSize("_OBJECT_HEADER_NAME_INFO");
                    offsetMarker -= size;
                    Debug.WriteLine("\tHeaderNameInfo found at: 0x" + offsetMarker.ToString("X8") + " Size: " + size);
                    _headerNameInfo = new HeaderNameInfo(_model, physicalAddress: offsetMarker);
                    _headerSize += size;
                }
                if ((_infoMask & (byte)Mask.HEADER_HANDLE_INFO) > 0)
                {
                    uint size = (uint)_profile.GetStructureSize("_OBJECT_HEADER_HANDLE_INFO");
                    offsetMarker -= size;
                    Debug.WriteLine("\tHeaderHandleInfo found at: 0x" + offsetMarker.ToString("X8") + " Size: " + size);
                    _headerHandleInfo = new HeaderHandleInfo(_model, physicalAddress: offsetMarker);
                    _headerSize += size;
                }
                if ((_infoMask & (byte)Mask.HEADER_QUOTA_INFO) > 0)
                {
                    uint size = (uint)_profile.GetStructureSize("_OBJECT_HEADER_QUOTA_INFO");
                    offsetMarker -= size;
                    Debug.WriteLine("\tHeaderQuotaInfo found at: 0x" + offsetMarker.ToString("X8") + " Size: " + size);
                    _headerQuotaInfo = new HeaderQuotaInfo(_model, physicalAddress: offsetMarker);
                    _headerSize += size;
                }
                if ((_infoMask & (byte)Mask.HEADER_PROCESS_INFO) > 0)
                {
                    uint size = (uint)_profile.GetStructureSize("_OBJECT_HEADER_PROCESS_INFO");
                    offsetMarker -= size;
                    Debug.WriteLine("\tHeaderProcessInfo found at: 0x" + offsetMarker.ToString("X8") + " Size: " + size);
                    _headerProcessInfo = new HeaderProcessInfo(_model, physicalAddress: offsetMarker);
                    _headerSize += size;
                }
                if ((_infoMask & (byte)Mask.HEADER_AUDIT_INFO) > 0)
                {
                    uint size = (uint)_profile.GetStructureSize("_OBJECT_HEADER_AUDIT_INFO");
                    offsetMarker -= size;
                    Debug.WriteLine("\tHeaderAuditInfo found at: 0x" + offsetMarker.ToString("X8") + " Size: " + size);
                    _headerAuditInfo = new HeaderAuditInfo(_model, physicalAddress: offsetMarker);
                    _headerSize += size;
                }
            }
            catch (Exception)
            {
                return;
            }
            
        }
        public dynamic dynamicObject
        {
            get { return _oh; }
        }
        public ulong TypeInfo
        {
            get
            {
                try
                {
                    var s = _oh.TypeIndex;
                    return (ulong)s;
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
                    var s = _oh.PointerCount;
                    return (ulong)s;
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
                    var s = _oh.HandleCount;
                    return (ulong)s;
                }
                catch { return 0; }
            }
        }
        public string Name
        {
            get
            {
                try
                {
                    if (_headerNameInfo != null)
                        return _headerNameInfo.Name;
                    return "";
                }
                catch (Exception)
                {
                    return "";
                }
            }
        }
    }
}
