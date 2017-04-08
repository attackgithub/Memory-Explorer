using MemoryExplorer.Data;
using MemoryExplorer.Profiles;

namespace MemoryExplorer.ModelObjects
{
    public class ObjectDirectoryEntry : StructureBase
    {
        public ObjectDirectoryEntry(Profile profile, DataProviderBase dataProvider, ulong virtualAddress = 0, ulong physicalAddress = 0) : base(profile, dataProvider, virtualAddress)
        {
            _physicalAddress = physicalAddress;
            Overlay("_OBJECT_DIRECTORY_ENTRY");
        }
    }
}
