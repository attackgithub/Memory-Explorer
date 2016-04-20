using MemoryExplorer.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.Address
{
    public abstract class AddressBase
    {
        protected DataProviderBase _dataProvider;
        protected UInt64 _dtb;
        protected string _processName;
        protected uint _pid;
        protected bool _isKernel = false;
        protected MemoryMap _memoryMap;
        protected List<AddressRecord> _translationLookasideBuffer = new List<AddressRecord>();
        protected bool _is64 = false;


        public AddressBase()
        {
            _memoryMap = new MemoryMap();
            _memoryMap.MemoryRecords = new List<AddressRecord>();
            _memoryMap.P4Tables = new HashSet<ulong>();
            _memoryMap.PdpteTables = new HashSet<ulong>();
            _memoryMap.PdeTables = new HashSet<ulong>();
            _memoryMap.PteTables = new HashSet<ulong>();
        }
        protected abstract void PopulateTlb(ulong startAddress, ulong endAddress);
        protected List<AddressRecord> BuildMemoryMap(ulong startAddress, ulong endAddress)
        {
            List<AddressRecord> memoryRecords = new List<AddressRecord>();
            PopulateTlb(startAddress, endAddress);
            UInt64 contiguousVOffset = 0;
            UInt64 contiguousPOffset = 0;
            UInt32 totalLength = 0;
            if (_translationLookasideBuffer == null)
                return null;
            uint lastFlag = _translationLookasideBuffer[0].Flags;
            foreach (AddressRecord ar in _translationLookasideBuffer)
            {
                if (ar.IsSoftware)
                {
                    memoryRecords.Add(ar);
                    continue;
                }
                if (ar.VirtualAddress == contiguousVOffset + totalLength && ar.PhysicalAddress == contiguousPOffset + totalLength)
                {
                    totalLength += ar.Size;
                    lastFlag = ar.Flags;
                }
                else
                {
                    if (totalLength > 0)
                    {
                        AddressRecord ar2 = new AddressRecord();
                        ar2.VirtualAddress = contiguousVOffset;
                        ar2.PhysicalAddress = contiguousPOffset;
                        ar2.Size = totalLength;
                        ar2.Flags = lastFlag;
                        memoryRecords.Add(ar2);
                    }
                    contiguousPOffset = ar.PhysicalAddress;
                    contiguousVOffset = ar.VirtualAddress;
                    totalLength = ar.Size;
                    lastFlag = ar.Flags;
                }
            }
            if (totalLength > 0)
            {
                AddressRecord ar2 = new AddressRecord();
                ar2.VirtualAddress = contiguousVOffset;
                ar2.PhysicalAddress = contiguousPOffset;
                ar2.Size = totalLength;
                ar2.Flags = lastFlag;
                memoryRecords.Add(ar2);
            }
            return memoryRecords;
        }
        protected void ProcessPdpteEntry(PageDirectoryPointerTableEntry pdpte, ulong virtualAddress, ulong start, ulong end)
        {
            ulong originalVA = virtualAddress;
            // read in the PDPTE buffer and process each entry
            _memoryMap.PdeTables.Add(pdpte.RealEntry);
            byte[] buffer = ReadData(pdpte.RealEntry);
            for (ulong pdeIndex = 0; pdeIndex < 512; pdeIndex++)
            {
                virtualAddress = originalVA + (pdeIndex << 21);
                if (virtualAddress < start || virtualAddress > end)
                    continue;
                ulong pdeValue = BitConverter.ToUInt64(buffer, (int)pdeIndex * 8);
                PageDirectoryEntry pde = new PageDirectoryEntry(pdeValue);
                if (pde.InUse)
                {
                    if (pde.IsLarge)
                    {
                        ProcessLargePdeEntry(new LargePageDirectoryEntry(pdeValue), virtualAddress);
                    }
                    else
                        ProcessPdeEntry(pde, virtualAddress, start, end);
                }
            }
        }
        protected void ProcessPdeEntry(PageDirectoryEntry pde, ulong virtualAddress, ulong start, ulong end)
        {
            ulong originalVA = virtualAddress;
            // read in the PDE buffer and process each entry
            _memoryMap.PteTables.Add(pde.RealEntry);
            byte[] buffer = ReadData(pde.RealEntry);
            for (ulong pteIndex = 0; pteIndex < 512; pteIndex++)
            {
                virtualAddress = originalVA + (pteIndex << 12);
                if (virtualAddress < start || virtualAddress > end)
                    continue;
                ulong pteValue = BitConverter.ToUInt64(buffer, (int)pteIndex * 8);
                PageTableEntry pte = new PageTableEntry(pteValue);
                if (pde.InUse)
                    ProcessPteEntry(pte, virtualAddress);
                else
                    ProcessSoftwareEntry(new SoftwarePageTableEntry(pteValue), virtualAddress);
            }
        }
        protected void ProcessSoftwareEntry(SoftwarePageTableEntry spte, ulong virtualAddress)
        {
            if (spte.Entry == 0)
                return;
            if (spte.IsTransition && !spte.IsPrototype)
            {
                AddressRecord ar = new AddressRecord();
                ar.VirtualAddress = virtualAddress;
                ar.PhysicalAddress = spte.RealEntry;
                ar.Size = 0x1000;
                ar.Flags = spte.Flags;
                _translationLookasideBuffer.Add(ar);
            }
            else
            {
                AddressRecord ar = new AddressRecord();
                ar.VirtualAddress = virtualAddress;
                ar.PhysicalAddress = spte.Entry; // full original entry
                ar.Size = 0x1000;
                ar.Flags = spte.Flags;
                ar.IsSoftware = true;
                _translationLookasideBuffer.Add(ar);
            }
        }
        protected void ProcessPteEntry(PageTableEntry pte, ulong virtualAddress)
        {
            AddressRecord ar = new AddressRecord();
            ar.VirtualAddress = virtualAddress;
            ar.PhysicalAddress = pte.RealEntry;
            ar.Size = 0x1000;
            ar.Flags = pte.Flags;
            _translationLookasideBuffer.Add(ar);
        }
        protected void ProcessLargePdeEntry(LargePageDirectoryEntry lpde, ulong virtualAddress)
        {
            AddressRecord ar = new AddressRecord();
            ar.VirtualAddress = virtualAddress;
            ar.PhysicalAddress = lpde.RealEntry;
            ar.Size = 0x200000;
            ar.Flags = lpde.Flags;
            _translationLookasideBuffer.Add(ar);
        }
        public string ProcessName
        {
            get { return _processName; }
            set { _processName = value; }
        }

        public ulong Dtb { get { return _dtb; } }
        public bool Is64 { get { return _is64; } }

        public MemoryMap MemoryMap { get { return _memoryMap; } }

        public byte[] ReadData(ulong physicalMemory, uint count=1)
        {
            try
            {
                byte[] buffer = _dataProvider.ReadMemory(physicalMemory, count);
                return buffer;
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Error Reading Data - " + ex.Message);
            }
        }
        public string GetMd5(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    var h = md5.ComputeHash(stream);
                    return GetMd5Hash(h);
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
        public void PersistMemoryMap(MemoryMap source, string fileName)
        {
            var test = JsonConvert.SerializeObject(source);
            byte[] bytesToCompress = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(source));
            using (FileStream fileToCompress = File.Create(fileName))
            using (GZipStream compressionStream = new GZipStream(fileToCompress, CompressionMode.Compress))
            {
                compressionStream.Write(bytesToCompress, 0, bytesToCompress.Length);
            }
        }
        public MemoryMap RetrieveMemoryMap(FileInfo sourceFile)
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
                return JsonConvert.DeserializeObject<MemoryMap>(Encoding.UTF8.GetString(decompressed));
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
        public abstract UInt64 vtop(ulong virtualAddress, bool live=false);
        public abstract ulong ptov(ulong physicalAddress);

        public abstract string TraceAddress(ulong virtualAddress);
    }
}
