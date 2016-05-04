using MemoryExplorer.Model;
using MemoryExplorer.Profiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.Tree
{
    public class ProfileTreeItem : BindableBase
    {
        private readonly ProfileEntry _profileEntry;
        private readonly DataModel _dataSource;
        public ProfileTreeItem(ProfileEntry item, DataModel dataSource)
        {
            _profileEntry = item;
            _dataSource = dataSource;
        }
        public ProfileEntry Item
        {
            get { return _profileEntry; }
        }
        public IEnumerable<ProfileTreeItem> Children
        {
            get
            {
                IEnumerable<ProfileTreeItem> retval = null;
                try
                {
                    lock (_dataModel.AccessLock)
                    {
                        retval =
                        from item in _dataSource.ProfileEntries
                        where _profileEntry == item.Parent
                        select new ProfileTreeItem(item, _dataSource);
                    }
                }
                catch { }
                return retval;
            }
        }
        public bool IsExpanded
        {
            get { return _profileEntry.IsExpanded; }
            set
            {
                _profileEntry.IsExpanded = value;
            }
        }
        public bool IsSelected
        {
            get { return _profileEntry.IsSelected; }
            set
            {
                _profileEntry.IsSelected = value;
                //if (value)
                //    _dataSource.UpdateDetails(_profileEntry);
            }
        }
        public string Name
        {
            get
            {
                return _profileEntry.Name;
            }
        }
    }
}
