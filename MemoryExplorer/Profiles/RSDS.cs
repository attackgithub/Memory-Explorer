using MemoryExplorer.Data;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace MemoryExplorer.Profiles
{
    public class RSDS
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct RSDS_HEADER
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] Signature;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] Guid;
            public UInt32 Age;
        }
        private string _signature = "error";
        private string _filename = "";
        private uint _age = 0;
        private Guid _guid = Guid.Empty;
        private string _targetFile;
        private ulong _offset;

        public string Signature { get { return _signature; } }
        public string Filename { get { return _filename; } }
        public uint Age { get { return _age; } }
        public Guid ImageGuid { get { return _guid; } }
        public string GuidAge { get { return (_guid.ToString("N") + _age.ToString()).ToUpper(); } }

        public RSDS(DataProviderBase dataProvider, ulong offset)
        {
            try
            {
                ulong alignedAddress = offset & 0xfffffffff000;
                byte[] buffer = dataProvider.ReadMemory(alignedAddress, 2);
                GCHandle pinnedPacket = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                RSDS_HEADER rsds = (RSDS_HEADER)Marshal.PtrToStructure(Marshal.UnsafeAddrOfPinnedArrayElement(buffer, (int)(offset - alignedAddress)), typeof(RSDS_HEADER));
                pinnedPacket.Free();
                _signature = System.Text.Encoding.UTF8.GetString(rsds.Signature);
                _guid = new Guid(rsds.Guid);
                _age = rsds.Age;
                int marker = 24 + (int)(offset - alignedAddress);
                char c = (char)buffer[marker];
                while (c != 0 && marker < 0x2000)
                {
                    _filename += c;
                    c = (char)buffer[++marker];
                }

            }
            catch (Exception ex)
            {
                throw new ArgumentException("Error Creating RSDS: " + ex.Message);
            }
        }

        public RSDS(string targetFile, ulong offset)
        {
            try
            {
                _targetFile = targetFile;
                _offset = offset;
                using (FileStream stream = new FileStream(_targetFile, FileMode.Open, FileAccess.Read))
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    stream.Seek((long)_offset, SeekOrigin.Begin);
                    RSDS_HEADER rsds = FromBinaryReader<RSDS_HEADER>(reader);
                    char c = (char)reader.ReadByte();
                    while (c != (char)0 && c != -1)
                    {
                        _filename += c;
                        c = (char)reader.ReadByte();
                    }
                    _signature = System.Text.Encoding.UTF8.GetString(rsds.Signature);
                    _guid = new Guid(rsds.Guid);
                    _age = rsds.Age;
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Error Creating RSDS: " + ex.Message);
            }
        }
        public T FromBinaryReader<T>(BinaryReader reader)
        {
            // Read in a byte array
            byte[] bytes = reader.ReadBytes(Marshal.SizeOf(typeof(T)));

            // Pin the managed memory while, copy it out the data, then unpin it
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            T theStructure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();

            return theStructure;
        }
    }
}
