using MemoryExplorer.Data;
using MemoryExplorer.Model;
using MemoryExplorer.Profiles;
using System;
using System.Runtime.InteropServices;

namespace MemoryExplorer.ModelObjects
{
    public class MmVadShort : MmVadBase
    {
        private uint _flags;
        private uint _flags1;
        public MmVadShort(DataModel model, ulong virtualAddress = 0, ulong physicalAddress = 0) : base(model, virtualAddress, physicalAddress)
        {
            Overlay("_MMVAD_SHORT");
            _flags = Members.u;
            _flags1 = Members.u1;
        }
        public MmVadShort(DataModel model, byte[] buffer) : base(model, 0, 0)
        {
            //var dll = _profile.GetStructureAssembly("_MMVAD_SHORT");
            //Type t = dll.GetType("liveforensics.MMVAD_SHORT");
            //GCHandle pinedPacket = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            //_members = Marshal.PtrToStructure(Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0), t);
            //pinedPacket.Free();
            //_flags = Members.u;
            //_flags1 = Members.u1;
        }
        public VadProtection Protection { get { return (VadProtection)((_flags & 0xf8) >> 3); } }
        public VadType Type { get { return (VadType)(_flags & 0x03); } }
        public bool PrivateMemory { get { return (_flags & 0x8000) > 0; } }
        public bool CommitMem { get { return (_flags1 & 0x80000000) > 0; } }
        public int Commit { get { return (int)(_flags1 & 0x7fffffff); } }

    }
}
