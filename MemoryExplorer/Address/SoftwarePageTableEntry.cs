using System;

namespace MemoryExplorer.Address
{
    public class SoftwarePageTableEntry : PxeBase
    {
        public SoftwarePageTableEntry(UInt64 entry)
        {
            Entry = entry;
        }

        public bool IsTransition { get { return ((Entry & _TRANSITION_MASK) > 0); } }
        public bool IsPrototype { get { return ((Entry & _PROTOTYPE_MASK) > 0); } }
        public bool IsReadWrite { get { return ((Flags & _READWRITE_MASK) > 0); } }
        public bool IsUserSupervisor { get { return ((Flags & _USERSUP_MASK) > 0); } }
        public bool IsWriteThrough { get { return ((Flags & _WRITETHRU_MASK) > 0); } }
        public bool IsCacheDisabled { get { return ((Flags & _CACHEDISABLED_MASK) > 0); } }
        public bool IsAccessed { get { return ((Flags & _ACCESSED_MASK) > 0); } }
        public bool IsNx { get { return ((Entry & 0x8000000000000000) > 0); } }

        public ulong ProtoAddress { get { return (Entry & 0xffffffffffff0000) >> 16; } }
        public ulong PageFileOffset { get { return (Entry & 0xffffffff00000000) >> 32; } }
        public ulong PageFileNumber { get { return (Entry & 0x01e) >> 1; } }
        public ulong UsedPageTableEntries { get { return (Entry & 0x1ff8000) >> 15; } }
        public ulong Protection { get { return (Entry & 0x3e0) >> 5; } }
    }
}
