using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.Details
{
    public class ProcessInformationViewModel : BindableBase
    {
        public IEnumerable<KvpResult> ProcessInfoDictionary
        {
            get
            {
                if (_dataModel == null || _dataModel.ProcessInfoDictionary == null)
                    return null;
                var r =
                    from item in _dataModel.ProcessInfoDictionary
                    select new KvpResult(item);

                return r;
            }
        }
    }
}
