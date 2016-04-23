using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.Details
{
    public class ProcessViewerViewModel : BindableBase
    {
        public IEnumerable<PsListResult> Processes
        {
            get
            {
                if (_dataModel == null)
                    return null;
                var r =
                    from item in _dataModel.ProcessList
                    orderby item.Pid
                    select new PsListResult(item);

                return r;
            }
        }
    }
}
