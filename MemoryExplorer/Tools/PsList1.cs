using MemoryExplorer.Data;
using MemoryExplorer.ModelObjects;
using MemoryExplorer.Profiles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.Tools
{
    public class PsList1 : ToolBase
    {
        /// <summary>
        /// Method 1 to find the Process List uses the PsActiveProcessHead pointer which is embedded in the kernel image
        /// This pointer points to a doubly linked list of pointers in a LIST_ENTRY structure whic is itself embedded in
        /// each EPROCESS structure in the ActiveProcessLinks member. Thus if you walk the list, you'll step through aa
        /// the active EPROCESS structures and thus all the active processes
        /// </summary>
        /// <prerequisites>
        /// Active Profile
        /// Profile must contain a valid Architecture
        /// Profile must contain a valid KernelBaseAddress
        /// Profile must contain a valid KernelAddressSpace
        /// </prerequisites>
        /// <param name="profile"></param>
        public PsList1(Profile_Deprecated profile, DataProviderBase dataProvider) : base(profile, dataProvider)
        {
            // check pre-reqs
            if (_profile == null || _profile.KernelBaseAddress == 0 || _profile.KernelAddressSpace == null)
                throw new ArgumentException("Missing Prerequisites");
        }
        public HashSet<ulong> Run()
        {
            // first let's see if it already exists
            FileInfo cachedFile = new FileInfo(_dataProvider.CacheFolder + "\\pslist_PsActiveProcessHead.gz");
            if (cachedFile.Exists && !_dataProvider.IsLive)
            {
                OffsetMap cachedMap = RetrieveOffsetMap(cachedFile);
                if (cachedMap != null)
                    return cachedMap.OffsetRecords;
            }
                
            HashSet<ulong> results = new HashSet<ulong>();
            uint processHeadOffset = (uint)_profile.GetConstant("PsActiveProcessHead");
            ulong vAddr = _profile.KernelBaseAddress + processHeadOffset;
            _dataProvider.ActiveAddressSpace = _profile.KernelAddressSpace;
            LIST_ENTRY le = new LIST_ENTRY(_dataProvider, vAddr);
            ulong apl = (ulong)_profile.GetOffset("_EPROCESS", "ActiveProcessLinks");
            List<LIST_ENTRY> lists = FindAllLists(_dataProvider, le);
            foreach (LIST_ENTRY entry in lists)
            {
                if (entry.VirtualAddress == vAddr)
                    continue;
                if (entry.VirtualAddress == 0)
                    continue;
                results.Add(entry.VirtualAddress - apl);
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
                PersistOffsetMap(map, _dataProvider.CacheFolder + "\\pslist_PsActiveProcessHead");
            return results;
        }
    }
}
