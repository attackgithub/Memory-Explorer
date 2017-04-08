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
    public class PsList4 : ToolBase
    {
        /// <summary>
        /// Method 4 - pslist_Sessions
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
        public PsList4(Profile profile, DataProviderBase dataProvider, List<ProcessInfo> processList = null) : base(profile, dataProvider)
        {
            _processList = processList;
            // check pre-reqs
            if (_profile == null || _dataProvider.KernelBaseAddress == 0 || _profile.KernelAddressSpace == null)
                throw new ArgumentException("Missing Prerequisites");
        }
        public HashSet<ulong> Run()
        {
            // first let's see if it already exists
            FileInfo cachedFile = new FileInfo(_dataProvider.CacheFolder + "\\pslist_Sessions.gz");
            if (cachedFile.Exists && !_dataProvider.IsLive)
            {
                OffsetMap cachedMap = RetrieveOffsetMap(cachedFile);
                if (cachedMap != null)
                    return cachedMap.OffsetRecords;
            }
            HashSet<ulong> results = new HashSet<ulong>();
            HashSet<ulong> sessionList = new HashSet<ulong>(); // a list of pointers to _MM_SESSION_SPAVE objects
            if (_processList != null)
            {
                foreach (ProcessInfo info in _processList)
                {
                    if (info.Session != 0)
                        sessionList.Add(info.Session);
                }
            }
            ////ulong sOffset = (ulong)_profile.GetOffset("_EPROCESS", "SessionProcessLinks");
            ////ulong plOffset = (ulong)_profile.GetOffset("_MM_SESSION_SPACE", "ProcessList");
            foreach (ulong item in sessionList)
            {
                SessionSpace ss = new SessionSpace(_profile, _dataProvider, item);
                LIST_ENTRY sle = ss.ProcessList;
                List<LIST_ENTRY> procLists = FindAllLists(_dataProvider, sle);
                HashSet<ulong> tempList = new HashSet<ulong>();
                foreach (LIST_ENTRY entry in procLists)
                {
                    tempList.Add(entry.Blink);
                    tempList.Add(entry.Flink);
                }
                //foreach (ulong ul in tempList)
                //{
                //    if (ul - plOffset == item)
                //        continue;
                //    if (ul == 0)
                //        continue;
                //    results.Add(ul - sOffset);
                //}
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
                PersistOffsetMap(map, _dataProvider.CacheFolder + "\\pslist_Sessions");
            return results;
        }
    }
}
