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
        public AddressSpacex64(DataProviderBase dataProvider, string processName, UInt64 dtb, bool kernel = false)
        {
            _dataProvider = dataProvider;
            _dtb = dtb;
            _isKernel = kernel;
            _processName = processName;
            _is64 = true;
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
            string output = "";
            output += "Tracing Virtual Address: 0x" + virtualAddress.ToString("x16") + "\n\n";
            output += "Directory Table Base: " + _dtb.ToString("x08") + "\n";
            ulong pml4eAddress = ((UInt64)_dtb & 0x0000fffffffff000);
            byte[] buffer = ReadData(pml4eAddress, 4096);
            ulong pml4eIndex = (virtualAddress & 0xff8000000000) >> 39;
            ulong pdpteIndex = (virtualAddress & 0x7fc0000000) >> 30;
            ulong pdeIndex = (virtualAddress & 0x3fe00000) >> 21;
            ulong pteIndex = (virtualAddress & 0x1ff000) >> 12;
            output += "P4[" + pml4eIndex + "]   ";
            output += "PDPTE[" + pdpteIndex + "]   ";
            output += "PDE[" + pdeIndex + "]   ";
            output += "PTE[" + pteIndex + "]   ";
            output += "Offset[" + (virtualAddress & 0xfff) + "]\n\n";

            // L4
            ulong pml4eEntry = BitConverter.ToUInt64(buffer, (int)(pml4eIndex * 8));
            L4PageDirectoryEntry l4de = new L4PageDirectoryEntry(pml4eEntry);
            output += "pml4e value @ " + (pml4eAddress + pml4eIndex * 8).ToString("x08") + " is " + pml4eEntry.ToString("x08").PadRight(20) + "Flags: " + GetFlagString(l4de);

            if (!l4de.InUse)
            {
                output += "Entry Not In Use\n\n";
                return output;
            }

            //PDPTE
            buffer = ReadData(l4de.RealEntry, 4096);
            ulong pdpteEntry = BitConverter.ToUInt64(buffer, (int)(pdpteIndex * 8));
            PageDirectoryPointerTableEntry pdpte = new PageDirectoryPointerTableEntry(pdpteEntry);
            output += "pdpte value @ " + (l4de.RealEntry + pdpteIndex * 8).ToString("x08") + " is " + pdpteEntry.ToString("x08").PadRight(20) + "Flags: " + GetFlagString(pdpte);

            if (!pdpte.InUse)
            {
                output += "Entry Not In Use\n\n";
                return output;
            }
            if (pdpte.IsLarge)
            {
                output += "Entry is LARGE\n\n";
                return output;
            }

            // PDE
            buffer = ReadData(pdpte.RealEntry, 4096);
            ulong pdeEntry = BitConverter.ToUInt64(buffer, (int)(pdeIndex * 8));
            PageDirectoryEntry pde = new PageDirectoryEntry(pdeEntry);
            output += "pde value   @ " + (pdpte.RealEntry + pdeIndex * 8).ToString("x08") + " is " + pdeEntry.ToString("x08").PadRight(20);

            if (!pde.InUse)
            {
                output += "Entry Not In Use\n\n";
                return output;
            }
            if (pde.IsLarge)
            {
                LargePageDirectoryEntry lpde = new LargePageDirectoryEntry(pdeEntry);
                output += "Flags: " + GetFlagString(lpde);
                ulong physicalAddressL = (pdeEntry & 0xffffffe00000) + (virtualAddress & 0x1fffff);
                output += "\nPhysical Address is in a LARGE 2M page @ 0x" + physicalAddressL.ToString("x08") + "\n\n";
                return output;
            }
            output += "Flags: " + GetFlagString(pde);

            // PTE
            buffer = ReadData(pde.RealEntry, 4096);
            ulong pteEntry = BitConverter.ToUInt64(buffer, (int)(pteIndex * 8));
            PageTableEntry pte = new PageTableEntry(pteEntry);
            output += "pte value   @ " + (pde.RealEntry + pteIndex * 8).ToString("x08") + " is " + pteEntry.ToString("x08").PadRight(20) + "Flags: " + GetFlagString(pte);
            if (!pte.InUse)
            {
                SoftwarePageTableEntry spte = new SoftwarePageTableEntry(pteEntry);
                output += ProcessSoftwarePte(spte);
                output += "Entry Not In Use\n\n";
                return output;
            }
            ulong physicalAddress = (pteEntry & 0xfffffffff000) + (virtualAddress & 0xfff);
            output += "\nPhysical Address is 0x" + physicalAddress.ToString("x08") + "\n\n";

            return output;
        }
        string ProcessSoftwarePte(SoftwarePageTableEntry entry)
        {
            string result = "";
            result += "Software Page Table Entry\n";
            if (entry.IsTransition && !entry.IsPrototype)
                result += "[TRANSITION]";
            else if (entry.IsPrototype)
            {
                result += "[PROTO]\n";
                ulong protoAddress = entry.ProtoAddress;
                result += "Proto Address: " + protoAddress.ToString("x08");
                if (protoAddress == 0xFFFFFFFF0000)
                    result += "[VAD]";
            }
            else if (!entry.IsTransition && !entry.IsPrototype)
            {
                result += "[SOFTWARE]\n";
                ulong pfh = entry.PageFileOffset;
                ulong pfl = entry.PageFileNumber;
                ulong ue = entry.UsedPageTableEntries;
                ulong pt = entry.Protection;
                result += "Page File High: " + pfh.ToString("x08");
                result += "\nPage File Low: " + pfl.ToString("x08");
                result += "\nProtection: " + pt.ToString("x08");
                result += "\nUsed Entries: " + ue.ToString("x08");
            }


            if (result != "")
                result += "\n";
            return result;
        }
        string GetFlagString(PxeBase entry)
        {
            L4PageDirectoryEntry l4de = entry as L4PageDirectoryEntry;
            PageDirectoryPointerTableEntry pdpte = entry as PageDirectoryPointerTableEntry;
            PageDirectoryEntry pde = entry as PageDirectoryEntry;
            LargePageDirectoryEntry lpde = entry as LargePageDirectoryEntry;
            PageTableEntry pte = entry as PageTableEntry;


            string result = "";
            if (entry.InUse)
                result += "[INUSE]";
            else
                result += "[inuse]";
            #region PML4E
            if (l4de != null)
            {
                if (l4de.IsReadWrite)
                    result += "[RW]";
                else
                    result += "[rw]";
                if (l4de.IsUserSupervisor)
                    result += "[US]";
                else
                    result += "[us]";
                if (l4de.IsWriteThrough)
                    result += "[WT]";
                else
                    result += "[wt]";
                if (l4de.IsCacheDisabled)
                    result += "[CD]";
                else
                    result += "[cd]";
                if (l4de.IsAccessed)
                    result += "[ACCESSED]";
                else
                    result += "[accessed]";
                if (l4de.IsNx)
                    result += "[NX]";
                else
                    result += "[nx]";
            }
            #endregion
            #region PDPTE
            else if (pdpte != null)
            {
                if (pdpte.IsReadWrite)
                    result += "[RW]";
                else
                    result += "[rw]";
                if (pdpte.IsUserSupervisor)
                    result += "[US]";
                else
                    result += "[us]";
                if (pdpte.IsWriteThrough)
                    result += "[WT]";
                else
                    result += "[wt]";
                if (pdpte.IsCacheDisabled)
                    result += "[CD]";
                else
                    result += "[cd]";
                if (pdpte.IsAccessed)
                    result += "[ACCESSED]";
                else
                    result += "[accessed]";
                if (pdpte.IsLarge)
                    result += "[LARGE]";
                else
                    result += "[large]";
                if (pdpte.IsNx)
                    result += "[NX]";
                else
                    result += "[nx]";
            }
            #endregion
            #region PDE
            else if (pde != null)
            {
                if (pde.IsReadWrite)
                    result += "[RW]";
                else
                    result += "[rw]";
                if (pde.IsUserSupervisor)
                    result += "[US]";
                else
                    result += "[us]";
                if (pde.IsWriteThrough)
                    result += "[WT]";
                else
                    result += "[wt]";
                if (pde.IsCacheDisabled)
                    result += "[CD]";
                else
                    result += "[cd]";
                if (pde.IsAccessed)
                    result += "[ACCESSED]";
                else
                    result += "[accessed]";
                if (pde.IsLarge)
                    result += "[LARGE]";
                else
                    result += "[large]";
                if (pde.IsNx)
                    result += "[NX]";
                else
                    result += "[nx]";
            }
            #endregion
            #region Large PDE
            else if (lpde != null)
            {
                if (lpde.IsReadWrite)
                    result += "[RW]";
                else
                    result += "[rw]";
                if (lpde.IsUserSupervisor)
                    result += "[US]";
                else
                    result += "[us]";
                if (lpde.IsWriteThrough)
                    result += "[WT]";
                else
                    result += "[wt]";
                if (lpde.IsCacheDisabled)
                    result += "[CD]";
                else
                    result += "[cd]";
                if (lpde.IsAccessed)
                    result += "[ACCESSED]";
                else
                    result += "[accessed]";
                if (lpde.IsDirty)
                    result += "[DIRTY]";
                else
                    result += "[dirty]";
                if (lpde.IsLarge)
                    result += "[LARGE]";
                else
                    result += "[large]";
                if (lpde.IsGlobal)
                    result += "[GLOB]";
                else
                    result += "[glob]";
                if (lpde.IsPat)
                    result += "[PAT]";
                else
                    result += "[pat]";
                if (lpde.IsNx)
                    result += "[NX]";
                else
                    result += "[nx]";
            }
            #endregion
            #region PTE
            else if (pte != null)
            {
                if (pte.IsReadWrite)
                    result += "[RW]";
                else
                    result += "[rw]";
                if (pte.IsUserSupervisor)
                    result += "[US]";
                else
                    result += "[us]";
                if (pte.IsWriteThrough)
                    result += "[WT]";
                else
                    result += "[wt]";
                if (pte.IsCacheDisabled)
                    result += "[CD]";
                else
                    result += "[cd]";
                if (pte.IsAccessed)
                    result += "[ACCESSED]";
                else
                    result += "[accessed]";
                if (pte.IsDirty)
                    result += "[DIRTY]";
                else
                    result += "[dirty]";
                if (pte.IsGlobal)
                    result += "[GLOB]";
                else
                    result += "[glob]";
                if (pte.IsPat)
                    result += "[PAT]";
                else
                    result += "[pat]";
                if (pte.IsNx)
                    result += "[NX]";
                else
                    result += "[nx]";
            }
            #endregion
            result += "\n";
            return result;
        }
        public override ulong ptov(ulong physicalAddress)
        {
            foreach (AddressRecord ar in _memoryMap.MemoryRecords)
            {
                if (ar.IsSoftware)
                    continue;
                ulong first = ar.PhysicalAddress;
                ulong last = ar.PhysicalAddress + ar.Size;
                if(physicalAddress >= first && physicalAddress < last)
                {
                    var offset = physicalAddress - ar.PhysicalAddress;
                    return ar.VirtualAddress + offset;
                }
            }
            return 0;
        }
        public override ulong vtop(ulong virtualAddress, bool live=false)
        {
            if(!live)
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
            ulong pml4eAddress = ((UInt64)_dtb & 0x0000fffffffff000);
            byte[] buffer = ReadData(pml4eAddress, 4096);
            ulong pml4eIndex = (virtualAddress & 0xff8000000000) >> 39;
            ulong pdpteIndex = (virtualAddress & 0x7fc0000000) >> 30;
            ulong pdeIndex = (virtualAddress & 0x3fe00000) >> 21;
            ulong pteIndex = (virtualAddress & 0x1ff000) >> 12;
            // L4
            ulong pml4eEntry = BitConverter.ToUInt64(buffer, (int)(pml4eIndex * 8));
            L4PageDirectoryEntry l4de = new L4PageDirectoryEntry(pml4eEntry);
            if (!l4de.InUse)
                return 0;
            //PDPTE
            buffer = ReadData(l4de.RealEntry, 4096);
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
    }
}
