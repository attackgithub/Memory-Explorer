using MemoryExplorer.Data;
using MemoryExplorer.Profiles;

namespace MemoryExplorer.ModelObjects
{
    public class ControlArea : StructureBase
    {
        public ControlArea(Profile profile, DataProviderBase dataProvider, ulong virtualAddress = 0, ulong physicalAddress = 0) : base(profile, dataProvider, virtualAddress)
        {
            _physicalAddress = physicalAddress;
            Overlay("_CONTROL_AREA");
        }
    }
}
