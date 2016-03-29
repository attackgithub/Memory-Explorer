using MemoryExplorer.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.Scanners
{
    public class StringSearch
    {
        private DataProviderBase _dataProvider;
        private List<string> _needleList = new List<string>();
        private Dictionary<string, List<ulong>> _hitListDict = new Dictionary<string, List<ulong>>();

        public StringSearch(DataProviderBase dataProvider)
        {
            _dataProvider = dataProvider;
        }
        public void AddNeedle(string needle)
        {
            _needleList.Add(needle);
        }
        public void ClearNeedles()
        {
            _needleList.Clear();
        }
        public Dictionary<string, List<ulong>> Scan()
        {
            ulong filePointer = 0;
            ulong bufferStart = 0;
            byte[] dataBuffer = null;
            while (bufferStart < _dataProvider.ImageLength)
            {
                if (filePointer != 0)
                    bufferStart = filePointer - 0x1000; // read backwards one page
                try
                {
                    dataBuffer = _dataProvider.ReadMemory(bufferStart, 4);
                    filePointer += 0x4000;
                }
                catch
                {
                    try
                    {
                        dataBuffer = _dataProvider.ReadMemory(bufferStart, 2);
                        filePointer += 0x2000;
                    }
                    catch
                    {
                        filePointer += 0x1000;
                        continue;
                    }
                }
                foreach (string needle in _needleList)
                {
                    List<ulong> hitList = new List<ulong>();
                    byte[] pattern = Encoding.UTF8.GetBytes(needle);
                    List<uint> result = IndexOfSequence(dataBuffer, pattern);
                    foreach (uint i in result)
                        hitList.Add(bufferStart + i);
                    if (hitList.Count > 0)
                    {
                        List<ulong> existingHitList;
                        if (_hitListDict.TryGetValue(needle, out existingHitList))
                        {
                            foreach (ulong l in hitList)
                                existingHitList.Add(l);
                        }
                        else
                            _hitListDict.Add(needle, hitList);
                    }
                }                                
            }
            return _hitListDict;
        }
        protected List<uint> IndexOfSequence(byte[] buffer, byte[] pattern, uint startIndex = 0)
        {
            List<uint> positions = new List<uint>();
            uint i = (uint)Array.IndexOf<byte>(buffer, pattern[0], (int)startIndex);
            while (i >= 0 && i <= buffer.Length - pattern.Length)
            {
                byte[] segment = new byte[pattern.Length];
                Buffer.BlockCopy(buffer, (int)i, segment, 0, pattern.Length);
                if (segment.SequenceEqual<byte>(pattern))
                    positions.Add(i);
                i = (uint)Array.IndexOf<byte>(buffer, pattern[0], (int)i + pattern.Length);
            }
            return positions;
        }
    }
}
