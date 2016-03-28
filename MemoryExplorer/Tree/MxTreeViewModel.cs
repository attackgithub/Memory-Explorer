using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.Tree
{
    public class MxTreeViewModel : BindableBase
    {
        IEnumerable<TreeItem> _treeItems;

        public MxTreeViewModel()
        {

        }
        public IEnumerable<TreeItem> TreeItems
        {
            get
            {
                IEnumerable<TreeItem> retval = null;
                try
                {
                    retval =
                    from item in _dataModel.Artifacts
                    where item.Parent == null
                    select new TreeItem(item, _dataModel);
                }
                catch { }
                return retval;
            }
            set { SetProperty(ref _treeItems, value); }
        }

    }
}
