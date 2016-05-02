using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.Details
{
    public class DriverViewerViewModel : BindableBase
    {
        public IEnumerable<DriverResult> Drivers
        {
            get
            {
                if (_dataModel == null || _dataModel.DriverList == null)
                    return null;
                lock(_dataModel.AccessLock)
                {
                    var r =
                    from item in _dataModel.DriverList
                    orderby item.Name
                    select new DriverResult(item);
                    return r;
                }                                
            }
        }
    }
}
