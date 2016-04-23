using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.Details
{
    public class ObjectTypesViewModel : BindableBase
    {
        public ObjectTypesViewModel()
        {
        }
        public IEnumerable<KvpResult> ObjectTypeList
        {
            get
            {
                if (_dataModel == null || _dataModel.ObjectTypeList == null)
                    return null;
                if (_dataModel.ObjectTypeList.Count == 0)
                    return null;
                var r =
                    from item in _dataModel.ObjectTypeList
                    select new KvpResult(item);

                return r;
            }
        }
    
    }
}
