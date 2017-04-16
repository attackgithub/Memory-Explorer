using MemoryExplorer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.ModelObjects
{
    public class HeaderProcessInfo : StructureBase
    {
        private dynamic _hpi;
        public HeaderProcessInfo(DataModel model, ulong virtualAddress = 0, ulong physicalAddress = 0) : base(model, virtualAddress, physicalAddress)
        {
            _hpi = _profile.GetStructure("_OBJECT_HEADER_PROCESS_INFO", physicalAddress);
        }
        public dynamic dynamicObject
        {
            get { return _hpi; }
        }
    }
}
