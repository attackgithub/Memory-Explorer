using MemoryExplorer.Data;
using MemoryExplorer.Model;
using MemoryExplorer.Profiles;

namespace MemoryExplorer.ModelObjects
{
    public class FileObject : StructureBase
    {
        public FileObject(DataModel model, ulong virtualAddress = 0, ulong physicalAddress = 0) : base(model, virtualAddress)
        {
            _physicalAddress = physicalAddress;
            Overlay("_FILE_OBJECT");
        }
    }
}
