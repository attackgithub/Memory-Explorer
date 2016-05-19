using MemoryExplorer.Data;
using MemoryExplorer.ModelObjects;
using MemoryExplorer.Profiles;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.Tools
{
    public class HandleEntryMap
    {
        public string Md5;
        public List<HandleTableEntry> HandleRecords;
    }
    public class Handles :ToolBase
    {
        private ulong _pid;
        private ulong _handleTableAddress;
        private HandleEntryMap _handleEntryMap;

        public Handles(Profile profile, DataProviderBase dataProvider, ulong pid, ulong handleTableAddress) : base(profile, dataProvider)
        {
            // check pre-reqs
            if (_profile == null || _profile.KernelBaseAddress == 0 || _profile.KernelAddressSpace == null)
                throw new ArgumentException("Missing Prerequisites");
            _pid = pid;
            _handleTableAddress = handleTableAddress;
        }
        public List<HandleTableEntry> Run()
        {
            List<HandleTableEntry> results = new List<HandleTableEntry>();
            // first let's see if it already exists
            string filename = "handles_" + _pid.ToString() + ".gz";
            FileInfo cachedFile = new FileInfo(_dataProvider.CacheFolder + "\\" + filename);
            if (cachedFile.Exists && !_dataProvider.IsLive)
            {
                HandleEntryMap hem = RetrieveHandleMap(cachedFile);
                if (hem != null)
                {
                    _handleEntryMap = hem;
                    return _handleEntryMap.HandleRecords;
                }
            }
           

            try
            {
                Debug.WriteLine("Handle Table Address: 0x" + _handleTableAddress.ToString("X"));
                HandleTable ht = new HandleTable(_profile, _dataProvider, _handleTableAddress);
                List<HandleTableEntry> records = EnumerateHandles(ht.TableStartAddress, ht.Level);
                foreach (HandleTableEntry e in records)
                {
                    ulong pa = _dataProvider.ActiveAddressSpace.vtop(e.ObjectPointer);
                    if (pa == 0)
                        continue;
                    results.Add(e);
                }  
            }
            catch (Exception)
            {
                return null;
            }
            if (!_dataProvider.IsLive && results.Count > 0)
            {
                _handleEntryMap = new HandleEntryMap();
                _handleEntryMap.HandleRecords = results;
                PersistHandleMap(_handleEntryMap, _dataProvider.CacheFolder + "\\" + filename);
            }
            return results;
        }
        HandleEntryMap RetrieveHandleMap(FileInfo sourceFile)
        {
            try
            {
                byte[] buffer;
                using (FileStream fs = new FileStream(sourceFile.FullName, FileMode.Open, FileAccess.Read))
                using (BinaryReader br = new BinaryReader(fs))
                {
                    buffer = br.ReadBytes((int)sourceFile.Length);
                }
                byte[] decompressed = Decompress(buffer);
                return JsonConvert.DeserializeObject<HandleEntryMap>(Encoding.UTF8.GetString(decompressed));
            }
            catch { return null; }
        }
        public void PersistHandleMap(HandleEntryMap source, string fileName)
        {
            byte[] bytesToCompress = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(source));
            using (FileStream fileToCompress = File.Create(fileName))
            using (GZipStream compressionStream = new GZipStream(fileToCompress, CompressionMode.Compress))
            {
                compressionStream.Write(bytesToCompress, 0, bytesToCompress.Length);
            }
        }
    }
}
