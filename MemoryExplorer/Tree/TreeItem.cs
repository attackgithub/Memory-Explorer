using MemoryExplorer.Artifacts;
using MemoryExplorer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.Tree
{
    public class TreeItem : BindableBase
    {
        private readonly ArtifactBase _artifactItem;
        private readonly DataModel _dataSource;

        public TreeItem(ArtifactBase item, DataModel dataSource)
        {
            _artifactItem = item;
            _dataSource = dataSource;
        }
        public ArtifactBase Item
        {
            get { return _artifactItem; }
        }

        public IEnumerable<TreeItem> Children
        {
            get
            {
                IEnumerable<TreeItem> retval = null;
                try
                {
                    retval =
                        from item in _dataSource.Artifacts
                        where item.Parent == _artifactItem
                        select new TreeItem(item, _dataSource);
                }
                catch { }
                return retval;
            }
        }

        public bool IsExpanded
        {
            get { return _artifactItem.IsExpanded; }
            set
            {
                _artifactItem.IsExpanded = value;
            }
        }
        public bool IsSelected
        {
            get { return _artifactItem.IsSelected; }
            set
            {
                _artifactItem.IsSelected = value;
                if(value)
                    _dataSource.UpdateDetails(_artifactItem);
            }
        }
        public string Name
        {
            get
            {
                return _artifactItem.Name;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj == this)
                return true;
            TreeItem that = obj as TreeItem;
            if (that == null)
                return false;
            return Object.Equals(this._artifactItem, that._artifactItem);
        }

        public override int GetHashCode()
        {
            return _artifactItem.GetHashCode();
        }
    }
}
