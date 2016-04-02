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

        public override ulong vtop(ulong virtualAddress)
        {
            throw new NotImplementedException();
        }
    }
}
