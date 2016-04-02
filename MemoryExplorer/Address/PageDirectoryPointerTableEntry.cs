using System;

namespace MemoryExplorer.Address
{
    public class PageDirectoryPointerTableEntry : PxeBase
    {
        public PageDirectoryPointerTableEntry(UInt64 entry)
        {
            Entry = entry;
        }
        public bool IsReadWrite { get { return ((Flags & _READWRITE_MASK) > 0); } }
        public bool IsUserSupervisor { get { return ((Flags & _USERSUP_MASK) > 0); } }
        public bool IsWriteThrough { get { return ((Flags & _WRITETHRU_MASK) > 0); } }
        public bool IsCacheDisabled { get { return ((Flags & _CACHEDISABLED_MASK) > 0); } }
        public bool IsAccessed { get { return ((Flags & _ACCESSED_MASK) > 0); } }
        public bool IsLarge { get { return ((Flags & _PAGESIZE_MASK) > 0); } }
        public bool IsNx { get { return ((Entry & 0x8000000000000000) > 0); } }
    }
}
