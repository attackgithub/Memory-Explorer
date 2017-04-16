using MemoryExplorer.Data;
using MemoryExplorer.Model;
using MemoryExplorer.Profiles;

namespace MemoryExplorer.ModelObjects
{
    public class ObjectDirectoryEntry : StructureBase
    {
        private dynamic _ode;
        public ObjectDirectoryEntry(DataModel model, ulong virtualAddress = 0, ulong physicalAddress = 0) : base(model, virtualAddress, physicalAddress)
        {
            _ode = _profile.GetStructure("_OBJECT_DIRECTORY_ENTRY", _physicalAddress);
        }
        public dynamic dynamicObject
        {
            get { return _ode; }
        }
        public ulong Object
        {
            get
            {
                try
                {
                    ulong u = _ode.Object;
                    return u;
                }
                catch (System.Exception)
                {
                    return 0;
                }
            }
        }
        public ulong ChainLink
        {
            get
            {
                try
                {
                    ulong u = _ode.ChainLink;
                    return u;
                }
                catch (System.Exception)
                {
                    return 0;
                }
            }
        }
    }
}
