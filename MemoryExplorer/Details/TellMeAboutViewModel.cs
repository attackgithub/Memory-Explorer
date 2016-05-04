using MemoryExplorer.Tree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.Details
{
    public class TellMeAboutViewModel : BindableBase
    {
        public string TellMeAboutTitle { get { return _dataModel.TellMeAboutTitle; } }

        public IEnumerable<ProfileTreeItem> ProfileTreeItems
        {
            get
            {
                IEnumerable<ProfileTreeItem> retval = null;
                try
                {
                    lock (_dataModel.AccessLock)
                    {
                        retval =
                            from item in _dataModel.ProfileEntries
                            where item.Parent == null
                            select new ProfileTreeItem(item, _dataModel);
                    }
                }
                catch { }
                return retval;
            }
            //set { SetProperty(ref _treeItems, value); }
        }
    }
}
