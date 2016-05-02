using MemoryExplorer.Address;
using MemoryExplorer.Memory;
using MemoryExplorer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.Data
{
    public abstract class DataProviderBase
    {
        protected DataModel _data;
        protected List<MemoryRange> _memoryRangeList = new List<MemoryRange>();
        private bool _isLive;
        private string _cacheFolder;
        private AddressBase _activeAddressSpace;

        public DataProviderBase(DataModel data, string cacheFolder)
        {
            _data = data;
            _cacheFolder = cacheFolder;
        }
        protected abstract byte[] ReadMemoryPage(ulong address);
        public abstract Dictionary<string, object> GetInformation();
        public abstract byte[] ReadMemory(ulong startAddress, uint pageCount);

        public ulong ImageLength { get; set; }

        public List<MemoryRange> MemoryRangeList
        {
            get { return _memoryRangeList; }
        }

        public bool IsLive
        {
            get { return _isLive; }
            set { _isLive = value; }
        }

        public string CacheFolder
        {
            get { return _cacheFolder; }
        }

        public AddressBase ActiveAddressSpace
        {
            get { return _activeAddressSpace; }
            set { _activeAddressSpace = value; }
        }
        public byte[] ReadPhysicalMemory(ulong startAddress, uint byteCount)
        {
            uint pages = 1;
            uint test = (uint)(startAddress - (startAddress & 0xfffffffff000)) + byteCount;
            if (test > 0x1000)
                pages = 2;
            byte[] buffer = new byte[byteCount];
            byte[] pageBuffer = ReadMemory(startAddress & 0xfffffffff000, pages);
            if (pageBuffer == null)
                return null;
            uint realOffset = (uint)(startAddress - (startAddress & 0xfffffffff000));
            Array.Copy(pageBuffer, realOffset, buffer, 0, byteCount);
            return buffer;
        }
        public byte[] ReadMemoryBlock(ulong startAddress, uint byteCount)
        {
            byte[] buffer = new byte[byteCount];
            ulong startPage;
            if(_activeAddressSpace.Is64)
                startPage = startAddress & 0xfffffffff000;
            else
                startPage = startAddress & 0xfffff000;

            uint pageCount = (uint)(((startAddress + byteCount) - startPage) / 0x1000);
            uint pageDiff = (uint)(((startAddress + byteCount) - startPage) % 0x1000);
            if (pageDiff > 0)
                pageCount++;
            byte[] bigBuffer = new byte[pageCount * 0x1000];
            for (int i = 0; i < pageCount; i++)
            {
                ulong pAddr = _activeAddressSpace.vtop(startPage + (ulong)(i * 0x1000));
                if (pAddr == 0)
                    return null;
                byte[] pageBuffer = ReadMemory(pAddr, 1);
                if (pageBuffer == null)
                    return null;
                Array.Copy(pageBuffer, 0, bigBuffer, (i * 0x1000), 0x1000);
            }
            Array.Copy(bigBuffer, (int)(startAddress - startPage), buffer, 0, byteCount);
            return buffer;
        }
        public uint? ReadUInt32(ulong startAddress)
        {
            byte[] buffer = ReadMemoryBlock(startAddress, 4);
            if (buffer == null)
                return null;
            return BitConverter.ToUInt32(buffer, 0);
        }
        public ulong? ReadUInt64(ulong startAddress)
        {
            byte[] buffer = ReadMemoryBlock(startAddress, 8);
            if (buffer == null)
                return null;
            return BitConverter.ToUInt64(buffer, 0);
        }
        public int? ReadInt32(ulong startAddress)
        {
            byte[] buffer = ReadMemoryBlock(startAddress, 4);
            if (buffer == null)
                return null;
            return BitConverter.ToInt32(buffer, 0);
        }
        public long? ReadInt64(ulong startAddress)
        {
            byte[] buffer = ReadMemoryBlock(startAddress, 8);
            if (buffer == null)
                return null;
            return BitConverter.ToInt64(buffer, 0);
        }
    }
}
