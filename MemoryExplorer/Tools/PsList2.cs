using MemoryExplorer.Data;
using MemoryExplorer.ModelObjects;
using MemoryExplorer.Processes;
using MemoryExplorer.Profiles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.Tools
{
    public class PsList2 : ToolBase
    {
        /// <summary>
        /// Method 2  pslist_CSRSS
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
        List<ProcessInfo> _processList;
        public PsList2(Profile profile, DataProviderBase dataProvider, List<ProcessInfo> processList=null) : base(profile, dataProvider)
        {
            _processList = processList;
            // check pre-reqs
            if (_profile == null || _profile.KernelBaseAddress == 0 || _profile.KernelAddressSpace == null)
                throw new ArgumentException("Missing Prerequisites");
        }
        public HashSet<ulong> Run()
        {
            // first let's see if it already exists
            FileInfo cachedFile = new FileInfo(_dataProvider.CacheFolder + "\\pslist_CSRSS.gz");
            if (cachedFile.Exists && !_dataProvider.IsLive)
            {
                OffsetMap cachedMap = RetrieveOffsetMap(cachedFile);
                if (cachedMap != null)
                    return cachedMap.OffsetRecords;
            }
            HashSet<ulong> results = new HashSet<ulong>();
            // check to see if we already have a process list with CSRSS in it
            if(_processList != null)
            {
                foreach (ProcessInfo info in _processList)
                {
                    try
                    {
                        if (info.ProcessName == "csrss.exe")
                        {
                            ulong handleTableAddress = info.ObjectTableAddress;
                            HandleTable ht = new HandleTable(_profile, _dataProvider, handleTableAddress);
                            List<HandleTableEntry> records = EnumerateHandles(ht.TableStartAddress, ht.Level);
                            foreach (HandleTableEntry e in records)
                            {
                                try
                                {
                                    ObjectHeader header = new ObjectHeader(_profile, _dataProvider, e.ObjectPointer);
                                    //Debug.WriteLine(e.ObjectPointer.ToString("X8"));
                                    if (e.ObjectPointer == 0xE0019B4EC010)
                                        Debug.WriteLine("eee");
                                    string objectName = GetObjectName(e.TypeInfo);
                                    if (objectName == "Process")
                                        results.Add(e.ObjectPointer + (ulong)header.Size);
                                }
                                catch (Exception)
                                {
                                    continue;
                                }                                
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        continue;
                    }                    
                }
                if(results.Count > 0)
                    return TrySave(results);
            }
            // either we didn't have a process list, or it didn't contain any CSRSS processes
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
                EProcess ep = new EProcess(_profile, _dataProvider, entry.VirtualAddress - apl);
                if (ep.ImageFileName == "csrss.exe")
                {
                    ulong handleTableAddress = ep.ObjectTable;
                    HandleTable ht = new HandleTable(_profile, _dataProvider, handleTableAddress);
                    List<HandleTableEntry> records = EnumerateHandles(ht.TableStartAddress, ht.Level);
                    foreach (HandleTableEntry e in records)
                    {
                        try
                        {
                            ObjectHeader header = new ObjectHeader(_profile, _dataProvider, e.ObjectPointer);
                            string objectName = GetObjectName(e.TypeInfo);
                            if (objectName == "Process")
                                results.Add(e.ObjectPointer + (ulong)header.Size);
                        }
                        catch (Exception)
                        {
                            continue;
                        }                        
                    }
                }
            }
            return TrySave(results);
        }
        private HashSet<ulong> TrySave(HashSet<ulong> results)
        {
            //OffsetMap map = new OffsetMap();
            //map.OffsetRecords = results;
            //if (!_dataProvider.IsLive)
            //    PersistOffsetMap(map, _dataProvider.CacheFolder + "\\pslist_CSRSS");
            return results;
        }
    }
}
