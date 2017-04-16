using MemoryExplorer.Address;
using MemoryExplorer.Data;
using MemoryExplorer.Model;
using MemoryExplorer.ModelObjects;
using MemoryExplorer.Profiles;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace MemoryExplorer
{
    public class MemberInfo
    {
        public string Name;
        public ulong Offset;
        public uint Size;
        public bool IsArray;
        public long Count;
    }
    public class Structure
    {
        public string Name;
        public string StructureName;
        public ulong Offset;
        public string EntryType;
        public string PointerType;
        public uint StartBit;
        public uint EndBit;
        public string BitType;
        public string ArrayType;
        public ulong ArrayCount;
        public ulong Size;

        public Structure(string structure)
        {
            StructureName = structure;
        }
    }
    public abstract class StructureBase
    {
        protected Profile _profile;
        protected string _imageFile;
        protected ulong _physicalAddress;
        protected ulong _virtualAddress;
        protected bool _is64;
        protected byte[] _buffer = null;
        protected List<Structure> _structure;
        protected long _structureSize = -1;
        protected DataProviderBase _dataProvider;
        protected ObjectHeader _header = null;
        protected dynamic _members;
        protected AddressBase _addressSpace;
        protected DataModel _model = null;
        

        protected StructureBase(DataModel model, ulong virtualAddress = 0, ulong physicalAddress = 0)
        {
            _model = model;
            _profile = model.ActiveProfile;
            _dataProvider = model.DataProvider;
            _is64 = (_profile.Architecture == "AMD64");
            _virtualAddress = (virtualAddress & 0xffffffffffff);
            _physicalAddress = physicalAddress;
            if (virtualAddress != 0 && physicalAddress == 0)
                _physicalAddress = model.ActiveAddressSpace.vtop(_virtualAddress);
            if (virtualAddress == 0 && physicalAddress != 0)
                _virtualAddress = model.ActiveAddressSpace.ptov(_physicalAddress);
        }

        protected Structure GetStructureMember(string member)
        {
            if (_structure == null)
                return null;
            foreach (Structure s in _structure)
                if (s.Name == member)
                    return s;
            return null;
        }
        protected byte[] ReadData(ulong offset, int count)
        {
            byte[] buffer = new byte[count];
            FileInfo fi = new FileInfo(_imageFile);
            FileStream fs = fi.OpenRead();
            fs.Seek((long)offset, SeekOrigin.Begin);
            fs.Read(buffer, 0, (int)count);
            fs.Close();
            return buffer;
        }
        public string GetMd5(string filename)
        {
            try
            {
                if (filename != _imageFile)
                    throw new ArgumentException("arrrg");
                //return _profile.Project["fileHash"].ToString();
                return null;
            }
            catch
            {
                using (var md5Array = MD5.Create())
                {
                    using (var stream = File.OpenRead(filename))
                    {
                        var h = md5Array.ComputeHash(stream);
                        return GetMd5Hash(h);
                    }
                }
            }
        }
        string GetMd5Hash(byte[] data)
        {
            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }
        public static ulong GetMask(uint startBit, uint endBit)
        {
            ulong result = 0x1;
            int realEndBit = (int)endBit - 1;
            var start = result << (int)startBit;
            var end = result << realEndBit;

            result = 0;
            uint multiplier = 1;
            while (multiplier <= end)
            {
                if (multiplier >= start && multiplier <= end)
                    result += multiplier;
                multiplier *= 2;
            }
            return result;
        }
        public long Size { get { return _structureSize; } }

        public ulong VirtualAddress { get { return _virtualAddress; } }
        public ulong PhysicalAddress { get { return _physicalAddress; } }

        public uint ReadUInt32(int offset)
        {
            if (_buffer == null || offset > _buffer.Length)
                return 0;
            return BitConverter.ToUInt32(_buffer, offset);
        }
        public ulong ReadUInt64(int offset)
        {
            if (_buffer == null || offset > _buffer.Length)
                return 0;
            return BitConverter.ToUInt64(_buffer, offset);
        }
        public byte[] Decompress(byte[] inputData)
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
        protected void Overlay(string name)
        {
            //string shorterVersion = name.TrimStart(new char[] { '_' });
            //_is64 = (_profile.Architecture == "AMD64");
            //_addressSpace = _dataProvider.ActiveAddressSpace;
            //_structureSize = (int)_profile.GetStructureSize(name);
            //if (_structureSize == -1)
            //    throw new ArgumentException("Error: Profile didn't contain a definition for " + name);
            //if (_virtualAddress == 0)
            //    _buffer = _dataProvider.ReadPhysicalMemory(_physicalAddress, (uint)_structureSize);
            //else
            //{
            //    _physicalAddress = _addressSpace.vtop(_virtualAddress);
            //    _buffer = _dataProvider.ReadMemoryBlock(_virtualAddress, (uint)_structureSize);
            //}
            //if (_buffer == null)
            //    throw new ArgumentException("Invallid address " + _virtualAddress.ToString("x12"));
            //var dll = _profile.GetStructureAssembly(name);
            //Type t = dll.GetType("liveforensics." + shorterVersion);
            //GCHandle pinedPacket = GCHandle.Alloc(_buffer, GCHandleType.Pinned);
            //_members = Marshal.PtrToStructure(Marshal.UnsafeAddrOfPinnedArrayElement(_buffer, 0), t);
            //pinedPacket.Free();
        }
        public dynamic Members {get { return _members;} }
    }
}
