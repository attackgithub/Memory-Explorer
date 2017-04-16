using MemoryExplorer.Address;
using MemoryExplorer.Data;
using MemoryExplorer.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.ModelObjects
{
    public class LIST_ENTRY : StructureBase
    {
        private string _targetFile;
        private ulong _blink;
        private ulong _flink;
        private bool _x64;

        public ulong Blink { get { return _blink & 0xffffffffffff; } }
        public ulong Flink { get { return _flink & 0xffffffffffff; } }

        public LIST_ENTRY(DataModel model, byte[] buffer, ulong offset, bool isX64) : base(model, 0)
        {
            _x64 = isX64;
            if (_x64)
            {
                _blink = BitConverter.ToUInt64(buffer, (int)offset);
                _flink = BitConverter.ToUInt64(buffer, (int)offset + 8);
            }
            else
            {
                _blink = BitConverter.ToUInt32(buffer, (int)offset);
                _flink = BitConverter.ToUInt32(buffer, (int)offset + 4);
            }
        }
        public LIST_ENTRY(DataModel model, string targetFile, ulong physicalAddress, ulong virtualAddress, bool isX64) : base(model, virtualAddress)
        {
            try
            {
                _targetFile = targetFile;
                using (FileStream stream = new FileStream(_targetFile, FileMode.Open, FileAccess.Read))
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    stream.Seek((long)_physicalAddress, SeekOrigin.Begin);
                    if (_x64)
                    {
                        byte[] buffer = reader.ReadBytes(16);
                        _blink = BitConverter.ToUInt64(buffer, 0);
                        _flink = BitConverter.ToUInt64(buffer, 8);
                    }
                    else
                    {
                        byte[] buffer = reader.ReadBytes(8);
                        _blink = BitConverter.ToUInt32(buffer, 0);
                        _flink = BitConverter.ToUInt32(buffer, 4);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Error Creating LIST_ENTRY: " + ex.Message);
            }
        }
        public LIST_ENTRY(DataModel model, ulong virtualAddress) : base(model, virtualAddress)
        {
            try
            {
                AddressBase addressSpace = model.ActiveAddressSpace;
                _virtualAddress = virtualAddress;
                _physicalAddress = addressSpace.vtop(virtualAddress, _dataProvider.IsLive);
                _x64 = addressSpace.Is64;
                if (_x64)
                {
                    var checkFirst = _dataProvider.ReadUInt64(_virtualAddress);
                    if (checkFirst == null)
                        throw new ArgumentException("Invalid Address");
                    _blink = (ulong)checkFirst;
                    checkFirst = _dataProvider.ReadUInt64(_virtualAddress + 8);
                    if (checkFirst == null)
                        throw new ArgumentException("Invalid Address");
                    _flink = (ulong)checkFirst;
                }
                else
                {
                    var checkFirst = _dataProvider.ReadUInt32(_virtualAddress);
                    if (checkFirst == null)
                        throw new ArgumentException("Invalid Address");
                    _blink = (ulong)checkFirst;
                    checkFirst = _dataProvider.ReadUInt32(_virtualAddress + 4);
                    if (checkFirst == null)
                        throw new ArgumentException("Invalid Address");
                    _flink = (ulong)checkFirst;
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Error: " + ex.Message);
            }
        }
    }
}
