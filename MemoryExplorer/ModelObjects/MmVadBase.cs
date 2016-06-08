using MemoryExplorer.Data;
using MemoryExplorer.Profiles;

namespace MemoryExplorer.ModelObjects
{
    public enum VadProtection : uint
    {
        NOACCESS = 0,
        READONLY = 1,
        EXECUTE = 2,
        EXECUTE_READ = 3,
        READWRITE = 4,
        WRITECOPY = 5,
        EXECUTE_READWRITE = 6,
        EXECUTE_WRITECOPY = 7,
        NOACCESS1 = 8,
        NOCACHE_READONLY = 9,
        NOCACHE_EXECUTE = 10,
        NOCACHE_EXECUTE_READ = 11,
        NOCACHE_READWRITE = 12,
        NOCACHE_WRITECOPY = 13,
        NOCACHE_EXECUTE_READWRITE = 14,
        NOCACHE_EXECUTE_WRITECOPY = 15,
        NOACCESS2 = 16,
        GUARD_READONLY = 17,
        GUARD_EXECUTE = 18,
        GUARD_EXECUTE_READ = 19,
        GUARD_READWRITE = 20,
        GUARD_WRITECOPY = 21,
        GUARD_EXECUTE_READWRITE = 22,
        GUARD_EXECUTE_WRITECOPY = 23,
        NOACCESS3 = 24,
        WRITECOMBINE_READONLY = 25,
        WRITECOMBINE_EXECUTE = 26,
        WRITECOMBINE_EXECUTE_READ = 27,
        WRITECOMBINE_READWRITE = 28,
        WRITECOMBINE_WRITECOPY = 29,
        WRITECOMBINE_EXECUTE_READWRITE = 30,
        WRITECOMBINE_EXECUTE_WRITECOPY = 31
    }
    public enum VadType : uint
    {
        VadNone = 0,
        VadDevicePhysicalMemory = 1,
        VadImageMap = 2,
        VadAwe = 3,
        VadWriteWatch = 4,
        VadLargePages = 5,
        VadRotatePhysical = 6,
        VadLargePageSection = 7
    }
    public class MmVadBase : StructureBase
    {
        public MmVadBase(Profile profile, DataProviderBase dataProvider, ulong virtualAddress=0, ulong physicalAddress=0) : base(profile, dataProvider, virtualAddress)
        {

        }
    }
}
