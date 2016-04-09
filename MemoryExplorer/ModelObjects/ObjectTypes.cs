using MemoryExplorer.Address;
using MemoryExplorer.Data;
using MemoryExplorer.Profiles;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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

        public ObjectTypes(DataProviderBase dataProvider, Profile profile)
        {
            _dataProvider = dataProvider;
            _profile = profile;
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

            uint indexTableOffset = (uint)_profile.GetConstant("ObTypeIndexTable");
            ulong startOffset = _profile.KernelBaseAddress + indexTableOffset;
            ulong[] indexTable = new ulong[64];

            // the object index table has 64 8 byte entries = 512 bytes
            // if the startOffset is less than 512 bytes from the end of the page, I'll need to read the next page as well
            byte[] bigBuffer = new byte[8192];
            ulong pAddr = kernelAS.vtop(startOffset, _dataProvider.IsLive);
            _objectMap.StartAddress = startOffset;

            byte[] buffer = _dataProvider.ReadMemory(pAddr & 0xfffffffff000, 1);
            Array.Copy(buffer, 0, bigBuffer, 0, 4096);
            pAddr = kernelAS.vtop(startOffset + 0x1000, _dataProvider.IsLive);
            buffer = _dataProvider.ReadMemory(pAddr & 0xfffffffff000, 1);
            Array.Copy(buffer, 0, bigBuffer, 4096, 4096);

            for (int i = 0; i < 64; i++)
            {
                indexTable[i] = (BitConverter.ToUInt64(bigBuffer, (int)(startOffset & 0xfff) + (i * 8))) & 0xffffffffffff;
                if (indexTable[i] == 0)
                    continue;
                ulong pAddress = kernelAS.vtop(indexTable[i]);
                if (pAddress == 0)
                    continue;
                ObjectType ot = new ObjectType(_profile, _dataProvider, pAddress);
                ObjectTypeRecord otr = new ObjectTypeRecord();
                otr.Name = ot.Name;
                otr.Index = ot.Index;
                _objectMap.ObjectTypeRecords.Add(otr);
            }
            if(!dataProvider.IsLive)
                PersistObjectMap(_objectMap, _dataProvider.CacheFolder + "\\object_type_map.gz");
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
        byte[] Decompress(byte[] inputData)
        {
            if (inputData == null)
                throw new ArgumentNullException("inputData must be non-null");

            using (var compressedMs = new MemoryStream(inputData))
            {
                using (var decompressedMs = new MemoryStream())
                {
                    using (var gzs = new BufferedStream(new GZipStream(compressedMs, CompressionMode.Decompress)))
                    {
                        gzs.CopyTo(decompressedMs);
                    }
                    return decompressedMs.ToArray();
                }
            }
        }
    }
    
    public class ObjectType : StructureBase
    {
        private ulong _index;
        private string _name;
        public ObjectType(Profile profile, DataProviderBase dataProvider, ulong address)
        {
            _dataProvider = dataProvider;
            _profile = profile;
            _is64 = (_profile.Architecture == "AMD64");
            int structureSize = (int)_profile.GetStructureSize("_OBJECT_TYPE");
            if (structureSize == -1)
                throw new ArgumentException("Error - Profile didn't contain a definition for _OBJECT_TYPE");
            _structure = _profile.GetEntries("_OBJECT_TYPE");
            Structure s = GetStructureMember("Index");
            _buffer = _dataProvider.ReadMemory(address & 0xfffffffff000, 1);
            _index = _buffer[(int)s.Offset + (int)(address & 0xfff)];
            s = GetStructureMember("Name");
            UnicodeString us = new UnicodeString(_profile, _dataProvider, address + s.Offset);
            _name = us.Name;

        }
        public ulong Index { get { return _index; } }
        public string Name { get { return _name; } }
    }
}
