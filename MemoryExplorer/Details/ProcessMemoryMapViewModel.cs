using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.Details
{
    public class ProcessMemoryMapViewModel : BindableBase
    {
        public IEnumerable<MemMapResult> ProcessMemoryMap
        {
            get
            {
                if (_dataModel == null || _dataModel.ActiveProcess == null || _dataModel.ActiveProcess.AddressSpace == null || _dataModel.ActiveProcess.AddressSpace.MemoryMap == null || _dataModel.ActiveProcess.AddressSpace.MemoryMap.MemoryRecords == null)
                    return null;
                var r =
                    from item in _dataModel.ActiveProcess.AddressSpace.MemoryMap.MemoryRecords
                    select new MemMapResult(item);

                return r;
            }
        }
    }
}
