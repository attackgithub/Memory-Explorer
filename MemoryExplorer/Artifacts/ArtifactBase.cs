using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.Artifacts
{
    public enum ArtifactType
    {
        Unknown = -1,
        Root = 0,
        Process = 1
    }
    public abstract class ArtifactBase
    {
        protected string _name;
        protected ArtifactBase _parent;
        protected bool _expanded = false;
        protected bool _selected = false;

        public ArtifactBase()
        {

        }
        public bool IsExpanded
        {
            get { return _expanded; }
            set { _expanded = value; }
        }

        public bool IsSelected
        {
            get { return _selected; }
            set { _selected = value; }
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public ArtifactBase Parent
        {
            get { return _parent; }
            set
            {
                if (IsValidParent(value))
                    _parent = value;
            }
        }
        public bool IsValidParent(ArtifactBase candidate)
        {
            while (true)
            {
                if (candidate == null)
                    return true;
                if (candidate == this)
                    return false;
                candidate = candidate.Parent;
            }
        }

    }
}
