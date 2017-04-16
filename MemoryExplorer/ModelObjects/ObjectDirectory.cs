using MemoryExplorer.Data;
using MemoryExplorer.Model;
using MemoryExplorer.Profiles;

namespace MemoryExplorer.ModelObjects
{
    public class ObjectDirectory : StructureBase
    {
        private dynamic _od;
        public ObjectDirectory(DataModel model, ulong virtualAddress = 0, ulong physicalAddress = 0) : base(model, virtualAddress)
        {
            _physicalAddress = physicalAddress;
            if(_physicalAddress == 0 && virtualAddress != 0)
                _physicalAddress = _model.ActiveAddressSpace.vtop(virtualAddress);
            _od = _profile.GetStructure("_OBJECT_DIRECTORY", _physicalAddress);
        }
        public dynamic dynamicObject
        {
            get { return _od; }
        }
        public dynamic HashBuckets
        {
            get
            {
                try
                {
                    var size = _od.HashBuckets[0].GetType().Name;
                    if (size == "UInt32")
                    {
                        uint[] test = _od.HashBuckets;
                        return test;
                    }
                    if (size == "UInt64")
                    {
                        ulong[] test = _od.HashBuckets;
                        return test;
                    }
                    return null;
                }
                catch (System.Exception)
                {
                    return null;
                }
            }
        }
    }
}
