using MemoryExplorer.Address;
using MemoryExplorer.Data;
using MemoryExplorer.Model;
using MemoryExplorer.Profiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.ModelObjects
{
    public class HeaderHandleInfo : StructureBase
    {
        private dynamic _hhi;
        public HeaderHandleInfo(DataModel model, ulong virtualAddress=0, ulong physicalAddress=0) : base(model, virtualAddress)
        {
            _hhi = _profile.GetStructure("_OBJECT_HEADER_HANDLE_INFO", physicalAddress);
        }
        public dynamic dynamicObject
        {
            get { return _hhi; }
        }
    }
}
