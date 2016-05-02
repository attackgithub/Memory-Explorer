using MemoryExplorer.Data;
using MemoryExplorer.ModelObjects;
using MemoryExplorer.Profiles;
using MemoryExplorer.Scanners;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.Tools
{
    public class DriverScan : ToolBase
    {
        public DriverScan(Profile profile, DataProviderBase dataProvider) : base(profile, dataProvider)
        {
            // check pre-reqs
            if (_profile == null || _profile.KernelBaseAddress == 0 || _profile.KernelAddressSpace == null)
                throw new ArgumentException("Missing Prerequisites");
        }
        public List<DriverObject> Run()
        {
            FileInfo cachedFile = new FileInfo(_dataProvider.CacheFolder + "\\driverscan.gz");
            if (cachedFile.Exists && !_dataProvider.IsLive)
            {
                OffsetMap cachedMap = RetrieveOffsetMap(cachedFile);
                if (cachedMap != null)
                    return DoIt(cachedMap.OffsetRecords);
            }                
            PoolScan scanner = new PoolScan(_profile, _dataProvider, PoolType.Driver);
            var results = scanner.Scan();
            OffsetMap map = new OffsetMap();
            map.OffsetRecords = new HashSet<ulong>();
            foreach (ulong item in results)
                map.OffsetRecords.Add(item);
            if (!_dataProvider.IsLive)
                PersistOffsetMap(map, _dataProvider.CacheFolder + "\\driverscan");
            return DoIt(map.OffsetRecords);
        }
        List<DriverObject> DoIt(HashSet<ulong> records)
        {
            List<DriverObject> drivers = new List<DriverObject>();
            foreach (var item in records)
            {
                ObjectHeader oh = new ObjectHeader(_profile, _dataProvider, physicalAddress: item);
                string name = oh.HeaderNameInfo.Name;
                ulong offset = (ulong)oh.Size + item;
                DriverObject drvObj = new DriverObject(_profile, _dataProvider, oh, physicalAddress: offset);
                drvObj.Name = name;
                drivers.Add(drvObj);
            }
            return drivers;
        }
        
    }
}
