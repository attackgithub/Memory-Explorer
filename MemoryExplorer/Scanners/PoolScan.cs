using MemoryExplorer.Data;
using MemoryExplorer.ModelObjects;
using MemoryExplorer.Profiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.Scanners
{
    public class PoolScan
    {
        DataProviderBase _dataProvider;
        StringSearch _scanner;
        PoolType _poolType;
        Profile _profile;
        ulong _objectTypeIndex = 0;
        string _poolTypeName;

        public PoolScan(Profile profile, DataProviderBase dataProvider, PoolType poolType)
        {
            _profile = profile;
            _dataProvider = dataProvider;
            _poolType = poolType;
            _scanner = new StringSearch(_dataProvider);
            switch (_poolType)
            {
                case PoolType.Driver:
                    _scanner.AddNeedle("Driv");
                    _poolTypeName = "Driv";
                    _objectTypeIndex = GetObjectTypeIndex("Driver");
                    break;
                default:
                    break;
            }
        }
        public List<ulong> Scan()
        {            
            ulong tagOffset = (ulong)_profile.GetOffset("_POOL_HEADER", "PoolTag");
            ulong inforMaskOffset = (ulong)_profile.GetOffset("_OBJECT_HEADER", "InfoMask");
            ulong poolAlign = _profile.PoolAlign;
            List<ulong> matches = new List<ulong>();
            ulong maxHeader = GetMaximumHeaderSize();
            foreach (var item in _scanner.Scan())
            {
                List<ulong> hitList = item[_poolTypeName];
                foreach (ulong hit in hitList)
                {
                    ulong realHit = hit - tagOffset;
                    PoolHeader h = new PoolHeader(_profile, _dataProvider, physicalAddress: realHit);
                    var bs = h.BlockSize;
                    var pt = h.PoolType;
                    var tag = h.Tag;
                    var index = h.PoolIndex;
                    var ep = h.ProcessBilled;
                    ulong allocationSize = bs * poolAlign;
                    try
                    {
                        for (ulong i = realHit; i < realHit + maxHeader + inforMaskOffset; i += poolAlign)
                        {
                            ObjectHeader oh = new ObjectHeader(_profile, _dataProvider, physicalAddress: i);
                            if (oh.TypeInfo != _objectTypeIndex)
                                continue;
                            if (oh.HeaderSize > i - realHit)
                                continue;
                            if (oh.PointerCount > 0x100000 || oh.HandleCount > 0x1000)
                                continue;
                            if (oh.TypeInfo > 50 || oh.TypeInfo < 1)
                                continue;
                            if (oh.HeaderNameInfo == null) // specific to driver type?
                                continue;
                            // there's a good chance we now have a valid ObjectHeader
                            matches.Add(i);
                        }
                    }
                    catch (Exception ex)
                    {
                        continue;
                    }
                    
                }            
            }
            return matches;
        }
        ulong GetObjectTypeIndex(string name)
        {
            try
            {
                List<ObjectTypeRecord> lookup = _profile.ObjectTypeList;
                foreach (ObjectTypeRecord item in lookup)
                {
                    if (item.Name == name)
                        return item.Index;
                }
                return 0;
            }
            catch { return 0; }
        }
        ulong GetMaximumHeaderSize()
        {
            ulong result = 0;
            result += (uint)_profile.GetStructureSize("_OBJECT_HEADER_CREATOR_INFO");
            result += (uint)_profile.GetStructureSize("_OBJECT_HEADER_NAME_INFO");
            result += (uint)_profile.GetStructureSize("_OBJECT_HEADER_HANDLE_INFO");
            result += (uint)_profile.GetStructureSize("_OBJECT_HEADER_QUOTA_INFO");
            result += (uint)_profile.GetStructureSize("_OBJECT_HEADER_PROCESS_INFO");
            result += (uint)_profile.GetStructureSize("_OBJECT_HEADER_AUDIT_INFO");
            result += (uint)_profile.GetStructureSize("_OBJECT_HEADER_PADDING_INFO");
            return result;
        }
    }
}
