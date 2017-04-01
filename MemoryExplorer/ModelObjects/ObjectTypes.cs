﻿using MemoryExplorer.Address;
using MemoryExplorer.Data;
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

        public ObjectTypes(DataProviderBase dataProvider, Profile_Deprecated profile) : base(profile, dataProvider, 0)
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
                kernelAS = _profile.KernelAddressSpace as AddressSpacex64;
            else
                kernelAS = _profile.KernelAddressSpace as AddressSpacex86Pae;
            uint indexTableOffset = (uint)_profile.GetConstant("ObpObjectTypes");
            ulong startOffset = _profile.KernelBaseAddress + indexTableOffset;
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
                        startOffset = _profile.KernelBaseAddress + indexTableOffset + (uint)(i * 8);
                        ptr = (BitConverter.ToUInt64(_dataProvider.ReadMemoryBlock(startOffset, 8), 0) & 0xffffffffffff);
                    }
                    else
                    {
                        startOffset = _profile.KernelBaseAddress + indexTableOffset + (uint)(i * 4);
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
                if (!dataProvider.IsLive)
                    PersistObjectMap(_objectMap, _dataProvider.CacheFolder + "\\object_type_map.gz");
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


        public ObjectType(Profile_Deprecated profile, DataProviderBase dataProvider, ulong virtualAddress) : base(profile, dataProvider, virtualAddress)
        {
            _is64 = (_profile.Architecture == "AMD64");
            int structureSize = (int)_profile.GetStructureSize("_OBJECT_TYPE");
            if (structureSize == -1)
                throw new ArgumentException("Error - Profile didn't contain a definition for _OBJECT_TYPE");
            _structure = _profile.GetEntries("_OBJECT_TYPE");            
            _buffer = _dataProvider.ReadMemoryBlock(virtualAddress, (uint)structureSize);
            //_buffer = _dataProvider.ReadMemory(virtualAddress & 0xfffffffff000, 1);
            //_index = _buffer[(int)s.Offset + (int)(virtualAddress & 0xfff)];
            Structure s = GetStructureMember("Index");
            _index = _buffer[(int)s.Offset];
            s = GetStructureMember("Name");
            UnicodeString us = new UnicodeString(_profile, _dataProvider, virtualAddress + s.Offset);
            _name = us.Name;
            s = GetStructureMember("TotalNumberOfObjects");
            _totalNumberOfObjects = BitConverter.ToUInt64(_buffer, (int)s.Offset);
            s = GetStructureMember("TotalNumberOfHandles");
            _totalNumberOfHandles = BitConverter.ToUInt64(_buffer, (int)s.Offset);
            s = GetStructureMember("HighWaterNumberOfHandles");
            _highWaterNumberOfHandles = BitConverter.ToUInt64(_buffer, (int)s.Offset);
            s = GetStructureMember("HighWaterNumberOfObjects");
            _highWaterNumberOfObjects = BitConverter.ToUInt64(_buffer, (int)s.Offset);

        }
        public ulong Index { get { return _index; } }
        public string Name { get { return _name; } }
        public ulong TotalNumberOfObjects { get { return _totalNumberOfObjects; } }
        public ulong TotalNumberOfHandles { get { return _totalNumberOfObjects; } }
        public ulong HighWaterNumberOfHandles { get { return _highWaterNumberOfHandles; } }
        public ulong HighWaterNumberOfObjects { get { return _highWaterNumberOfObjects; } }
    }
}
