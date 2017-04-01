using MemoryExplorer.Data;
using MemoryExplorer.ModelObjects;
using MemoryExplorer.Processes;
using MemoryExplorer.Profiles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.Tools
{
    public class PsList3 : ToolBase
    {
        /// <summary>
        /// Method 3 - PspCidTable
        /// </summary>
        /// <prerequisites>
        /// Active Profile
        /// Profile must contain an active project
        /// Profile must contain a valid Architecture
        /// Project must contain a filePath pointing to the imageFile
        /// Project must contain a valid kernelBase
        /// Project must contain a valid kernelAs - kernel address space
        /// Project must contain the image file MD5 hash (fileHash)
        /// </prerequisites>
        /// <param name="profile"></param>
        /// <param name="dataProvider"></param>
        /// <param name="processList"></param>
        List<ProcessInfo> _processList;
        public PsList3(Profile_Deprecated profile, DataProviderBase dataProvider, List<ProcessInfo> processList = null) : base(profile, dataProvider)
        {
            _processList = processList;
            // check pre-reqs
            if (_profile == null || _profile.KernelBaseAddress == 0 || _profile.KernelAddressSpace == null)
                throw new ArgumentException("Missing Prerequisites");
        }
        public HashSet<ulong> Run()
        {
            // first let's see if it already exists
            FileInfo cachedFile = new FileInfo(_dataProvider.CacheFolder + "\\pslist_PspCidTable.gz");
            if (cachedFile.Exists && !_dataProvider.IsLive)
            {
                OffsetMap cachedMap = RetrieveOffsetMap(cachedFile);
                if (cachedMap != null)
                    return cachedMap.OffsetRecords;
            }
            HashSet<ulong> results = new HashSet<ulong>();
            uint tableOffset = (uint)_profile.GetConstant("PspCidTable");
            ulong vAddr = _profile.KernelBaseAddress + tableOffset;
            ulong tableAddress = 0;
            if (_isx64)
            {
                var v = _dataProvider.ReadUInt64(vAddr);
                if (v == null)
                    return null;
                tableAddress = (ulong)v & 0xffffffffffff;
            }
            else
            {
                var v = _dataProvider.ReadUInt32(vAddr);
                if (v == null)
                    return null;
                tableAddress = (ulong)v;
            }
            HandleTable ht = new HandleTable(_profile, _dataProvider, tableAddress);
            List<HandleTableEntry> records = EnumerateHandles(ht.TableStartAddress, ht.Level);
            ulong bodyOffset = (ulong)_profile.GetOffset("_OBJECT_HEADER", "Body");
            foreach (HandleTableEntry e in records)
            {
                try
                {
                    vAddr = e.ObjectPointer - bodyOffset;
                    ObjectHeader header = new ObjectHeader(_profile, _dataProvider, vAddr);
                    string objectName = GetObjectName(header.TypeInfo);
                    if (objectName == "Process")
                        results.Add(e.ObjectPointer);
                }
                catch (Exception)
                {
                    continue;
                }
            }
            return TrySave(results);
        }
        private HashSet<ulong> TrySave(HashSet<ulong> results)
        {
            if (results.Count == 0)
                return null;
            if (_dataProvider.IsLive)
                return results;
            OffsetMap map = new OffsetMap();
            map.OffsetRecords = results;
            if (!_dataProvider.IsLive)
                PersistOffsetMap(map, _dataProvider.CacheFolder + "\\pslist_PspCidTable");
            return results;
        }
    }
}
