using MemoryExplorer.Data;
using MemoryExplorer.Model;
using MemoryExplorer.ModelObjects;
using MemoryExplorer.Profiles;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;

namespace MemoryExplorer.Tools
{
    class ObjectTreeMap
    {
        public string Md5;
        public ulong ObjectHeaderSize;
        public uint ObjectDirectorySize;
        public uint ObjectDirectoryEntrySize;
        public byte ObjectTypeCookie;
        public List<ObjectTreeRecord> ObjectTreeRecords;
    }
    public class ObjectTreeRecord
    {
        public ulong ObjectHeaderVirtualAddress;
        public int Parent;
        public int Index;
    }
    public class ObjectTree : ToolBase
    {
        private int _index = 1;
        ObjectTreeMap _objectMap = null;

        public ObjectTree(DataModel model) : base(model)
        {
            // check pre-reqs
            if (_profile == null || _dataProvider == null || model.KernelBaseAddress == 0 || model.KernelAddressSpace == null || _dataProvider.CacheFolder == "")
                throw new ArgumentException("Missing Prerequisites");
            _objectMap = new ObjectTreeMap();
            _objectMap.ObjectTreeRecords = new List<ObjectTreeRecord>();
            ObjectHeader oh = new ObjectHeader(_model);            
            _objectMap.ObjectHeaderSize = (ulong)oh.Size; // this is wrong, but will be corrected later
            _objectMap.ObjectDirectoryEntrySize = (uint)_profile.GetStructureSize("_OBJECT_DIRECTORY_ENTRY");
            _objectMap.ObjectDirectorySize = (uint)_profile.GetStructureSize("_OBJECT_DIRECTORY");
            // in Windows 10 they obfuscate the object types with this cookie and some jiggery pokery
            try
            {
                ulong offset = (ulong)_profile.GetConstant("ObHeaderCookie");
                ulong location = model.KernelBaseAddress + offset;
                var c = _dataProvider.ReadByte(location);
                if (c != null)
                    _objectMap.ObjectTypeCookie = (byte)c;
            }
            catch { }
            Run();
        }
        private void Run()
        {
            _isx64 = (_profile.Architecture == "AMD64");
            // first let's see if it already exists
            FileInfo cachedFile = new FileInfo(_dataProvider.CacheFolder + "\\object_tree_map.gz");
            if (cachedFile.Exists && !_dataProvider.IsLive)
            {
                ObjectTreeMap otm = RetrieveObjectMap(cachedFile);
                if (otm != null)
                {
                    _objectMap = otm;
                    return;
                }
            }

            uint rootDirectoryOffset = 0;
            try
            {
                rootDirectoryOffset = (uint)_profile.GetConstant("ObpRootDirectoryObject");
            }
            catch (Exception)
            {
                rootDirectoryOffset = (uint)_profile.GetConstant("_ObpRootDirectoryObject");
            }
            

            ulong vAddr = _model.KernelBaseAddress + rootDirectoryOffset;
            _model.KernelAddressSpace = _model.ActiveAddressSpace;
            ulong tableAddress = 0;
            if(_isx64)
            {
                var v = _dataProvider.ReadUInt64(vAddr);
                if (v == null)
                    return;
                tableAddress = (ulong)v & 0xffffffffffff;
            }
            else
            {
                var v = _dataProvider.ReadUInt32(vAddr);
                if (v == null)
                    return;
                tableAddress = (ulong)v;
            }
            ProcessDirectory(tableAddress, 0);
            //if (!_dataProvider.IsLive)
            //    PersistObjectMap(_objectMap, _dataProvider.CacheFolder + "\\object_tree_map.gz");
            return;
        }
        private void PersistObjectMap(ObjectTreeMap source, string fileName)
        {
            byte[] bytesToCompress = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(source));
            using (FileStream fileToCompress = File.Create(fileName))
            using (GZipStream compressionStream = new GZipStream(fileToCompress, CompressionMode.Compress))
            {
                compressionStream.Write(bytesToCompress, 0, bytesToCompress.Length);
            }
        }
        private ObjectTreeMap RetrieveObjectMap(FileInfo sourceFile)
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
                return JsonConvert.DeserializeObject<ObjectTreeMap>(Encoding.UTF8.GetString(decompressed));
            }
            catch { return null; }
        }
        private void ProcessDirectory(ulong tableAddress, int parent)
        {
            ObjectDirectory objectDirectory = new ObjectDirectory(_model, virtualAddress: tableAddress);
            if(objectDirectory.HashBuckets != null)
            {
                foreach(var ptr in objectDirectory.HashBuckets)
                {
                    if (ptr == 0)
                        continue;
                    BuildTree((ulong)ptr, parent);
                }
            }
        }
        private void BuildTree(ulong ptr, int parent)
        {
            ObjectDirectoryEntry objectDirectoryEntry = new ObjectDirectoryEntry(_model, virtualAddress: ptr);            
            // some jiggery to get the real object header size
            ulong addr = (objectDirectoryEntry.Object - _objectMap.ObjectHeaderSize) & 0xffffffffffff;
            ObjectHeader oh = new ObjectHeader(_model, addr);
            _objectMap.ObjectHeaderSize = oh.HeaderSize;
            addr = (objectDirectoryEntry.Object - _objectMap.ObjectHeaderSize) & 0xffffffffffff;
            oh = new ObjectHeader(_model, addr);
            string name = oh.Name;
            string objectType = GetObjectName(oh.TypeInfo, addr, _objectMap.ObjectTypeCookie);
            int index = _index++;
            Debug.WriteLine("Object Tree: 0x" + addr.ToString("X8") + "\tType: " + oh.TypeInfo + "\tName: " + name);
            //if(name == "Directory")
            //{
            //    ProcessDirectory(objectDirectoryEntry.Members.Object & 0xffffffffffff, index);
            //}
            //if (oh.HeaderNameInfo != null)
            //    name += ("\t" + oh.HeaderNameInfo.Name);
            //Debug.WriteLine("[" + parent + "][" + index + "]" + addr.ToString("X08") + " (0x" + oh.PhysicalAddress.ToString("X08") + ")(p)\t" + name);
            _objectMap.ObjectTreeRecords.Add(new ObjectTreeRecord() { ObjectHeaderVirtualAddress = addr, Parent = parent, Index = index });
            ulong chainlinkPtr = (objectDirectoryEntry.ChainLink) & 0xffffffffffff;
            if (chainlinkPtr != 0)
            {
                BuildTree(chainlinkPtr, parent);
            }
        }
        
        public List<ObjectTreeRecord> Records { get { return _objectMap.ObjectTreeRecords; } }
    }
}
