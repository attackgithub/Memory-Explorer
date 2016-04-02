using MemoryExplorer.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.Address
{
    public class AddressSpacex64 : AddressBase
    {
        private ulong _p4Index;
        private ulong _pdpteIndex;
        private ulong _pdeIndex;
        private ulong _pteIndex;
        private uint _p4Flags;
        private uint _pdpteFlags;
        private uint _pdeFlags;
        private uint _pteFlags;
        private ulong _p4Address;
        private ulong _pdpteAddress;
        private ulong _pdeAddress;
        private ulong _pteAddress;


        public AddressSpacex64(DataProviderBase dataProvider, string processName, UInt64 dtb, bool kernel = false)
        {
            _dataProvider = dataProvider;
            _dtb = dtb;
            _isKernel = kernel;
            _processName = processName;
            // first check to see if it is already cached
            FileInfo cachedFile = new FileInfo(_dataProvider.CacheFolder + "\\" + _processName + "_memorymap.gz");
            if (cachedFile.Exists)
            {
                MemoryMap test = RetrieveMemoryMap(cachedFile);
                if (test != null)
                {
                    _memoryMap = test;
                    return;
                }
            }
            // it isn't cached, so generate a new one
            //_memoryMap.Md5 = GetMd5(_imageFile);
            _memoryMap.StartAddress = _isKernel ? (ulong)0x800000000000 : 0;
            _memoryMap.EndAddress = _isKernel ? (ulong)0xffffffffffff : 0x7fffffffffff;
            _memoryMap.MemoryRecords = BuildMemoryMap(_memoryMap.StartAddress, _memoryMap.EndAddress);
            if (!_dataProvider.IsLive)
                PersistMemoryMap(_memoryMap, _dataProvider.CacheFolder + "\\" + _processName + "_memorymap.gz");
        }


        protected override void PopulateTlb(ulong start, ulong end)
        {
            ulong virtualAddress;
            // read in the L4 buffer and process each entry
            _memoryMap.P4Tables.Add(_dtb & 0xfffffffff000);
            byte[] buffer = ReadData(_dtb & 0xfffffffff000);
            for (ulong pml4eIndex = 0; pml4eIndex < 512; pml4eIndex++)
            {
                virtualAddress = (pml4eIndex << 39);
                if (virtualAddress < start || virtualAddress > end)
                    continue;
                ulong pml4eValue = BitConverter.ToUInt64(buffer, (int)pml4eIndex * 8);
                L4PageDirectoryEntry l4pde = new L4PageDirectoryEntry(pml4eValue);
                if (l4pde.InUse)
                    ProcessL4Entry(l4pde, virtualAddress, start, end);
            }
        }
        private void ProcessL4Entry(L4PageDirectoryEntry l4pde, ulong virtualAddress, ulong start, ulong end)
        {
            ulong originalVA = virtualAddress;
            // read in the PDPTE buffer and process each entry
            _memoryMap.PdpteTables.Add(l4pde.RealEntry);
            byte[] buffer = ReadData(l4pde.RealEntry);
            for (ulong pdpteIndex = 0; pdpteIndex < 512; pdpteIndex++)
            {
                virtualAddress = originalVA + (pdpteIndex << 30);
                if (virtualAddress < start || virtualAddress > end)
                    continue;
                ulong pdpteValue = BitConverter.ToUInt64(buffer, (int)pdpteIndex * 8);
                PageDirectoryPointerTableEntry pdpte = new PageDirectoryPointerTableEntry(pdpteValue);
                if (pdpte.InUse)
                    ProcessPdpteEntry(pdpte, virtualAddress, start, end);
            }
        }        
        public override string TraceAddress(ulong virtualAddress)
        {
            throw new NotImplementedException();
        }

        public override ulong vtop(ulong virtualAddress)
        {
            throw new NotImplementedException();
        }
    }
}
