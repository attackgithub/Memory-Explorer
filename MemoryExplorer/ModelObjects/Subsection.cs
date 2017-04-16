using MemoryExplorer.Data;
using MemoryExplorer.Model;
using MemoryExplorer.Profiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.ModelObjects
{
    public class Subsection :StructureBase
    {
        public Subsection(DataModel model, ulong virtualAddress = 0, ulong physicalAddress = 0) : base(model, virtualAddress)
        {
            _physicalAddress = physicalAddress;
            Overlay("_SUBSECTION");
        }
        public Subsection(DataModel model, byte[] buffer) : base(model, 0)
        {
            //var dll = _profile.GetStructureAssembly("_SUBSECTION");
            //Type t = dll.GetType("liveforensics.SUBSECTION");
            //GCHandle pinedPacket = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            //_members = Marshal.PtrToStructure(Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0), t);
            //pinedPacket.Free();
        }
    }
}
