using MemoryExplorer.Memory;
using MemoryExplorer.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.Data
{
    
    public class ImageDataProvider : DataProviderBase
    {
        private string _imageFilename;
        private Dictionary<ulong, byte[]> _memoryCache = new Dictionary<ulong, byte[]>();
        private Queue<ulong> _cacheTracker = new Queue<ulong>();


        public ImageDataProvider(DataModel data, string cacheFolder) : base(data, cacheFolder)
        {
            _imageFilename = "";
            ImageLength = 0;
            if (_data.MemoryImageFilename == "")
                throw new ArgumentException("Memory Image Name Isn't Set");
            IsLive = false;
            // check to see if we are looking at a new image file
            if (_data.MemoryImageFilename != _imageFilename)
            {
                _imageFilename = _data.MemoryImageFilename;
                FileInfo fiCheck = new FileInfo(_imageFilename);
                if (!fiCheck.Exists)
                {
                    _imageFilename = "";
                    ImageLength = 0;
                    throw new ArgumentException("Memory Image Doesn't Exist: " + _imageFilename);
                }
                ImageLength = (ulong)fiCheck.Length;
                MemoryRange range = new MemoryRange();
                range.StartAddress = 0;
                range.Length = ImageLength;
                range.PageCount = (uint)(ImageLength / 0x1000);
                _memoryRangeList.Add(range);
            }
        }
        public override Dictionary<string, object> GetInformation()
        {
            Dictionary<string, object> info = new Dictionary<string, object>();
            info.Add("maximumPhysicalAddress", ImageLength - 1);
            return info;
        }
        protected override byte[] ReadMemoryPage(ulong address)
        {            
            if(address > ImageLength - 4096)
                throw new ArgumentException("Address Beyond End Of File");

            byte[] buffer = new byte[4096];
            try
            {
                using (FileStream fs = new FileStream(_imageFilename, FileMode.Open, FileAccess.Read))
                using (BinaryReader br = new BinaryReader(fs))
                {
                    fs.Seek((long)address, SeekOrigin.Begin);
                    buffer = br.ReadBytes(4096);
                }
                return buffer;
            }
            catch
            {
                return null;
            }
        }
        public override byte[] ReadMemory(ulong startAddress, uint pageCount = 1)
        {
            try
            {
                byte[] buffer = new byte[pageCount * 0x1000];
                for (uint i = 0; i < pageCount; i++)
                {
                    byte[] tempBuffer = CheckCache(startAddress + (i * 0x1000));
                    if (tempBuffer == null)
                    {
                        tempBuffer = ReadMemoryPage(startAddress + (i * 0x1000));
                        if (tempBuffer != null)
                        {
                            UpdateCache(startAddress + (i * 0x1000), tempBuffer);
                        }
                    }
                    if (tempBuffer != null)
                        Array.Copy(tempBuffer, 0, buffer, (i * 0x1000), 0x1000);
                    else
                        throw new ArgumentException("Unable to read requested memory");
                }
                return buffer;
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.Message);
            }
        }
        private byte[] CheckCache(ulong address)
        {
            if (_memoryCache.ContainsKey(address))
                return _memoryCache[address];
            return null;
        }
        private void UpdateCache(ulong address, byte[] buffer)
        {
            try
            {
                if (_memoryCache.ContainsKey(address))
                    _memoryCache[address] = buffer;
                else
                {
                    while (_cacheTracker.Count > 256)
                    {
                        _memoryCache.Remove(_cacheTracker.Dequeue());
                    }
                    _memoryCache.Add(address, buffer);
                    _cacheTracker.Enqueue(address);
                }
            }
            catch { }            
        }
        public void FlushCache()
        {
            _memoryCache.Clear();
            _cacheTracker.Clear();
        }
    }
}
