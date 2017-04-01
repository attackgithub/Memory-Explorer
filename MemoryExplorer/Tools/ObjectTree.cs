using MemoryExplorer.Data;
using MemoryExplorer.ModelObjects;
using MemoryExplorer.Profiles;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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

        public ObjectTree(Profile_Deprecated profile, DataProviderBase dataProvider) : base(profile, dataProvider)
        {
            // check pre-reqs
            if (_profile == null || _profile.KernelBaseAddress == 0 || _profile.KernelAddressSpace == null || _dataProvider == null || _dataProvider.CacheFolder == "")
                throw new ArgumentException("Missing Prerequisites");
            _objectMap = new ObjectTreeMap();
            _objectMap.ObjectTreeRecords = new List<ObjectTreeRecord>();
            ObjectHeader oh = new ObjectHeader(_profile);
            _objectMap.ObjectHeaderSize = (ulong)oh.Size;
            _objectMap.ObjectDirectoryEntrySize = (uint)_profile.GetStructureSize("_OBJECT_DIRECTORY_ENTRY");
            _objectMap.ObjectDirectorySize = (uint)_profile.GetStructureSize("_OBJECT_DIRECTORY");
            

        }
        public List<ObjectTreeRecord> Run()
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
                    return Records;
                }
            }

            uint rootDirectoryOffset = (uint)_profile.GetConstant("ObpRootDirectoryObject");
            ulong vAddr = _profile.KernelBaseAddress + rootDirectoryOffset;
            _dataProvider.ActiveAddressSpace = _profile.KernelAddressSpace;
            ulong tableAddress = 0;
            if(_isx64)
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
            ProcessDirectory(tableAddress, 0);
            if (!_dataProvider.IsLive)
                PersistObjectMap(_objectMap, _dataProvider.CacheFolder + "\\object_tree_map.gz");
            return Records;
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
            ObjectDirectory objectDirectory = new ObjectDirectory(_profile, _dataProvider, virtualAddress: tableAddress);
            
            //byte[] buffer = _dataProvider.ReadMemoryBlock(tableAddress, _objectMap.ObjectDirectorySize);
            //var dll = _profile.GetStructureAssembly("_OBJECT_DIRECTORY");
            //Type t = dll.GetType("liveforensics.OBJECT_DIRECTORY");
            //GCHandle pinnedPacket = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            //objectDirectory = Marshal.PtrToStructure(Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0), t);
            //pinnedPacket.Free();

            byte[] hashBucket = objectDirectory.Members.HashBuckets;
            int count = hashBucket.Length / 8;

            for (int i = 0; i < count; i++)
            {
                ulong ptr = (BitConverter.ToUInt64(hashBucket, i * 8)) & 0xffffffffffff;
                if (ptr == 0)
                    continue;
                BuildTree(ptr, parent);
            }
        }
        private void BuildTree(ulong ptr, int parent)
        {
            ObjectDirectoryEntry objectDirectoryEntry = new ObjectDirectoryEntry(_profile, _dataProvider, virtualAddress: ptr);            
            //uint objectDirectoryEntrySize = (uint)_profile.GetStructureSize("_OBJECT_DIRECTORY_ENTRY");
            //var dll = _profile.GetStructureAssembly("_OBJECT_DIRECTORY_ENTRY");
            //Type t = dll.GetType("liveforensics.OBJECT_DIRECTORY_ENTRY");
            //byte[] buffer = _dataProvider.ReadMemoryBlock(ptr, _objectMap.ObjectDirectoryEntrySize);
            //GCHandle pinnedPacket = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            //objectDirectoryEntry = Marshal.PtrToStructure(Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0), t);
            //pinnedPacket.Free();
            ulong addr = (objectDirectoryEntry.Members.Object - _objectMap.ObjectHeaderSize) & 0xffffffffffff;
            ObjectHeader oh = new ObjectHeader(_profile, _dataProvider, addr);
            string name = _profile.GetObjectName(oh.TypeInfo);
            int index = _index++;
            if(name == "Directory")
            {
                ProcessDirectory(objectDirectoryEntry.Members.Object & 0xffffffffffff, index);
            }
            //if (oh.HeaderNameInfo != null)
            //    name += ("\t" + oh.HeaderNameInfo.Name);
            //Debug.WriteLine("[" + parent + "][" + index + "]" + addr.ToString("X08") + " (0x" + oh.PhysicalAddress.ToString("X08") + ")(p)\t" + name);
            _objectMap.ObjectTreeRecords.Add(new ObjectTreeRecord() { ObjectHeaderVirtualAddress = addr, Parent = parent, Index = index });
            ulong chainlinkPtr = (objectDirectoryEntry.Members.ChainLink) & 0xffffffffffff;
            if (chainlinkPtr != 0)
            {
                BuildTree(chainlinkPtr, parent);
            }
        }
        public List<ObjectTreeRecord> Records { get { return _objectMap.ObjectTreeRecords; } }
    }
}
