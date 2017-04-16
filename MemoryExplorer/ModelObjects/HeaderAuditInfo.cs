using MemoryExplorer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.ModelObjects
{
    public class HeaderAuditInfo : StructureBase
    {
        private dynamic _hai;
        public HeaderAuditInfo(DataModel model, ulong virtualAddress = 0, ulong physicalAddress = 0) : base(model, virtualAddress, physicalAddress)
        {
            _hai = _profile.GetStructure("_OBJECT_HEADER_AUDIT_INFO", physicalAddress);
        }
        public dynamic dynamicObject
        {
            get { return _hai; }
        }
    }
}
