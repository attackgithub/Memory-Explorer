using MemoryExplorer.Data;
using MemoryExplorer.Profiles;

namespace MemoryExplorer.ModelObjects
{
    public class FileObject : StructureBase
    {
        public FileObject(Profile profile, DataProviderBase dataProvider, ulong virtualAddress = 0, ulong physicalAddress = 0) : base(profile, dataProvider, virtualAddress)
        {
            _physicalAddress = physicalAddress;
            Overlay("_FILE_OBJECT");
        }
    }
}
