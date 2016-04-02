using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.Address
{
    public abstract class PxeBase
    {
        private UInt64 _entry;
        private UInt64 _realEntry;
        private uint _flags;

        public const UInt64 _VALID_MASK = 1;
        public const UInt64 _READWRITE_MASK = (1 << 1);
        public const UInt64 _USERSUP_MASK = (1 << 2);
        public const UInt64 _WRITETHRU_MASK = (1 << 3);
        public const UInt64 _CACHEDISABLED_MASK = (1 << 4);
        public const UInt64 _ACCESSED_MASK = (1 << 5);
        public const UInt64 _DIRTY_MASK = (1 << 6);
        public const UInt64 _PAGESIZE_MASK = (1 << 7);
        public const UInt64 _GLOBAL_MASK = (1 << 8);
        public const UInt64 _PAT_MASK = (1 << 9);
        public const UInt64 _PROTOTYPE_MASK = (1 << 10);
        public const UInt64 _TRANSITION_MASK = (1 << 11);
        public ulong Entry
        {
            get { return _entry; }
            set
            {
                _entry = value;
                _realEntry = _entry & 0x0000fffffffff000;
                _flags = (uint)(_entry & 0xfff);
            }
        }

        public uint Flags
        {
            get { return _flags; }
            private set { _flags = value; }
        }

        public ulong RealEntry { get { return _realEntry; } }
        public bool InUse { get { return ((Flags & _VALID_MASK) > 0); } }
    }
}
