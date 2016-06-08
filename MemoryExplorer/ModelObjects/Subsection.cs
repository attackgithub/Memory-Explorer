using MemoryExplorer.Data;
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
        public Subsection(Profile profile, DataProviderBase dataProvider, ulong virtualAddress = 0, ulong physicalAddress = 0) : base(profile, dataProvider, virtualAddress)
        {
            _physicalAddress = physicalAddress;
            Overlay("_SUBSECTION");
        }
        public Subsection(Profile profile, DataProviderBase dataProvider, byte[] buffer) : base(profile, dataProvider, 0)
        {
            var dll = _profile.GetStructureAssembly("_SUBSECTION");
            Type t = dll.GetType("liveforensics.SUBSECTION");
            GCHandle pinedPacket = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            _members = Marshal.PtrToStructure(Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0), t);
            pinedPacket.Free();
        }
    }
}
