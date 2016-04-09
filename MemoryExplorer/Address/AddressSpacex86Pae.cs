using MemoryExplorer.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.Address
{
    public class AddressSpacex86Pae : AddressBase
    {

        public AddressSpacex86Pae(DataProviderBase dataProvider, string processName, UInt64 dtb, bool kernel = false)
        {
            _dataProvider = dataProvider;
            _dtb = dtb;
            _isKernel = kernel;
            _processName = processName;
            // first check to see if it is already cached
            FileInfo cachedFile = new FileInfo(_dataProvider.CacheFolder + "\\" + _processName + "_memorymap.gz");
            if (cachedFile.Exists && !_dataProvider.IsLive)
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
            _memoryMap.StartAddress = _isKernel ? 0x80000000 : 0;
            _memoryMap.EndAddress = _isKernel ? 0xffffffff : 0x7fffffff;
            _memoryMap.MemoryRecords = BuildMemoryMap(_memoryMap.StartAddress, _memoryMap.EndAddress);
            if (!_dataProvider.IsLive)
                PersistMemoryMap(_memoryMap, _dataProvider.CacheFolder + "\\" + _processName + "_memorymap.gz");
        }        

        protected override void PopulateTlb(ulong startAddress, ulong endAddress)
        {
            ulong virtualAddress;
            _memoryMap.PdpteTables.Add(_dtb & 0xfffffffff000);
            byte[] buffer = ReadData(_dtb & 0xfffffffff000);
            for (UInt64 pdpteIndex = 0; pdpteIndex < 4; pdpteIndex++)
            {
                virtualAddress = pdpteIndex << 30;
                if (virtualAddress < startAddress || virtualAddress > endAddress)
                    continue;
                ulong pdpteValue = BitConverter.ToUInt64(buffer, (int)pdpteIndex * 8);
                PageDirectoryPointerTableEntry pdpte = new PageDirectoryPointerTableEntry(pdpteValue);
                if (pdpte.InUse)
                    ProcessPdpteEntry(pdpte, virtualAddress, startAddress, endAddress);
            }
        }        
        

        public override string TraceAddress(ulong virtualAddress)
        {
            throw new NotImplementedException();
        }

        public override ulong vtop(ulong virtualAddress, bool live=false)
        {
            if (!live)
            {
                ulong testingFor = virtualAddress & 0xffffffffffff;
                foreach (AddressRecord ar in _memoryMap.MemoryRecords)
                {
                    ulong first = ar.VirtualAddress;
                    ulong last = ar.VirtualAddress + ar.Size;
                    if (testingFor >= first && testingFor < last)
                    {
                        var w = testingFor - ar.VirtualAddress;
                        return (ar.PhysicalAddress + w);
                    }
                }
                return 0;
            }
            UInt64 pdpteAddress = _dtb & 0xffffffe0;
            UInt64 pdpteIndex = (virtualAddress & 0xc0000000) >> 30;
            UInt64 pdeIndex = (virtualAddress & 0x3fe00000) >> 21;
            UInt64 pteIndex = (virtualAddress & 0x1ff000) >> 12;
            UInt64 pageOffset = virtualAddress & 0xfff;
            byte[] buffer = ReadData(pdpteAddress, 4096);


            //ulong pml4eAddress = ((UInt64)_dtb & 0x0000fffffffff000);
            //byte[] buffer = ReadData(pml4eAddress, 4096);
            //ulong pml4eIndex = (virtualAddress & 0xff8000000000) >> 39;
            //ulong pdpteIndex = (virtualAddress & 0x7fc0000000) >> 30;
            //ulong pdeIndex = (virtualAddress & 0x3fe00000) >> 21;
            //ulong pteIndex = (virtualAddress & 0x1ff000) >> 12;
            //// L4
            //ulong pml4eEntry = BitConverter.ToUInt64(buffer, (int)(pml4eIndex * 8));
            //L4PageDirectoryEntry l4de = new L4PageDirectoryEntry(pml4eEntry);
            //if (!l4de.InUse)
            //    return 0;
            ////PDPTE
            //buffer = ReadData(l4de.RealEntry, 4096);
            ulong pdpteEntry = BitConverter.ToUInt64(buffer, (int)(pdpteIndex * 8));
            PageDirectoryPointerTableEntry pdpte = new PageDirectoryPointerTableEntry(pdpteEntry);
            if (!pdpte.InUse)
                return 0;
            if (pdpte.IsLarge)
                return 0;
            // PDE
            buffer = ReadData(pdpte.RealEntry, 4096);
            ulong pdeEntry = BitConverter.ToUInt64(buffer, (int)(pdeIndex * 8));
            PageDirectoryEntry pde = new PageDirectoryEntry(pdeEntry);
            if (!pde.InUse)
                return 0;
            if (pde.IsLarge)
                return (pdeEntry & 0xfffffffe00000) | (virtualAddress & 0x1fffff);
            // PTE
            buffer = ReadData(pde.RealEntry, 4096);
            ulong pteEntry = BitConverter.ToUInt64(buffer, (int)(pteIndex * 8));
            PageTableEntry pte = new PageTableEntry(pteEntry);
            if (!pte.InUse)
                return 0;
            ulong physicalAddress = (pteEntry & 0xfffffffff000) + (virtualAddress & 0xfff);
            return physicalAddress;
        }

        public override ulong ptov(ulong physicalAddress)
        {
            foreach (AddressRecord ar in _memoryMap.MemoryRecords)
            {
                if (ar.IsSoftware)
                    continue;
                ulong first = ar.PhysicalAddress;
                ulong last = ar.PhysicalAddress + ar.Size;
                if (physicalAddress >= first && physicalAddress < last)
                {
                    var offset = physicalAddress - ar.PhysicalAddress;
                    return ar.VirtualAddress + offset;
                }
            }
            return 0;
        }
    }
}
