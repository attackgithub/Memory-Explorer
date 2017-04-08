using MemoryExplorer.Address;
using MemoryExplorer.Data;
using MemoryExplorer.Profiles;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace MemoryExplorer.ModelObjects
{
    public class ObjectTypeMap
    {
        public string Md5;
        public UInt64 StartAddress;
        public UInt64 EndAddress;
        public List<ObjectTypeRecord> ObjectTypeRecords;
    }
    public class ObjectTypeRecord
    {
        public ulong Index;
        public string Name;
    }
    public class ObjectTypes : StructureBase
    {
        //List<ObjectTypeRecord> _records = new List<ObjectTypeRecord>();
        ObjectTypeMap _objectMap = null;

        public ObjectTypes(DataProviderBase dataProvider, Profile profile) : base(profile, dataProvider, 0)
        {
            _is64 = (_profile.Architecture == "AMD64");
            _objectMap = new ObjectTypeMap();
            _objectMap.ObjectTypeRecords = new List<ObjectTypeRecord>();

            // first let's see if it already exists
            FileInfo cachedFile = new FileInfo(_dataProvider.CacheFolder + "\\object_type_map.gz");
            if (cachedFile.Exists && !dataProvider.IsLive)
            {
                ObjectTypeMap otm = RetrieveObjectMap(cachedFile);
                if (otm != null)
                {
                    _objectMap = otm;
                    return;
                }
            }

            AddressBase kernelAS;
            if (_is64)
                kernelAS = _dataProvider.KernelAddressSpace as AddressSpacex64;
            else
                kernelAS = _dataProvider.KernelAddressSpace as AddressSpacex86Pae;
            uint indexTableOffset = (uint)_profile.GetConstant("ObpObjectTypes");
            ulong startOffset = _dataProvider.KernelBaseAddress + indexTableOffset;
            ulong pAddr = kernelAS.vtop(startOffset, _dataProvider.IsLive);
            if (pAddr == 0)
                return;
            _buffer = _dataProvider.ReadMemory(pAddr & 0xfffffffff000, 1);
            ulong ptr = 0;
            if (_is64)
                ptr = ReadUInt64((int)(pAddr & 0xfff));
            else
                ptr = ReadUInt32((int)(pAddr & 0xfff));
            ObjectType ot = new ObjectType(_profile, _dataProvider, ptr);
            try
            {
                int count = (int)ot.TotalNumberOfObjects;
                for (int i = 0; i < count; i++)
                {
                    if (_is64)
                    {
                        startOffset = _dataProvider.KernelBaseAddress + indexTableOffset + (uint)(i * 8);
                        ptr = (BitConverter.ToUInt64(_dataProvider.ReadMemoryBlock(startOffset, 8), 0) & 0xffffffffffff);
                    }
                    else
                    {
                        startOffset = _dataProvider.KernelBaseAddress + indexTableOffset + (uint)(i * 4);
                        ptr = (BitConverter.ToUInt32(_dataProvider.ReadMemoryBlock(startOffset, 4), 0));
                    }
                    ot = new ObjectType(_profile, _dataProvider, ptr);
                    ObjectTypeRecord otr = new ObjectTypeRecord();
                    otr.Name = ot.Name;
                    otr.Index = ot.Index;
                    if (otr.Index == 0 || otr.Name == "")
                        continue;
                    _objectMap.ObjectTypeRecords.Add(otr);
                }
                //if (!dataProvider.IsLive)
                //    PersistObjectMap(_objectMap, _dataProvider.CacheFolder + "\\object_type_map.gz");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error: " + ex.Message);
            }
            
        }
        public List<ObjectTypeRecord> Records { get { return _objectMap.ObjectTypeRecords; } }

        public void PersistObjectMap(ObjectTypeMap source, string fileName)
        {
            byte[] bytesToCompress = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(source));
            using (FileStream fileToCompress = File.Create(fileName))
            using (GZipStream compressionStream = new GZipStream(fileToCompress, CompressionMode.Compress))
            {
                compressionStream.Write(bytesToCompress, 0, bytesToCompress.Length);
            }
        }
        public ObjectTypeMap RetrieveObjectMap(FileInfo sourceFile)
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
                return JsonConvert.DeserializeObject<ObjectTypeMap>(Encoding.UTF8.GetString(decompressed));
            }
            catch { return null; }
        }        
    }
    
    public class ObjectType : StructureBase
    {
        private ulong _index;
        private string _name;
        private ulong _totalNumberOfObjects;
        private ulong _totalNumberOfHandles;
        private ulong _highWaterNumberOfHandles;
        private ulong _highWaterNumberOfObjects;
        private dynamic _ot;

        public ObjectType(Profile profile, DataProviderBase dataProvider, ulong virtualAddress) : base(profile, dataProvider, virtualAddress)
        {
            _is64 = (_profile.Architecture == "AMD64");
            int structureSize = (int)_profile.GetStructureSize("_OBJECT_TYPE");
            if (structureSize == -1)
                throw new ArgumentException("Error - Profile didn't contain a definition for _OBJECT_TYPE");
            _buffer = _dataProvider.ReadMemoryBlock(virtualAddress, (uint)structureSize);
            _ot = profile.GetStructure("_OBJECT_TYPE", _buffer, 0);
        }
        public dynamic dynamicObject
        {
            get { return _ot; }
        }
        public ulong Index
        {
            get
            {
                try
                {
                    var index = _ot.Index;
                    return (ulong)index;
                }
                catch (Exception)
                {
                    throw new ArgumentException("Couldn't extract Index from current OBJECT_TYPE structure.");
                }
            }
        }
        public string Name
        {
            get
            {
                try
                {
                    var name = _ot.Name;
                    UnicodeString us = new UnicodeString(_profile, _dataProvider, name.Buffer, name.Length, name.MaximumLength);
                    return us.Name;
                }
                catch (Exception)
                {
                    throw new ArgumentException("Couldn't extract Name from current OBJECT_TYPE structure.");
                }                
            }
        }
        public ulong TotalNumberOfObjects
        {
            get
            {
                try
                {
                    var totalNumberOfObjects = _ot.TotalNumberOfObjects;
                    return (ulong)totalNumberOfObjects;
                }
                catch (Exception)
                {
                    throw new ArgumentException("Couldn't extract TotalNumberOfObjects from current OBJECT_TYPE structure.");
                }
            }
        }
        public ulong TotalNumberOfHandles
        {
            get
            {
                try
                {
                    var totalNumberOfHandles = _ot.TotalNumberOfHandles;
                    return (ulong)totalNumberOfHandles;
                }
                catch (Exception)
                {
                    throw new ArgumentException("Couldn't extract TotalNumberOfHandles from current OBJECT_TYPE structure.");
                }
            }
        }
        public ulong HighWaterNumberOfHandles
        {
            get
            {
                try
                {
                    var highWaterNumberOfHandles = _ot.HighWaterNumberOfHandles;
                    return (ulong)highWaterNumberOfHandles;
                }
                catch (Exception)
                {
                    throw new ArgumentException("Couldn't extract HighWaterNumberOfHandles from current OBJECT_TYPE structure.");
                }
            }
        }
        public ulong HighWaterNumberOfObjects
        {
            get
            {
                try
                {
                    var highWaterNumberOfObjects = _ot.HighWaterNumberOfObjects;
                    return (ulong)highWaterNumberOfObjects;
                }
                catch (Exception)
                {
                    throw new ArgumentException("Couldn't extract HighWaterNumberOfObjects from current OBJECT_TYPE structure.");
                }
            }
        }
    }
}
